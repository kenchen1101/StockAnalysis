﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingStrategy.Strategy
{
    public abstract class MetricBasedTradingStrategyComponentBase<T> 
        : GeneralTradingStrategyComponentBase
        where T : IRuntimeMetric
    {
        protected RuntimeMetricManager<T> MetricManager { get; private set; }

        public abstract Func<T> Creator { get; }

        public override void Initialize(
            IEvaluationContext context, 
            IDictionary<ParameterAttribute, object> parameterValues)
        {
            base.Initialize(context, parameterValues);

            if (Creator == null)
            {
                throw new InvalidProgramException("Creator property must not be null");
            }

            MetricManager = new RuntimeMetricManager<T>(Creator);
        }

        public override void WarmUp(ITradingObject tradingObject, StockAnalysis.Share.Bar bar)
        {
            MetricManager.Update(tradingObject, bar);
        }

        public override void Evaluate(ITradingObject tradingObject, StockAnalysis.Share.Bar bar)
        {
            MetricManager.Update(tradingObject, bar);
        }

        public override void Finish()
        {
            MetricManager = null;
        }
    }
}