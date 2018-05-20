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

        //
        // This is the string that names RTD server.
        // Users will use it from Excel: =RTD("crypto",, ....)
        //
#if RELEASE
        ProgId("crypto")
#else
        ProgId("crypto-debug")
#endif
    ]
    public class CryptoRtdServer : IRtdServer
    {
        IRtdUpdateEvent _callback;
        DispatcherTimer _timer;
        readonly SubscriptionManager _subMgr;


        // Oldie but goodie. WebSocket library that works on .NET 4.0
        GdaxWebSocketClient _socket;

        public const string BINANCE = "BINANCE";
        public const string GDAX = "GDAX";

        public CryptoRtdServer ()
        {
            _subMgr = new SubscriptionManager();
            _socket = new GdaxWebSocketClient(new Uri("wss://ws-feed.gdax.com"));


            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            //Assembly assem = Assembly.LoadFile(@"P:\crypto-rtd\strong\CryptoExchange.Net.dll");
            //if (assem == null)
            //    Console.WriteLine("Unable to load assembly...");
            //else
            //    Console.WriteLine(assem.FullName);
        }

        //
        // Excel calls this. It's an entry point. It passes us a callback
        // structure which we save for later.
        //
        int IRtdServer.ServerStart (IRtdUpdateEvent callback)
        {
            _callback = callback;

            //
            // We will throttle out updates so that Excel can keep up.
            // It is also important to invoke the Excel callback notify
            // function from the COM thread. System.Windows.Threading' 
            // DispatcherTimer will use COM thread's message pump.
            //
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(0.5);
            _timer.Tick += TimerElapsed;
            _timer.Start();

            _socket.Start();
            _socket.MessageReceived += OnWebSocketMessageReceived;

            //_binanceViewModel.Init();

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

            //_binanceViewModel = null;
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
            newValues = true;

            // We assume 3 strings: Origin, Instrument, Field
            if (strings.Length == 3)
            {
                // Crappy COM-style arrays...
                string origin = strings.GetValue(0).ToString().ToUpperInvariant();         // The Exchange
                string instrument = strings.GetValue(1).ToString().ToUpperInvariant();
                string field = strings.GetValue(2).ToString().ToUpperInvariant();

                if (field.Equals("ALL_FIELDS"))
                    return RtdFields.ALL_FIELDS;

                switch (origin)
                {
                    case GDAX:
                        lock (_subMgr)
                        {
                            // Let's use Empty strings for now
                            _subMgr.Subscribe(
                                topicId,
                                origin,
                                String.Empty,
                                instrument,
                                field);
                        }
                        SubscribeGdaxWebSocketToTicker(instrument);
                        break;
                    case BINANCE:
                        return SubscribeBinance(instrument, field);
                    default:
                        return SubscriptionManager.UninitializedValue;
                }

            }

            return "ERROR: Expected: origin, vendor, instrument, field";
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

            for (int i = 0; i < topicCount; ++i)
            {
                UpdatedValue info = updates[i];

                data[0, i] = info.TopicId;
                data[1, i] = info.Value;
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
                string bid = jobj.best_bid;
                string ask = jobj.best_ask;
                string ltp = jobj.price;
                string ltq = jobj.last_size;
                string side = jobj.side;
                string origin = GDAX;

                lock (_subMgr)
                {
                    _subMgr.Set(
                        SubscriptionManager.FormatPath(
                            origin: origin,
                            vendor: String.Empty,
                            instrument: prod,
                            field: "BID"),
                        bid);

                    _subMgr.Set(
                        SubscriptionManager.FormatPath(
                            origin: origin,
                            vendor: String.Empty,
                            instrument: prod,
                            field: "ASK"),
                        ask);

                    _subMgr.Set(
                        SubscriptionManager.FormatPath(
                            origin: origin,
                            vendor: String.Empty,
                            instrument: prod,
                            field: "LAST_SIZE"),
                        ltq);

                    _subMgr.Set(
                        SubscriptionManager.FormatPath(
                            origin: origin,
                            vendor: String.Empty,
                            instrument: prod,
                            field: "LAST_PRICE"),
                        ltp);
                    
                    _subMgr.Set(
                        SubscriptionManager.FormatPath(
                            origin: origin,
                            vendor: String.Empty,
                            instrument: prod,
                            field: "LAST_SIDE"),
                        side);
                }
            }
        }

        private void SubscribeGdaxWebSocketToTicker (string instrument)
        {
            _socket.SubscribeTickers(instrument);
        }
        private object SubscribeBinance(string instrument, string field)
        {
            switch(field)
            {
                case RtdFields.PRICE:
                case RtdFields.SYMBOL:
                    return SubscribeBinancePrice(instrument, field);
                default:
                    return SubscribeBinance24HPrice(instrument, field);
            }
        }

        private object CacheResult(string origin, string instrument, string field, object value)
        {
            lock (_subMgr)
                _subMgr.Set(SubscriptionManager.FormatPath(BINANCE, string.Empty, instrument, field), value.ToString());

            return value;
        }

        private object SubscribeBinancePrice(string instrument, string field)
        {
            using (var client = new BinanceClient())
            {
                CallResult<BinancePrice> result = client.GetPrice(instrument);

                if (result.Success) {
                    switch (field)
                    {
                        case RtdFields.PRICE: return CacheResult(BINANCE, instrument, field, result.Data.Price);
                        case RtdFields.SYMBOL: return CacheResult(BINANCE, instrument, field, result.Data.Symbol);
                    }
                    return SubscriptionManager.UninitializedValue;
                } else
                    return CacheResult(BINANCE, instrument, field, result.Error.Message);
            }
        }
        private object SubscribeBinance24HPrice(string instrument, string field)
        {
            using (var client = new BinanceClient())
            {
                CallResult<Binance24HPrice> result = client.Get24HPrice(instrument);

                if (result.Success)
                {
                    switch (field)
                    {
                        case RtdFields.FIRST_ID: return CacheResult(BINANCE, instrument, field, result.Data.FirstId);
                        case RtdFields.LAST_ID: return CacheResult(BINANCE, instrument, field, result.Data.LastId);
                        case RtdFields.QUOTE_VOL: return CacheResult(BINANCE, instrument, field, result.Data.QuoteVolume);
                        case RtdFields.VOL: return CacheResult(BINANCE, instrument, field, result.Data.Volume);

                        case RtdFields.ASK: return CacheResult(BINANCE, instrument, field, result.Data.AskPrice);
                        case RtdFields.ASK_SIZE: return CacheResult(BINANCE, instrument, field, result.Data.AskQuantity);
                        case RtdFields.BID: return CacheResult(BINANCE, instrument, field, result.Data.BidPrice);
                        case RtdFields.BID_SIZE: return CacheResult(BINANCE, instrument, field, result.Data.BidQuantity);

                        case RtdFields.LOW: return CacheResult(BINANCE, instrument, field, result.Data.LowPrice);
                        case RtdFields.HIGH: return CacheResult(BINANCE, instrument, field, result.Data.HighPrice);
                        case RtdFields.LAST: return CacheResult(BINANCE, instrument, field, result.Data.LastPrice);
                        case RtdFields.LAST_SIZE: return CacheResult(BINANCE, instrument, field, result.Data.LastQuantity);
                        case RtdFields.OPEN: return CacheResult(BINANCE, instrument, field, result.Data.OpenPrice);
                        case RtdFields.OPEN_TIME: return CacheResult(BINANCE, instrument, field, result.Data.OpenTime);
                        case RtdFields.CLOSE: return CacheResult(BINANCE, instrument, field, result.Data.PreviousClosePrice);
                        case RtdFields.CLOSE_TIME: return CacheResult(BINANCE, instrument, field, result.Data.CloseTime);

                        case RtdFields.VWAP: return CacheResult(BINANCE, instrument, field, result.Data.WeightedAveragePrice);
                        case RtdFields.PRICE_PCT: return CacheResult(BINANCE, instrument, field, result.Data.PriceChangePercent/100);
                        case RtdFields.PRICE_CHG: return CacheResult(BINANCE, instrument, field, result.Data.PriceChange);
                        case RtdFields.TRADES: return CacheResult(BINANCE, instrument, field, result.Data.Trades);

                        case RtdFields.SPREAD: return CacheResult(BINANCE, instrument, field, result.Data.AskPrice - result.Data.BidPrice);
                    }
                    return SubscriptionManager.UninitializedValue;
                }
                else
                    return CacheResult(BINANCE, instrument, field, result.Error.Message);
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

        public UpdatedValue (int topicId, string value) : this()
        {
            TopicId = topicId;

            Decimal dec;
            if (Decimal.TryParse(value, out dec))
                Value = dec;
            else 
                Value = value;
        }
    }


    class SubscriptionManager
    {
        public static readonly string UninitializedValue = "<?>";
        
        readonly Dictionary<string, SubInfo> _subByPath;
        readonly Dictionary<int, SubInfo> _subByTopicId;

        public SubscriptionManager ()
        {
            _subByPath = new Dictionary<string, SubInfo>();
            _subByTopicId = new Dictionary<int, SubInfo>();
        }

        public bool IsDirty { get; private set; }

        public void Subscribe (int topicId, string origin, string vendor, string instrument, string field)
        {
            var subInfo = new SubInfo(
                topicId,
                FormatPath(origin, vendor, instrument, field));

            _subByTopicId.Add(topicId, subInfo);
            _subByPath.Add(subInfo.Path, subInfo);
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

            // For simplicity, let's just do a linear scan
            foreach (var subInfo in _subByTopicId.Values)
            {
                if (subInfo.IsDirty)
                {
                    updated.Add(new UpdatedValue(subInfo.TopicId, subInfo.Value));
                    subInfo.IsDirty = false;
                }
            }

            IsDirty = false;

            return updated;
        }

        public void Set(string path, string value)
        {
            SubInfo subInfo;
            if (_subByPath.TryGetValue(path, out subInfo))
            {
                if (value != subInfo.Value)
                {
                    subInfo.Value = value;
                    IsDirty = true;
                }
            }
        }

        public static string FormatPath (string origin, string vendor, string instrument, string field)
        {
            return string.Format("{0}/{1}/{2}/{3}",
                                 origin.ToUpperInvariant(),
                                 vendor.ToUpperInvariant(),
                                 instrument.ToUpperInvariant(),
                                 field.ToUpperInvariant());
        }

        class SubInfo
        {
            public int TopicId { get; private set; }
            public string Path { get; private set; }

            private string _value;

            public string Value
            {
                get { return _value; }
                set
                {
                    _value = value;
                    IsDirty = true;
                }
            }

            public bool IsDirty { get; set; }

            public SubInfo (int topicId, string path)
            {
                TopicId = topicId;
                Path = path;
                Value = UninitializedValue;
                IsDirty = false;
            }
        }
    }
}
