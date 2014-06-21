using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using BotFair.BusinessLayer.BetEngine;
using BotFair.BusinessLayer.BetEngine.Masters;
using BotFair.DataLayer.BetFairExchange;
using BotFair.DataLayer;
using BotFair.DataLayer.BotDataSetTableAdapters;
using Tools.Aspects;

namespace BotFair.BusinessLayer
{
    public class Markets : EntityBase
    {
        private Account acc;

        private Configuration cfg;

        //private object lastScanSync = new object();

        private DateTime lastScanFrom = DateTime.MinValue, lastScanTo = DateTime.MinValue;

        public Markets(Account acc, Configuration cfg)
            : base()
        {
            this.acc = acc;
            this.cfg = cfg;

        }



        public void UpdateExistingMarkets_Thread()
        {
            var lastScanTimes = GetLastMarketScan();

            if (lastScanTimes[0] != DateTime.MinValue)
            {
                var timeScanned = GetLatestActiveMarketScanFrom();

                if (timeScanned != DateTime.MinValue)
                {

                    List<BotDataSet.MarketsRow> markets;

                    markets = GetMarketInfoFromService(timeScanned, lastScanTimes[1]);
                    //Trace.TraceInformation("Updating from " + timeScanned + " to " + lastScanTimes[1]);

                    foreach (var marketFromService in markets)
                    {

                        var cachedMarkeQ =
                            cache.GetMarkets().Where(marketItem => marketItem.id == marketFromService.id);

                        var hotMarkets =
                           cache.GetMarkets().Where(marketItem => marketItem.id == marketFromService.id && marketItem.isHot == true);


                        if (cachedMarkeQ.Any())
                        {
                            var cachedMarket = cachedMarkeQ.First();

                            if (marketFromService.eventDate < DateTime.Now.Subtract(TimeSpan.FromMinutes(10))) marketFromService.marketStatus = "closed";
                            SetMarketStatus(cachedMarket, marketFromService.marketStatus, marketFromService.eventDate);


                        }
                        //else Trace.TraceWarning("market " + marketFromService.id + " not found in cache");

                    }

                    CommitMarketsStatus();
                }
            }
        }

        public DateTime GetLatestActiveMarketScanFrom()
        {
            var marketQ = cache.GetMarkets().Where(m => m.marketStatus.ToLower() == "active").OrderBy(market => market.timeScanned);
            if (marketQ.Any()) return marketQ.First().timeScanned;

            return DateTime.MinValue;
        }

     
        public void SetMarketStatus(BotDataSet.MarketsRow market, string status, DateTime eventDate)
        {
            if (status.Equals("closed", StringComparison.CurrentCultureIgnoreCase)) market.isHot = false;
            market.marketStatus = status;
            market.eventDate = eventDate;
        }

     
        private void CommitMarketsStatus()
        {
            Cache.Instance.CommitMarkets();
        }


        public void SetHotMarkets_Thread()
        {

            int hotMarketsSeconds = cfg.ActiveHotMarketsSeconds;
            int riskValue = cfg.ActiveRiskValue;
            double percentage = cfg.ActivePercentage;


            var markets = cache.GetMarkets();

            var currentHotMarkets = markets.Where(market => market.isHot == true);

            if (!currentHotMarkets.Any())  //allow only 1 hot market
            {

                var hotMarketCandidates =
                    markets.Where(market => market.eventType == (int)EventTypes.HorseRacing &&
                                            market.winners == 1 &&
                                            market.totalAmount > 0 &&
                                            market.eventDate < TimeZone.Convert(DateTime.Now).AddSeconds(hotMarketsSeconds) &&
                                            market.eventDate > TimeZone.Convert(DateTime.Now) &&
                                            market.marketStatus.ToLower() == "active" &&
                                            market.isHot == false &&
                                            market.turningInPlay &&
                                            market.bspMarket).OrderBy(market => market.eventDate);

                if (hotMarketCandidates.Any())
                {
                    var hotMarket = hotMarketCandidates.First();

                    var hotMarkets = new BotDataSet.MarketsRow[] { hotMarket };
                    MarkHotMarketsAndGetDetails(hotMarkets);
                    int i = InsertSelectionsIntoCache(hotMarket);

                    if (i > 0)
                    {
                        SetTrackedSelections(hotMarket.id);

                        Config.RegisterMasters(hotMarket.id);
                    }
                    else
                    {
                        DeMarkHotMarket(hotMarket);
                        Config.UnRegisterMasters(hotMarket.id);
                    }


                    cache.CommitMarkets();
                    cache.CommitSelections();
                }

            }

        }

      
        protected void MarkHotMarketsAndGetDetails(IEnumerable<BotDataSet.MarketsRow> markets)
        {
            foreach (var market in markets)
            {

                BFExchangeServiceClient service = new BFExchangeServiceClient();
                var marketDetails = service.getMarket(new GetMarketReq()
                                                          {
                                                              header = acc.GetExchangeHeader(),
                                                              marketId = market.id,
                                                              locale = "de"
                                                          });
                if (marketDetails.errorCode == GetMarketErrorEnum.OK)
                {
                    market.isHot = true;
                    if (marketDetails.market.interval != null) market.interval = (double)marketDetails.market.interval;
                    else market.interval = 0;
                    market.marketSuspendTime = marketDetails.market.marketSuspendTime;

                }
                else
                    throw new ServiceException("Problem with service call 'GetMarket':" + marketDetails.errorCode + "/" + marketDetails.header.errorCode);

            }
        }

    
        protected void DeMarkHotMarket(BotDataSet.MarketsRow market)
        {
            market.isHot = false;
          
        }

      
        protected void SetTrackedSelections(int marketId)
        {
            BFExchangeServiceClient service = new BFExchangeServiceClient();
            var marketPricesResp = service.getMarketPrices(new GetMarketPricesReq { header = acc.GetExchangeHeader(), marketId = marketId });
            if (marketPricesResp.errorCode == GetMarketPricesErrorEnum.OK)
            {
                var runnerPrices = marketPricesResp.marketPrices.runnerPrices.OrderBy(p => p.lastPriceMatched).ToList();
                int i = 0;
                foreach (RunnerPrices runner in runnerPrices)
                {
                    var cachedRunner = Cache.Instance.GetSelections().Where(p => p.marketId == marketId && p.selectionId == runner.selectionId).ToList();
                    if (cachedRunner.Any())
                    {
                        cachedRunner.First().position = i;
                        cachedRunner.First().tracked = true;
                    }
                  
                    i++;
                }
                //var trackedSelection = Cache.Instance.GetSelections().Where(p => p.marketId == marketId && p.selectionId == q.selectionId).ToArray().First();
            }
            else throw new ServiceException("Problem with service call 'GetMarketPrices;error code='" + marketPricesResp.errorCode);
        }

