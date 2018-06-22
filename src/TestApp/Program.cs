using System;
using System.Collections.Generic;
using System.Windows.Threading;
using CryptoRtd;

namespace TestApp
{
    class Program : IRtdUpdateEvent
    {
        [STAThread]
        public static void Main (string[] args)
        {
            var me = new Program();

            IRtdUpdateEvent me2 = me;
            me2.HeartbeatInterval = 100;
            
            me.Run();
        }

        IRtdServer _rtd;

        void Run ()
        {
            _rtd = new CryptoRtdServer();
            _rtd.ServerStart(this);

            const string BINANCE = "BINANCE";

            foreach(string field in RtdFields.ALL_FIELDS) 
                Sub(BINANCE, "ETHUSDT", field);

            Sub(CryptoRtdServer.GDAX, "BTC-USD", "BID");
            Sub(CryptoRtdServer.GDAX, "BTC-USD", "ASK");
            Sub(CryptoRtdServer.GDAX, "BTC-USD", "LAST_PRICE");
            Sub(CryptoRtdServer.GDAX, "BTC-USD", "LAST_SIZE");
            Sub(CryptoRtdServer.GDAX, "BTC-USD", "LAST_SIDE");

            //Sub(CryptoRtdServer.GDAX, "ETH-USD", "BID");
            //Sub(CryptoRtdServer.GDAX, "ETH-USD", "ASK");
            //Sub(CryptoRtdServer.GDAX, "ETH-USD", "LAST_PRICE");
            //Sub(CryptoRtdServer.GDAX, "ETH-USD", "LAST_SIZE");
            //Sub(CryptoRtdServer.GDAX, "ETH-USD", "LAST_SIDE");

            // Start up a Windows message pump and spin forever.
            Dispatcher.Run();
        }

        int _topic;
        Dictionary<int, Array> topics = new Dictionary<int, Array>();

        void Sub (string origin, string instrument, string field)
        {
            Console.WriteLine("Subscribing: topic={0}, instr={1}, field={2}, origin={3}", _topic, instrument, field, origin);

            var a = new[]
                    {
                        origin,
                        instrument,
                        field
                    };

            Array crappyArray = a;
            topics.Add(_topic, crappyArray);

            bool newValues = false;
            _rtd.ConnectData(_topic++, ref crappyArray, ref newValues);
        }


        void IRtdUpdateEvent.UpdateNotify ()
        {
            Console.WriteLine("UpdateNotified called ---------------------");

            int topicCount = 0;
            var values = _rtd.RefreshData(ref topicCount);

            for (int i = 0; i < topicCount; ++i)
            {
                int topic = (int)values.GetValue(0, i);
                Array arr;
                topics.TryGetValue(topic, out arr);

                Console.WriteLine("{0}|{1}|{2}\t{3}", arr.GetValue(0), arr.GetValue(1), arr.GetValue(2), values.GetValue(1, i));
            }
        }

        int IRtdUpdateEvent.HeartbeatInterval { get; set; }
        
        void IRtdUpdateEvent.Disconnect ()
        {
            Console.WriteLine("Disconnect called.");
        }
    }
}
