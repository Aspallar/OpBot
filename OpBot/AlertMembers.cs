using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OpBot
{
    internal class AlertMembers
    {
        const string saveFileName = "alerts.txt";
        HashSet<ulong> _alertMembers;

        public AlertMembers()
        {
            _alertMembers = Load();
        }

        public enum AlertStates
        {
            On, Off
        };

        public async Task<AlertStates> Toggle(ulong userID)
        {
            AlertStates newState;
            lock (this)
            {
                if (_alertMembers.Add(userID))
                {
                    newState = AlertStates.On;
                }
                else
                {
                    _alertMembers.Remove(userID);
                    newState = AlertStates.Off;
                }
            }
            await Save(_alertMembers);
            return newState;
        }

        public async Task SendAlerts(DiscordGuild guild, IReadOnlyOperation op, string userName)
        {
            bool isAtLeastOneMissingMember = false;
            ulong[] alertRecipents = GetRecipients();
            string message = $"A new \"{op.OperationName}\" event has been posted by {userName} in {guild.Name}.";
            foreach (ulong userId in alertRecipents)
            {
                DiscordMember member;
                try
                {
                    member = await guild.GetMemberAsync(userId);
                    await member.SendMessageAsync(message);
                }
                catch (NotFoundException)
                {
                    lock (this) _alertMembers.Remove(userId);
                    isAtLeastOneMissingMember = true;
                }
            }
            if (isAtLeastOneMissingMember)
                await Save(_alertMembers);
        }

        private static async Task Save(HashSet<ulong> keys)
        {
            using (StreamWriter writer = new StreamWriter(saveFileName))
            {
                foreach (ulong key in keys)
                    await writer.WriteLineAsync(key.ToString());
            }
        }

        private static HashSet<ulong> Load()
        {
            HashSet<ulong> keys = new HashSet<ulong>();
            try
            {
                using (StreamReader reader = new StreamReader(saveFileName))
                {
                    string key;
                    while ((key = reader.ReadLine()) != null)
                        keys.Add(ulong.Parse(key));
                }
            }
            catch (FileNotFoundException)
            {
            }
            return keys;
        }

        public ulong[] GetRecipients()
        {
            ulong[] alertRecipents;
            lock (this)
            {
                alertRecipents = new ulong[_alertMembers.Count];
                _alertMembers.CopyTo(alertRecipents);
            }
            return alertRecipents;
        }
    }
}