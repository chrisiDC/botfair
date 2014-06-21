using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using BotFair.BusinessLayer;

namespace BotFair
{
    class CleanUpThread
    {
        private Markets m;
        private Log l;

        public CleanUpThread(Markets m,Log l)
        {
            this.m = m;
            this.l = l;
        }
        public void Start()
        {
            while (true)
            {
                new Cleanup(m,l).Run();
                Thread.Sleep(2000);
            }
        }
    }
}