        protected List<BotDataSet.MarketsRow> GetMarketInfoFromService(DateTime from, DateTime to)
        {

            List<BotDataSet.MarketsRow> results = new List<BotDataSet.MarketsRow>();
            BFExchangeServiceClient service = new BFExchangeServiceClient();

            var markets = service.getAllMarkets(new GetAllMarketsReq()
            {
                header = acc.GetExchangeHeader(),
                fromDate = DateTime.Parse(from.ToString()),
                toDate = DateTime.Parse(to.ToString()),//

                countries = new[] { "GBR" },
                eventTypeIds = new int?[] { 7 },
                locale = "de"

            });


            if (markets.errorCode == GetAllMarketsErrorEnum.OK)
            {
                if (!String.IsNullOrEmpty(markets.marketData))
                {
                    int i = 0;
                    if (markets.errorCode == GetAllMarketsErrorEnum.OK)
                    {
                        string[] foundMarkets = markets.marketData.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (string s in foundMarkets)
                        {
                            string[] data = s.Split(new[] { "~" }, StringSplitOptions.RemoveEmptyEntries);
                            var marketRow = Cache.Instance.GetMarkets().NewMarketsRow();
                            ServiceDataMapper.MapNewMarket(marketRow, (int)EventTypes.HorseRacing, data);
                            results.Add(marketRow);
                        }


                    }
                }
            }
            else
                throw new ServiceException("Problem with service call 'GetAllMarkets':" + markets.errorCode + "/" + markets.header.errorCode);


            return results;
        }

        //[ReadWriterLock(ReadWriterLock.Mode.Write, Sync.scanWindow)]
        public void ScanNewMarkets_Thread()
        {

            DateTime scanStart;
            DateTime scanTo;

            if (GetNewMarketsScanFromTo(out scanStart, out scanTo))
            {
            
                if (scanTo != DateTime.MinValue)
                {

                    Trace.TraceInformation("Scanning new markets from " + scanStart + " to " + scanTo);
                    List<BotDataSet.MarketsRow> markets;


                    markets = GetMarketInfoFromService(scanStart, scanTo);

                    AddNewMarkets(markets, scanStart);
                }


            }
        }


        private bool IsMarketOfInterest(BotDataSet.MarketsRow market)
        {
            return market.winners==1 && market.totalAmount >0;
        }
    
        private void AddNewMarkets(IEnumerable<BotDataSet.MarketsRow> markets, DateTime scanStart)
        {
            var x = markets.OrderByDescending(m => m.eventDate);
            int i = 0;
            foreach (var market in markets)
            {
                if (IsMarketOfInterest(market))
                {
                    var marketInCache = Cache.Instance.GetMarkets().Where(m => m.id == market.id);
                    if (!marketInCache.Any() && market.marketStatus.ToLower() == "active")
                    {
                        i++;
                        market.timeScanned = scanStart;
                        market.wasHot = false;

                        Cache.Instance.GetMarkets().AddMarketsRow(market);
                        //InsertSelectionsIntoCache(market);
                    }
                }
            }

            if (i > 0) Trace.TraceInformation("Added "+i+" new markets");
            Cache.Instance.CommitMarkets();

        }

        public DateTime[] GetLastMarketScan()
        {

            return new DateTime[] { lastScanFrom, lastScanTo };

        }

