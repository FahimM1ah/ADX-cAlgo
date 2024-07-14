# ADXAlgo

## Overview
The ADX Algo is a custom trading algorithm for cTrader designed to leverage the Average Directional Movement Index Rating (ADXR) and the Exponential Moving Average (EMA) for making trading decisions. This bot also includes a pyramiding feature to manage positions dynamically based on predefined profit and loss thresholds. The purpose of this algorithm was to automate a client's manual strategy.

## Features
- **ADX and EMA Indicators**: Utilizes ADXR to determine the trend strength and EMA for trend direction.
- **Customizable Parameters**: Parameters for volume, stop loss, take profit, ADXR levels, EMA period, and trading session times.
- **Pyramiding**: Optional feature to add to positions based on the performance of existing trades.
- **Profit and Loss Management**: Closes all positions if a defined profit or loss target is reached.

## Parameters
### General Settings
- **Volume (Lots)**: The volume of each trade in lots.
- **Max Lot Size**: Maximum allowable lot size for trades.
- **Stop Loss (Pips)**: Stop loss in pips.
- **Take Profit (Pips)**: Take profit in pips.
- **Label**: Label to identify the positions opened by this bot.

### ADXR Settings
- **Periods**: Number of periods for the ADXR calculation.
- **ADXR Level**: Threshold value for ADXR to determine trend strength.

### EMA Settings
- **EMA Period**: Number of periods for the EMA calculation.
- **Source**: The data series to calculate the EMA.

### Trading Times
- **Session Start Time**: Start time for trading sessions.
- **Session End Time**: End time for trading sessions.

### Pyramiding Settings
- **Use Pyramiding**: Enable or disable the pyramiding feature.
- **Pyramid Step**: Number of pips profit before adding to a position.
- **Pyramid Multiplier**: Multiplier for the additional volume when pyramiding.
- **Pyramid Profit Target**: Profit target for closing all positions.
- **Pyramid Loss Limit**: Loss limit for closing all positions.

## Logic
### Initialization
- **OnStart()**: Initializes the ADXR and EMA indicators with the specified parameters.

### Trading Session
- **OnTick()**: Monitors the trading session and manages pyramiding if enabled.
- **OnBar()**: Executes buy or sell entry logic based on the ADXR and EMA indicators during the defined trading times.

### Entry Logic
- **BuyEntryLogic()**: Executes a buy order if ADXR is bullish, EMA indicates an uptrend, and the current position count is below the maximum allowed.
- **SellEntryLogic()**: Executes a sell order if ADXR is bearish, EMA indicates a downtrend, and the current position count is below the maximum allowed.

### Pyramiding Management
- **ManagePyramiding()**: Adds to positions if the profit threshold is met and the additional volume is below the maximum lot size.
- **ProfitMonitor()**: Closes all positions if the profit target or loss limit is reached.

## Helper Functions
- **IsTimeToTrade()**: Checks if the current time is within the defined trading session.
- **IsBullishCross()**: Checks for a bullish ADXR cross.
- **IsBearishCross()**: Checks for a bearish ADXR cross.
- **IsADXBearish()**: Checks if ADXR is below the threshold level.
- **IsADXBullish()**: Checks if ADXR is above the threshold level.
- **IsBullishEMA()**: Checks if the current price is above the EMA.
- **IsBearishEMA()**: Checks if the current price is below the EMA.

## Usage
1. **Compile and Deploy**: Add the ADXAlgo to your cTrader platform, compile, and deploy it on a chosen market.
2. **Configure Parameters**: Adjust the parameters to match your trading strategy and risk management preferences.
3. **Start Trading**: Run the bot during your defined trading session times and monitor its performance.

## Disclaimer
Trading in financial markets involves risk, and there is a possibility of losing capital. This bot should be used as a tool to assist in trading decisions and not as a guaranteed method for profit. Users are responsible for configuring and testing the bot to ensure it aligns with their trading strategy and risk tolerance. Past performance is not indicative of future results when backwards testing over historical data.
