using System;
namespace BotFair
{
    interface IBetFairProxy
    {
        void GetMarketPrices(int marketId);
        void Login();
        void Logout();
        BotFair.BetFairExchange.PlaceBetsResp PlaceBet(BotFair.BetFairExchange.PlaceBets[] bets);
        void RefreshSession();
        void ScanNewMarkets();
        void TrackPrices();
        void UpdateExistingMarkets();
    }
}
