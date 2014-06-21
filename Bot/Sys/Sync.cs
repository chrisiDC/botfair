namespace BotFair.Sys
{
    internal static class Sync
    {
        public static CommitSync syncMarkets = new CommitSync();
        public static CommitSync syncBets = new CommitSync();
        public static CommitSync syncSelections = new CommitSync();

        public static object configSync = new object();
    }
}