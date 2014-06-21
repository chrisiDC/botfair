using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using BotFair.DataLayer;
using BotFair.DataLayer.BotDataSetTableAdapters;


namespace BotFair.BusinessLayer
{
    public class Configuration : EntityBase
    {
        private int configurationId;
        private BotDataSet.ConfigurationRow configRow;


        public Configuration()
            : base()
        {
            configRow = GetActiveConfiguration();
        }


        public int ActiveRiskValue { get { return configRow.hotMarketsSeconds; } }
        public double ActivePercentage { get { return configRow.percentage; } }
        public int ActiveHotMarketsSeconds { get { return configRow.hotMarketsSeconds; } }
        public int ActiveNewMarketsPeriod { get { return configRow.newMarketsPeriod; } }

        public int RiskValue { get; set; }
        public double Percentage { get; set; }
        public int HotMarketsSeconds { get; set; }
        public int NewMarketsPeriod { get; set; }


        public bool IsConfigured { get { return configurationId > 0; } }
      
        private BotDataSet.ConfigurationRow GetActiveConfiguration()
        {
            if (configurationId == 0) return null;

            BotDataSet.ConfigurationRow config = null;

            //lock (Sync.configSync)
            //{
                var stateQuery = cache.GetConfiguration().Where(state => state.id == configurationId);

                if (stateQuery.Count() != 0) config = stateQuery.First();
            //}

            return config;
        }

        public int GetConfigId()
        {
            return GetActiveConfiguration().id;
        }

        public bool ChangeConfiguration(int configurationId)
        {
            //lock (Sync.configSync)
            //{
                var dbAdapter2 = new ConfigurationTableAdapter();
                //dbAdapter2.Connection.ConnectionString = ConnectionString;

                dbAdapter2.Fill(cache.GetConfiguration());


                this.configurationId = configurationId;
                configRow = GetActiveConfiguration();
            //}


            return true;
        }

        public Configuration GetConfiguration(int id)
        {

            var q = cache.GetConfiguration().First(state => state.id == id);

            Configuration cfg = new Configuration
                                    {
                                        RiskValue = q.riskValue,
                                        Percentage = q.percentage,
                                        NewMarketsPeriod = q.newMarketsPeriod,
                                        HotMarketsSeconds = q.hotMarketsSeconds
                                    };

            return cfg;

        }


        protected override void Init()
        {
          
        }
    }
}
