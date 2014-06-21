using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace BotFair.BusinessLayer
{
    public class PriceTrack : EntityBase
    {
        public void RemoveMarket(int marketId)
        {

            cache.RemoveMarketTrackFromCache(marketId);
        }

        protected override void Init()
        {

        }
    }
}
