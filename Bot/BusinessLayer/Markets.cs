using System;
using System.Diagnostics;
using System.Linq;
using BotFair.BetFairExchange;
using BotFair.BetFairGlobal;
using BotFair.Services;
using BotFair.Sys;

namespace BotFair.BusinessLayer
{
    public class Markets : BusinessObject
    {
        public Markets()
            : base(AppCache.Instance, new BFGlobalServiceClient(), new BFExchangeServiceClient())
        {
        }


        public Markets(IAppCache cache, BFGlobalServiceClient globalService, BFExchangeServiceClient exchangeService)
            : base(cache, globalService, exchangeService)
        {
        }

        /// <summary>
        /// updates currently only ONE market(the oldest one) which is not suspended yet
        /// </summary>
        public void UpdateExistingMarkets()
        {
            var marketQ =
                from market in cache.Content.Markets.ToArray()
                where
                    market.marketStatus.ToLower() != "suspended"
                orderby market.eventDate ascending
                select market;

            if (marketQ.Count() > 0)
            {
                var m = marketQ.First();

                var r = exchangeService.getMarketInfo(new GetMarketInfoReq
                                                          {
                                                              header = exchangeHeader,
                                                              marketId = m.id
                                                          });

                if (r.errorCode == GetMarketErrorEnum.OK)
                {
                    try
                    {
                        Sync.syncMarkets.WaitUpdate();
                        var market = cache.Content.Markets.FindByid(m.id);
                        market.marketStatus = Enum.GetName(typeof (MarketStatusEnum), r.marketLite.marketStatus);
                        if (market.marketStatus.ToLower() == "closed")
                        {
                            cache.RemoveMarket(market);
                        }
                        else if (market.isHot)
                        {
                            market.isHot = r.marketLite.marketStatus == MarketStatusEnum.ACTIVE;
                        }
                    }
                    finally
                    {
                        Sync.syncMarkets.ReleaseUpdate();
                    }
                }
                else
                    Trace.TraceWarning("Problem with service call 'GetMarketInfo':" + r.errorCode + "/" +
                                       r.header.errorCode);
            }
        }

        public void ScanNewMarkets()
        {
            //todo: scanned markets : no need to scan again!

            Trace.TraceInformation("Scanning for new markets");

            const int eventType = (int) Enums.Betfair.EventTypes.HorseRacing;

            int marketsPeriod = cache.GetActiveConfiguration().newMarketsPeriod;

            var markets = exchangeService.getAllMarkets(new BetFairExchange.GetAllMarketsReq
                                                            {
                                                                header = exchangeHeader,
                                                                ////fromDate = DateTime.Now,
                                                                toDate =
                                                                    DateTime.Now.Add(TimeSpan.FromSeconds(marketsPeriod)),
                                                                fromDate = DateTime.Now,
                                                                ////toDate = DateTime.Now,
                                                                ////locale = "de",
                                                                countries = new[] {"GBR", "IRL"},
                                                                eventTypeIds = new int?[] {eventType}
                                                                // 1...soccer, 7...horse race
                                                            });


            if (!String.IsNullOrEmpty(markets.marketData))
            {
                if (markets.errorCode == BetFairExchange.GetAllMarketsErrorEnum.OK)
                {
                    string[] foundMarkets = markets.marketData.Split(new[] {":"}, StringSplitOptions.RemoveEmptyEntries);

                    int i = 1;
                    foreach (string s in foundMarkets)
                    {
                        string[] data = s.Split(new[] {"~"}, StringSplitOptions.RemoveEmptyEntries);

                        int id = Convert.ToInt32(data[0]);
                        var row = cache.Content.Markets.FindByid(id);

                        if (row == null)
                        {
                            try
                            {
                                row = cache.Content.Markets.NewMarketsRow();
                                ServiceDataMapper.MapNewMarket(row, eventType, data);
                                cache.Content.Markets.AddMarketsRow(row);
                                i++;
                            }
                            catch (Exception ex)
                            {
                                Trace.TraceWarning("invalid service market data: " + s);
                            }
                        }
                    }

                    Trace.TraceInformation("Found " + i + " new markets");
                    cache.CommitMarkets();
                }
            }
            else Trace.TraceWarning("market data was empty");
        }
    }
}