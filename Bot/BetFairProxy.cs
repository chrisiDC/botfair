//using System;
//using System.Data.Linq;
//using BotFair.BetFairExchange;
//using BotFair.BetFairGlobal;
//using System.Collections.Generic;
//using System.Linq;
//using System.Diagnostics;
//using APIErrorEnum = BotFair.BetFairExchange.APIErrorEnum;
//using APIResponse = BotFair.BetFairExchange.APIResponse;

//namespace BotFair
//{
//    public class BetFairProxy : BotFair.IBetFairProxy
//    {


//        private BFGlobalServiceClient globalService;
//        private BFExchangeServiceClient exchangeService;
//        private BetFairExchange.APIRequestHeader exchangeHeader;
//        private BetFairGlobal.APIRequestHeader globalHeader;
//        //private MessageHandler messages;

//        //public delegate void MessageHandler(string message);

//        public BetFairProxy(BFGlobalServiceClient globalService, BFExchangeServiceClient exchangeService)
//        {


//            this.globalService = globalService;
//            this.exchangeService = exchangeService;


//        }

//        public void Login()
//        {
//            var loginRequest = new BetFairGlobal.LoginReq()
//            {
//                username = "Abrakadabra",
//                password = "rolling123",
//                productId = 82
//            };


//            var response = globalService.login(loginRequest);

//            if (response.errorCode == LoginErrorEnum.OK)
//            {
//                exchangeHeader = new BetFairExchange.APIRequestHeader() { sessionToken = response.header.sessionToken };
//                globalHeader = new BetFairGlobal.APIRequestHeader() { sessionToken = response.header.sessionToken };
//            }
//            else throw new Exception("login failed:" + response.errorCode);

//        }

//        public void Logout()
//        {
//            var logoutRequest = new BetFairGlobal.LogoutReq();
//            logoutRequest.header = globalHeader;
//            globalService.logout(logoutRequest);

//        }

//        /// <summary>
//        /// updates currently only ONE market(the oldest one) which is not suspended yet
//        /// </summary>
//        public void UpdateExistingMarkets()
//        {

//            var marketQ =
//                from market in Cache.Instance.Markets.ToArray()
//                where
//                    market.marketStatus.ToLower() != "suspended"
//                orderby market.eventDate ascending
//                select market;

//            if (marketQ.Count() > 0)
//            {
//                var m = marketQ.First();

//                var r = exchangeService.getMarketInfo(new GetMarketInfoReq()
//                                                          {
//                                                              header = exchangeHeader,
//                                                              marketId = m.id

//                                                          });

//                if (r.errorCode == GetMarketErrorEnum.OK)
//                {

                   
//                    try
//                    {
//                        Sync.syncMarkets.WaitUpdate();
//                        var market =  Cache.Instance.Markets.FindByid(m.id);
//                        market.marketStatus = Enum.GetName(typeof(MarketStatusEnum),r.marketLite.marketStatus);
//                        if (market.marketStatus.ToLower() == "closed")
//                        {
//                            Cache.Instance.RemoveMarket(market);

//                        }
//                        else if (market.isHot)
//                        {
//                            market.isHot = r.marketLite.marketStatus == MarketStatusEnum.ACTIVE;
//                        }
//                    }
//                    finally
//                    {
//                        Sync.syncMarkets.ReleaseUpdate();
//                    }
//                }
//                else
//                    Trace.TraceWarning("Problem with service call 'GetMarketInfo':" + r.errorCode + "/" +
//                                       r.header.errorCode);


//            }


//        }

//        public void ScanNewMarkets()
//        {
//            //todo: scanned markets : no need to scan again!

//            Trace.TraceInformation("Scanning for new markets");

//            const int eventType = (int)Enums.Betfair.EventTypes.HorseRacing;

//            int marketsPeriod = Cache.Instance.GetActiveConfiguration().newMarketsPeriod;

//            var markets = exchangeService.getAllMarkets(new BetFairExchange.GetAllMarketsReq()
//            {
//                header = exchangeHeader,
//                ////fromDate = DateTime.Now,
//                toDate = DateTime.Now.Add(TimeSpan.FromSeconds(marketsPeriod)),
//                fromDate = DateTime.Now,
//                ////toDate = DateTime.Now,
//                ////locale = "de",
//                countries = new string[] { "GBR", "IRL" },
//                eventTypeIds = new int?[] { eventType } // 1...soccer, 7...horse race
//            });


