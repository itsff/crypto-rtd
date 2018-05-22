using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CryptoRtd
{
    public class RtdFields
    {
        public const string BINANCE = "BINANCE";

        // Price
        public const string PRICE = "PRICE";
        public const string SYMBOL = "SYMBOL";
        public static readonly string[] PRICE_FIELDS = { PRICE, SYMBOL };

        // 24Price
        public const string FIRST_ID = "FIRST_ID";
        public const string LAST_ID = "LAST_ID";
        public const string QUOTE_VOL = "QUOTE_VOL";
        public const string VOL = "VOL";
        public const string ASK = "ASK";
        public const string ASK_SIZE = "ASK_SIZE";
        public const string BID = "BID";
        public const string BID_SIZE = "BID_SIZE";
        public const string LOW = "LOW";
        public const string HIGH = "HIGH";
        public const string LAST = "LAST";
        public const string LAST_SIZE = "LAST_SIZE";
        public const string OPEN = "OPEN";
        public const string OPEN_TIME = "OPEN_TIME";
        public const string CLOSE = "CLOSE";
        public const string CLOSE_TIME = "CLOSE_TIME";
        public const string VWAP = "VWAP";
        public const string PRICE_PCT = "PRICE%";
        public const string PRICE_CHG = "PRICE_CHANGE";
        public const string TRADES = "TRADES";

        public const string SPREAD = "SPREAD";  // calculated

        public static readonly string[] PRICE_24H = { FIRST_ID, LAST_ID, QUOTE_VOL, VOL, ASK, ASK_SIZE, BID, BID_SIZE, LOW, HIGH, LAST, LAST_SIZE,
                                               OPEN, OPEN_TIME, CLOSE, CLOSE_TIME,VWAP, PRICE_PCT, PRICE_CHG, TRADES, SPREAD };

        // Depth
        public const string ASK_DEPTH = "ASK_DEPTH";
        public const string ASK_DEPTH_SIZE = "ASK_DEPTH_SIZE";
        public const string BID_DEPTH = "BID_DEPTH";
        public const string BID_DEPTH_SIZE = "BID_DEPTH_SIZE";
        public static readonly string[] DEPTH = { ASK_DEPTH, ASK_DEPTH_SIZE, BID_DEPTH, BID_DEPTH_SIZE };

        public static string[] ALL_FIELDS { get { return PRICE_FIELDS.Concat(PRICE_24H).ToArray(); } }
    }
}
