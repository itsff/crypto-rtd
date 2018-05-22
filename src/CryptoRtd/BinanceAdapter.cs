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

        SubscriptionManager _subMgr;

        public BinanceAdapter(SubscriptionManager subMgr)
        {
            _subMgr = subMgr;

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            //BinanceSocketClient.SetDefaultOptions(new BinanceSocketClientOptions()
            //{
            //    ApiCredentials = new CryptoExchange.Net.Authentication.ApiCredentials(
            //        "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx", 
            //        "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"),
            //    LogVerbosity = CryptoExchange.Net.Logging.LogVerbosity.Debug,
            //    LogWriters = { Console.Out }
            //});


            //BinanceClient.SetDefaultOptions(new BinanceClientOptions()
            //{
            //    ApiCredentials = new CryptoExchange.Net.Authentication.ApiCredentials(
            //        "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx", 
            //        "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"),
            //    LogVerbosity = CryptoExchange.Net.Logging.LogVerbosity.Debug,
            //    LogWriters = { Console.Out }
            //});

        }

        public object SubscribeBinance(string instrument, string field, int depth)
        {
            switch (field)
            {
                case RtdFields.PRICE:
                case RtdFields.SYMBOL:
                    return GetPrice(instrument, field);

                case RtdFields.OPEN:
                case RtdFields.OPEN_TIME:
                case RtdFields.CLOSE:
                case RtdFields.CLOSE_TIME:
                    return GetPrice(instrument, field);

                case RtdFields.ASK_DEPTH:
                case RtdFields.ASK_DEPTH_SIZE:
                case RtdFields.BID_DEPTH:
                case RtdFields.BID_DEPTH_SIZE:
                    return SubscribeBinanceDepth(instrument, field, depth);

                default:
                    return SubscribeBinanceSymbolTicker(instrument, field);
            }
        }

        private object CacheResult(string origin, string instrument, string field, object value)
        {
            lock (_subMgr)
            {
                _subMgr.Set(SubscriptionManager.FormatPath(BINANCE, string.Empty, instrument, field), value.ToString());
            }

            return value;
        }
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
        private object Get24HPrice(string instrument, string field)
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
                        case RtdFields.PRICE_PCT: return CacheResult(BINANCE, instrument, field, result.Data.PriceChangePercent / 100);
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

        private Dictionary<String, BinanceSocketClient> SocketTickerCache = new Dictionary<string, BinanceSocketClient>();
        private Dictionary<String, BinanceSocketClient> SocketDepthCache = new Dictionary<string, BinanceSocketClient>();
        private Dictionary<BinanceSocketClient, BinanceStreamTick> TickerCache = new Dictionary<BinanceSocketClient, BinanceStreamTick>();
        private Dictionary<BinanceSocketClient, BinanceStreamDepth> DepthCache = new Dictionary<BinanceSocketClient, BinanceStreamDepth>();

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

        private object SubscribeBinanceSymbolTicker(string instrument, string field)
        {
            var key = instrument;// + "|" + field;

            BinanceSocketClient client;
            if (SocketTickerCache.TryGetValue(key, out client))
            {
                BinanceStreamTick tick;
                if (TickerCache.TryGetValue(client, out tick))
                    return DecodeTick(tick, field);
                else
                    return SubscriptionManager.UninitializedValue;
            }
            else
            {
                client = new BinanceSocketClient();
                SocketTickerCache[key] = client;

                var successSymbol = client.SubscribeToSymbolTicker(instrument, (BinanceStreamTick data) =>
                {
                    TickerCache[client] = data;
                    CacheTick(data);
                });
                return SubscriptionManager.UninitializedValue;
            }
        }

        private object CacheResult(string origin, string instrument, string field, int depth, object value)
        {
            lock (_subMgr)
            {
                _subMgr.Set(SubscriptionManager.FormatPath(BINANCE, string.Empty, instrument, field, depth), value);
            }

            return value;
        }

        private void CacheDepth(BinanceStreamDepth stream, int depth)
        {
            var instrument = stream.Symbol;
            var bidCount = stream.Bids.Count;
            var askCount = stream.Asks.Count;


            CacheResult(BINANCE, instrument, RtdFields.BID_DEPTH, depth, depth < bidCount ? stream.Bids[depth].Price.ToString() : SubscriptionManager.UninitializedValue);
            CacheResult(BINANCE, instrument, RtdFields.BID_DEPTH_SIZE, depth, depth < bidCount ? stream.Bids[depth].Quantity.ToString() : SubscriptionManager.UninitializedValue);
            CacheResult(BINANCE, instrument, RtdFields.ASK_DEPTH, depth, depth < askCount ? stream.Asks[depth].Price.ToString() : SubscriptionManager.UninitializedValue);
            CacheResult(BINANCE, instrument, RtdFields.ASK_DEPTH_SIZE, depth, depth < askCount ? stream.Asks[depth].Quantity.ToString() : SubscriptionManager.UninitializedValue);
            _subMgr.MakeDepthDirty();
        }

        private object DecodeDepth(BinanceStreamDepth stream, string field, int depth)
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

        private object SubscribeBinanceDepth(string instrument, string field, int depth)
        {
            var key = instrument;// + "|" + field;

            BinanceSocketClient client;
            if (SocketDepthCache.TryGetValue(key, out client))
            {
                BinanceStreamDepth stream;
                if (DepthCache.TryGetValue(client, out stream))
                    return DecodeDepth(stream, field, depth);
                else
                    return SubscriptionManager.UninitializedValue;
            }
            else
            {
                client = new BinanceSocketClient();
                SocketDepthCache[key] = client;

                var successSymbol = client.SubscribeToDepthStream(instrument, (BinanceStreamDepth stream) =>
                {
                    DepthCache[client] = stream;
                    CacheDepth(stream, depth);
                });
                return SubscriptionManager.UninitializedValue;
            }
        }
    }
}
