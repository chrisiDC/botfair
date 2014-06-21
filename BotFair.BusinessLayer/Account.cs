using System;
using BotFair.DataLayer.BetFairExchange;
using BotFair.DataLayer.BotFairGlobal;


namespace BotFair.BusinessLayer
{
    public class Account : EntityBase
    {
       
        private BFGlobalService globalService;
        private DataLayer.BetFairExchange.APIRequestHeader exchangeHeader;
        private DataLayer.BotFairGlobal.APIRequestHeader globalHeader;
        public Account() : base()
        {      
            globalService = new BFGlobalServiceClient();
        }
        //username = System.Configuration.ConfigurationManager.AppSettings["betfairuser"],
        //                           password = System.Configuration.ConfigurationManager.AppSettings["betfairpassword"],

       
        public void Login(string userName,string password)
        {
            var loginRequest = new loginIn(new LoginReq()
                                               {

                                                   username = userName,
                                                   password = password,
                                                   productId = 82
                                               });


            var response = globalService.login(loginRequest);

            if (response.Result.errorCode == LoginErrorEnum.OK)
            {
                exchangeHeader = new  DataLayer.BetFairExchange.APIRequestHeader{sessionToken = response.Result.header.sessionToken};
                globalHeader = new DataLayer.BotFairGlobal.APIRequestHeader(){sessionToken = response.Result.header.sessionToken};
            }
            else throw new Exception("login failed:" + response.Result.errorCode);
        }

        public void Logout()
        {
            var logoutRequest = new LogoutReq();
            logoutRequest.header = globalHeader;
            globalService.logout(logoutRequest);
        }

        public DataLayer.BetFairExchange.APIRequestHeader GetExchangeHeader()
        {
            return exchangeHeader;
        }

        internal DataLayer.BotFairGlobal.APIRequestHeader GetGlobalHeader()
        {
            return globalHeader;
        }

        protected override void Init()
        {
          
        }
    }
}