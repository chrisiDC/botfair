using System;
using BotFair.BetFairExchange;

namespace BotFair.BusinessLayer
{
    public class ServiceDataMapper
    {
        public static void MapNewMarket(BotDataSet.MarketsRow row, int eventType, string[] data)
        {
            int id = Convert.ToInt32(data[0]);

            row.id = id;
            row.name = data[1];
            row.menuPath = data[5];
            row.type = data[2];
            row.eventType = eventType;
            row.eventHierarchy = data[6];
            row.country = data[9];

            DateTime unixEpoch = new DateTime(1970, 1, 1);
            double timeToAdd = Convert.ToDouble(data[4]);

            if (timeToAdd > 0)
            {
                row.eventDate = unixEpoch.AddMilliseconds(timeToAdd);
                row.eventDate = row.eventDate.AddHours(2); //GMT+2
            }
            row.isHot = false;

            MapExistingMarket(row, eventType, data);
        }

        public static void MapExistingMarket(BotDataSet.MarketsRow row, int eventType, string[] data)
        {
            row.marketStatus = data[3];
            row.interval = 0;
            row.totalAmount = Convert.ToDouble(data[13]);

            row.betDelay = data[7];
            row.runners = Convert.ToInt32(data[11]);
            row.winners = Convert.ToInt32(data[12]);
            row.bspMarket = data[14] == "Y";
            row.turningInPlay = data[15] == "Y";
        }

        public static void MapExistingBet(BotDataSet.BetsRow row, int marketId, int selectionId, Price p, bool isLay)
        {
            row.currentPrice = p.price;
            row.amount = p.amountAvailable;
        }

        public static void MapNewBet(BotDataSet.BetsRow row, int marketId, int selectionId, Price p, bool isLay)
        {
            row.fk_market = marketId;
            row.isLay = isLay;
            row.fk_selection = selectionId;
            row.firstPrice = p.price;
            row.amount = p.amountAvailable;
            row.betFairId = 0;
            row.datePosted = DateTime.Parse("1.1.1970");
            row.errorCode = "";
            row.pricePosted = 0;
            row.sizePosted = 0;

            MapExistingBet(row, marketId, selectionId, p, isLay);
        }
    }
}