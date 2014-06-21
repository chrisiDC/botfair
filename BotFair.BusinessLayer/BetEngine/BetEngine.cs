using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using BotFair.DataLayer;
using BotFair.DataLayer.BetFairExchange;
using Tools.Aspects;

namespace BotFair.BusinessLayer.BetEngine
{
    public class BetEngine
    {
        private static List<IBetMaster> betMasters = new List<IBetMaster>();
        private static Dictionary<IBetMaster, BetMasterInfo> masterInfo = new Dictionary<IBetMaster, BetMasterInfo>();

     

        public void Run()
        {
        
            var defaultConfig =
                new Configuration().GetConfiguration(
                    int.Parse(System.Configuration.ConfigurationManager.AppSettings["configId"]));

        
             
                    foreach (var betMasterItem in betMasters.ToList())
                    {
                        int marketId = betMasterItem.GetMarketId();

                        var interval = betMasterItem.GetInterval();
                        var info = masterInfo[betMasterItem];
                        var now = DateTime.Now;

                        if (info.LastRun < now.AddSeconds(interval))
                        {
                            info.LastRun = now;
                            var marketInfo = GetMarket(marketId);
                            if (marketInfo != null)
                            {
                                var marketPrices = GetPriceTrack(marketId);
                                var existingBets = GetBets(marketId);
                                var selections = GetSelections(marketId);

                                var bets = betMasterItem.Run(defaultConfig, marketInfo, selections, marketPrices,
                                                             existingBets);
                                if (bets.Count > 0)
                                {
                                    foreach (var bet in bets)
                                    {
                                        var dbBet = Cache.Instance.GetBets().NewBetsRow();


                                        dbBet.fk_market = marketId;
                                        dbBet.fk_selection = bet.selectionId;
                                        dbBet.isLay = bet.betType == BetTypeEnum.L;
                                        dbBet.pricePosted = bet.price;
                                        dbBet.sizePosted = (double) bet.size;
                                        dbBet.datePosted = DateTime.Now;
                                        dbBet.masterId = betMasterItem.GetMasterId();
                                        Cache.Instance.GetBets().AddBetsRow(dbBet);

                                        Cache.Instance.CommitBets();

                                        Trace.TraceInformation("new bet: type=" + bet.betType);
                                    }
                                }
                            }
                        }

                    
                }

           


        }

       
        private static BotDataSet.MarketsRow GetMarket(int marketId)
        {
            var marketInfo = Cache.Instance.GetMarkets().FirstOrDefault(item => item.id == marketId);

            return marketInfo;


        }

  
        private static IEnumerable<BotDataSet.PriceTrackRow> GetPriceTrack(int marketId)
        {
            var o = Cache.Instance.GetPriceTrack().Where(item => item.fk_market == marketId);

            return o.ToArray();
        }

      
        private static IEnumerable<BotDataSet.BetsRow> GetBets(int marketId)
        {
            var o = Cache.Instance.GetBets().Where(item => item.fk_market == marketId);

            return o.ToArray();
        }

      
        private static IEnumerable<BotDataSet.SelectionsRow> GetSelections(int marketId)
        {
            var o = Cache.Instance.GetSelections().Where(item => item.marketId == marketId);

            return o.ToArray();
        }

        public static void RegisterBetMaster(int marketId, IBetMaster master)
        {
            betMasters.Add(master);
            var info = new BetMasterInfo { LastRun = DateTime.MinValue };
            masterInfo.Add(master, info);

        }

        public static void UnRegisterBetMaster(int marketId, IBetMaster master)
        {
            betMasters.Remove(master);
            masterInfo.Remove(master);

        }
    }
}