//            if (!String.IsNullOrEmpty(markets.marketData))
//            {

//                if (markets.errorCode == BetFairExchange.GetAllMarketsErrorEnum.OK)
//                {
//                    string[] foundMarkets = markets.marketData.Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);

//                    int i = 1;
//                    foreach (string s in foundMarkets)
//                    {

//                        string[] data = s.Split(new string[] { "~" }, StringSplitOptions.RemoveEmptyEntries);

//                        int id = Convert.ToInt32(data[0]);
//                        var row = Cache.Instance.Markets.FindByid(id);

//                        if (row == null)
//                        {

//                            try
//                            {
//                                row = Cache.Instance.Markets.NewMarketsRow();
//                                ServiceDataMapper.MapNewMarket(row, eventType, data);
//                                Cache.Instance.Markets.AddMarketsRow(row);
//                                i++;

//                            }
//                            catch (Exception ex)
//                            {

//                                Trace.TraceWarning("invalid service market data: " + s);

//                            }

//                        }

//                    }

//                    Trace.TraceInformation("Found "+i+" new markets");
//                    Cache.Instance.CommitMarkets();

//                }
//            }
//            else Trace.TraceWarning("market data was empty");



//        }

//        public void TrackPrices()
//        {
//            var hotMarkets =
//                           from markets in Cache.Instance.Markets
//                           where markets.isHot == true
//                           select markets;

//            foreach (var hotMarket in hotMarkets)
//            {
//                var trackedCandidates =
//                        from selections in Cache.Instance.Selections
//                        where selections.marketId == hotMarket.id && selections.tracked == true
//                        select selections;

//                var marketPricesResp = exchangeService.getMarketPrices(new GetMarketPricesReq() { header = exchangeHeader, marketId = hotMarket.id });
//                Trace.TraceInformation("PriceTracker called 'GetMarketPrices'");

//                if (marketPricesResp.errorCode == GetMarketPricesErrorEnum.OK)
//                {
//                    foreach (var candidate in trackedCandidates)
//                    {
//                        var candidateQuery = marketPricesResp.marketPrices.runnerPrices.Where(p => p.selectionId == candidate.selectionId);
//                        if (candidateQuery.Count() > 0)
//                        {
//                            var candidatePrice = candidateQuery.First();

//                            var backPrice = candidatePrice.bestPricesToBack.Where(p => p.depth == 1).First();
//                            var layPrice = candidatePrice.bestPricesToLay.Where(p => p.depth == 1).First();

//                            DateTime priceDate = DateTime.Now;
//                            var priceRow = Cache.Instance.PriceTrack.NewPriceTrackRow();
//                            priceRow.fk_market = hotMarket.id;
//                            priceRow.fk_selection = candidate.selectionId;
//                            priceRow.price = backPrice.price;
//                            priceRow.isLay = false;
//                            priceRow.priceDate = priceDate;
//                            Cache.Instance.PriceTrack.Rows.Add(priceRow);

//                            priceRow = Cache.Instance.PriceTrack.NewPriceTrackRow();
//                            priceRow.fk_market = hotMarket.id;
//                            priceRow.fk_selection = candidate.selectionId;
//                            priceRow.price = layPrice.price;
//                            priceRow.isLay = true;
//                            priceRow.priceDate = priceDate;
//                            Cache.Instance.PriceTrack.Rows.Add(priceRow);
//                        }
//                        else
//                        {
//                            Trace.TraceWarning("No Selection Found To Track: SelectionId=" + candidate.selectionId + ";marketid=" + hotMarket.id + ";status=" + hotMarket.marketStatus);
//                        }

//                    }

//                    Cache.Instance.CommitPriceTrack();
//                }

//                else
//                {
//                    Trace.TraceWarning("Problem with service call 'getMarketPrices':" + marketPricesResp.errorCode +
//                                       "/" +
//                                       marketPricesResp.header.errorCode);
//                }
//            }

           

//        }



//        //public void LoadEventTypes()
//        //{
//        //    var request = new BetFairGlobal.GetEventTypesReq()
//        //    {
//        //        header = globalHeader
//        //    };

//        //    var response = globalService.getAllEventTypes(request);

//        //    foreach (EventType t in response.eventTypeItems)
//        //    {
//        //        var newRow = Cache.Instance.EventTypes.NewEventTypesRow();
//        //        newRow.id = t.id;
//        //        newRow.name = t.name;

