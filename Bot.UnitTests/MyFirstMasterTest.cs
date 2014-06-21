using BotFair.BusinessLayer.BetEngine.Masters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using BotFair.BusinessLayer;
using BotFair.DataLayer;
using System.Collections.Generic;
using BotFair.DataLayer.BetFairExchange;

namespace Bot.UnitTests
{
    
    
    /// <summary>
    ///This is a test class for MyFirstMasterTest and is intended
    ///to contain all MyFirstMasterTest Unit Tests
    ///</summary>
    [TestClass()]
    public class MyFirstMasterTest
    {


        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for Run
        ///</summary>
        [TestMethod()]
        public void RunTest()
        {
            int marketId = 0;  
            int betMasterId = 1;  
            MyFirstMaster target = new MyFirstMaster(marketId, betMasterId);  




            Configuration config = new Configuration()
                                       {
                                           HotMarketsSeconds = 60,
                                           NewMarketsPeriod = 120,
                                           Percentage = 4,
                                           RiskValue = 100
                                       };
            BotDataSet.MarketsRow marketInfo = new BotDataSet.MarketsDataTable().NewMarketsRow();
            marketInfo.isHot = true;
    
            var selection1 = new BotDataSet.SelectionsDataTable().NewSelectionsRow();
            selection1.selectionId = 1;
            selection1.tracked = true;
            selection1.marketId = marketId;

            var priceTrackRow1 = new BotDataSet.PriceTrackDataTable().NewPriceTrackRow();
            priceTrackRow1.fk_market = marketId;
            priceTrackRow1.fk_selection = selection1.selectionId;
            priceTrackRow1.price = 1.0;
            priceTrackRow1.priceDate = DateTime.Now;

            IEnumerable<BotDataSet.SelectionsRow> selections = new BotDataSet.SelectionsRow[]{selection1};  
            IEnumerable<BotDataSet.PriceTrackRow> prices = new BotDataSet.PriceTrackRow[]{priceTrackRow1};  
            IEnumerable<BotDataSet.BetsRow> placedBets = null;  

            List<PlaceBets> expected = null;  
            List<PlaceBets> results = target.Run(config, marketInfo, selections, prices, placedBets);

            Assert.IsTrue(results.Count > 0);
          
        }
    }
}
