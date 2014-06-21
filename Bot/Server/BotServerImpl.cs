using System;
using System.Data;
using Bot.Shared;
using BotFair.BusinessLayer;

namespace BotFair.Server
{
    public class BotServerImpl : System.MarshalByRefObject, IBotServer
    {
        internal delegate object BotEventHandler(ServerEventArgs e);

        public static Configuration cfg;
        // private static DataLayer.ConfigurationRow configRow;

        private static object configSync = new object();
        // Declare the event.
        internal static event BotEventHandler BotEvent;

        //public static object ConfigurationSync { get { return configSync; } }

        public void ShutDown()
        {
            if (BotEvent != null)
                BotEvent.BeginInvoke(new ServerEventArgs {ServerAction = ServerEventArgs.Action.SHUTDOWN}, null, null);

            //Console.WriteLine("shutdown");
            //lock (sync)
            //{
            //    if (isRunning)
            //    {

            //        isRunning = false;
            //    }
            //}
        }

        public void Start()
        {
            if (BotEvent != null)
                BotEvent.BeginInvoke(new ServerEventArgs {ServerAction = ServerEventArgs.Action.START}, null, null);
            //Console.WriteLine("start");
            //lock (sync)
            //{
            //    if (!isRunning)
            //    {

            //        isRunning = true;
            //    }
            //}
        }

        public int GetState()
        {
            if (BotEvent == null) throw new Exception("No Bot Event registered");
            BotState state = (BotState) BotEvent(new ServerEventArgs {ServerAction = ServerEventArgs.Action.READ});


            return Convert.ToInt32(state);
        }

        public bool ChangeConfiguration(int configurationId)
        {
            return cfg.ChangeConfiguration(configurationId);
        }

        public int GetActiveConfiguration()
        {
            if (cfg.IsConfigured) return cfg.GetConfigId();

            return 0;
      
        }

        public DataTable GetCacheContent(string contentName,string orderBy)
        {
            DataView view = new DataView(new Debug().GetCache().Tables[contentName],null,orderBy,DataViewRowState.CurrentRows);
          
            return view.Table;
        }
    }
}