//        //        Cache.Instance.EventTypes.AddEventTypesRow(newRow);
//        //    }
//        //}

//        //private void SetMarketValuesFromService(string[] serviceValues, Markets m)
//        //{
//        //    m.Id = Convert.ToInt32(serviceValues[0]);
//        //    m.Name = serviceValues[1];
//        //    m.MenuPath = serviceValues[5];
//        //    m.TotalAmount = Convert.ToDouble(serviceValues[13]);
//        //}

//        //public void GetMarketPrices()
//        //{

//        //    var cache = Cache.Instance;
//        //    foreach (var observerMarket in Cache.Instance.ObserverMarkets)
//        //    {
//        //        var servicePrices = exchangeService.getMarketPrices(new GetMarketPricesReq() { header = exchangeHeader, marketId = observerMarket.fk_market });
//        //        var dbAdapter = new DataLayerTableAdapters.SelectionsTableAdapter();
//        //        dbAdapter.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["botfairConnectionString"].ConnectionString;

//        //        var pricesAdpater = new DataLayerTableAdapters.PricesTableAdapter();
//        //        pricesAdpater.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["botfairConnectionString"].ConnectionString;

//        //        if (observerMarket.initialized)
//        //        {
//        //            //servicePrices.marketPrices.runnerPrices[0].
//        //        }
//        //        else
//        //        {
//        //            foreach (var runnerPrice in servicePrices.marketPrices.runnerPrices)
//        //            {

//        //                var selection = cache.Selections.FindBymarketIdselectionId(observerMarket.fk_market, runnerPrice.selectionId);
//        //                if (selection == null)
//        //                {
//        //                    dbAdapter.Insert(observerMarket.fk_market, runnerPrice.selectionId, runnerPrice.totalAmountMatched, runnerPrice.lastPriceMatched);
//        //                    var newRow = cache.Selections.NewSelectionsRow();
//        //                    newRow.marketId = observerMarket.fk_market;
//        //                    newRow.selectionId = runnerPrice.selectionId;
//        //                    newRow.totalAmoutMatched = runnerPrice.totalAmountMatched;
//        //                    newRow.lastPriceMatched = runnerPrice.lastPriceMatched;
//        //                    cache.Selections.Rows.Add(newRow);
//        //                }
//        //                else
//        //                {

//        //                    selection.lastPriceMatched = runnerPrice.lastPriceMatched;
//        //                    selection.totalAmoutMatched = runnerPrice.totalAmountMatched;
//        //                    dbAdapter.Update(selection);


//        //                    foreach (var price in runnerPrice.bestPricesToBack)
//        //                    {

//        //                        DateTime now = DateTime.Now;
//        //                        pricesAdpater.Insert(observerMarket.fk_market, runnerPrice.selectionId, now, price.price, price.amountAvailable, false);
//        //                        var newRow = cache.Prices.NewPricesRow();
//        //                        newRow.fk_market = observerMarket.fk_market;
//        //                        newRow.timestamp = now;
//        //                        newRow.fk_selection = runnerPrice.selectionId;
//        //                        newRow.price = price.price;
//        //                        newRow.amount = price.amountAvailable;
//        //                        Cache.Instance.Prices.AddPricesRow(newRow);
//        //                    }

//        //                    foreach (var price in runnerPrice.bestPricesToLay)
//        //                    {
//        //                        DateTime now = DateTime.Now;
//        //                        pricesAdpater.Insert(observerMarket.fk_market, runnerPrice.selectionId, now, price.price, price.amountAvailable, true);
//        //                        var newRow = cache.Prices.NewPricesRow();
//        //                        newRow.fk_market = observerMarket.fk_market;
//        //                        newRow.fk_selection = runnerPrice.selectionId;
//        //                        newRow.timestamp = now;
//        //                        newRow.price = price.price;
//        //                        newRow.amount = price.amountAvailable;
//        //                        Cache.Instance.Prices.AddPricesRow(newRow);
//        //                    }

//        //                }



//        //            }

//        //            cache.Selections.AcceptChanges();
//        //            cache.Prices.AcceptChanges();
//        //        }


//        //    }
//        //    messages("prices scan done");


//        //}

//        //public void GetMarketPrices2(int marketId)
//        //{

