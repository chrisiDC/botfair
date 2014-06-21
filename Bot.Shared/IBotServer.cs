using System.Data;

namespace Bot.Shared
{
    public enum BotState
    {
        STARTED,
        STOPPED,
        STOPPING,
        STARTING
    };

    public interface IBotServer
    {
        void ShutDown();

        void Start();

        int GetState();

        bool ChangeConfiguration(int configurationId);

        int GetActiveConfiguration();

        DataTable GetCacheContent(string name,string orderBy);
    }
}