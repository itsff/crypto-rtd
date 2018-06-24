using Binance.Net;
using Binance.Net.Objects;
using CryptoExchange.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace CryptoRtd
{
    [
        Guid("982325F9-B1F3-4D12-9618-76A1C8B950B2"),
        // This is the string that names RTD server.
        // Users will use it from Excel: =RTD("crypto",, ....)
        ProgId("crypto")
    ]
    public class CryptoRtdServer : IRtdServer
    {
        IRtdUpdateEvent _callback;
        DispatcherTimer _timer;
        readonly SubscriptionManager _subMgr;

        // Oldie but goodie. WebSocket library that works on .NET 4.0
        GdaxWebSocketClient _socket;
        BinanceAdapter _binanceAdapter;

        public const string GDAX = "GDAX";
        public const string CLOCK = "CLOCK";

        public CryptoRtdServer ()
        {
            _subMgr = new SubscriptionManager();

            _binanceAdapter = new BinanceAdapter(_subMgr);
        }

        //
        // Excel calls this. It's an entry point. It passes us a callback
        // structure which we save for later.
        //
        int IRtdServer.ServerStart (IRtdUpdateEvent callback)
        {
            _callback = callback;

            // We will throttle out updates so that Excel can keep up.
            // It is also important to invoke the Excel callback notify
            // function from the COM thread. System.Windows.Threading' 
            // DispatcherTimer will use COM thread's message pump.
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(33);  // Make this fast
            _timer.Tick += TimerElapsed;
            _timer.Start();

            // moved from constructor as this is a blocking call when network is down
            Task.Run(() => {
                _socket = new GdaxWebSocketClient(new Uri("wss://ws-feed.gdax.com"));
                _socket.Start();
                _socket.MessageReceived += OnWebSocketMessageReceived;
            });
            return 1;
        }

        //
        // Excel calls this when it wants to shut down RTD server.
        //
        void IRtdServer.ServerTerminate ()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer = null;
            }

            if (_socket != null)
            {
                _socket.Disconnect();
                _socket = null;
            }
            if (_binanceAdapter != null)
            {
                _binanceAdapter.UnsubscribeAllStreams();
                _binanceAdapter = null;
            }
        }

        //
        // Excel calls this when it wants to make a new topic subscription.
        // topicId becomes the key representing the subscription.
        // String array contains any aux data user provides to RTD macro.
        //
        object IRtdServer.ConnectData (int topicId,
                                       ref Array strings,
                                       ref bool newValues)
        {
            try
            {
                newValues = true;

                // We assume 3 strings: Origin, Instrument, Field
                if (strings.Length == 1)
                {
                    string origin = strings.GetValue(0).ToString().ToUpperInvariant();
                    switch (origin)
                    {
                        case CLOCK:
                            _subMgr.Subscribe(topicId, CLOCK);
                            return DateTime.Now.ToLocalTime();
                    }
                    return "Unsupported origin: " + origin;
                }
                else if (strings.Length == 2)
                {
                    string origin = strings.GetValue(0).ToString().ToUpperInvariant();
                    string stat = strings.GetValue(1).ToString().ToUpperInvariant();

                    switch(origin)
                    {
                        case BinanceAdapter.BINANCE:
                            return _binanceAdapter.SubscribeStats(stat);
                    }
                    return "Unsupported origin: " + origin;
                }
                else if (strings.Length >= 3)
                {
                    // Crappy COM-style arrays...
                    string origin = strings.GetValue(0).ToString().ToUpperInvariant();         // The Exchange
                    string instrument = strings.GetValue(1).ToString().ToUpperInvariant();
                    string field = strings.GetValue(2).ToString().ToUpperInvariant();

                    //if (field.Equals("ALL_FIELDS"))
                    //{
                    //    Array comArray = new object[1,RtdFields.ALL_FIELDS.Length];
                    //    for (int i = 0; i < RtdFields.ALL_FIELDS.Length; i++)
                    //        comArray.SetValue(RtdFields.ALL_FIELDS[i], 0,i);
                    //}

                    switch (origin)
                    {
                        case GDAX:
                            lock (_subMgr)
                            {
                                // Let's use Empty strings for now
                                _subMgr.Subscribe(topicId,origin,String.Empty,instrument,field);
                            }
                            Task.Run(() => SubscribeGdaxWebSocketToTicker(topicId,instrument));  // dont block excel
                            return SubscriptionManager.UninitializedValue;

                        case BinanceAdapter.BINANCE:
                        case BinanceAdapter.BINANCE_24H:
                        case BinanceAdapter.BINANCE_DEPTH:
                        case BinanceAdapter.BINANCE_CANDLE:
                        case BinanceAdapter.BINANCE_TRADE:
                        case BinanceAdapter.BINANCE_HISTORY:
                            int depth = -1;
                            if (strings.Length > 3)
                                Int32.TryParse(strings.GetValue(3).ToString(), out depth);

                            lock (_subMgr)
                            {
                                if (depth < 0)
                                    _subMgr.Subscribe(topicId,origin,String.Empty,instrument,field);
                                else
                                    _subMgr.Subscribe(topicId,origin,String.Empty,instrument,field,depth);
                            }
                            return _binanceAdapter.Subscribe(origin, instrument, field, depth);
                        default:
                            return "ERROR: Unsupported origin: " + origin;
                    }
                    return SubscriptionManager.UninitializedValue;
                }
                return "ERROR: Expected: origin, vendor, instrument, field, [depth]";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        //
        // Excel calls this when it wants to cancel subscription.
        //
        void IRtdServer.DisconnectData (int topicId)
        {
            lock (_subMgr)
            {
                _subMgr.Unsubscribe(topicId);
            }
        }

        //
        // Excel calls this every once in a while.
        //
        int IRtdServer.Heartbeat ()
        {
            return 1;
        }

        //
        // Excel calls this to get changed values. 
        //
        Array IRtdServer.RefreshData (ref int topicCount)
        {
            var updates = GetUpdatedValues();
            topicCount = updates.Count;

            object[,] data = new object[2, topicCount];

            int i = 0;
            foreach (var info in updates)
            {
                data[0, i] = info.TopicId;
                data[1, i] = info.Value;

                if (info.Value.GetType().IsArray)
                {
                    data[1, i] = info.Value.ToString();  // RTD Does not supprt arrays, so pass a string
                }
                else
                {
                    data[1, i] = info.Value;
                }

                i++;
            }
            return data;
        }
        
        //
        // Helper function which checks if new data is available and,
        // if so, notifies Excel about it.
        //
        private void TimerElapsed (object sender, EventArgs e)
        {
            bool wasMarketDataUpdated;

            lock (_subMgr)
            {
                wasMarketDataUpdated = _subMgr.IsDirty;
            }

            if (wasMarketDataUpdated)
            {
                // Notify Excel that Market Data has been updated
                _subMgr.Set(CLOCK, DateTime.Now.ToLocalTime());
                _callback.UpdateNotify();
            }
        }

        private void OnWebSocketMessageReceived (object sender, WebSocketMessageEventArgs e)
        {
            // Assume the incoming string represents a JSON message.
            // Parse it, and access it via "dynamic" variable (no ["field"] and casts necessary).
            dynamic jobj = e.Message;

            if (jobj.type == "ticker")
            {
                string prod = jobj.product_id;
                string origin = GDAX;

                lock (_subMgr)
                {
                    _subMgr.Set(SubscriptionManager.FormatPath(origin, String.Empty, prod, "BID"),jobj.best_bid);
                    _subMgr.Set(SubscriptionManager.FormatPath(origin, String.Empty, prod, "ASK"),jobj.best_ask);
                    _subMgr.Set(SubscriptionManager.FormatPath(origin, String.Empty, prod, "LAST_SIZE"),jobj.last_size);
                    _subMgr.Set(SubscriptionManager.FormatPath(origin, String.Empty, prod, "LAST_PRICE"),jobj.price);
                    _subMgr.Set(SubscriptionManager.FormatPath(origin, String.Empty, prod, "LAST_SIDE"),jobj.side);
                    _subMgr.Set(SubscriptionManager.FormatPath(origin, String.Empty, prod, "high_24h"), jobj.high_24h);
                    _subMgr.Set(SubscriptionManager.FormatPath(origin, String.Empty, prod, "low_24h"), jobj.low_24h);
                    _subMgr.Set(SubscriptionManager.FormatPath(origin, String.Empty, prod, "open_24h"), jobj.open_24h);
                    _subMgr.Set(SubscriptionManager.FormatPath(origin, String.Empty, prod, "volume_24h"), jobj.volume_24h);
                }
            }
        }

        private void SubscribeGdaxWebSocketToTicker (int topicId, string instrument)
        {
            try
            {
                _socket.SubscribeTickers(instrument);
            }
            catch (Exception e)
            {
                _subMgr.Set(topicId, e.Message);
            }
        }
        List<UpdatedValue> GetUpdatedValues ()
        {
            lock (_subMgr)
            {
                return _subMgr.GetUpdatedValues();
            }
        }
    }


    struct UpdatedValue
    {
        public int TopicId { get; private set; }
        public object Value { get; private set; }

        public UpdatedValue (int topicId, object value) : this()
        {
            TopicId = topicId;

            if (value is String || value is Newtonsoft.Json.Linq.JValue)
            {
                if (Decimal.TryParse(value.ToString(), out Decimal dec))
                    Value = dec;
                else
                    Value = value;

                if (dec > 1500_000_000_000 && dec < 1600_000_000_000)
                    Value = DateTimeOffset
                        .FromUnixTimeMilliseconds(Decimal.ToInt64(dec))
                        .DateTime
                        .ToLocalTime();
            }
            else
            {
                Value = value;
            }
        }
    }


    class SubscriptionManager
    {
        public static readonly string UninitializedValue = "<?>";
        public static readonly string UnsupportedField = "<!>";

        readonly Dictionary<string, SubInfo> _subByPath;
        readonly Dictionary<int, SubInfo> _subByTopicId;
        readonly Dictionary<int, SubInfo> _dirtyMap;

        public SubscriptionManager ()
        {
            _subByPath = new Dictionary<string, SubInfo>();
            _subByTopicId = new Dictionary<int, SubInfo>();
            _dirtyMap = new Dictionary<int, SubInfo>();
        }

        public bool IsDirty {
            get
            {
                return _dirtyMap.Count > 0;
            }
        }
        public void Subscribe(int topicId, string origin)
        {
            var subInfo = new SubInfo(topicId, origin);

            _subByTopicId[topicId] = subInfo;
            _subByPath[subInfo.Path] = subInfo;
        }
        public void Subscribe(int topicId, string origin, string vendor, string instrument, string field)
        {
            var subInfo = new SubInfo(
                topicId,
                FormatPath(origin, vendor, instrument, field));

            _subByTopicId[topicId] = subInfo;
            _subByPath[subInfo.Path] = subInfo;
        }
        public void Subscribe(int topicId, string origin, string vendor, string instrument, string field, int depth)
        {
            var subInfo = new SubInfo(
                topicId,
                FormatPath(origin, vendor, instrument, field, depth));

            _subByTopicId[topicId] = subInfo;
            _subByPath[subInfo.Path] = subInfo;
        }

        public void Unsubscribe (int topicId)
        {
            SubInfo subInfo;
            if (_subByTopicId.TryGetValue(topicId, out subInfo))
            {
                _subByTopicId.Remove(topicId);
                _subByPath.Remove(subInfo.Path);
            }
        }

        public List<UpdatedValue> GetUpdatedValues ()
        {
            var updated = new List<UpdatedValue>(_subByTopicId.Count);

            lock (_dirtyMap)
            {
                foreach (var subInfo in _dirtyMap.Values)
                {
                    updated.Add(new UpdatedValue(subInfo.TopicId, subInfo.Value));
                }
                _dirtyMap.Clear();
            }

            return updated;
        }

        public void Set(string path, object value)
        {
            SubInfo subInfo;
            if (_subByPath.TryGetValue(path, out subInfo))
            {
                if (value != subInfo.Value)
                {
                    subInfo.Value = value;
                    lock (_dirtyMap)
                        _dirtyMap[subInfo.TopicId] = subInfo;
                }
            }
        }
        public void Set(int topicId, object value)
        {
            SubInfo subInfo;
            if (_subByTopicId.TryGetValue(topicId, out subInfo))
            {
                if (value != subInfo.Value)
                {
                    subInfo.Value = value;
                    lock(_dirtyMap)
                        _dirtyMap[subInfo.TopicId] = subInfo;
                }
            }
        }

        public static string FormatPath(string origin, string vendor, string instrument, string field)
        {
            return string.Format("{0}/{1}/{2}/{3}",
                                 origin.ToUpperInvariant(),
                                 vendor?.ToUpperInvariant(),
                                 instrument?.ToUpperInvariant(),
                                 field?.ToUpperInvariant());
        }
        public static string FormatPath(string origin, string vendor, string instrument, string field, int num)
        {
            return string.Format("{0}/{1}/{2}/{3}/{4}",
                                 origin.ToUpperInvariant(),
                                 vendor?.ToUpperInvariant(),
                                 instrument?.ToUpperInvariant(),
                                 field?.ToUpperInvariant(),
                                 num);  // can be depth or limit
        }
        class SubInfo
        {
            public int TopicId { get; private set; }
            public string Path { get; private set; }
            public object Value { get; set; }

            public SubInfo (int topicId, string path)
            {
                TopicId = topicId;
                Path = path;
                Value = UninitializedValue;
            }
        }
    }
}
