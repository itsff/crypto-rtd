using Binance.Net;
using Binance.Net.Objects;
using CryptoExchange.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CryptoRtd
{
    class BinanceAdapter
    {
        public const string BINANCE = "BINANCE";

        private SubscriptionManager _subMgr;

        BinanceSocketClient socketClient;
        private Dictionary<string, bool> SubscribedTick = new Dictionary<string, bool>();
        private Dictionary<string, bool> SubscribedDepth = new Dictionary<string, bool>();
        private Dictionary<string, bool> SubscribedTrade = new Dictionary<string, bool>();

        private Dictionary<string, BinanceStreamTick> TickCache = new Dictionary<string, BinanceStreamTick>();
        private Dictionary<string, BinanceStreamOrderBook> DepthCache = new Dictionary<string, BinanceStreamOrderBook>();
        private Dictionary<string, BinanceStreamTrade> TradeCache = new Dictionary<string, BinanceStreamTrade>();

        public BinanceAdapter(SubscriptionManager subMgr)
        {
            _subMgr = subMgr;

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            BinanceSocketClient.SetDefaultOptions(new BinanceSocketClientOptions()
            {
            //    ApiCredentials = new CryptoExchange.Net.Authentication.ApiCredentials(
            //        "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx", 
            //        "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"),
                LogVerbosity = CryptoExchange.Net.Logging.LogVerbosity.Debug,
                LogWriters = { Console.Out }
            });

            BinanceClient.SetDefaultOptions(new BinanceClientOptions()
            {
            //    ApiCredentials = new CryptoExchange.Net.Authentication.ApiCredentials(
            //        "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx", 
            //        "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"),
                LogVerbosity = CryptoExchange.Net.Logging.LogVerbosity.Debug,
                LogWriters = { Console.Out }
            });

            socketClient = new BinanceSocketClient();
        }

        private object CacheResult(string origin, string instrument, string field, object value)
        {
            lock (_subMgr)
            {
                _subMgr.Set(SubscriptionManager.FormatPath(BINANCE, string.Empty, instrument, field), value);
            }

            return value;
        }
        private object CacheResult(string origin, string instrument, string field, int depth, object value)
        {
            lock (_subMgr)
            {
                _subMgr.Set(SubscriptionManager.FormatPath(BINANCE, string.Empty, instrument, field, depth), value);
            }

            return value;
        }

        public object Subscribe(string instrument, string field, int depth)
        {
            switch (field)
            {
                case RtdFields.OPEN:
                case RtdFields.OPEN_TIME:
                case RtdFields.CLOSE:
                case RtdFields.CLOSE_TIME:
                    return Get24HPrice(instrument, field);

                case RtdFields.ASK_DEPTH:
                case RtdFields.ASK_DEPTH_SIZE:
                case RtdFields.BID_DEPTH:
                case RtdFields.BID_DEPTH_SIZE:
                    return SubscribeOrderBook(instrument, field, depth);

                case RtdFields.TRADE_ID:
                case RtdFields.TRADE_PRICE:
                case RtdFields.TRADE_QUANTITY:
                case RtdFields.BUYER_ORDER_ID:
                case RtdFields.SELLER_ORDER_ID:
                case RtdFields.BUYER_IS_MAKER:
                case RtdFields.IGNORE:
                    return SubscribeTrade(instrument, field);

                default:
                    return SubscribeTick(instrument, field);
            }
        }
        // synchronously
        [Obsolete]
        private object GetPrice(string instrument, string field)
        {
            using (var client = new BinanceClient())
            {
                CallResult<BinancePrice> result = client.GetPrice(instrument);

                if (result.Success)
                {
                    switch (field)
                    {
                        case RtdFields.PRICE: return CacheResult(BINANCE, instrument, field, result.Data.Price);
                        case RtdFields.SYMBOL: return CacheResult(BINANCE, instrument, field, result.Data.Symbol);
                    }
                    return SubscriptionManager.UninitializedValue;
                }
                else
                    return CacheResult(BINANCE, instrument, field, result.Error.Message);
            }
        }
        // synchronously
        [Obsolete]
        private object Get24HPrice(string instrument, string field)
        {
            using (var client = new BinanceClient())
            {
                CallResult<Binance24HPrice> result = client.Get24HPrice(instrument);

                if (result.Success)
                {
                    var data = result.Data;
                    switch (field)
                    {
                        case RtdFields.FIRST_ID: return CacheResult(BINANCE, instrument, field, data.FirstId);
                        case RtdFields.LAST_ID: return CacheResult(BINANCE, instrument, field, data.LastId);
                        case RtdFields.QUOTE_VOL: return CacheResult(BINANCE, instrument, field, data.QuoteVolume);
                        case RtdFields.VOL: return CacheResult(BINANCE, instrument, field, data.Volume);

                        case RtdFields.ASK: return CacheResult(BINANCE, instrument, field, data.AskPrice);
                        case RtdFields.ASK_SIZE: return CacheResult(BINANCE, instrument, field, data.AskQuantity);
                        case RtdFields.BID: return CacheResult(BINANCE, instrument, field, data.BidPrice);
                        case RtdFields.BID_SIZE: return CacheResult(BINANCE, instrument, field, data.BidQuantity);

                        case RtdFields.LOW: return CacheResult(BINANCE, instrument, field, data.LowPrice);
                        case RtdFields.HIGH: return CacheResult(BINANCE, instrument, field, data.HighPrice);
                        case RtdFields.LAST: return CacheResult(BINANCE, instrument, field, data.LastPrice);
                        case RtdFields.LAST_SIZE: return CacheResult(BINANCE, instrument, field, data.LastQuantity);
                        case RtdFields.OPEN: return CacheResult(BINANCE, instrument, field, data.OpenPrice);
                        case RtdFields.OPEN_TIME: return CacheResult(BINANCE, instrument, field, data.OpenTime);
                        case RtdFields.CLOSE: return CacheResult(BINANCE, instrument, field, data.PreviousClosePrice);
                        case RtdFields.CLOSE_TIME: return CacheResult(BINANCE, instrument, field, data.CloseTime);

                        case RtdFields.VWAP: return CacheResult(BINANCE, instrument, field, data.WeightedAveragePrice);
                        case RtdFields.PRICE_PCT: return CacheResult(BINANCE, instrument, field, data.PriceChangePercent / 100);
                        case RtdFields.PRICE_CHG: return CacheResult(BINANCE, instrument, field, data.PriceChange);
                        case RtdFields.TRADES: return CacheResult(BINANCE, instrument, field, data.Trades);

                        case RtdFields.SPREAD: return CacheResult(BINANCE, instrument, field, data.AskPrice - data.BidPrice);
                    }
                    return SubscriptionManager.UninitializedValue;
                }
                else
                    return CacheResult(BINANCE, instrument, field, result.Error.Message);
            }
        }

        private void CacheTick(BinanceStreamTick data)
        {
            var instrument = data.Symbol;
            CacheResult(BINANCE, instrument, RtdFields.FIRST_ID, data.FirstTradeId);
            CacheResult(BINANCE, instrument, RtdFields.LAST_ID, data.LastTradeId);
            CacheResult(BINANCE, instrument, RtdFields.QUOTE_VOL, data.TotalTradedQuoteAssetVolume);
            CacheResult(BINANCE, instrument, RtdFields.VOL, data.TotalTradedBaseAssetVolume);

            CacheResult(BINANCE, instrument, RtdFields.ASK, data.BestAskPrice);
            CacheResult(BINANCE, instrument, RtdFields.ASK_SIZE, data.BestAskQuantity);
            CacheResult(BINANCE, instrument, RtdFields.BID, data.BestBidPrice);
            CacheResult(BINANCE, instrument, RtdFields.BID_SIZE, data.BestBidQuantity);

            CacheResult(BINANCE, instrument, RtdFields.LOW, data.LowPrice);
            CacheResult(BINANCE, instrument, RtdFields.HIGH, data.HighPrice);

            CacheResult(BINANCE, instrument, RtdFields.VWAP, data.WeightedAverage);
            CacheResult(BINANCE, instrument, RtdFields.PRICE_PCT, data.PriceChangePercentage / 100);
            CacheResult(BINANCE, instrument, RtdFields.PRICE_CHG, data.PriceChange);
            CacheResult(BINANCE, instrument, RtdFields.TRADES, data.TotalTrades);

            CacheResult(BINANCE, instrument, RtdFields.SPREAD, data.BestAskPrice - data.BestBidPrice);
        }

        private object DecodeTick(BinanceStreamTick data, string field)
        {
            var instrument = data.Symbol;
            switch (field)
            {
                case RtdFields.FIRST_ID: return data.FirstTradeId;
                case RtdFields.LAST_ID: return data.LastTradeId;
                case RtdFields.QUOTE_VOL: return data.TotalTradedQuoteAssetVolume;
                case RtdFields.VOL: return data.TotalTradedBaseAssetVolume;

                case RtdFields.ASK: return data.BestAskPrice;
                case RtdFields.ASK_SIZE: return data.BestAskQuantity;
                case RtdFields.BID: return data.BestBidPrice;
                case RtdFields.BID_SIZE: return data.BestBidQuantity;

                case RtdFields.LOW: return data.LowPrice;
                case RtdFields.HIGH: return data.HighPrice;

                case RtdFields.VWAP: return data.WeightedAverage;
                case RtdFields.PRICE_PCT: return data.PriceChangePercentage / 100;
                case RtdFields.PRICE_CHG: return data.PriceChange;
                case RtdFields.TRADES: return data.TotalTrades;

                case RtdFields.SPREAD: return data.BestAskPrice - data.BestBidPrice;
            }
            return SubscriptionManager.UninitializedValue;
        }

        private object SubscribeTick(string instrument, string field)
        {
            var key = instrument;// + "|" + field;

            if (SubscribedTick.ContainsKey(key)) { 

                BinanceStreamTick tick;
                if (TickCache.TryGetValue(key, out tick))
                    return DecodeTick(tick, field);
                else
                    return SubscriptionManager.UninitializedValue;
            }
            else
            {
                SubscribedTick.Add(instrument, true);
                var successSymbol = socketClient.SubscribeToSymbolTicker(instrument, (BinanceStreamTick data) =>
                {
                    TickCache[key] = data;
                    CacheTick(data);
                });
                return SubscriptionManager.UninitializedValue;
            }
        }

        private void CacheOrderBook(BinanceStreamOrderBook stream)
        {
            var instrument = stream.Symbol;
            var bidCount = stream.Bids.Count;
            var askCount = stream.Asks.Count;


            for(int depth = 0; depth < bidCount; depth++)
            {
                CacheResult(BINANCE, instrument, RtdFields.BID_DEPTH, depth, stream.Bids[depth].Price);
                CacheResult(BINANCE, instrument, RtdFields.BID_DEPTH_SIZE, depth, stream.Bids[depth].Quantity);
            }
            //for (int depth = bidCount; depth < 10; depth++)
            //{
            //    CacheResult(BINANCE, instrument, RtdFields.BID_DEPTH, depth, SubscriptionManager.UninitializedValue);
            //    CacheResult(BINANCE, instrument, RtdFields.BID_DEPTH_SIZE, depth, SubscriptionManager.UninitializedValue);
            //}

            for (int depth = 0; depth < askCount; depth++)
            {
                CacheResult(BINANCE, instrument, RtdFields.ASK_DEPTH, depth, stream.Asks[depth].Price);
                CacheResult(BINANCE, instrument, RtdFields.ASK_DEPTH_SIZE, depth, stream.Asks[depth].Quantity);
            }
            //for (int depth = askCount; depth < 10; depth++)
            //{
            //    CacheResult(BINANCE, instrument, RtdFields.ASK_DEPTH, depth, SubscriptionManager.UninitializedValue);
            //    CacheResult(BINANCE, instrument, RtdFields.ASK_DEPTH_SIZE, depth, SubscriptionManager.UninitializedValue);
            //}
        }

        private object DecodeOrderBook(BinanceStreamOrderBook stream, string field, int depth)
        {
            int askCount = stream.Asks.Count;
            int bidCount = stream.Bids.Count;

            switch (field)
            {
                case RtdFields.ASK_DEPTH:
                    if (depth >= askCount)
                        return SubscriptionManager.UninitializedValue;

                    return stream.Asks[depth].Price;

                case RtdFields.ASK_DEPTH_SIZE:
                    if (depth >= askCount)
                        return SubscriptionManager.UninitializedValue;

                    return stream.Asks[depth].Quantity;

                case RtdFields.BID_DEPTH:
                    if (depth >= bidCount)
                        return SubscriptionManager.UninitializedValue;

                    return stream.Bids[depth].Price;

                case RtdFields.BID_DEPTH_SIZE:
                    if (depth >= bidCount)
                        return SubscriptionManager.UninitializedValue;

                    return stream.Bids[depth].Quantity;
            }
            return SubscriptionManager.UninitializedValue;
        }

        private object SubscribeOrderBook(string instrument, string field, int depth)
        {
            var key = instrument;// + "|" + field;

            if (SubscribedDepth.ContainsKey(key))
            {
                BinanceStreamOrderBook stream;
                if (DepthCache.TryGetValue(key, out stream))
                    return DecodeOrderBook(stream, field, depth);
                else
                    return SubscriptionManager.UninitializedValue;
            }
            else
            {
                SubscribedDepth.Add(key, true);
                var successSymbol = socketClient.SubscribeToPartialBookDepthStream(instrument, 10, (BinanceStreamOrderBook stream) =>
                {
                    DepthCache[key] = stream;
                    CacheOrderBook(stream);
                });
                return SubscriptionManager.UninitializedValue;
            }
        }
        private void CacheTrade(BinanceStreamTrade stream)
        {
            var instrument = stream.Symbol;
            CacheResult(BINANCE, instrument, RtdFields.TRADE_ID, stream.TradeId);
            CacheResult(BINANCE, instrument, RtdFields.TRADE_PRICE, stream.Price);
            CacheResult(BINANCE, instrument, RtdFields.TRADE_QUANTITY, stream.Quantity);
            CacheResult(BINANCE, instrument, RtdFields.BUYER_ORDER_ID, stream.BuyerOrderId);
            CacheResult(BINANCE, instrument, RtdFields.SELLER_ORDER_ID, stream.SellerOrderId);
            CacheResult(BINANCE, instrument, RtdFields.BUYER_IS_MAKER, stream.BuyerIsMaker);
            CacheResult(BINANCE, instrument, RtdFields.IGNORE, stream.Ignore);
        }

        private object DecodeTrade(BinanceStreamTrade stream, string field)
        {
            switch (field)
            {
                case RtdFields.SYMBOL: return stream.Symbol;
                case RtdFields.TRADE_ID: return stream.TradeId;
                case RtdFields.TRADE_PRICE: return stream.Price;
                case RtdFields.TRADE_QUANTITY: return stream.Quantity;

                case RtdFields.BUYER_ORDER_ID: return stream.BuyerOrderId;
                case RtdFields.SELLER_ORDER_ID: return stream.SellerOrderId;
                case RtdFields.BUYER_IS_MAKER: return stream.BuyerIsMaker;
                case RtdFields.IGNORE: return stream.Ignore;
            }
            return SubscriptionManager.UninitializedValue;
        }
        private object SubscribeTrade(string instrument, string field)
        {
            var key = instrument;// + "|" + field;

            if (SubscribedTrade.ContainsKey(key))
            {
                BinanceStreamTrade stream;
                if (TradeCache.TryGetValue(key, out stream))
                    return DecodeTrade(stream, field);
                else
                    return SubscriptionManager.UninitializedValue;
            }
            else
            {
                SubscribedTrade.Add(key, true);
                var successSymbol = socketClient.SubscribeToTradesStream (instrument, (BinanceStreamTrade stream) =>
                {
                    TradeCache[key] = stream;
                    CacheTrade(stream);
                });
                return SubscriptionManager.UninitializedValue;
            }
        }
    }
}
