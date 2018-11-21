using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OpBot
{
    class ParsedCommand
    {
        public string Command { get; private set; }
        public string[] CommandParts { get; private set; }
        public int OperationId { get; private set; }
        public bool IsPermanent { get; private set; }
        public bool Quiet { get; private set; }
        public DiscordUser User { get; private set; }

        private static readonly Regex _mentionsRegex = new Regex(@"\<@!?\d+\>");

        public ParsedCommand(MessageCreateEventArgs e, int defaultOperationId, ulong opBotUserId, string commandCharacters)
        {
            if (e.Message.MentionedUsers.Count > 2)
                throw new CommandParseException("There are to many mentions in that command");

            OperationId = defaultOperationId;

            var mentionedUser = e.Message.MentionedUsers.Where(m => m.Id != opBotUserId).SingleOrDefault();
            User = mentionedUser ?? e.Message.Author;

            string content = StripCommandCharacters(e.Message.Content, commandCharacters); 
            string contentWithNoMentions = _mentionsRegex.Replace(content, string.Empty);
            string[] parts = contentWithNoMentions.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> commandParts = new List<string>();

            if (parts.Length > 0)
            {
                Command = parts[0].ToUpperInvariant();
                foreach (string part in parts)
                {
                    if (part.StartsWith("-"))
                    {
                        ParseSwitch(part.ToUpperInvariant());
                    }
                    else
                    {
                        commandParts.Add(part);
                    }
                }

            }

            CommandParts = commandParts.ToArray();
        }

        private static string StripCommandCharacters(string messageContent, string commandCharacters)
        {
            System.Diagnostics.Debug.Assert(messageContent.Length > 0);
            if (commandCharacters.IndexOf(messageContent[0]) >= 0)
            {
                return messageContent.Substring(1);
            }
            else
            {
                return messageContent;
            }
        }

        private void ParseSwitch(string part)
        {
            if (part == "-PERM")
            {
                IsPermanent = true;
                return;
            }

            if (part.StartsWith("-OP"))
            {
                ParseOperation(part);
                return;
            }

            if (part.StartsWith("-Q"))
            {
                Quiet = true;
                return;
            }

            int operationId;
            if (int.TryParse(part.Substring(1), out operationId))
            {
                if (operationId > 0)
                {
                    OperationId = operationId;
                    return;
                }
            }
            
            throw new CommandParseException($"I don't understand \"{part}\"");
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "OPn")]
        private void ParseOperation(string part)
        {
            int operationId = 0;
            bool valid = part.Length > 3;

            if (valid)
            {
                string numberPart = part.Substring(3);
                valid = int.TryParse(numberPart, out operationId);
            }

            if (valid)
                OperationId = operationId;
            else
                throw new CommandParseException("Invalid operation, specify -OPn where n is the operation number");
        }
    }
}
