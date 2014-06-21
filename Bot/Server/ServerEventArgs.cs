using System;

namespace BotFair.Server
{
    internal class ServerEventArgs : EventArgs
    {
        public enum Action
        {
            SHUTDOWN,
            START,
            READ
        };

        public Action ServerAction { get; set; }
    }
}