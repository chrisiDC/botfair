using System.Threading;

namespace BotFair.Sys
{
    public class CommitSync
    {
        private object sync = new object();
        private object commitSync = new object();
        private bool commit;

        private int i;

        public void WaitCommit()
        {
            lock (sync)
            {
                Monitor.Enter(commitSync);
                commit = true;
                while (i != 0) Thread.Sleep(20);
            }
        }

        public void ReleaseCommit()
        {
            commit = false;
            Monitor.Exit(commitSync);
        }

        public void WaitUpdate()
        {
            lock (sync)
            {
                while (commit) Thread.Sleep(20);
                i++;
            }
        }

        public void ReleaseUpdate()
        {
            i--;
        }
    }
}