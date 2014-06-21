using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using BotFair.DataLayer.BotDataSetTableAdapters;

namespace BotFair.DataLayer
{
    public class Cache
    {
        private static object cacheLock = new object();
        private static volatile Cache instance;

        private BotDataSet data;

        public List<int> TrackedSelections { get; set; }



        public static Cache Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (cacheLock)
                    {
                        if (instance == null) instance = new Cache();
                    }
                }

                return instance;
            }
        }


        //private BotDataSet Content
        //{
        //    get { return data; }
        //}

        private Cache()
        {
            Clear();
        }

        public void LoadActiveMarkets()
        {

            SqlDataAdapter adapter = new SqlDataAdapter();
            adapter.TableMappings.Add("Table", "markets");
            adapter.TableMappings.Add("Table1", "bets");
            adapter.TableMappings.Add("Table2", "selections");
            adapter.TableMappings.Add("Table3", "pricetrack");

            var command = new SqlCommand()
                              {
                                  CommandType = CommandType.StoredProcedure,
                                  CommandText = "GetDataCache",
                                  Connection = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["BotFair.DataLayer.Properties.Settings.botfairConnectionString"].ConnectionString)
                              };

            adapter.SelectCommand = command;

            adapter.Fill(data);
            Trace.TraceInformation(String.Format("Loaded {0} markets into cache", data.Markets.Rows.Count.ToString()));
            Trace.TraceInformation(String.Format("Loaded {0} bets into cache", data.Bets.Rows.Count.ToString()));
            Trace.TraceInformation(String.Format("Loaded {0} selections into cache", data.Selections.Rows.Count.ToString()));
            Trace.TraceInformation(String.Format("Loaded {0} pricetracks into cache", data.PriceTrack.Rows.Count.ToString()));

        }

        public BotDataSet.MarketsDataTable GetMarkets()
        {

            return data.Markets;
        }

        public BotDataSet.LogDataTable GetLog()
        {

            return data.Log;
        }

        public void Clear()
        {
            data = new BotDataSet();
            TrackedSelections = new List<int>();

        }

        public void ClearLog()
        {
            //data.Log.Rows.Clear();
        }

        public void Log(int eventId, string type, string msg)
        {

            var row = data.Log.NewLogRow();
            row.eventId = eventId;
            row.type = type;
            row.message = msg;

            row.date = DateTime.Now;

            data.Log.Rows.Add(row);
        }

        public BotDataSet.BetsDataTable GetBets()
        {
            return data.Bets;
        }

        public BotDataSet.BetMastersDataTable GetBetMasters()
        {
            SqlDataAdapter adapter = new SqlDataAdapter();
            var command = new SqlCommand()
            {
                CommandType = CommandType.Text,
                CommandText = "SELECT * FROM BetMasters",
                Connection = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["BotFair.DataLayer.Properties.Settings.botfairConnectionString"].ConnectionString)
            };

            adapter.SelectCommand = command;

            adapter.Fill(data.BetMasters);

            return data.BetMasters;
        }

        public BotDataSet.PriceTrackDataTable GetPriceTrack()
        {
            return data.PriceTrack;
        }

        public BotDataSet.SelectionsDataTable GetSelections()
        {
            return data.Selections;
        }

        public BotDataSet GetAll()
        {

            return data;
        }

        public BotDataSet.ConfigurationDataTable GetConfiguration()
        {
            return data.Configuration;
        }

        public void CommitMarkets()
        {
            try
            {
                //Sync.syncMarkets.WaitCommit();


                var changes = data.Markets.GetChanges();
                if (changes != null && changes.Rows.Count > 0)
                {
                    var adapter = new MarketsTableAdapter();
                    //adapter.Connection.ConnectionString = ConnectionString;
                  
                    int rowsAffected = adapter.Update(data.Markets);
                }
            }
            catch (Exception ex)
            {
                //var x = data.Markets.Where(item => item.RowState != DataRowState.Unchanged);
                //throw ex;
                Trace.TraceWarning("Failure when comitting markets;{0}"+ex+"{0}"+ex.StackTrace,Environment.NewLine);
                if (ex.InnerException != null) Trace.TraceWarning("Failure at Db Update: Changes count: inner= " + ex.InnerException);
            }
            finally
            {
                //Sync.syncMarkets.ReleaseCommit();
            }
        }

        public void CommitSelections()
        {
            try
            {
                //Sync.syncSelections.WaitCommit();

                var changes = data.Selections.GetChanges();
                if (changes != null && changes.Rows.Count > 0)
                {
                    var adapter = new SelectionsTableAdapter();
                    //adapter.Connection.ConnectionString = ConnectionString;

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
                //Sync.syncSelections.ReleaseCommit();
            }
        }

        public void RemoveMarketTrackFromCache(int marketId)
        {
            try
            {
                //Sync.priceTrack.WaitCommit();
                var trackedSelections =
                    Enumerable.Where(GetPriceTrack(), selections => selections.fk_market == marketId).ToArray();

                foreach (var selection in trackedSelections)
                {
                    GetPriceTrack().Rows.Remove(selection);

                }
            }

            finally
            {
                //Sync.priceTrack.ReleaseCommit();
            }

        }

        public void CommitBets()
        {
            try
            {
                //Sync.syncBets.WaitCommit();
                var changes = data.Bets.GetChanges();

                if (changes != null && changes.Rows.Count > 0)
                {
                    var adapter = new BetsTableAdapter();
                    //adapter.Connection.ConnectionString = ConnectionString;

                    adapter.Update(data.Bets);

                    //data.Bets.AcceptChanges();
                }
            }

            finally
            {
                //Sync.syncBets.ReleaseCommit();
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
                    var adapter = new PriceTrackTableAdapter();
                    //adapter.Connection.ConnectionString = ConnectionString;


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

        public void CommitLog()
        {
            try
            {
                //Sync.syncMarkets.WaitCommit();


                var changes = data.Log.GetChanges();
                if (changes != null && changes.Rows.Count > 0)
                {
                    var adapter = new LogTableAdapter();
                    //adapter.Connection.ConnectionString = ConnectionString;

                    int rowsAffected = adapter.Update(data.Log);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Failure at Db Update: Changes count: " + ex);
                if (ex.InnerException != null) Trace.TraceWarning("Failure at Db Update: Changes count: inner= " + ex.InnerException);
            }
            finally
            {
                //Sync.syncMarkets.ReleaseCommit();
            }
        }
    }
}
