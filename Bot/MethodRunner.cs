using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BotFair
{
    class MethodRunner
    {
        private List<MethodRun> methods;
        private DateTime firstPingTime=DateTime.MinValue;
 
        class MethodRun
        {
            public bool FirstRunDone { get; set; }
            public int FirstStartAfter { get; set; }
            public MethodInfo MethodToRun { get; set; }
            public object RelatedObject { get; set; }
            public DateTime TimeStamp { get; set; }
            public int Interval { get; set; }
        }

        public MethodRunner()
        {
            methods = new List<MethodRun>();
        }

        public void AddMethod(object o,MethodInfo m, int firstStartAfter, int interval)
        {
            methods.Add(new MethodRun()
                {
                    FirstStartAfter = firstStartAfter,
                    Interval = interval,
                    MethodToRun = m,
                    TimeStamp = DateTime.MinValue,
                    FirstRunDone = false,
                    RelatedObject = o
                });
         
        }

        public void Ping()
        {
            if (firstPingTime == DateTime.MinValue) firstPingTime = DateTime.Now;

            foreach (var m in methods)
            {
                if (IsPending(m))
                {
                    m.MethodToRun.Invoke(m.RelatedObject, BindingFlags.Default, null, null, CultureInfo.CurrentCulture);
                    m.TimeStamp = DateTime.Now;
                    m.FirstRunDone = true;
                }
            }
        }

        private bool IsPending(MethodRun m)
        {
            if (m.FirstRunDone)
            {
                var timeElapsed = DateTime.Now.Subtract(m.TimeStamp);
                return timeElapsed.Seconds >= m.Interval;
            }
            else
            {
                var timeElapsed = DateTime.Now.Subtract(firstPingTime);
                return timeElapsed.Seconds >= m.FirstStartAfter;
            }
        }
    }
}
