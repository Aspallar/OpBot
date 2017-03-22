using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using System.Text.RegularExpressions;

namespace OpBot
{
    internal class CommandProcessor
    {
        private readonly ulong _opBotUserId;
        private readonly NicknameList _names;
        private readonly Regex _removeMentionsRegex = new Regex(@"\<@!?\d+\>");
        private readonly OperationRepository _repository;
        private readonly IAdminUser _adminUsers;


        public Operation Operation { get; private set; }

        public CommandProcessor(CommandProcessorConfig config)
        {
            _opBotUserId = config.OpBotUserId;
            _names = config.Names;
            _repository = config.Repository;
            Operation = config.Operation;
            _adminUsers = config.AdminUsers;
        }

        public bool IsCommand(MessageCreateEventArgs e)
        {
            return e.Message.Mentions.Count > 0
                && e.Message.Mentions.Any(m => m.ID == _opBotUserId);
        }

        public async Task Execute(MessageCreateEventArgs e)
        {
            if (e.Message.Mentions.Count > 2)
            {
                await e.Channel.SendMessage("Sorry. There were too many mentions in that command.");
                return;
            }

            string[] commandParts = ParseCommand(e.Message.Content);

            if (commandParts.Length == 0)
            {
                await e.Channel.SendMessage($"Hey {_names.GetName(e.Message.Author)}. My instructions are here <{Constants.InstrucionUrl}>");
            }
            else
            {
                string command = commandParts[0].ToUpperInvariant();

                var user = e.Message.Mentions.Where(m => m.ID != _opBotUserId).SingleOrDefault();
                if (user == null)
                    user = e.Message.Author;

                if (command == "CREATE")
                {
                    await CreateCommand(e, commandParts);
                }
                else if (command == "TANK" || command == "DPS" || command == "HEAL")
                {
                    await SignupCommand(e, command, user);
                }
                else if (command == "ADDNOTE")
                {
                    await AddNoteCommand(e, commandParts);
                }
                else if (command == "REMOVE")
                {
                    await RemoveCommand(e, user);
                }
                else if (command == "VER" || command == "VERSION")
                {
                    await e.Channel.SendMessage(OpBotUtils.GetVersionText());
                }
                else if (command == "GF")
                {
                    await GroupFinderCommand(e.Channel, commandParts);
                }
                else if (command == "PURGE")
                {
                    await PurgeCommand(e);
                }
                else if (command == "EDIT")
                {
                    await EditCommand(e, commandParts);
                }
                else
                {
                    await e.Channel.SendMessage($"I'm sorry {_names.GetName(e.Message.Author)} but I don't understand that command.");
                }
            }

        }

        private async Task EditCommand(MessageCreateEventArgs e, string[] commandParts)
        {
            if (!CheckForOperation(e))
                return;

            if (commandParts.Length == 1)
            {
                await e.Channel.SendMessage($"Edit what {_names.GetName(e.Message.Author)}?");
                return;
            }
            else if (commandParts.Length > 2)
            {
                await e.Channel.SendMessage($"One edit item at a time please {_names.GetName(e.Message.Author)}.");
                return;
            }

            TimeSpan time;
            string param = commandParts[1].ToUpperInvariant();

            try
            {
                if (OpBotUtils.IsOperationMode(param))
                {
                    Operation.Mode = param;
                }
                else if (param == "8" || param == "16")
                {
                    Operation.Size = int.Parse(param);
                }
                else if (param.IndexOf(":") > 0 
                    && TimeSpan.TryParse(param, out time)
                    && time.Hours <= 23)
                {
                    Operation.Date = Operation.Date.Date + time;
                }
                else if (DateHelper.IsDayName(param))
                {
                    DateTime newDate = DateHelper.GetDateForNextOccuranceOfDay(param);
                    Operation.Date = newDate + Operation.Date.TimeOfDay;
                }
                else
                {
                    Operation.OperationName = param;
                }
                await UpdateOperationMessage(e.Channel);
            }
            catch (OpBotInvalidValueException)
            {
                await e.Channel.SendMessage($"Sorry {_names.GetName(e.Message.Author)}. I don't understand what you mean by {param}.");
            }
        }


        private async Task GroupFinderCommand(DiscordChannel channel, string[] commandParts)
        {
            int days = 7;
            if (commandParts.Length > 2)
            {
                await channel.SendMessage($"Invalid GF command");
                return;
            }
            if (commandParts.Length == 2)
            {
                string dayString = commandParts[1];
                if (!int.TryParse(dayString, out days))
                {
                    await channel.SendMessage($"{dayString} is not a number");
                    return;
                }
                if (days > 14) days = 14;
            }
            List<string> ops = GroupFinder.NextDays(days);
            DateTime dt = DateTime.Now.Date;
            StringBuilder msg = new StringBuilder("Group Finder Operations for the next ", 512);
            msg.Append(days);
            msg.AppendLine(" days are");
            msg.AppendLine("```");
            foreach (string opCode in ops)
            {
                msg.Append(dt.ToString("ddd"));
                msg.Append(' ');
                msg.Append(opCode.PadRight(4));
                msg.AppendLine(Operation.GetFullName(opCode));
                dt = dt.AddDays(1);
            }
            msg.AppendLine("```");
            await channel.SendMessage(msg.ToString());
        }

