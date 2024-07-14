using cAlgo.API;
using cAlgo.API.Indicators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None, AddIndicators = true)]
    public class ADXAlgo : Robot
    {
        private double _volumeInUnits;

        private AverageDirectionalMovementIndexRating _adxr;

        private ExponentialMovingAverage _ema;

        [Parameter("Volume (Lots)", DefaultValue = 0.1)]
        public double VolumeInLots { get; set; }

        [Parameter("Max Lot Size", DefaultValue = 100)]
        public double MaxLot { get; set; }

        [Parameter("Stop Loss (Pips)", DefaultValue = 10, MaxValue = 1000, MinValue = 1, Step = 1)]
        public double StopLossInPips { get; set; }

        [Parameter("Take Profit (Pips)", DefaultValue = 10, MaxValue = 1000, MinValue = 1, Step = 1)]
        public double TakeProfitInPips { get; set; }

        [Parameter("Label", DefaultValue = "ADXAlgo")]
        public string Label { get; set; }

        [Parameter("Periods", DefaultValue = 14, Group = "Average Directional Movement Index Rating")]
        public int Periods { get; set; }
        
        [Parameter("ADXR Level", DefaultValue = 25, Group = "Average Directional Movement Index Rating")]
        public int ADXRLevel { get; set; }

        [Parameter("EMA Period", DefaultValue = 50, Group = "Exponential Moving Average")]
        public int EMAPeriod { get; set; }

        [Parameter("Source", Group = "Exponential Moving Average")]
        public DataSeries Source { get; set; }

        [Parameter("Session Start Time", DefaultValue = "13:30", Group = "Trading Times")]
        public string StartTime { get; set; }

        [Parameter("Session End Timings", DefaultValue = "15:30", Group = "Trading Times")]
        public string EndTime { get; set; }

        [Parameter("Use Pyramiding", DefaultValue = false, Group = "Pyramiding")]
        public bool UsePyramid { get; set; }

        [Parameter("Pyramid Step", DefaultValue = 10, Group = "Pyramiding")]
        public double PyramidPips { get; set; }

        [Parameter("Pyramid Multiplier", DefaultValue = 1, Group = "Pyramiding")]
        public double PyramidMultiplier { get; set; }

        [Parameter("Pyramid Profit Target", DefaultValue = 100, Group = "Pyramiding")]
        public double PyramidProfitTarget { get; set; }

        [Parameter("Pyramid Loss Limit", DefaultValue = 100, Group = "Pyramiding")]
        public double PyramidLossLimit { get; set; }

        private int maxPositions = 1;
        private double _maxLot;
        private State state;
        private List<Position> pyramidPositions = new();
        private bool targetHit = false;

        public enum State
        {
            Bullish,
            Bearish
        };

        public Position[] BotPositions
        {
            get
            {
                return Positions.FindAll(Label);
            }
        }

        protected override void OnStart()
        {
            _volumeInUnits = Symbol.QuantityToVolumeInUnits(VolumeInLots);
            _maxLot = Symbol.QuantityToVolumeInUnits(MaxLot);

            _adxr = Indicators.AverageDirectionalMovementIndexRating(Periods);

            _ema = Indicators.ExponentialMovingAverage(Source, EMAPeriod);
        }

        protected override void OnTick()
        {
            if (UsePyramid)
            {
                ProfitMonitor();
                if (IsTimeToTrade())
                    ManagePyramiding();
            }

        }

        protected override void OnBar()
        {
            if (IsTimeToTrade()) 
            { 
                BuyEntryLogic(); 
                SellEntryLogic();
            }
            else if (Server.Time.Hour < 1)
            {
                targetHit = false;
            }


        }

        private void ClosePositions(TradeType tradeType)
        {
            foreach (var position in BotPositions)
            {
                if (position.TradeType != tradeType) continue;

                ClosePosition(position);
            }
        }

        private bool IsBullishCross() => _adxr.DIPlus.Last(0) > _adxr.DIMinus.Last(0);

        private bool IsBearishCross() => _adxr.DIPlus.Last(0) < _adxr.DIMinus.Last(0);

        private bool IsADXBearish() => _adxr.ADXR.Last(0) < ADXRLevel;

        private bool IsADXBullish() => _adxr.ADXR.Last(0) > ADXRLevel;

        private bool IsBullishEMA() => Bars.ClosePrices.LastValue > _ema.Result.Last(0);

        private bool IsBearishEMA() => Bars.ClosePrices.LastValue < _ema.Result.Last(0);

        private TimeOnly ConvertToTime(string time)
        {
            var timeSplit = time.Split(':');
            int hour = int.Parse(timeSplit[0]);
            int minute = int.Parse(timeSplit[1]);

            return new TimeOnly(hour, minute);
        }

        private bool IsTimeToTrade()
        {
            var startTime = ConvertToTime(StartTime);
            var endTime = ConvertToTime(EndTime);

            var startDateTime = new DateTime(Server.Time.Year, Server.Time.Month, Server.Time.Day, startTime.Hour, startTime.Minute, startTime.Second);
            var endDateTime = new DateTime(Server.Time.Year, Server.Time.Month, Server.Time.Day, endTime.Hour, endTime.Minute, endTime.Second);

            if (Server.Time >= startDateTime
                && Server.Time <= endDateTime)
            { return true; }
            return false;   
        }

        private void BuyEntryLogic()
        {
            if (IsBullishCross() 
                && IsADXBullish() 
                && IsBullishEMA() 
                && Positions.Count < maxPositions
                && !targetHit)
            {
                if (state != State.Bullish)
                {
                    ExecuteMarketOrder(TradeType.Buy, SymbolName, _volumeInUnits, Label, StopLossInPips, TakeProfitInPips);
                    state = State.Bullish;
                    pyramidPositions.Add(Positions.Last()); 
                }
            }
        }


        private void SellEntryLogic()
        {
            if (IsBearishCross() 
                && IsADXBearish() 
                && IsBearishEMA() 
                && Positions.Count < maxPositions 
                && !targetHit)
            {
                if (state != State.Bearish)
                {
                    ExecuteMarketOrder(TradeType.Sell, SymbolName, _volumeInUnits, Label, StopLossInPips, TakeProfitInPips);

                    state = State.Bearish;
                    pyramidPositions.Add(Positions.Last());
                }

            }
        }


        private void ManagePyramiding()
        {
            foreach (var position in pyramidPositions)
            {
                if (Positions.Contains(position))
                {
                    var pipsProfit = position.Pips;

                    // If the profit threshold is met, add to the position
                    if (pipsProfit >= PyramidPips)
                    {
                        double additionalVolume = Math.Round(position.Quantity * PyramidMultiplier, 1); 
                        if (additionalVolume < _maxLot)
                        {
                            var stoploss = (Bars.ClosePrices.LastValue - position.StopLoss) / Symbol.PipValue;

                            if (position.TradeType == TradeType.Buy)
                                ExecuteMarketOrder(TradeType.Buy, Symbol.Name, Symbol.QuantityToVolumeInUnits(additionalVolume), $"{position.Label}");
                            else
                                ExecuteMarketOrder(TradeType.Sell, Symbol.Name, Symbol.QuantityToVolumeInUnits(additionalVolume), $"{position.Label}");
                            if (pyramidPositions.Contains(position))
                            {
                                pyramidPositions.Remove(position);
                                pyramidPositions.Add(Positions.Last());
                            }
                            break;
                        }
                    }
                }
            }
        }

        private void ProfitMonitor()
        {
            if (Positions.Where(p => p.Label == Label).Sum(p => p.NetProfit) >= PyramidProfitTarget)
            {
                foreach (var position in Positions.Where(p => p.Label == Label && p.SymbolName == SymbolName))
                {
                    position.Close();
                }
                targetHit = true;
                Print("Take Profit Triggered");
            }
            if (Positions.Where(p => p.Label == Label).Sum(p => p.NetProfit) <= -PyramidLossLimit)
            {
                foreach (var position in Positions.Where(p => p.Label == Label && p.SymbolName == SymbolName))
                {
                    position.Close();
                }
                targetHit = true;
                Print("Loss Limit Triggered");
            }
        }
    }
}