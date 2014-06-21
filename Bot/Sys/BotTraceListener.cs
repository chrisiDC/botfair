using System;
using System.Diagnostics;

namespace BotFair.Sys
{
    public class BotTraceListener : DefaultTraceListener
    {
        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id,
                                        string message)
        {
            TraceEvent(eventCache, source, eventType, id, null, message);
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id,
                                        string format, params object[] args)
        {
            if (args == null) return;
            string message = null;

            Exception ex = args[0] as Exception;
            if (ex != null)
            {
                message = ex.ToString();

                if (ex.InnerException != null) message+= "; INNER EXCEPTION:" +ex.InnerException.ToString();
                 
            }

            else message = (string) args[0];

            Console.WriteLine("{0}",message);
            new BusinessLayer.Log().LogEvent(id,Enum.GetName(typeof (TraceEventType), eventType),message);
           
        }
    }
}