//        //    var cache = Cache.Instance;
//        //    var market = cache.Markets.FindByid(marketId);

//        //    var servicePrices = exchangeService.getMarketPrices(new GetMarketPricesReq() { header = exchangeHeader, marketId = market.id });
//        //    var dbAdapter = new DataLayerTableAdapters.SelectionsTableAdapter();

//        //    dbAdapter.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["botfairConnectionString"].ConnectionString;

//        //    var pricesAdpater = new DataLayerTableAdapters.Prices2TableAdapter();
//        //    pricesAdpater.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["botfairConnectionString"].ConnectionString;


//        //    if (servicePrices.marketPrices != null)
//        //    {

//        //        foreach (var runnerPrice in servicePrices.marketPrices.runnerPrices)
//        //        {

//        //            var selection = cache.Selections.FindBymarketIdselectionId(market.id, runnerPrice.selectionId);
//        //            if (selection == null)
//        //            {
//        //                dbAdapter.Insert(market.id, runnerPrice.selectionId, runnerPrice.totalAmountMatched, runnerPrice.lastPriceMatched);
//        //                var newRow = cache.Selections.NewSelectionsRow();
//        //                newRow.marketId = market.id;
//        //                newRow.selectionId = runnerPrice.selectionId;
//        //                newRow.totalAmoutMatched = runnerPrice.totalAmountMatched;
//        //                newRow.lastPriceMatched = runnerPrice.lastPriceMatched;

//        //                cache.Selections.Rows.Add(newRow);
//        //            }
//        //            else
//        //            {

//        //                selection.lastPriceMatched = runnerPrice.lastPriceMatched;
//        //                selection.totalAmoutMatched = runnerPrice.totalAmountMatched;
//        //                dbAdapter.Update(selection);


//        //                foreach (var price in runnerPrice.bestPricesToBack)
//        //                {


//        //                    var priceFromCache = Cache.Instance.Bets.FindByfk_marketfk_selection(market.id, runnerPrice.selectionId);
//        //                    if (priceFromCache == null)
//        //                    {
//        //                        pricesAdpater.Insert(market.id, runnerPrice.selectionId, price.amountAvailable, false, price.price, price.price);
//        //                        var newRow = Cache.Instance.Bets.NewBetsRow();
//        //                        newRow.fk_market = market.id;
//        //                        newRow.isLay = false;
//        //                        newRow.fk_selection = runnerPrice.selectionId;
//        //                        newRow.firstPrice = price.price;
//        //                        newRow.currentPrice = price.price;
//        //                        newRow.amount = price.amountAvailable;
//        //                        Cache.Instance.Bets.AddBetsRow(newRow);
//        //                    }
//        //                    else
//        //                    {
//        //                        priceFromCache.currentPrice = price.price;
//        //                        priceFromCache.amount = price.amountAvailable;
//        //                    }
//        //                }

//        //                foreach (var price in runnerPrice.bestPricesToLay)
//        //                {
//        //                    var priceFromCache = Cache.Instance.Bets.FindByfk_marketfk_selection(market.id, runnerPrice.selectionId);
//        //                    if (priceFromCache == null)
//        //                    {
//        //                        pricesAdpater.Insert(market.id, runnerPrice.selectionId, price.amountAvailable, true, price.price, price.price);
//        //                        var newRow = Cache.Instance.Bets.NewBetsRow();
//        //                        newRow.fk_market = market.id;
//        //                        newRow.isLay = true;
//        //                        newRow.fk_selection = runnerPrice.selectionId;
//        //                        newRow.firstPrice = price.price;
//        //                        newRow.currentPrice = price.price;
//        //                        newRow.amount = price.amountAvailable;
//        //                        Cache.Instance.Bets.AddBetsRow(newRow);
//        //                    }
//        //                    else
//        //                    {
//        //                        priceFromCache.currentPrice = price.price;
//        //                        priceFromCache.amount = price.amountAvailable;
//        //                    }
//        //                }

//        //            }





//        //            cache.Selections.AcceptChanges();
//        //            cache.Prices.AcceptChanges();
//        //            Cache.Instance.Bets.AcceptChanges();
//        //        }
//        //    }



//        //    messages("prices scan done");


//        //}

//        public void GetMarketPrices(int marketId)
//        {

//            var cache = Cache.Instance;
//            var market = cache.Markets.FindByid(marketId);

