using DSharpPlus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpBot
{
    class CodeHolder
    {
        private const string lastMessageIdFilename = "last.txt";

        private static ulong GetLastMessageId(ulong lastMessageId)
        {
            if (File.Exists(lastMessageIdFilename))
            {
                string contents;
                ulong value;
                using (var sr = new StreamReader(lastMessageIdFilename))
                    contents = sr.ReadLine();
                if (ulong.TryParse(contents, out value))
                {
                    lastMessageId = value;
                }
                else
                {
                    Console.WriteLine($"Invalid {lastMessageIdFilename}");
                }
            }

            return lastMessageId;
        }

        private static void SaveLastMessageId(ulong lastMessageId)
        {
            using (var sw = new StreamWriter(lastMessageIdFilename, false))
                sw.WriteLine(lastMessageId);
            Console.WriteLine($"last message id = {lastMessageId}");
        }

        private static async Task<ulong> GetMessages(ulong channelId, ulong lastMessageId, DiscordClient client)
        {
            DiscordChannel channel = await client.GetChannelByID(channelId);

            //List<DiscordMessage> messages = await channel.GetMessages(after: lastMessageId); // not working, neither does limit
            List<DiscordMessage> messages = await channel.GetMessages(limit: 80);
            //messages = messages.Where(msg => msg.ID > lastMessageId).OrderBy(msg => msg.ID).ToList();
            messages = messages.Where(msg => msg.ID > lastMessageId).ToList();

            foreach (DiscordMessage msg in messages)
            {
                Console.WriteLine($"{msg.ID} {msg.Content}");
                Console.WriteLine("Mentions:");
                foreach (var mention in msg.Mentions)
                {
                    Console.WriteLine($"  {mention.Username}");
                }
                Console.WriteLine("*******************************");
                if (msg.ID > lastMessageId)
                    lastMessageId = msg.ID;
            }

            await client.Disconnect();
            return lastMessageId;
        }


    }
}
