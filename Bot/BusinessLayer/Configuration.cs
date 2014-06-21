//using System;
//using System.Collections.Generic;
//using System.Data;
//using System.Linq;
//using System.Text;
//using BotFair.BetFairExchange;
//using BotFair.BetFairGlobal;
//using BotFair.Services;

//namespace BotFair.BusinessLayer
//{
//    public class Configuration:BusinessObject
//    {
//         public Configuration()
//            : base(null, new BFGlobalServiceClient(), new BFExchangeServiceClient())
//        {
//        }

//         public Configuration(DataTable table, BFGlobalServiceClient globalService, BFExchangeServiceClient exchangeService)
//            : base(table, globalService, exchangeService)
//        {
//        }


//        public int GetHotMarketsSeconds()
//        {
//            //BotDataSet.ConfigurationRow configRow = cache.GetActiveConfiguration();
//            //if (configRow == null)
//            //{
//            //    Trace.TraceWarning("no bot configuration defined!");
//            //    return;
//            //}

//            //int hotMarketsSeconds = configRow.hotMarketsSeconds;
//            //int riskValue = configRow.riskValue;
//            //double percentage = configRow.percentage / 100;

//            return 0;

//        }
//    }
//}
