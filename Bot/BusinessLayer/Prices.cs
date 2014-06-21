using System;
using System.Diagnostics;
using System.Linq;
using BotFair.BetFairExchange;
using BotFair.BetFairGlobal;
using BotFair.Services;
using BotFair.Sys;

namespace BotFair.BusinessLayer
{
    public class Prices : BusinessObject
    {
        public Prices()
            : base(AppCache.Instance, new BFGlobalServiceClient(), new BFExchangeServiceClient())
        {
        }

        public Prices(IAppCache cache, BFGlobalServiceClient globalService, BFExchangeServiceClient exchangeService)
            : base(cache, globalService, exchangeService)
        {
        }

        public void TrackPrices()
        {
            var hotMarkets =
                from markets in cache.Content.Markets
                where markets.isHot
                select markets;

            foreach (var hotMarket in hotMarkets)
            {
                var trackedCandidates =
                    from selections in cache.Content.Selections
                    where selections.marketId == hotMarket.id && selections.tracked
                    select selections;

                var marketPricesResp =
                    exchangeService.getMarketPrices(new GetMarketPricesReq
                                                        {header = exchangeHeader, marketId = hotMarket.id});
                Trace.TraceInformation("PriceTracker called 'GetMarketPrices'");

                if (marketPricesResp.errorCode == GetMarketPricesErrorEnum.OK)
                {
                    foreach (var candidate in trackedCandidates)
                    {
                        var candidateQuery =
                            marketPricesResp.marketPrices.runnerPrices.Where(p => p.selectionId == candidate.selectionId);
                        if (candidateQuery.Count() > 0)
                        {
                            var candidatePrice = candidateQuery.First();

                            var backPrice = candidatePrice.bestPricesToBack.Where(p => p.depth == 1).First();
                            var layPrice = candidatePrice.bestPricesToLay.Where(p => p.depth == 1).First();

                            DateTime priceDate = DateTime.Now;
                            var priceRow = cache.Content.PriceTrack.NewPriceTrackRow();
                            priceRow.fk_market = hotMarket.id;
                            priceRow.fk_selection = candidate.selectionId;
                            priceRow.price = backPrice.price;
                            priceRow.isLay = false;
                            priceRow.priceDate = priceDate;
                            cache.Content.PriceTrack.Rows.Add(priceRow);

                            priceRow = cache.Content.PriceTrack.NewPriceTrackRow();
                            priceRow.fk_market = hotMarket.id;
                            priceRow.fk_selection = candidate.selectionId;
                            priceRow.price = layPrice.price;
                            priceRow.isLay = true;
                            priceRow.priceDate = priceDate;
                            cache.Content.PriceTrack.Rows.Add(priceRow);
                        }
                        else
                        {
                            Trace.TraceWarning("No Selection Found To Track: SelectionId=" + candidate.selectionId +
                                               ";marketid=" + hotMarket.id + ";status=" + hotMarket.marketStatus);
                        }
                    }

                    cache.CommitPriceTrack();
                }

                else
                {
                    Trace.TraceWarning("Problem with service call 'getMarketPrices':" + marketPricesResp.errorCode +
                                       "/" +
                                       marketPricesResp.header.errorCode);
                }
            }
        }

