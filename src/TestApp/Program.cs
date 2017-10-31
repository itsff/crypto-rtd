using System;
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
            _rtd = new WebSocketRtdServer();
            _rtd.ServerStart(this);

            Sub("BTC-USD", "BID");
            Sub("BTC-USD", "ASK");
            Sub("BTC-USD", "LAST_PRICE");
            Sub("BTC-USD", "LAST_SIZE");
            Sub("BTC-USD", "LAST_SIDE");
            
            Sub("ETH-USD", "BID");
            Sub("ETH-USD", "ASK");
            Sub("ETH-USD", "LAST_PRICE");
            Sub("ETH-USD", "LAST_SIZE");
            Sub("ETH-USD", "LAST_SIDE");

            // Start up a Windows message pump and spin forever.
            Dispatcher.Run();
        }

        int _topic;
        void Sub (string instrument, string field)
        {
            Console.WriteLine("Subscribing: topic={0}, instr={1}, field={2}", _topic, instrument, field);
            
            var a = new[]
                    {
                        "GDAX",
                        instrument,
                        field
                    };

            Array crappyArray = a;

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
                Console.WriteLine("{0}\t{1}", values.GetValue(0, i), values.GetValue(1, i));
            }
        }

        int IRtdUpdateEvent.HeartbeatInterval { get; set; }
        
        void IRtdUpdateEvent.Disconnect ()
        {
            Console.WriteLine("Disconnect called.");
        }
    }
}
