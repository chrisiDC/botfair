using System;
using Bot.Shared;
using BotFair;
using BotFair.Server;

namespace BotConsole
{
    class Program
    {
        private static System.Diagnostics.Process process;

        static void Main(string[] args)
        {
            string ipcPort = System.Configuration.ConfigurationManager.AppSettings["ipc_port"];
            var server = (IBotServer)Activator.GetObject(typeof(IBotServer), "ipc://" + ipcPort + "/botserver/o");
            bool success;

            process = new System.Diagnostics.Process
            {
                StartInfo =
                {
                    FileName =
                        System.Configuration.ConfigurationManager.AppSettings["process"] +
                        "BotFair.exe",
                    WorkingDirectory =
                        System.Configuration.ConfigurationManager.AppSettings["process"],
                    CreateNoWindow = true
                }
            };


            do
            {
                try
                {


                    Console.Write("> ");

                    string input = Console.ReadLine();

                    string[] items = input.Split(' ');

                    string command = items[0];

                    if (command.Equals("start", StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (items.Length != 2) WriteHelp("start");
                        else
                        {
                            if (items[1].Equals("bot", StringComparison.OrdinalIgnoreCase)) server.Start();
                            else if (items[1].Equals("proc", StringComparison.OrdinalIgnoreCase))
                            {

                                success = process.Start();
                                if (success) Console.WriteLine("process started");
                                else Console.WriteLine("starting failed");

                            }
                            else WriteHelp("start");
                        }
                    }

                    else if (command.Equals("getstate", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Console.WriteLine("state=" + server.GetState());
                    }
                    else if (command.Equals("getconfig", StringComparison.CurrentCultureIgnoreCase))
                    {
                        Console.WriteLine("state=" + server.GetActiveConfiguration());
                    }
                    else if (command.Equals("setconfig", StringComparison.CurrentCultureIgnoreCase))
                    {
                        int id = Int32.Parse(items[1]);
                        server.ChangeConfiguration(id);
                    }
                    else if (command.Equals("getcache", StringComparison.CurrentCultureIgnoreCase))
                    {
                        var data = server.GetCacheContent((string)items[1],(string)items[2]);
                        Console.WriteLine("rowcount="+data.Rows.Count);
                    }
                    else if (command.Equals("stop", StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (items.Length != 2) WriteHelp("stop");
                        else
                        {
                            if (items[1].Equals("bot", StringComparison.OrdinalIgnoreCase)) server.ShutDown();
                            else if (items[1].Equals("proc", StringComparison.OrdinalIgnoreCase))
                            {

                                process.Kill();
                                Console.WriteLine("process killed");
                            }
                            else WriteHelp("stop");
                        }
                    }
                    else if (command.Equals("help", StringComparison.CurrentCultureIgnoreCase)) WriteAvailableCommands();
                    else WriteAvailableCommands();
                }
                catch (Exception ex)
                {

                    Console.WriteLine(ex);
                    if (ex.InnerException != null) Console.WriteLine(ex.InnerException);
                }
            } while (true);
        }

        private static void WriteHelp(string command)
        {
            Console.WriteLine("help for command " + command + ": help yourself!");
        }

        private static void WriteAvailableCommands()
        {
            Console.WriteLine("start <proc|bot>");
            Console.WriteLine("getstate");


        }
    }
}
