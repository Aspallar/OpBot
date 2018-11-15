using System.Collections.Generic;
using System.Linq;
using DSharpPlus;
using DSharpPlus.Entities;

namespace OpBot
{
    internal class NicknameList : List<NicknameEntry>
    {
        public NicknameEntry FindById(ulong UserId)
        {
            return this.Where(n => n.UserId == UserId).SingleOrDefault();
        }

        public string GetName(DiscordUser user)
        {
            NicknameEntry nickname = FindById(user.Id);
            if (nickname == null)
                return user.Username;
            else
                return nickname.Nickname;
        }

        public string GetName(DiscordMember member)
        {
            NicknameEntry nickname = FindById(member.Id);
            if (nickname == null)
                return member.Username;
            else
                return nickname.Nickname;
        }

        public void Add(IReadOnlyList<DiscordMember> members)
        {
            foreach (var member in members)
                Add(member);
        }

        public void Add(DiscordMember member)
        {
            if (!string.IsNullOrEmpty(member.Nickname))
                AddNew(member.Id, member.Nickname);
        }

        public void Update(ulong userId, string nickName)
        {
            NicknameEntry entry = this.Where(n => n.UserId == userId).SingleOrDefault();
            if (string.IsNullOrEmpty(nickName))
            {
                if (entry != null)
                    Remove(entry);
            }
            else
            {
                if (entry != null)
                    entry.Nickname = nickName;
                else
                    AddNew(userId, nickName);
            }
        }

        private void AddNew(ulong userId, string nickName)
        {
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(nickName));
            System.Diagnostics.Debug.Assert(!this.Any(n => n.UserId == userId));
            Add(new NicknameEntry()
            {
                UserId = userId,
                Nickname = nickName,
            });
        }
    }
}
