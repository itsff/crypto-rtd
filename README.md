# crypto-rtd
Excel RTD server sourcing GDAX and BINANCE ticker data

The code is using the old .NET Framework 4.0 in order to allow it
to run on Windows 7 machines.

## Installation
1. Clone the repository and go to its folder.
2. Compile the code using Visual Studio, MSBuild or via this handy script file:

   `build.cmd`

3. Register the COM server by running the following script in admin command prompt:
   
   `register.cmd`

## Usage

Once the RTD server has been installed, you can use it from Excel via the RTD macro.
This is the syntax:

* `=RTD("crypto",,"GDAX", instrument, field)`
* `=RTD("crypto",,"BINANCE", instrument, field)`
* `=RTD("crypto",,"BINANCE_DEPTH",instrument, field,depth)` // depth is 0-9
* `=RTD("crypto",,"BINANCE_TRADE",instrument, field)`
* `=RTD("crypto",,"BINANCE_CANDLE",instrument, interval, field)`   // interval is 0-11
* `=RTD("crypto",,"BINANCE_HISTORY",instrument)`  // not yet working

*All* currency pairs traded on GDAX are supported, including the main ones:
* BTC-USD
* ETH-USD
* LTC-USD

*All* currency pairs traded on BINANCE are supported, including the main ones:
* BTCUSDT
* ETHUSDT
* LTCUSDT

You can use the following fields for GDAX:
* BID, ASK, LAST_SIZE, LAST_PRICE, LAST_SIDE
* high_24h, low_24h, open_24h, volume_24h

You can use the following fields for BINANCE:
* BINANCE: SYMBOL	LOW	HIGH	CLOSE	OPEN	BID_SIZE	BID	SPREAD	ASK	ASK_SIZE	VOL	QUOTE_VOL	TRADES	PRICE%	PRICE_CHANGE
* BINANCE_24H: CLOSE	OPEN +++ TODO
* BINACE_TRADE: SYMBOL	TRADE_ID	PRICE	QUANTITY	BUYER_IS_MAKER	IGNORE	FIRST_ID	LAST_ID	TRADE_TIME
* BINANCE_DEPTH: BID_DEPTH_SIZE	BID_DEPTH
* BINANCE_CANDLE: OPEN	HIGH	LOW	CLOSE	OPEN_TIME	CLOSE_TIME	FINAL	QUOTE_VOL	VOL	TAKE_BUY_VOL	TAKE_BUY_QUOTE_VOL	INTERVAL	TRADES	Event	Event_Time	FIRST_ID	LAST_ID
* BINANCE_HISTORY: PRICE    QUANTITY

Environment Variables: for BINANCE_HISTORY and coming APIs
* BINANCE_API_KEY=XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
* BINANCE_SECRET=xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx


![Excel screenshot](doc/crypto-rtd-excel.png)

