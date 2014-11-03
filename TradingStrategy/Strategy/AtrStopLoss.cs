﻿using System;

namespace TradingStrategy.Strategy
{
    public sealed class AtrStopLoss 
        : MetricBasedStopLossBase<GenericRuntimeMetric>
    {
        [Parameter(10, "ATR计算窗口大小")]
        public int AtrWindowSize { get; set; }

        [Parameter(3.0, "ATR停价倍数")]
        public double AtrStopLossFactor { get; set; }


        public override string Name
        {
            get { return "ATR停价"; }
        }

        public override string Description
        {
            get { return "当价格低于买入价，并且差值大于ATR乘以Atr停价倍数时停价"; }
        }

        protected override Func<GenericRuntimeMetric> Creator
        {
            get 
            {
                return (() => new GenericRuntimeMetric(string.Format("ATR[{0}]", AtrWindowSize)));
            }
        }

        protected override void ValidateParameterValues()
        {
            base.ValidateParameterValues();

            if (AtrWindowSize <= 1 || AtrStopLossFactor <= 0.0)
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        public override double EstimateStopLossGap(ITradingObject tradingObject, double assumedPrice, out string comments)
        {
            var metric = MetricManager.GetOrCreateRuntimeMetric(tradingObject);

            var atr = metric.LatestData[0][0];
            var stoplossGap = -atr * AtrStopLossFactor;
            comments = string.Format(
                "stoplossgap({2:0.000}) = ATR({0:0.000}) * AtrStopLossFactor({1:0.000})",
                atr,
                AtrStopLossFactor,
                stoplossGap);

            return stoplossGap;
        }
    }
}
