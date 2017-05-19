using DSharpPlus;
using System;
using System.Threading.Tasks;

namespace OpBot
{
    internal class ServersAvailableEventArgs : EventArgs
    {
        public ServersAvailableEventArgs(DiscordChannel channel, bool expired) : base()
        {
            Channel = channel;
            Expired = expired;
        }
        public DiscordChannel Channel { get; private set; }
        public bool Expired { get; private set; }
    }
}