using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Net.Mail;
using System.Runtime.Remoting;
using System.Threading;
using System.Timers;
using Bot.Shared;
using BotFair.BusinessLayer;
using BotFair.BusinessLayer.BetEngine;
using BotFair.Server;


namespace BotFair
{
    internal class Program
    {
      
        private static BotState state;
        private static bool restart;
        private static object stateLock;
        private static object waitObject;

        private const int T_ScanForNewMarkets = 60,
                          T_UpdateMarkets = 60,
                          T_SetHotMarkets = 5,
                          T_BetEngine = 5,
                          T_Cleanup = 10,
                          T_PriceTracker = 5;
        public delegate void BetFairHandler();

        public static void Main(string[] args)
        {
           
            Trace.TraceInformation("bot.exe started");
            restart = true;
            RemotingConfiguration.Configure("BotFair.exe.config", false);
            

            while (restart)
            {

                restart = false;
            
                var dbException = CheckSqlServerConnection();
                CleanUpDb();


                if (dbException != null)
                {
                    Trace.TraceError("Cant connect to database");
                }
                else
                {

                    BotServerImpl.cfg = new Configuration();
                    if (!String.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["configId"]))
                    {
                        BotServerImpl.cfg.ChangeConfiguration(
                            int.Parse(System.Configuration.ConfigurationManager.AppSettings["configId"]));
                    }

                    Trace.TraceInformation(String.Format("Using Configuration Id {0}",
                                                         BotServerImpl.cfg.GetConfigId().ToString()));

                    var p = new Program();
                    var accountBO = new Account();
                    var marketsBO = new Markets(accountBO, BotServerImpl.cfg);
                    var pricesBO = new Prices(accountBO, BotServerImpl.cfg, marketsBO);
                    var betEngine = new BetEngine();
                 

                    marketsBO.LoadActiveMarkets();

                

                    stateLock = new object();
                    waitObject = new object();
                   
                    BotServerImpl.BotEvent += BotServerImpl_BotEvent;
                    state = BotState.STOPPED;

                    var t1 = new Cleanup(marketsBO, new Log());

                    //    var accountBO = new Account();
                   

                    accountBO.Login(System.Configuration.ConfigurationManager.AppSettings["betfairuser"], System.Configuration.ConfigurationManager.AppSettings["betfairpassword"]);

                   
                    var mRunner = new MethodRunner();
                    mRunner.AddMethod(t1, t1.GetType().GetMethod("Run"), 10, T_Cleanup);
                    mRunner.AddMethod(marketsBO, marketsBO.GetType().GetMethod("ScanNewMarkets_Thread"), 0, T_ScanForNewMarkets);
                    mRunner.AddMethod(marketsBO, marketsBO.GetType().GetMethod("SetHotMarkets_Thread"), 0, T_SetHotMarkets);
                    mRunner.AddMethod(pricesBO, pricesBO.GetType().GetMethod("TrackPrices_Thread"), 0, T_PriceTracker);
                    mRunner.AddMethod(marketsBO, marketsBO.GetType().GetMethod("UpdateExistingMarkets_Thread"), 0, T_UpdateMarkets);
                    mRunner.AddMethod(betEngine, betEngine.GetType().GetMethod("Run"), 0, T_BetEngine);
                    mRunner.AddMethod(p, p.GetType().GetMethod("FlushLog"), 10, 10);

                    if (!String.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["runonstartup"]))
                    {
                        bool start = bool.Parse(System.Configuration.ConfigurationManager.AppSettings["runonstartup"]);
                        if (start)
                            BotServerImpl_BotEvent(new ServerEventArgs { ServerAction = ServerEventArgs.Action.START });
                    }
                 
                    do
                    {
                        mRunner.Ping();
                        Thread.Sleep(200);
                    } while (!restart);

              
                }
            }
        }

        public static void Do()
        {
            Console.WriteLine("x1");
        }

        public static void Do2()
        {
            Console.WriteLine("x2");
        }

        public static void CleanUpDb()
        {
            //var db = new Tools.DbAccess(System.Configuration.ConfigurationManager.ConnectionStrings["botfair"].ConnectionString);
            //db.DoUpdate("update selections set tracked=0");
            //db.DoUpdate("update markets set ishot=0");
        }

        private static void SendFaultMail(string message, Exception ex)
        {
            var location = System.Configuration.ConfigurationManager.AppSettings["location"].ToLower();
            if (location == "live" || location == "staging")
            {
                try
                {
                    MailMessage mail = new MailMessage("chrisiDC@gmail.com", "chrisiDC@gmail.com");
                    mail.Subject = "Bot Failure";
                    mail.Body = message;
                    if (ex != null)
                    {
                        mail.Body += Environment.NewLine + ex.StackTrace;
                        if (ex.InnerException != null)
                        {
                            mail.Body += Environment.NewLine + "INNER EXECPTION:";
                            mail.Body += Environment.NewLine + ex.InnerException.StackTrace;
                        }
                    }
                    //mail.To.Add("phath.san@gmail.com");

                    var client = new SmtpClient();
                    client.EnableSsl = true;
                    client.Send(mail);
                }
                catch (Exception mailException)
                {
                    Trace.TraceError("could not send mail");
                }
            }
        }



        private static object BotServerImpl_BotEvent(ServerEventArgs e)
        {
            if (e.ServerAction == ServerEventArgs.Action.READ)
            {
                return state;
            }
            else
            {
                lock (stateLock)
                {
                    if (e.ServerAction == ServerEventArgs.Action.START && state == BotState.STOPPED)
                        state = BotState.STARTING;
                    else if (e.ServerAction == ServerEventArgs.Action.SHUTDOWN && state == BotState.STARTED)
                        state = BotState.STOPPING;
                }
            }

            return null;
        }

        //private static bool Startup()
        //{


        //    int minMarketPeriod = 60 * 60 * 2;

        //    //if (BotServerImpl.cfg.ActiveNewMarketsPeriod < minMarketPeriod)
        //    //{
        //    //    Trace.TraceWarning("cant start: newmarketsperiod less than " + minMarketPeriod);

        //    //    return false;
        //    //}

        //    Trace.TraceInformation("starting up bot...");

        //    var accountBO = new Account();
        //    var marketsBO = new Markets(accountBO, BotServerImpl.cfg);

        //    var pricesBO = new Prices(accountBO, BotServerImpl.cfg, marketsBO);

        //    accountBO.Login(System.Configuration.ConfigurationManager.AppSettings["betfairuser"], System.Configuration.ConfigurationManager.AppSettings["betfairpassword"]);

        //    var ev = new AutoResetEvent(false);
       
        //    var ev2 = new AutoResetEvent(false);
        //    var ev3 = new AutoResetEvent(false);
        //    var ev9 = new AutoResetEvent(false);
        //    var ev10 = new AutoResetEvent(false);
        //    var waitObjects = new WaitHandle[] {ev, ev2, ev3,ev9, ev10};

        //    var marketScanner = new Thread(RunThread);
        //    int interval = 10;
        //    marketScanner.Name = "MarketScanner";
        //    marketScanner.Priority = ThreadPriority.Normal;
        //    marketScanner.IsBackground = false;
        

        

        //    marketScanner.Start(new object[] {interval, new BetFairHandler(marketsBO.ScanNewMarkets_Thread), ev,waitObjects });

        //    var hotMarketScanner = new Thread(RunThread);
        //    hotMarketScanner.Name = "HotMarketScanner";
        //    interval = 2;
        //    hotMarketScanner.Priority = ThreadPriority.Normal;
        //    hotMarketScanner.IsBackground = false;
         

          
        //    hotMarketScanner.Start(new object[] { interval, new BetFairHandler(marketsBO.SetHotMarkets_Thread),ev2, waitObjects });

        //    //var betEngine = new Thread(RunThread);
        //    //betEngine.Name = "BetEngine";
        //    //betEngine.IsBackground = true;
        //    //threads.Add(betEngine);
        //    //betEngine.Start(new object[] {5, new BetFairHandler(pricesBO.Run), ev1});

        //    var marketUpdate = new Thread(RunThread);
        //    marketUpdate.Name = "MarketUpdater";
        //    marketUpdate.Priority = ThreadPriority.Normal;
        //    marketUpdate.IsBackground = false;
        //    interval = 10;
          
          
        //    marketUpdate.Start(new object[] { interval, new BetFairHandler(marketsBO.UpdateExistingMarkets_Thread), ev3,waitObjects });

        //    var priceTracker = new Thread(RunThread);
        //    priceTracker.Name = "priceTracker";
        //    priceTracker.Priority = ThreadPriority.Normal;
        //    priceTracker.IsBackground = false;
        //    interval = 5;
           
          
      
        //    priceTracker.Start(new object[] { interval, new BetFairHandler(pricesBO.TrackPrices_Thread), ev10,waitObjects });


        //    var betEngine = new Thread(RunThread);
        //    betEngine.Name = "BetEngine";
        //    betEngine.Priority = ThreadPriority.Normal;
        //    betEngine.IsBackground = false;
        //    interval = 1;
          
         

        //betEngine.Start(new object[] { interval, new BetFairHandler(BetEngine.Run), ev9, waitObjects });


        //    var logThread = new Thread(LogWriterThread);
        //    logThread.Name = "logger";
        //    logThread.Priority = ThreadPriority.Normal;
        //    logThread.IsBackground = false;
        //    logThread.Start();

        //    bool ok = WaitHandle.WaitAll(waitObjects, 5000);

        //    if (ok)
        //    {
        //        //state = BotState.STARTED;
        //        Trace.TraceInformation("bot started");
               
        //    }
        //    else
        //    {
        //        Trace.TraceWarning("not all threads were started correctly");
        //    }

        //    return ok;
        //}

     


        public static Exception CheckSqlServerConnection()
        {
            Exception e = null;
            SqlConnection connection = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["BotFair.DataLayer.Properties.Settings.botfairConnectionString"].ConnectionString);
            SqlCommand command = new SqlCommand("select top 1 * from log", connection);
            SqlDataAdapter adapter = new SqlDataAdapter(command);

            try
            {
                adapter.Fill(new System.Data.DataSet());
            }
            catch (Exception ex)
            {
                e = ex;
            }

            return e;
        }

        public static void FlushLog()
        {
           
                new Log().FlushEvents();
               
        }


        //public static void WriteToConsoleFromThread(string message)
        //{
        //    Console.WriteLine(message);
        //}

        //static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        //{

        //    string msg = e.Exception.Message;
        //    if (e.Exception.InnerException != null) msg += "\r\n" + e.Exception.InnerException.Message;
        //    System.Diagnostics.Trace.TraceError(msg);
        //}

        //static void HandleApplicationWarning(Exception ex, string message)
        //{

        //}
    }
}