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


        //Info
        public const string DRIFT = "DRIFT";
        public const string EXCHANGE_TIME = "EXCHANGE_TIME";
        public const string EXCHANGE_TIMEZONE = "EXCHANGE_TIMEZONE";
        public const string EXCHANGE_SYMBOLS = "EXCHANGE_SYMBOLS";
        //public const string EXCHANGE_RATE_LIMITS = "EXCHANGE_RATE_LIMITS";
        //public const string EXCHANGE_FILTERS = "EXCHANGE_FILTERS";

        public const string BASE_ASSET = "BASE_ASSET";
        public const string BASE_ASSET_PRECISION = "BASE_ASSET_PRECISION";
        //public const string FILTERS = "FILTERS";
        public const string ICEBERG_ALLOWED = "ICEBERG_ALLOWED";
        public const string NAME = "NAME";
        public const string ORDER_TYPES = "ORDER_TYPES";
        public const string QUOTE_ASSET ="QUOTE_ASSET";
        public const string QUOTE_ASSET_PRECISION = "QUOTE_ASSET_PRECISION";
        public const string STATUS = "STATUS";

        // Price
        public const string PRICE = "PRICE";
        public const string SYMBOL = "SYMBOL";
        public static readonly string[] PRICE_FIELDS = { PRICE, SYMBOL, DRIFT };

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
        public static readonly string[] DEPTH = { SYMBOL, ASK_DEPTH, ASK_DEPTH_SIZE, BID_DEPTH, BID_DEPTH_SIZE };


        // Trade
        public const string TRADE_ID = "TRADE_ID";
        public const string QUANTITY = "QUANTITY";
        public const string TRADE_TIME = "TRADE_TIME";
        public const string BUYER_ORDER_ID = "BUYER_ORDER_ID";
        public const string SELLER_ORDER_ID = "SELLER_ORDER_ID";
        public const string BUYER_IS_MAKER = "BUYER_IS_MAKER";
        public const string IGNORE = "IGNORE";

        public const string IS_BEST_MATCH = "IS_BEST_MATCH";  // Historic

        public static readonly string[] TRADE = { SYMBOL, TRADE_ID, TRADE_TIME, PRICE, QUANTITY, BUYER_ORDER_ID, SELLER_ORDER_ID, BUYER_IS_MAKER, IGNORE };

        // Candles
        public const string EVENT = "EVENT";
        public const string EVENT_TIME = "EVENT_TIME";
        public const string FINAL = "FINAL";
        public const string INTERVAL = "INTERVAL";

        public const string TAKE_BUY_VOL = "TAKE_BUY_VOL";
        public const string TAKE_BUY_QUOTE_VOL = "TAKE_BUY_QUOTE_VOL";

        public static readonly string[] KLINE = { SYMBOL, EVENT, EVENT_TIME, OPEN_TIME, CLOSE_TIME, OPEN, CLOSE, HIGH, LOW, FINAL, INTERVAL};


        public static string[] ALL_FIELDS { get { return PRICE_FIELDS.Concat(PRICE_24H).Concat(DEPTH).Concat(TRADE).ToArray(); } }
    }
}