        protected bool GetNewMarketsScanFromTo(out DateTime from, out DateTime to)
        {

            if (lastScanFrom == DateTime.MinValue)
            {
                from = DateTime.Now.Subtract(TimeSpan.FromHours(1));
                to = from.Add(TimeSpan.FromSeconds(cfg.ActiveNewMarketsPeriod));
                lastScanFrom = from;
                lastScanTo = to;
            }
            else
            {
               
                from = lastScanTo.AddSeconds(1);
                to = from.Add(TimeSpan.FromSeconds(cfg.ActiveNewMarketsPeriod) - (lastScanTo - DateTime.Now.Subtract(TimeSpan.FromHours(1))));
            }


            lastScanFrom = from;
            lastScanTo = to;


            return true;

        }


        protected int InsertSelectionsIntoCache(BotDataSet.MarketsRow marketRow)
        {

            BFExchangeServiceClient service = new BFExchangeServiceClient();
            var r = service.getMarket(new GetMarketReq()
            {


                header = acc.GetExchangeHeader(),
                marketId = marketRow.id,
                locale = "de"

            });


            if (r.errorCode == GetMarketErrorEnum.OK)
            {
                if (r.market.runners.Length == 0)
                {
                    Trace.TraceWarning("Service Returned No Selections For Market " + marketRow.id);
                }


                foreach (var runner in r.market.runners)
                {
                    var q = cache.GetSelections().Where(item => item.selectionId == runner.selectionId);
                    if (!q.Any())
                    {
                        var newRow = cache.GetSelections().NewSelectionsRow();
                        newRow.marketId = marketRow.id;
                        newRow.selectionId = runner.selectionId;
                        newRow.handicap = runner.handicap;
                        newRow.asianLineId = runner.asianLineId;
                        newRow.name = runner.name;
                        newRow.tracked = false;
                        newRow.position = 0;

                        cache.GetSelections().AddSelectionsRow(newRow);


                    }

                }
                return r.market.runners.Length;
            }
            else
                throw new ServiceException("Problem with service call 'GetMarket':" + r.errorCode + "/" + r.header.errorCode);

            return 0;

        }
        //protected void InsertNewMarketIntoCache(string marketData)
        //{
        //    const int eventType = (int)EventTypes.HorseRacing;
        //    string[] data = marketData.Split(new[] { "~" }, StringSplitOptions.RemoveEmptyEntries);
        //    int id = Convert.ToInt32(data[0]);

        //    var market = Cache.Instance.GetMarkets().NewMarketsRow();

        //    var r = service.getMarket(new getMarketIn()
        //    {


        //        request = new GetMarketReq()
        //        {
        //            header = acc.GetExchangeHeader(),
        //            marketId = id
        //        }
        //    });


        //    if (r.Result.errorCode == GetMarketErrorEnum.OK)
        //    {
        //        market.id = r.Result.market.marketId;
        //        market.runners = r.Result.market.runners.Length;
        //        market.menuPath = r.Result.market.menuPath;
        //        market.country = r.Result.market.countryISO3;
        //        market.eventDate = r.Result.market.marketTime;
        //        market.type = Enum.GetName(typeof(MarketTypeEnum), r.Result.market.marketType);
        //        market.eventType = r.Result.market.eventTypeId;
        //        market.isHot = false;
        //        market.marketStatus = Enum.GetName(typeof(MarketStatusEnum), r.Result.market.marketStatus);
        //        market.timeScanned = DateTime.Now;

        //        ServiceDataMapper.MapNewMarket(market, eventType, data);
        //        Cache.Instance.GetMarkets().AddMarketsRow(market);

        //        foreach (var runner in r.Result.market.runners)
        //        {
        //            var selection = cache.GetSelections().FindBymarketIdselectionId(market.id,
        //                                                                            runner.selectionId);
        //            if (selection == null)
        //            {
        //                var newRow = cache.GetSelections().NewSelectionsRow();
        //                newRow.marketId = market.id;
        //                newRow.selectionId = runner.selectionId;
        //                newRow.handicap = runner.handicap;
        //                newRow.asianLineId = runner.asianLineId;
        //                newRow.name = runner.name;
        //                newRow.tracked = false;
        //                newRow.position = 0;
        //                cache.GetSelections().Rows.Add(newRow);
        //            }
        //        }
        //    }
        //    else
        //        Trace.TraceWarning("Problem with service call 'GetMarket':" + r.Result.errorCode + "/" +
        //                           r.Result.header.errorCode);
        //}



        //protected void CommitMarkets()
        //{
        //    try
        //    {
        //        Sync.syncMarkets.WaitCommit();


        //        var changes = cache.GetMarkets().GetChanges();
        //        if (changes != null && changes.Rows.Count > 0)
        //        {
        //            var adapter = new MarketsTableAdapter();
        //            //adapter.Connection.ConnectionString = ConnectionString;

        //            adapter.Update(cache.GetMarkets());
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Trace.TraceWarning("Failure at Db Update: Changes count: " + ex);
        //    }
        //    finally
        //    {
        //        Sync.syncMarkets.ReleaseCommit();
        //    }
        //}

        protected override void Init()
        {

        }

        public void LoadActiveMarkets()
        {
            cache.LoadActiveMarkets();
        }

    }
}