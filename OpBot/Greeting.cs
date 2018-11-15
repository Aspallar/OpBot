using DSharpPlus;
using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace OpBot
{
    internal static class Greeting
    {
        public static async Task Greet(ulong greetingsChannelId, DiscordClient client, string name, string guildName)
        {
            if (greetingsChannelId > 0)
            {
                DiscordChannel channel = await client.GetChannelAsync(greetingsChannelId);
                if (channel != null)
                {
                    string msg = "Yay! We have a new member :smile:\n\n";
                    msg += $"***Greetings {name}!***\nWelcome to {guildName}. ";
                    msg += "I am the droid that coordinates operation events and I hope you have a great time here.\n\n";
                    msg += $"You can find out how to command me at <{Constants.InstrucionUrl}>";
                    await channel.SendMessageAsync(msg);
                }
            }
        }
    }
}