        private async Task PurgeCommand(MessageCreateEventArgs e)
        {
            if (!_adminUsers.IsAdmin(e.Message.Author.ID))
            {
                await e.Channel.SendMessage("You need to be an admin user to use the PURGE command.");
                return;
            }

            var messages = await e.Channel.GetMessages();
            foreach (var message in messages)
            {
                if (Operation == null || message.ID != Operation.MessageId)
                {
                    await Task.Delay(1500);
                    await message.Delete();
                }
            }
        }

        private async Task RemoveCommand(MessageCreateEventArgs e, DiscordUser user)
        {
            if (!CheckForOperation(e))
                return;
            Operation.Remove(user.ID);
            await UpdateOperationMessage(e.Channel);
        }

        private async Task AddNoteCommand(MessageCreateEventArgs e, string[] commandParts)
        {
            if (!CheckForOperation(e))
                return;
            if (commandParts.Length > 1)
            {
                string text = string.Join(" ", commandParts, 1, commandParts.Length - 1);
                Operation.Notes.Add(text);
            }
            await UpdateOperationMessage(e.Channel);
        }

        private async Task SignupCommand(MessageCreateEventArgs e, string command, DiscordUser user)
        {
            if (!CheckForOperation(e))
                return;
            Operation.Signup(user.ID, _names.GetName(user), command);
            await UpdateOperationMessage(e.Channel);
        }

        private async Task CreateCommand(MessageCreateEventArgs e, string[] commandParts)
        {
            try
            {
                if (commandParts.Length < 4)
                    throw new OpBotInvalidValueException("Missing parameters. Must specify at least <op> <size> <day>.");

                var newOperation = new Operation();
                newOperation.SetSizeFromString(commandParts[2]);
                newOperation.Date = DateHelper.GetDateForNextOccuranceOfDay(commandParts[3]);
                if (commandParts[1].ToUpperInvariant() == "GF")
                {
                    newOperation.OperationName = GroupFinder.OperationOn(newOperation.Date);
                }
                else
                {
                    newOperation.OperationName = commandParts[1];
                }
                TimeSpan time;
                if (commandParts.Length > 4)
                {
                    if (!TimeSpan.TryParse(commandParts[4], out time) || time.Hours > 23)
                        throw new OpBotInvalidValueException($"{commandParts[4]} is not a valid time.");
                }
                else
                {
                    time = new TimeSpan(19, 30, 0);
                }
                newOperation.Date += time;
                if (commandParts.Length > 5)
                {
                    newOperation.Mode = commandParts[5].ToUpperInvariant();
                }
                else
                {
                    newOperation.Mode = "SM";
                }
                if (Operation != null)
                {
                    var message = await e.Channel.GetMessage(Operation.MessageId);
                    await UnpinMessage(e, message);
                }

                Operation = newOperation;
                var text = Operation.GetOperationMessageText();
                var newOpMessage = await e.Channel.SendMessage(text);
                Operation.MessageId = newOpMessage.ID;
                await PinMessage(e, newOpMessage);
                _repository.Save(Operation);
            }
            catch (OpBotInvalidValueException opEx)
            {
                await e.Channel.SendMessage($"Sorry {_names.GetName(e.Message.Author)} that is an invalid create command.\n{opEx.Message}");
            }

        }

        private async Task PinMessage(MessageCreateEventArgs e, DiscordMessage message)
        {
            try
            {
                await message.Pin();
            }
            catch (UnauthorizedException)
            {
                await e.Channel.SendMessage($"Sorry {_names.GetName(e.Message.Author)}. {NeedManagePermission("pin the operation")}");
            }
        }

        private async Task UnpinMessage(MessageCreateEventArgs e, DiscordMessage message)
        {
            try
            {
                await message.Unpin();
            }
            catch (UnauthorizedException)
            {
                await e.Channel.SendMessage($"Sorry {_names.GetName(e.Message.Author)}. {NeedManagePermission("unpin the previous operation")}");
            }
        }

        private static string NeedManagePermission(string actionText)
        {
            return $"I am unable to {actionText} as I need the 'manage messages' permission to do so. Please do so manually and get my permissions fixed.";
        }

        private async Task UpdateOperationMessage(DiscordChannel channel)
        {
            System.Diagnostics.Debug.Assert(Operation != null);
            var opMessage = await channel.GetMessage(Operation.MessageId);
            await opMessage.Edit(Operation.GetOperationMessageText());
            _repository?.Save(Operation);
        }

        private string[] ParseCommand(string content)
        {
            string contentWithNoMentions = _removeMentionsRegex.Replace(content, string.Empty);
            return contentWithNoMentions.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private bool CheckForOperation(MessageCreateEventArgs e)
        {
            if (Operation == null)
            {
                e.Channel.SendMessage($"Sorry {_names.GetName(e.Message.Author)}. I cannot do that as there is no current operation")
                    .GetAwaiter()
                    .GetResult();
                return false;
            }
            return true;
        }

    }
}
