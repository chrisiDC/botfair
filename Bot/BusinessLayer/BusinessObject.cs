using BotFair.BetFairExchange;
using BotFair.BetFairGlobal;
using BotFair.Services;

namespace BotFair.BusinessLayer
{
    public class BusinessObject
    {
        protected BFGlobalServiceClient globalService;
        protected BFExchangeServiceClient exchangeService;
        protected BetFairExchange.APIRequestHeader exchangeHeader;
        protected BetFairGlobal.APIRequestHeader globalHeader;
        protected IAppCache cache;

        public BusinessObject(IAppCache cache, BFGlobalServiceClient globalService,
                              BFExchangeServiceClient exchangeService)
        {
            this.globalService = globalService;
            this.exchangeService = exchangeService;
            this.cache = cache;
        }
    }
}