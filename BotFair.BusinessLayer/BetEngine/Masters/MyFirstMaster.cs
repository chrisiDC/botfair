using System;
using System.Collections.Generic;
using System.Linq;
using BotFair.DataLayer;
using BotFair.DataLayer.BetFairExchange;

namespace BotFair.BusinessLayer.BetEngine.Masters
{
    /// <summary>
    /// sets a back bet at beginning; then a lay bet after 3 minutes
    /// </summary>
    public class MyFirstMaster : IBetMaster
    {

        Dictionary<string, object> memory = new Dictionary<string, object>();
        private int betMasterId;
        private int marketId;

        public MyFirstMaster(int marketId, int betMasterId)
        {
            this.betMasterId = betMasterId;
            this.marketId = marketId;
        }


        public List<PlaceBets> Run(Configuration config, BotDataSet.MarketsRow marketInfo, IEnumerable<BotDataSet.SelectionsRow> selections, IEnumerable<BotDataSet.PriceTrackRow> prices, IEnumerable<BotDataSet.BetsRow> placedBets)
        {
            if (!memory.ContainsKey("done")) memory["done"] = false;
            bool done = (bool)memory["done"];
            List<PlaceBets> betsToDo = new List<PlaceBets>();


            if (!done)
            {

             
                //if (!memory.ContainsKey("firstCall")) memory["firstCall"] = DateTime.Now;
                if (!memory.ContainsKey("horse")) memory["horse"] = selections.First(item => item.position == 1);

                BotDataSet.SelectionsRow horse = (BotDataSet.SelectionsRow)memory["horse"];
                var lastPrices = GetLastPrice(prices.ToList(), horse.selectionId);
                if (lastPrices != null)
                {
                    var layPrice = lastPrices[1];
                    var backPrice = lastPrices[0];

                    if (!memory.ContainsKey("layBet"))
                    {

                        memory["layBet"] = PrepareServiceBet(marketInfo.id, horse.selectionId, layPrice, config.RiskValue, true);
                        betsToDo.Add((PlaceBets)memory["layBet"]);

                    }
                    else
                    {
                        PlaceBets layedBet = (PlaceBets)memory["layBet"];

                        //Console.WriteLine("Start layed bet price:", layedBet.price);
                        //Console.WriteLine("Start layed Bet size:", layedBet.size);
                        //Console.WriteLine("Current lay price:", layPrice);
                        //Console.WriteLine("Current back price:", backPrice);



                        if ((backPrice > layedBet.price * (1 + config.Percentage / 100)) ||
                            (DateTime.Now.AddSeconds(20) > marketInfo.eventDate))
                        {


                            var layedSize = layedBet.size != null ? (double)layedBet.size : 0;

                            double backSize = layedSize * layedBet.price / backPrice;// layedSize - layedSize * (layedBet.price - 1) * config.Percentage / 100;
                            //double backPriceToSet = (layedSize * (layedBet.price - 1) +
                            //                   layedSize * (layedBet.price - 1) * config.Percentage/100) / backSize + 1;


                            //double laySize = backedBet.size - lastPrices[1] * (backedBet.size - 1) * config.Percentage;
                            //Console.WriteLine("End Layed Bet price:", layedBet.price);
                            //Console.WriteLine("End Layed Bet price:", layedSize);
                            //Console.WriteLine("End Back Bet price:", backSize);
                            //Console.WriteLine("End Layed Bet price:", backPrice);

                            var backBet = PrepareServiceBet(marketInfo.id, horse.selectionId, backPrice, backSize, false);
                            betsToDo.Add(backBet);
                            memory["done"] = true;
                        }
                    }
                }

                //DateTime firstCall = (DateTime)memory["firstCall"];

                //if (DateTime.Now > firstCall.AddMinutes(3))
                //{

                //    if (lastPrices != null)
                //    {
                //        var layPrice = lastPrices[1];
                //        var layBet = PrepareServiceBet(marketInfo.id, horse.selectionId, layPrice, config.RiskValue, true);
                //        betsToDo.Add(layBet);
                //        memory["done"] = true;
                //    }
                //}
            }


            return betsToDo;
        }


        private PlaceBets PrepareServiceBet(int marketId, int selectionId, double quote, double size,
                                                           bool isLay)
        {
            var bet = new PlaceBets();
            bet.marketId = marketId;
            bet.selectionId = selectionId;
            if (isLay) bet.betType = BetTypeEnum.L;
            else bet.betType = BetTypeEnum.B;
            bet.betCategoryType = BetCategoryTypeEnum.E;
            bet.betPersistenceType = BetPersistenceTypeEnum.IP;
            bet.size = size;
            bet.price = quote;
            bet.bspLiability = 0;

            return bet;
        }

        private double[] GetLastPrice(List<BotDataSet.PriceTrackRow> prices, int selectionId)
        {
            try
            {
                var lastBackPrice =
                       prices.Where(item => item.fk_selection == selectionId && item.isLay == false).OrderByDescending(
                           item => item.priceDate).First();

                var lastLayPrice =
                    prices.Where(item => item.fk_selection == selectionId && item.isLay == true).OrderByDescending(
                        item => item.priceDate).First();


                return new double[] { lastBackPrice.price, lastLayPrice.price };
            }
            catch (Exception)
            {


            }
            return null;
        }


        public int GetInterval()
        {
            return 10;
        }

        public int GetMasterId()
        {
            return betMasterId;
        }

        public int GetMarketId()
        {
            return marketId;
        }

    }
}

//double laySize = riskValue / (layBet.currentPrice - 1);
//      laySize = Math.Round(laySize, 2);



