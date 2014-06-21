using System;
using System.Diagnostics;
using System.Linq;
using BotFair.Sys;

namespace BotFair.Services
{
    public class AppCache : IAppCache
    {
        private static object cacheLock = new object();
        private static volatile AppCache instance;

        private BotDataSet data;

        public static AppCache Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (cacheLock)
                    {
                        if (instance == null) instance = new AppCache();
                    }
                }

                return instance;
            }
        }

        public BotDataSet Content
        {
            get { return data; }
        }

        private int configurationId;

        private static string ConnectionString
        {
            get { return System.Configuration.ConfigurationManager.ConnectionStrings["botfair"].ConnectionString; }
        }

        //public int ActiveConfiguration { get; set; }


        private AppCache()
        {
            data = new BotDataSet();
        }

        public void RemoveMarket(BotDataSet.MarketsRow market)
        {
            try
            {
                var marketsAdapter = new DataLayerTableAdapters.MarketsTableAdapter();
                marketsAdapter.Connection.ConnectionString = ConnectionString;


                var selectionsAdapter = new DataLayerTableAdapters.SelectionsTableAdapter();
                selectionsAdapter.Connection.ConnectionString = ConnectionString;

                Sync.syncMarkets.WaitCommit();
                Sync.syncBets.WaitCommit();
                Sync.syncSelections.WaitCommit();


                var changes = data.Markets.GetChanges();
                if (changes != null && changes.Rows.Count > 0)
                {
                    var adapter = new DataLayerTableAdapters.MarketsTableAdapter();
                    adapter.Connection.ConnectionString = ConnectionString;


                    adapter.Update(data.Markets);
                }
                market.isHot = false;

                var betQ =
                    from bets in data.Bets.ToArray()
                    where
                        bets.fk_market == market.id
                    select bets;

                foreach (var item in betQ)
                {
                    data.Bets.Rows.Remove(item);
                }

                var selectionQ =
                    from selection in data.Selections.ToArray()
                    where
                        selection.marketId == market.id
                    select selection;

                foreach (var item in selectionQ)
                {
                    item.tracked = false;
                }


                marketsAdapter.Update(data.Markets);
                selectionsAdapter.Update(data.Selections);

                data.Markets.Rows.Remove(market);

                foreach (var item in selectionQ)
                {
                    data.Selections.Rows.Remove(item);
                }

                var priceTrackQ =
                    from priceTrack in data.PriceTrack.ToArray()
                    where
                        priceTrack.fk_market == market.id
                    select priceTrack;

                foreach (var item in priceTrackQ)
                {
                    data.PriceTrack.Rows.Remove(item);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("", ex);
            }
            finally
            {
                Sync.syncMarkets.ReleaseCommit();
                Sync.syncBets.ReleaseCommit();
                Sync.syncSelections.ReleaseCommit();
            }
        }

        public void CommitMarkets()
        {
            try
            {
                Sync.syncMarkets.WaitCommit();


                var changes = data.Markets.GetChanges();
                if (changes != null && changes.Rows.Count > 0)
                {
                    var adapter = new DataLayerTableAdapters.MarketsTableAdapter();
                    adapter.Connection.ConnectionString = ConnectionString;


                    adapter.Update(data.Markets);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Failure at Db Update: Changes count: " + ex);
            }
            finally
            {
                Sync.syncMarkets.ReleaseCommit();
            }
        }

        public void CommitSelections()
        {
            try
            {
                Sync.syncSelections.WaitCommit();

                var changes = data.Selections.GetChanges();
                if (changes != null && changes.Rows.Count > 0)
                {
                    var adapter = new DataLayerTableAdapters.SelectionsTableAdapter();
                    adapter.Connection.ConnectionString = ConnectionString;

                    adapter.Update(data.Selections);

                    //data.Selections.AcceptChanges();
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                Sync.syncSelections.ReleaseCommit();
            }
        }

        public void CommitBets()
        {
            try
            {
                Sync.syncBets.WaitCommit();
                var changes = data.Bets.GetChanges();

                if (changes != null && changes.Rows.Count > 0)
                {
                    var adapter = new DataLayerTableAdapters.BetsTableAdapter();
                    adapter.Connection.ConnectionString = ConnectionString;

                    adapter.Update(data.Bets);

                    //data.Bets.AcceptChanges();
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                Sync.syncBets.ReleaseCommit();
            }
        }

        public void CommitPriceTrack()
        {
            try
            {
                //Sync.syncMarkets.WaitCommit();


                var changes = data.PriceTrack.GetChanges();
                if (changes != null && changes.Rows.Count > 0)
                {
                    var adapter = new DataLayerTableAdapters.PriceTrackTableAdapter();
                    adapter.Connection.ConnectionString = ConnectionString;


                    adapter.Update(data.PriceTrack);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Failure at Db Update: Changes count: " + ex);
            }
            finally
            {
                //Sync.syncMarkets.ReleaseCommit();
            }
        }

        public BotDataSet.ConfigurationRow GetActiveConfiguration()
        {
            if (configurationId == 0) return null;

            BotDataSet.ConfigurationRow config = null;

            lock (Sync.configSync)
            {
                var stateQuery =
                    from state in data.Configuration
                    where
                        state.id == configurationId
                    select state;
                if (stateQuery.Count() != 0) config = stateQuery.First();
            }

            return config;
        }

        public bool ChangeConfiguration(int configurationId)
        {
            lock (Sync.configSync)
            {
                var dbAdapter2 = new DataLayerTableAdapters.ConfigurationTableAdapter();
                dbAdapter2.Connection.ConnectionString = ConnectionString;

                dbAdapter2.Fill(data.Configuration);


                this.configurationId = configurationId;
            }


            return true;
        }

        public void Load()
        {
            //todo remove

            ////TODO: Fill CacheLayer only with data that is not deprecated!
            //var dbAdapter2 = new DataLayerTableAdapters.MarketsTableAdapter();
            //dbAdapter2.Connection.ConnectionString = ConnectionString;

            ////dbAdapter2.Fill(data.Markets);

            //var dbAdapter3 = new DataLayerTableAdapters.SelectionsTableAdapter();
            //dbAdapter3.Connection.ConnectionString = ConnectionString;

            ////dbAdapter3.Fill(data.Selections);


            //var dbAdapter5 = new DataLayerTableAdapters.BetsTableAdapter();

            //dbAdapter5.Connection.ConnectionString = ConnectionString;

            ////dbAdapter5.Fill(data.Bets);

            //var dbAdapter6 = new DataLayerTableAdapters.ConfigurationTableAdapter();
            //dbAdapter6.Connection.ConnectionString = ConnectionString;

            //dbAdapter6.Fill(data.Configuration);

            //var dbAdapter7 = new DataLayerTableAdapters.PriceTrackTableAdapter();
            //dbAdapter7.Connection.ConnectionString = ConnectionString;

            ////dbAdapter7.Fill(data.PriceTrack);
        }
    }
}