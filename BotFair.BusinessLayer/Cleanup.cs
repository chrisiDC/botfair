using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using BotFair.DataLayer;
using BotFair.DataLayer.BotDataSetTableAdapters;
using Tools.Aspects;

namespace BotFair.BusinessLayer
{
    public class Cleanup
    {
        private Markets m;
        protected Cache cache;
        private Log log;

        public Cleanup(Markets m,Log log)
        {
            this.m = m;
            this.cache = Cache.Instance;
            this.log = log;
        }

     
        public void Run()
        {
            DateTime lastScanFrom = m.GetLatestActiveMarketScanFrom();

            var q = Cache.Instance.GetMarkets().Where(
                marketItem => marketItem.marketStatus.ToLower()  == "closed" ||
                    marketItem.marketStatus.ToLower() == "suspended" &&
                    marketItem.timeScanned < lastScanFrom);

            List<int> marketsToRemove = new List<int>();
            foreach (var market in q)
            {
                Console.Beep();
                Trace.TraceInformation(String.Format("Market {0} is now closed or suspended", market.id));
                marketsToRemove.Add(market.id);
                
                
            }

            foreach (var id in marketsToRemove)
            {
                Remove(id);
            }

            log.Clear();
         
        }

    
        public void Remove(int marketId)
        {
            try
            {
                Trace.TraceInformation("removing market " + marketId + " from cache");
                new PriceTrack().RemoveMarket(marketId);

                var marketsAdapter = new MarketsTableAdapter();

                var selectionsAdapter = new SelectionsTableAdapter();


                var marketQ = cache.GetMarkets().Where(markets => markets.id == marketId).ToArray();

                if (marketQ.Any())
                {
                    var market = marketQ.First();
                    if (market.isHot) Trace.TraceInformation("Removing hot market; id" + market.id);
                    market.isHot = false;

                    var betQ = cache.GetBets().Where(bets => bets.fk_market == market.id).ToArray();

                    foreach (var item in betQ)
                    {
                        cache.GetBets().Rows.Remove(item);

                    }

                    var selectionQ = cache.GetSelections().Where(selection => selection.marketId == market.id).ToArray();

                    marketsAdapter.Update(cache.GetMarkets());

                    //note: the try catch is just a bugfix
                  
                        cache.GetMarkets().Rows.Remove(market);
                   


                    foreach (var item in selectionQ)
                    {
                        item.tracked = false;

                    }

                    selectionsAdapter.Update(cache.GetSelections());



                    foreach (var item in selectionQ)
                    {
                        cache.GetSelections().Rows.Remove(item);



                    }

                    new PriceTrack().RemoveMarket(marketId);

                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("", ex);
            }
            finally
            {
                //Sync.syncMarkets.ReleaseCommit();
                //Sync.syncBets.ReleaseCommit();
                //Sync.syncSelections.ReleaseCommit();
            }


        }

    }
}