//            var servicePrices = exchangeService.getMarketPrices(new GetMarketPricesReq() { header = exchangeHeader, marketId = market.id });

//            if (servicePrices.errorCode == GetMarketPricesErrorEnum.OK)
//            {
//                if (servicePrices.marketPrices != null)
//                {

//                    foreach (var runnerPrice in servicePrices.marketPrices.runnerPrices)
//                    {

//                        var selection = cache.Selections.FindBymarketIdselectionId(market.id, runnerPrice.selectionId);
//                        if (selection == null)
//                        {

//                            var newRow = cache.Selections.NewSelectionsRow();
//                            newRow.marketId = market.id;
//                            newRow.selectionId = runnerPrice.selectionId;
//                            newRow.totalAmoutMatched = runnerPrice.totalAmountMatched;
//                            newRow.lastPriceMatched = runnerPrice.lastPriceMatched;
//                            newRow.tracked = false;
//                            newRow.position = 0;
//                            cache.Selections.Rows.Add(newRow);
//                        }
//                        else
//                        {
//                            try
//                            {
//                                Sync.syncSelections.WaitUpdate();
//                                selection.lastPriceMatched = runnerPrice.lastPriceMatched;
//                                selection.totalAmoutMatched = runnerPrice.totalAmountMatched;
//                            }
//                            finally
//                            {

//                                Sync.syncSelections.ReleaseUpdate();
//                            }

//                        }

//                        var backRow = Cache.Instance.Bets.FindByfk_marketfk_selectionisLay(market.id,
//                                                                                           runnerPrice.selectionId,
//                                                                                           false);
//                        var layRow = Cache.Instance.Bets.FindByfk_marketfk_selectionisLay(market.id,
//                                                                                          runnerPrice.selectionId, true);

//                        //bool isNewRow = false;


//                        if (runnerPrice.bestPricesToBack.Length > 0 && runnerPrice.bestPricesToLay.Length > 0)
//                        {

//                            var backPrice = runnerPrice.bestPricesToBack.Where(p => p.depth == 1).First();
//                            var layPrice = runnerPrice.bestPricesToLay.Where(p => p.depth == 1).First();


//                            try
//                            {
//                                if (backRow == null)
//                                {
//                                    backRow = Cache.Instance.Bets.NewBetsRow();
//                                    layRow = Cache.Instance.Bets.NewBetsRow();
//                                    ServiceDataMapper.MapNewBet(backRow, market.id, runnerPrice.selectionId, backPrice,
//                                                                false);
//                                    ServiceDataMapper.MapNewBet(layRow, market.id, runnerPrice.selectionId, backPrice,
//                                                                true);
//                                    Cache.Instance.Bets.AddBetsRow(backRow);
//                                    Cache.Instance.Bets.AddBetsRow(layRow);
//                                }
//                                else
//                                {

//                                    try
//                                    {
//                                        Sync.syncBets.WaitUpdate();
//                                        ServiceDataMapper.MapExistingBet(layRow, market.id, runnerPrice.selectionId,
//                                                                         layPrice, true);
//                                        ServiceDataMapper.MapExistingBet(backRow, market.id, runnerPrice.selectionId,
//                                                                         layPrice, false);
//                                    }
//                                    finally
//                                    {
//                                        Sync.syncBets.ReleaseUpdate();
//                                    }
//                                }



//                            }
//                            catch (Exception ex)
//                            {

//                                Trace.TraceWarning("invalid bet mapping data: market=" + market.id);

//                            }

//                        }
//                        else
//                        {
//                            Trace.TraceWarning("no quotes available;market=" + market.id);

//                        }



//                    }

//                    Cache.Instance.CommitSelections();
//                    Cache.Instance.CommitBets();

//                }

           
//            }
//            else
//            {

//                Trace.TraceWarning("GetMarketPrices Service Call Problem; error code = " + servicePrices.errorCode + "//" + servicePrices.header.errorCode);

             
//            }


//        }

//        //public void TrackMarketPrice(int marketId,int[] selectionIds)
//        //{

//        //    var cache = Cache.Instance;
//        //    var market = cache.Markets.FindByid(marketId);

//        //    var servicePrices = exchangeService.getMarketPrices(new GetMarketPricesReq() { header = exchangeHeader, marketId = market.id });

//        //    if (servicePrices.errorCode == GetMarketPricesErrorEnum.OK)
//        //    {
//        //        if (servicePrices.marketPrices != null)
//        //        {