        public void GetMarketPrices(int marketId)
        {
            var market = cache.Content.Markets.FindByid(marketId);

            var servicePrices =
                exchangeService.getMarketPrices(new GetMarketPricesReq {header = exchangeHeader, marketId = market.id});

            if (servicePrices.errorCode == GetMarketPricesErrorEnum.OK)
            {
                if (servicePrices.marketPrices != null)
                {
                    foreach (var runnerPrice in servicePrices.marketPrices.runnerPrices)
                    {
                        var selection = cache.Content.Selections.FindBymarketIdselectionId(market.id,
                                                                                           runnerPrice.selectionId);
                        if (selection == null)
                        {
                            var newRow = cache.Content.Selections.NewSelectionsRow();
                            newRow.marketId = market.id;
                            newRow.selectionId = runnerPrice.selectionId;
                            newRow.totalAmoutMatched = runnerPrice.totalAmountMatched;
                            newRow.lastPriceMatched = runnerPrice.lastPriceMatched;
                            newRow.tracked = false;
                            newRow.position = 0;
                            cache.Content.Selections.Rows.Add(newRow);
                        }
                        else
                        {
                            try
                            {
                                Sync.syncSelections.WaitUpdate();
                                selection.lastPriceMatched = runnerPrice.lastPriceMatched;
                                selection.totalAmoutMatched = runnerPrice.totalAmountMatched;
                            }
                            finally
                            {
                                Sync.syncSelections.ReleaseUpdate();
                            }
                        }

                        var backRow = cache.Content.Bets.FindByfk_marketfk_selectionisLay(market.id,
                                                                                          runnerPrice.selectionId,
                                                                                          false);
                        var layRow = cache.Content.Bets.FindByfk_marketfk_selectionisLay(market.id,
                                                                                         runnerPrice.selectionId, true);

                        //bool isNewRow = false;


                        if (runnerPrice.bestPricesToBack.Length > 0 && runnerPrice.bestPricesToLay.Length > 0)
                        {
                            var backPrice = runnerPrice.bestPricesToBack.Where(p => p.depth == 1).First();
                            var layPrice = runnerPrice.bestPricesToLay.Where(p => p.depth == 1).First();


                            try
                            {
                                if (backRow == null)
                                {
                                    backRow = cache.Content.Bets.NewBetsRow();
                                    layRow = cache.Content.Bets.NewBetsRow();
                                    ServiceDataMapper.MapNewBet(backRow, market.id, runnerPrice.selectionId, backPrice,
                                                                false);
                                    ServiceDataMapper.MapNewBet(layRow, market.id, runnerPrice.selectionId, backPrice,
                                                                true);
                                    cache.Content.Bets.AddBetsRow(backRow);
                                    cache.Content.Bets.AddBetsRow(layRow);
                                }
                                else
                                {
                                    try
                                    {
                                        Sync.syncBets.WaitUpdate();
                                        ServiceDataMapper.MapExistingBet(layRow, market.id, runnerPrice.selectionId,
                                                                         layPrice, true);
                                        ServiceDataMapper.MapExistingBet(backRow, market.id, runnerPrice.selectionId,
                                                                         layPrice, false);
                                    }
                                    finally
                                    {
                                        Sync.syncBets.ReleaseUpdate();
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Trace.TraceWarning("invalid bet mapping data: market=" + market.id);
                            }
                        }
                        else
                        {
                            Trace.TraceWarning("no quotes available;market=" + market.id);
                        }
                    }

                    cache.CommitSelections();
                    cache.CommitBets();
                }
            }
            else
            {
                Trace.TraceWarning("GetMarketPrices Service Call Problem; error code = " + servicePrices.errorCode +
                                   "//" + servicePrices.header.errorCode);
            }
        }

        //public void TrackMarketPrice(int marketId,int[] selectionIds)
        //{

        //    var CacheLayer = cache.Content;
        //    var market = CacheLayer.Markets.FindByid(marketId);

        //    var servicePrices = exchangeService.getMarketPrices(new GetMarketPricesReq() { header = exchangeHeader, marketId = market.id });

        //    if (servicePrices.errorCode == GetMarketPricesErrorEnum.OK)
        //    {
        //        if (servicePrices.marketPrices != null)
        //        {

        //            foreach (int selectionId in selectionIds)
        //            {

        //                var selection = CacheLayer.Selections.FindBymarketIdselectionId(market.id, selectionId);

        //                    try
        //                    {
        //                        Sync.syncSelections.WaitUpdate();
        //                        selection.lastPriceMatched = runnerPrice.lastPriceMatched;
        //                        selection.totalAmoutMatched = runnerPrice.totalAmountMatched;
        //                    }
        //                    finally
        //                    {

        //                        Sync.syncSelections.ReleaseUpdate();
        //                    }


        //                var backRow = cache.Content.Bets.FindByfk_marketfk_selectionisLay(market.id,
        //                                                                                   runnerPrice.selectionId,
        //                                                                                   false);
        //                var layRow = cache.Content.Bets.FindByfk_marketfk_selectionisLay(market.id,
        //                                                                                  runnerPrice.selectionId, true);

        //                //bool isNewRow = false;


        //                if (runnerPrice.bestPricesToBack.Length > 0 && runnerPrice.bestPricesToLay.Length > 0)
        //                {
        //                    var backPrice = runnerPrice.bestPricesToBack[0];
        //                    var layPrice = runnerPrice.bestPricesToLay[0];


        //                    try
        //                    {
        //                        if (backRow == null)
        //                        {
        //                            backRow = cache.Content.Bets.NewBetsRow();
        //                            layRow = cache.Content.Bets.NewBetsRow();
        //                            ServiceDataMapper.MapNewBet(backRow, market.id, runnerPrice.selectionId, backPrice,
        //                                                        false);
        //                            ServiceDataMapper.MapNewBet(layRow, market.id, runnerPrice.selectionId, backPrice,
        //                                                        true);
        //                            cache.Content.Bets.AddBetsRow(backRow);
        //                            cache.Content.Bets.AddBetsRow(layRow);
        //                        }
        //                        else
        //                        {

        //                            try
        //                            {
        //                                Sync.syncBets.WaitUpdate();
        //                                ServiceDataMapper.MapExistingBet(layRow, market.id, runnerPrice.selectionId,
        //                                                                 layPrice, true);
        //                                ServiceDataMapper.MapExistingBet(backRow, market.id, runnerPrice.selectionId,
        //                                                                 layPrice, false);
        //                            }
        //                            finally
        //                            {
        //                                Sync.syncBets.ReleaseUpdate();
        //                            }
        //                        }


        //                    }
        //                    catch (Exception ex)
        //                    {

        //                        Trace.TraceWarning("invalid bet mapping data: market=" + market.id);

        //                    }

        //                }
        //                else
        //                {
        //                    Trace.TraceWarning("no quotes available;market=" + market.id);

        //                }


        //            }

        //            cache.Content.CommitSelections();
        //            cache.Content.CommitBets();

        //        }
        //    }
        //    else
        //    {

        //        Trace.TraceWarning("GetMarketPrices Service Call Problem; error code = " + servicePrices.errorCode + "//" + servicePrices.header.errorCode);
        //    }


        //}

        //public void TrackBet(int betId)
        //{
        //    throw new NotImplementedException();
        //}

        //public List<int> GetMarketSelections(int marketId)
        //{
        //    var servicePrices = exchangeService.getMarketPrices(new GetMarketPricesReq() { header = exchangeHeader, marketId = marketId });
        //    List<int> selections = new List<int>();
        //    if (servicePrices.marketPrices != null)
        //    {
        //        foreach (var runnerPrice in servicePrices.marketPrices.runnerPrices)
        //        {

        //            selections.Add(runnerPrice.selectionId);

        //        }
        //    }

        //    return selections;
        //}

        //public void LoadEventTypes()
        //{
        //    var request = new BetFairGlobal.GetEventTypesReq()
        //    {
        //        header = globalHeader
        //    };

        //    var response = globalService.getAllEventTypes(request);

        //    foreach (EventType t in response.eventTypeItems)
        //    {
        //        var newRow = cache.Content.EventTypes.NewEventTypesRow();
        //        newRow.id = t.id;
        //        newRow.name = t.name;

        //        cache.Content.EventTypes.AddEventTypesRow(newRow);
        //    }
        //}

        //private void SetMarketValuesFromService(string[] serviceValues, Markets m)
        //{
        //    m.Id = Convert.ToInt32(serviceValues[0]);
        //    m.Name = serviceValues[1];
        //    m.MenuPath = serviceValues[5];
        //    m.TotalAmount = Convert.ToDouble(serviceValues[13]);
        //}

        //public void GetMarketPrices()
        //{

        //    var CacheLayer = cache.Content;
        //    foreach (var observerMarket in cache.Content.ObserverMarkets)
        //    {
        //        var servicePrices = exchangeService.getMarketPrices(new GetMarketPricesReq() { header = exchangeHeader, marketId = observerMarket.fk_market });
        //        var dbAdapter = new DataLayerTableAdapters.SelectionsTableAdapter();
        //        dbAdapter.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["botfairConnectionString"].ConnectionString;

        //        var pricesAdpater = new DataLayerTableAdapters.PricesTableAdapter();
        //        pricesAdpater.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["botfairConnectionString"].ConnectionString;

        //        if (observerMarket.initialized)
        //        {
        //            //servicePrices.marketPrices.runnerPrices[0].
        //        }
        //        else
        //        {
        //            foreach (var runnerPrice in servicePrices.marketPrices.runnerPrices)
        //            {

        //                var selection = CacheLayer.Selections.FindBymarketIdselectionId(observerMarket.fk_market, runnerPrice.selectionId);
        //                if (selection == null)
        //                {
        //                    dbAdapter.Insert(observerMarket.fk_market, runnerPrice.selectionId, runnerPrice.totalAmountMatched, runnerPrice.lastPriceMatched);
        //                    var newRow = CacheLayer.Selections.NewSelectionsRow();
        //                    newRow.marketId = observerMarket.fk_market;
        //                    newRow.selectionId = runnerPrice.selectionId;
        //                    newRow.totalAmoutMatched = runnerPrice.totalAmountMatched;
        //                    newRow.lastPriceMatched = runnerPrice.lastPriceMatched;
        //                    CacheLayer.Selections.Rows.Add(newRow);
        //                }
        //                else
        //                {

        //                    selection.lastPriceMatched = runnerPrice.lastPriceMatched;
        //                    selection.totalAmoutMatched = runnerPrice.totalAmountMatched;
        //                    dbAdapter.Update(selection);


        //                    foreach (var price in runnerPrice.bestPricesToBack)
        //                    {

        //                        DateTime now = DateTime.Now;
        //                        pricesAdpater.Insert(observerMarket.fk_market, runnerPrice.selectionId, now, price.price, price.amountAvailable, false);
        //                        var newRow = CacheLayer.Prices.NewPricesRow();
        //                        newRow.fk_market = observerMarket.fk_market;
        //                        newRow.timestamp = now;
        //                        newRow.fk_selection = runnerPrice.selectionId;
        //                        newRow.price = price.price;
        //                        newRow.amount = price.amountAvailable;
        //                        cache.Content.Prices.AddPricesRow(newRow);
        //                    }

        //                    foreach (var price in runnerPrice.bestPricesToLay)
        //                    {
        //                        DateTime now = DateTime.Now;
        //                        pricesAdpater.Insert(observerMarket.fk_market, runnerPrice.selectionId, now, price.price, price.amountAvailable, true);
        //                        var newRow = CacheLayer.Prices.NewPricesRow();
        //                        newRow.fk_market = observerMarket.fk_market;
        //                        newRow.fk_selection = runnerPrice.selectionId;
        //                        newRow.timestamp = now;
        //                        newRow.price = price.price;
        //                        newRow.amount = price.amountAvailable;
        //                        cache.Content.Prices.AddPricesRow(newRow);
        //                    }

        //                }


        //            }

        //            CacheLayer.Selections.AcceptChanges();
        //            CacheLayer.Prices.AcceptChanges();
        //        }


        //    }
        //    messages("prices scan done");


        //}

        //public void GetMarketPrices2(int marketId)
        //{

        //    var CacheLayer = cache.Content;
        //    var market = CacheLayer.Markets.FindByid(marketId);

        //    var servicePrices = exchangeService.getMarketPrices(new GetMarketPricesReq() { header = exchangeHeader, marketId = market.id });
        //    var dbAdapter = new DataLayerTableAdapters.SelectionsTableAdapter();

        //    dbAdapter.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["botfairConnectionString"].ConnectionString;

        //    var pricesAdpater = new DataLayerTableAdapters.Prices2TableAdapter();
        //    pricesAdpater.Connection.ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["botfairConnectionString"].ConnectionString;


        //    if (servicePrices.marketPrices != null)
        //    {

        //        foreach (var runnerPrice in servicePrices.marketPrices.runnerPrices)
        //        {

        //            var selection = CacheLayer.Selections.FindBymarketIdselectionId(market.id, runnerPrice.selectionId);
        //            if (selection == null)
        //            {
        //                dbAdapter.Insert(market.id, runnerPrice.selectionId, runnerPrice.totalAmountMatched, runnerPrice.lastPriceMatched);
        //                var newRow = CacheLayer.Selections.NewSelectionsRow();
        //                newRow.marketId = market.id;
        //                newRow.selectionId = runnerPrice.selectionId;
        //                newRow.totalAmoutMatched = runnerPrice.totalAmountMatched;
        //                newRow.lastPriceMatched = runnerPrice.lastPriceMatched;

        //                CacheLayer.Selections.Rows.Add(newRow);
        //            }
        //            else
        //            {

        //                selection.lastPriceMatched = runnerPrice.lastPriceMatched;
        //                selection.totalAmoutMatched = runnerPrice.totalAmountMatched;
        //                dbAdapter.Update(selection);


        //                foreach (var price in runnerPrice.bestPricesToBack)
        //                {


        //                    var priceFromCache = cache.Content.Bets.FindByfk_marketfk_selection(market.id, runnerPrice.selectionId);
        //                    if (priceFromCache == null)
        //                    {
        //                        pricesAdpater.Insert(market.id, runnerPrice.selectionId, price.amountAvailable, false, price.price, price.price);
        //                        var newRow = cache.Content.Bets.NewBetsRow();
        //                        newRow.fk_market = market.id;
        //                        newRow.isLay = false;
        //                        newRow.fk_selection = runnerPrice.selectionId;
        //                        newRow.firstPrice = price.price;
        //                        newRow.currentPrice = price.price;
        //                        newRow.amount = price.amountAvailable;
        //                        cache.Content.Bets.AddBetsRow(newRow);
        //                    }
        //                    else
        //                    {
        //                        priceFromCache.currentPrice = price.price;
        //                        priceFromCache.amount = price.amountAvailable;
        //                    }
        //                }

        //                foreach (var price in runnerPrice.bestPricesToLay)
        //                {
        //                    var priceFromCache = cache.Content.Bets.FindByfk_marketfk_selection(market.id, runnerPrice.selectionId);
        //                    if (priceFromCache == null)
        //                    {
        //                        pricesAdpater.Insert(market.id, runnerPrice.selectionId, price.amountAvailable, true, price.price, price.price);
        //                        var newRow = cache.Content.Bets.NewBetsRow();
        //                        newRow.fk_market = market.id;
        //                        newRow.isLay = true;
        //                        newRow.fk_selection = runnerPrice.selectionId;
        //                        newRow.firstPrice = price.price;
        //                        newRow.currentPrice = price.price;
        //                        newRow.amount = price.amountAvailable;
        //                        cache.Content.Bets.AddBetsRow(newRow);
        //                    }
        //                    else
        //                    {
        //                        priceFromCache.currentPrice = price.price;
        //                        priceFromCache.amount = price.amountAvailable;
        //                    }
        //                }

        //            }


        //            CacheLayer.Selections.AcceptChanges();
        //            CacheLayer.Prices.AcceptChanges();
        //            cache.Content.Bets.AcceptChanges();
        //        }
        //    }


        //    messages("prices scan done");


        //}


        //private Price GetBestPrice(Price[] prices)
        //{
        //    Price foundPrice = null;
        //    foreach (Price p in prices)
        //    {
        //        if (p.depth == 1) foundPrice 
        //    }
        //}

        public void Run()
        {
            BotDataSet.ConfigurationRow configRow = cache.GetActiveConfiguration();
            if (configRow == null)
            {
                Trace.TraceWarning("no bot configuration defined!");
                return;
            }

            int hotMarketsSeconds = configRow.hotMarketsSeconds;
            int riskValue = configRow.riskValue;
            double percentage = configRow.percentage/100;


            var markets = cache.Content.Markets.ToArray();

            var horseRaceQuery =
                from market in markets
                where
                    market.eventType == (int) Enums.Betfair.EventTypes.HorseRacing &&
                    market.winners == 1 &&
                    market.totalAmount > 0 &&
                    market.eventDate < DateTime.Now.AddSeconds(hotMarketsSeconds) &&
                    market.eventDate > DateTime.Now &&
                    market.marketStatus.ToLower() == "active" &&
                    market.isHot == false &&
                    market.turningInPlay &&
                    market.bspMarket
                select market;

            try
            {
                foreach (var market in horseRaceQuery)
                {
                    //market.AcceptChanges(); // set the row state to unmodified

                    new Prices().GetMarketPrices(market.id); // wird nur hier verwendet
                    var bestSelection =
                        from selections in cache.Content.Selections
                        where selections.marketId == market.id
                        orderby selections.lastPriceMatched ascending
                        select selections;

                    if (bestSelection.Count() == 0)
                        Trace.TraceInformation("no best selection can be found for market id =" + market.id);
                    else
                    {
                        var topCandidate = bestSelection.First();
                        var topCandidates = bestSelection.Take(3);
                        int position = 1;
                        foreach (var candidate in topCandidates)
                        {
                            candidate.tracked = true;
                            candidate.position = position;
                            position++;
                        }

                        cache.CommitSelections();

                        var betQ =
                            from prices in cache.Content.Bets
                            where prices.fk_market == market.id && prices.fk_selection == topCandidate.selectionId
                            select prices;


                        if (betQ.Count() != 2)
                        {
                            Trace.TraceWarning("Bet will not be done: no bets available; marketid=" + market.id +
                                               ";selectionid=" +
                                               topCandidate.selectionId);
                        }
                        else
                        {
                            var q1 = betQ.Where(p => p.isLay);
                            var q2 = betQ.Where(p => p.isLay == false);


                            var layBet = q1.First();
                            var backBet = q2.First();

                            if (layBet.currentPrice > 10)
                            {
                                Trace.TraceWarning("Bet will not be done;laysize>10;marketid=" + layBet.fk_market +
                                                   ";selectionid=" + layBet.fk_selection);
                            }
                            else
                            {
                                double laySize = riskValue/(layBet.currentPrice - 1);
                                laySize = Math.Round(laySize, 2);

                                var serviceLayBet = PrepareServiceBet(market.id, layBet.fk_selection,
                                                                      layBet.currentPrice,
                                                                      laySize, true);
                                var serviceBackBet = PrepareServiceBet(market.id, layBet.fk_selection,
                                                                       backBet.currentPrice,
                                                                       laySize, false);
                                double backSize = laySize - laySize*(serviceLayBet.price - 1)*percentage;
                                double backPrize = (laySize*(serviceLayBet.price - 1) +
                                                    laySize*(serviceLayBet.price - 1)*percentage)/backSize + 1;

                                serviceBackBet.size = backSize;
                                serviceBackBet.price = backPrize;

                                serviceBackBet.size = Math.Round(backSize, 2);

                                bool priceIsOk, tmp;
                                serviceBackBet.price = PreparePrice(backPrize, out tmp);
                                priceIsOk = tmp;
                                serviceLayBet.price = PreparePrice(serviceLayBet.price, out tmp);
                                priceIsOk = priceIsOk && tmp;

                                if (!priceIsOk)
                                {
                                    Trace.TraceWarning("Bet will not be done;invalid bet prize;marketid=" +
                                                       layBet.fk_market + ";selectionid=" + layBet.fk_selection);
                                }
                                else
                                {
                                    PrepareDbBet(layBet, serviceLayBet);
                                    PrepareDbBet(backBet, serviceBackBet);


                                    try
                                    {
                                        Sync.syncMarkets.WaitUpdate();
                                        market.isHot = true;
                                    }
                                    finally
                                    {
                                        Sync.syncMarkets.ReleaseUpdate();
                                    }


                                    var bets = new[] {serviceLayBet, serviceBackBet};
                                    var dbRows = new[] {layBet, backBet};

                                    //Exception responseException = null;
                                    //BetFairExchange.PlaceBetsResp response = null;
                                    //try
                                    //{
                                    //    response = proxy.PlaceBet(bets);
                                    //}
                                    //catch (Exception ex)
                                    //{
                                    //    responseException = ex;

                                    //}
                                    //finally
                                    //{
                                    //    HandleBetResponse(response, bets, dbRows, responseException);
                                    //}

                                    //                    Layeinsatz: 10
                                    //Lay: Einkaufsquote von Lay
                                    //Gewinn: %, wieviel wir pro Wette gewinnen wollen --> 0,04

                                    //Backeinsatz=Layeinsatz-Layeinsatz*(Lay-1)xGewinn
                                    //Backquote= (Layeinsatz*(Lay-1)+Layeinsatz*(Lay-1)*Gewinn)/Backeinsatz + 1
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                cache.CommitMarkets();
                cache.CommitBets();
            }
        }

        private void HandleBetResponse(BetFairExchange.PlaceBetsResp response, BetFairExchange.PlaceBets[] bets,
                                       BotDataSet.BetsRow[] dbRows, Exception ex)
        {
            if (ex != null)
            {
                PrepareDbBetResults(dbRows, "EXCEPTION: " + ex.ToString().Substring(0, 200), 0);
            }
            else
            {
                if (response.betResults != null)
                {
                    if (response.betResults.Length != bets.Length)
                        PrepareDbBetResults(dbRows, "Botfair_INVALID_BETRESULTS", 0);
                    else
                    {
                        foreach (var betResult in response.betResults)
                        {
                            PrepareDbBetResults(dbRows, betResult.resultCode.ToString(), betResult.betId);
                        }
                    }
                }
                else
                {
                    if (response.header.errorCode != BetFairExchange.APIErrorEnum.OK)
                        PrepareDbBetResults(dbRows, response.header.errorCode.ToString(), 0);
                    else
                    {
                        if (response.errorCode != BetFairExchange.PlaceBetsErrorEnum.OK)
                            PrepareDbBetResults(dbRows, response.errorCode.ToString(), 0);
                        else PrepareDbBetResults(dbRows, "POST WAS OK BUT NO BETRESULTS!", 0);
                    }
                }
            }
        }

        private void PrepareDbBetResults(BotDataSet.BetsRow[] rows, string errorCode, long id)
        {
            foreach (var row in rows)
            {
                row.errorCode = errorCode;
                row.betFairId = id;
            }
        }

        public double PreparePrice(double price, out bool ok)
        {
            //                    1.01 - 2.00 / 0.01
            //2.02 - 3.00 / 0.02
            //3.05 - 4.00 / 0.05
            //4.10 - 6.00 / 0.10
            //6.20 - 10 / 0.20
            //10.5 - 20 / 0.50

            double beforeFraction = 0;
            double fractionValue = 0;
            double p = Math.Round(price, 2);
            try
            {
                beforeFraction = Convert.ToInt32(Math.Round(price, 0));
                fractionValue = Convert.ToInt32((p - Math.Round(price, 0))*100);
            }
            catch (Exception ex)
            {
                Trace.TraceError("price=" + price);
            }

            //TODO: calculate firstDigit of fraction mit mod 10!
            if (price > 2.00 && price < 3.00)
            {
                var x = fractionValue%2;
                bool y = x == 0;
                if (!y)
                {
                    p = p + 0.011;
                    p = Math.Round(p, 2);
                }
            }
            else if (price > 3.00 && price < 4.00)
            {
                double firstDigitOfFraction = (fractionValue/10 - (int) fractionValue/10)*10;
                double d = firstDigitOfFraction%5;
                if (d > 2) p = p + (d - 1)/100;
                else p = p - d/100;

                p = Math.Round(p, 2);
            }

            else if (price > 4.00 && price < 6.00)
            {
                double firstDigitOfFraction = (fractionValue/10 - (int) fractionValue/10)*10;
                double d = firstDigitOfFraction;
                if (d > 4) p = p + (10 - firstDigitOfFraction)/100;
                else p = p - d/100;

                p = Math.Round(p, 2);
            }
            else if (price > 6.00 && price < 10.00)
            {
                double first2DigitsOfFraction = fractionValue%100;
                double d = first2DigitsOfFraction%20;
                if (d > 9) p = p + (20 - first2DigitsOfFraction)/100;
                else p = p - d/100;

                p = Math.Round(p, 2);
            }
            else if (price > 10.00 && price < 20.00)
            {
                double first2DigitsOfFraction = fractionValue%100;
                double d = first2DigitsOfFraction%50;
                if (d > 24) p = p + (50 - first2DigitsOfFraction)/100;
                else p = p - d/100;

                p = Math.Round(p, 2);
            }

            ok = price <= 20.00;

            return p;
        }

        private void PrepareDbBet(BotDataSet.BetsRow row, BetFairExchange.PlaceBets bet)
        {
            row.datePosted = DateTime.Now;
            row.fk_market = bet.marketId;
            row.fk_selection = bet.selectionId;
            row.isLay = bet.betType == BetFairExchange.BetTypeEnum.L;
            row.pricePosted = bet.price;
            row.sizePosted = (double) bet.size;
        }

        private BetFairExchange.PlaceBets PrepareServiceBet(int marketId, int selectionId, double quote, double size,
                                                            bool isLay)
        {
            var bet = new BetFairExchange.PlaceBets();
            bet.marketId = marketId;
            bet.selectionId = selectionId;
            if (isLay) bet.betType = BetFairExchange.BetTypeEnum.L;
            else bet.betType = BetFairExchange.BetTypeEnum.B;
            bet.betCategoryType = BetFairExchange.BetCategoryTypeEnum.E;
            bet.betPersistenceType = BetFairExchange.BetPersistenceTypeEnum.IP;
            bet.size = size;
            bet.price = quote;
            bet.bspLiability = 0;

            return bet;
        }
    }
}