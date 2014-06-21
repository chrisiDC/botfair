using System;
using BotFair.BetFairExchange;
using BotFair.BetFairGlobal;
using BotFair.Services;

namespace BotFair.BusinessLayer
{
    public class Account : BusinessObject
    {
        public Account() : base(AppCache.Instance, new BFGlobalServiceClient(), new BFExchangeServiceClient())
        {
        }

        public Account(IAppCache cache, BFGlobalServiceClient globalService, BFExchangeServiceClient exchangeService)
            : base(cache, globalService, exchangeService)
        {
        }

        
        public void Login()
        {
            var loginRequest = new BetFairGlobal.LoginReq
                                   {
                                       username = System.Configuration.ConfigurationManager.AppSettings["betfairuser"],
                                       password = System.Configuration.ConfigurationManager.AppSettings["betfairpassword"],
                                       productId = 82
                                   };


            var response = globalService.login(loginRequest);

            if (response.errorCode == LoginErrorEnum.OK)
            {
                exchangeHeader = new BetFairExchange.APIRequestHeader {sessionToken = response.header.sessionToken};
                globalHeader = new BetFairGlobal.APIRequestHeader {sessionToken = response.header.sessionToken};
            }
            else throw new Exception("login failed:" + response.errorCode);
        }

        public void Logout()
        {
            var logoutRequest = new BetFairGlobal.LogoutReq();
            logoutRequest.header = globalHeader;
            globalService.logout(logoutRequest);
        }
    }
}