//        //            foreach (int selectionId in selectionIds)
//        //            {

//        //                var selection = cache.Selections.FindBymarketIdselectionId(market.id, selectionId);

//        //                    try
//        //                    {
//        //                        Sync.syncSelections.WaitUpdate();
//        //                        selection.lastPriceMatched = runnerPrice.lastPriceMatched;
//        //                        selection.totalAmoutMatched = runnerPrice.totalAmountMatched;
//        //                    }
//        //                    finally
//        //                    {

//        //                        Sync.syncSelections.ReleaseUpdate();
//        //                    }



//        //                var backRow = Cache.Instance.Bets.FindByfk_marketfk_selectionisLay(market.id,
//        //                                                                                   runnerPrice.selectionId,
//        //                                                                                   false);
//        //                var layRow = Cache.Instance.Bets.FindByfk_marketfk_selectionisLay(market.id,
//        //                                                                                  runnerPrice.selectionId, true);

//        //                //bool isNewRow = false;


//        //                if (runnerPrice.bestPricesToBack.Length > 0 && runnerPrice.bestPricesToLay.Length > 0)
//        //                {
//        //                    var backPrice = runnerPrice.bestPricesToBack[0];
//        //                    var layPrice = runnerPrice.bestPricesToLay[0];


//        //                    try
//        //                    {
//        //                        if (backRow == null)
//        //                        {
//        //                            backRow = Cache.Instance.Bets.NewBetsRow();
//        //                            layRow = Cache.Instance.Bets.NewBetsRow();
//        //                            ServiceDataMapper.MapNewBet(backRow, market.id, runnerPrice.selectionId, backPrice,
//        //                                                        false);
//        //                            ServiceDataMapper.MapNewBet(layRow, market.id, runnerPrice.selectionId, backPrice,
//        //                                                        true);
//        //                            Cache.Instance.Bets.AddBetsRow(backRow);
//        //                            Cache.Instance.Bets.AddBetsRow(layRow);
//        //                        }
//        //                        else
//        //                        {

//        //                            try
//        //                            {
//        //                                Sync.syncBets.WaitUpdate();
//        //                                ServiceDataMapper.MapExistingBet(layRow, market.id, runnerPrice.selectionId,
//        //                                                                 layPrice, true);
//        //                                ServiceDataMapper.MapExistingBet(backRow, market.id, runnerPrice.selectionId,
//        //                                                                 layPrice, false);
//        //                            }
//        //                            finally
//        //                            {
//        //                                Sync.syncBets.ReleaseUpdate();
//        //                            }
//        //                        }



//        //                    }
//        //                    catch (Exception ex)
//        //                    {

//        //                        Trace.TraceWarning("invalid bet mapping data: market=" + market.id);

//        //                    }

//        //                }
//        //                else
//        //                {
//        //                    Trace.TraceWarning("no quotes available;market=" + market.id);

//        //                }



//        //            }

//        //            Cache.Instance.CommitSelections();
//        //            Cache.Instance.CommitBets();

//        //        }
//        //    }
//        //    else
//        //    {

//        //        Trace.TraceWarning("GetMarketPrices Service Call Problem; error code = " + servicePrices.errorCode + "//" + servicePrices.header.errorCode);
//        //    }


//        //}

//        //public void TrackBet(int betId)
//        //{
//        //    throw new NotImplementedException();
//        //}

//        //public List<int> GetMarketSelections(int marketId)
//        //{
//        //    var servicePrices = exchangeService.getMarketPrices(new GetMarketPricesReq() { header = exchangeHeader, marketId = marketId });
//        //    List<int> selections = new List<int>();
//        //    if (servicePrices.marketPrices != null)
//        //    {
//        //        foreach (var runnerPrice in servicePrices.marketPrices.runnerPrices)
//        //        {

//        //            selections.Add(runnerPrice.selectionId);

//        //        }
//        //    }

//        //    return selections;
//        //}

//        public void RefreshSession()
//        {

//        }

//        public PlaceBetsResp PlaceBet(PlaceBets[] bets)
//        {

//            var request = new BetFairExchange.PlaceBetsReq
//            {
//                header = exchangeHeader,
//                bets = bets

//            };

//            var response = exchangeService.placeBets(request);

//            return response;
//        }

      






//    }
//}
