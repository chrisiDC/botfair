


namespace BotFair.Services
{
    public interface IAppCache
    {
        BotDataSet Content { get;  }
        void RemoveMarket(BotDataSet.MarketsRow market);
        void CommitMarkets();
        void CommitSelections();
        void CommitBets();
        void CommitPriceTrack();
        BotDataSet.ConfigurationRow GetActiveConfiguration();
        bool ChangeConfiguration(int configurationId);
        void Load();
    }
}