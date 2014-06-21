//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using BotFair;
//using System.Diagnostics;
//using BotFair.Tools;

//namespace BotFair
//{
//    public class BetEngine
//    {
//        private BetFairProxy proxy;

//        public BetEngine(BetFairProxy proxy)
//        {
//            this.proxy = proxy;
//        }

//        public void Run()
//        {



//            DataLayer.ConfigurationRow configRow = Cache.Instance.GetActiveConfiguration();
//            if (configRow == null)
//            {
//                Trace.TraceWarning("no bot configuration defined!");
//                return;
//            }

//            int hotMarketsSeconds = configRow.hotMarketsSeconds;
//            int riskValue = configRow.riskValue;
//            double percentage = configRow.percentage / 100;


//            var markets = Cache.Instance.Markets.ToArray();

//            var horseRaceQuery =
//                from market in markets
//                where
//                    market.eventType == (int)Enums.Betfair.EventTypes.HorseRacing &&
//                    market.winners == 1 &&
//                    market.totalAmount > 0 &&
//                    market.eventDate < DateTime.Now.AddSeconds(hotMarketsSeconds) &&
//                    market.eventDate > DateTime.Now &&
//                    market.marketStatus.ToLower() == "active" &&
//                    market.isHot == false &&
//                    market.turningInPlay == true &&
//                    market.bspMarket == true
//                select market;

//            try
//            {
//                foreach (var market in horseRaceQuery)
//                {
//                    //market.AcceptChanges(); // set the row state to unmodified

//                    proxy.GetMarketPrices(market.id); // wird nur hier verwendet
//                    var bestSelection =
//                        from selections in Cache.Instance.Selections
//                        where selections.marketId == market.id
//                        orderby selections.lastPriceMatched ascending
//                        select selections;

//                    if (bestSelection.Count() == 0) Trace.TraceInformation("no best selection can be found for market id =" + market.id);
//                    else
//                    {
//                        var topCandidate = bestSelection.First();
//                        var topCandidates = bestSelection.Take(3);
//                        int position = 1;
//                        foreach (var candidate in topCandidates)
//                        {
//                            candidate.tracked = true;
//                            candidate.position = position;
//                            position++;
//                        }

//                        Cache.Instance.CommitSelections();

//                        var betQ =
//                            from prices in Cache.Instance.Bets
//                            where prices.fk_market == market.id && prices.fk_selection == topCandidate.selectionId
//                            select prices;


//                        if (betQ.Count() != 2)
//                        {
//                            Trace.TraceWarning("Bet will not be done: no bets available; marketid=" + market.id + ";selectionid=" +
//                                               topCandidate.selectionId);
//                        }
//                        else
//                        {
//                            var q1 = betQ.Where(p => p.isLay == true);
//                            var q2 = betQ.Where(p => p.isLay == false);


//                            var layBet = q1.First();
//                            var backBet = q2.First();

//                            if (layBet.currentPrice > 10)
//                            {
//                                Trace.TraceWarning("Bet will not be done;laysize>10;marketid=" + layBet.fk_market + ";selectionid=" + layBet.fk_selection);
//                            }
//                            else
//                            {

//                                double laySize = riskValue / (layBet.currentPrice - 1);
//                                laySize = Math.Round(laySize, 2);

//                                var serviceLayBet = PrepareServiceBet(market.id, layBet.fk_selection,
//                                                                      layBet.currentPrice,
//                                                                      laySize, true);
//                                var serviceBackBet = PrepareServiceBet(market.id, layBet.fk_selection,
//                                                                       backBet.currentPrice,
//                                                                       laySize, false);
//                                double backSize = laySize - laySize * (serviceLayBet.price - 1) * percentage;
//                                double backPrize = (laySize * (serviceLayBet.price - 1) +
//                                                    laySize * (serviceLayBet.price - 1) * percentage) / backSize + 1;

//                                serviceBackBet.size = backSize;
//                                serviceBackBet.price = backPrize;

//                                serviceBackBet.size = Math.Round(backSize, 2);

//                                bool priceIsOk, tmp;
//                                serviceBackBet.price = PreparePrice(backPrize, out tmp);
//                                priceIsOk = tmp;
//                                serviceLayBet.price = PreparePrice(serviceLayBet.price, out tmp);
//                                priceIsOk = priceIsOk && tmp;

//                                if (!priceIsOk)
//                                {
//                                    Trace.TraceWarning("Bet will not be done;invalid bet prize;marketid=" + layBet.fk_market + ";selectionid=" + layBet.fk_selection);
//                                }
//                                else
//                                {



//                                    PrepareDbBet(layBet, serviceLayBet);
//                                    PrepareDbBet(backBet, serviceBackBet);


//                                    try
//                                    {
//                                        Sync.syncMarkets.WaitUpdate();
//                                        market.isHot = true;
                                        
//                                    }
//                                    finally
//                                    {
//                                        Sync.syncMarkets.ReleaseUpdate();
//                                    }


//                                    var bets = new BetFairExchange.PlaceBets[] { serviceLayBet, serviceBackBet };
//                                    var dbRows = new DataLayer.BetsRow[] { layBet, backBet };

//                                    //Exception responseException = null;
//                                    //BetFairExchange.PlaceBetsResp response = null;
//                                    //try
//                                    //{
//                                    //    response = proxy.PlaceBet(bets);
//                                    //}
//                                    //catch (Exception ex)
//                                    //{
//                                    //    responseException = ex;

//                                    //}
//                                    //finally
//                                    //{
//                                    //    HandleBetResponse(response, bets, dbRows, responseException);
//                                    //}

//                                    //                    Layeinsatz: 10
//                                    //Lay: Einkaufsquote von Lay
//                                    //Gewinn: %, wieviel wir pro Wette gewinnen wollen --> 0,04

//                                    //Backeinsatz=Layeinsatz-Layeinsatz*(Lay-1)xGewinn
//                                    //Backquote= (Layeinsatz*(Lay-1)+Layeinsatz*(Lay-1)*Gewinn)/Backeinsatz + 1
//                                }
//                            }
//                        }
//                    }
//                }
//            }
//            finally
//            {
//                Cache.Instance.CommitMarkets();
//                Cache.Instance.CommitBets();
//            }


//        }

       
//    }
//}
