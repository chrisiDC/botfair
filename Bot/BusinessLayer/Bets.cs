using BotFair.BetFairExchange;
using BotFair.BetFairGlobal;
using BotFair.Services;

namespace BotFair.BusinessLayer
{
    public class Bets : BusinessObject
    {
        public Bets()
            : base(AppCache.Instance, new BFGlobalServiceClient(), new BFExchangeServiceClient())
        {
        }

        public Bets(IAppCache cache, BFGlobalServiceClient globalService, BFExchangeServiceClient exchangeService)
            : base(cache, globalService, exchangeService)
        {
        }

        public PlaceBetsResp PlaceBet(PlaceBets[] bets)
        {
            var request = new BetFairExchange.PlaceBetsReq
                              {
                                  header = exchangeHeader,
                                  bets = bets
                              };

            var response = exchangeService.placeBets(request);

            return response;
        }
    }
}