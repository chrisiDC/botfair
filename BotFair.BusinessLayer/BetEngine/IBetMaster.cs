using System.Collections.Generic;
using BotFair.DataLayer;
using BotFair.DataLayer.BetFairExchange;

namespace BotFair.BusinessLayer.BetEngine
{
    public interface IBetMaster
    {
        List<PlaceBets> Run(Configuration config, BotDataSet.MarketsRow marketInfo, IEnumerable<BotDataSet.SelectionsRow> selections,
                            IEnumerable<BotDataSet.PriceTrackRow> prices, IEnumerable<BotDataSet.BetsRow> placedBets);

        int GetInterval();

        int GetMasterId();

        int GetMarketId();
    }
}
