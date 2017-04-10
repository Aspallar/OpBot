using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using System.Text.RegularExpressions;

namespace OpBot
{
    internal class CommandProcessor : IDisposable
    {
        private readonly ulong _opBotUserId;
        private readonly NicknameList _names;
        private readonly Regex _removeMentionsRegex = new Regex(@"\<@!?\d+\>");
        private readonly OperationRepository _repository;
        private readonly IAdminUser _adminUsers;
        private readonly MessageDeleter _messageDeleter;


        public Operation Operation { get; private set; }

        public CommandProcessor(CommandProcessorConfig config)
        {
            _opBotUserId = config.OpBotUserId;
            _names = config.Names;
            _repository = config.Repository;
            Operation = config.Operation;
            _adminUsers = config.AdminUsers;
            _messageDeleter = new MessageDeleter();
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

                if ("CREATE".StartsWith(command))
                {
                    await CreateCommand(e, commandParts);
                }
                else if (command == "TANK" || command == "DPS" || command == "HEAL" || command == "HEALZ" || command == "HEALS")
                {
                    await SignupCommand(e, command, user);
                }
                else if (command == "ALT" || command == "RESERVE")
                {
                    await AltCommand(e, commandParts, user);
                }
                else if (command == "ADDNOTE")
                {
                    await AddNoteCommand(e, commandParts);
                }
                else if (command == "DELNOTE")
                {
                    await DeleteNoteCommand(e, commandParts);
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
                    await GroupFinderCommand(e, commandParts);
                }
                else if (command == "REPOST")
                {
                    await RepostCommand(e);
                }
                else if (command == "RAIDTIMES")
                {
                    await RaidTimesCommand(e);
                }
                else if (command == "EDIT")
                {
                    await EditCommand(e, commandParts);
                }
                else if (command == "NO" && commandParts.Length == 2 && commandParts[1].ToUpperInvariant() == "OPERATION")
                {
                    await NoOperationCommand(e);
                }
                else if (command == "BIGTEXT")
                {
                    await BigTextCommand(e, commandParts);
                }
                else if (command == "OFFLINE")
                {
                    await OfflineCommand(e, commandParts);
                }
                else if (command == "BACK" || command == "BK")
                {
                    await BackCommand(e, commandParts);
                }
                else if (command == "PURGE")
                {
                    await PurgeCommand(e);
                }
                else
                {
                    await e.Channel.SendMessage($"I'm sorry {_names.GetName(e.Message.Author)} but I don't understand that command.");
                }
            }

        }

        private async Task BackCommand(MessageCreateEventArgs e, string[] commandParts)
        {
            if (!CheckIsAdminUser(e, "BACK"))
                return;

            await e.Channel.SendMessage($"{DiscordText.BigText("I  AM  BACK")}\n\nI am back online and awaiting your commands.");
        }

        private async Task OfflineCommand(MessageCreateEventArgs e, string[] commandParts)
        {
            if (!CheckIsAdminUser(e, "OFFLINE"))
                return;

            string text;
            if (commandParts.Length == 1)
            {
                text = "I will be back as soon as possible.";
            }
            else if (commandParts[1].IndexOf(':') > -1)
            {
                TimeSpan time;
                if (!TimeSpan.TryParse(commandParts[1], out time) || time.TotalHours > 23)
                {
                    await e.Channel.SendMessage($"{commandParts[1]} is not a valid time.");
                    return;
                }
                text = $"I will be back around {commandParts[1]} (UTC)";
            }
            else
            {
                int minutes;
                if (!int.TryParse(commandParts[1], out minutes))
                {
                    await e.Channel.SendMessage($"{commandParts[1]} is not a valid number of minutes.");
                    return;
                }
                TimeSpan duration = new TimeSpan(0, minutes, 0);
                text = $"I will be back in approximately ";
                if (duration.Days > 0)
                    text += duration.Days.ToString() + " days ";
                if (duration.Hours > 0)
                    text += duration.Hours.ToString() + " hours ";
                if (duration.Minutes > 0)
                    text += duration.Minutes.ToString() + " minutes ";
            }
            string fullText = $"{DiscordText.BigText("offline")}\n\nI am going offline for a while.\n\n{text}\n\nLove you all {DiscordText.Kiss}";
            await e.Channel.SendMessage(fullText);
        }

        private async Task BigTextCommand(MessageCreateEventArgs e, string[] commandParts)
        {
            if (!CheckIsAdminUser(e, "BIGTEXT") || commandParts.Length <= 1)
                return;

            await SafeDeleteMessage(e.Channel, e.Message);

            bool autoDelete = true;
            int start = 1;

            if (commandParts[1].ToUpperInvariant() == "-PERM")
            {
                autoDelete = false;
                start = 2;
            }

            for (int k = start; k < commandParts.Length; k++)
            {
                DiscordMessage message = await e.Channel.SendMessage(DiscordText.BigText(commandParts[k]));
                if (autoDelete)
                    _messageDeleter.AddMessage(message, 30000);
            }
        }

        private async Task NoOperationCommand(MessageCreateEventArgs e)
        {
            if (!CheckForOperation(e))
                return;

            await UnpinPreviousOperation(e);
            Operation = null;
            _repository.Save(Operation);
            await e.Channel.SendMessage($"{DiscordText.OkHand} {DiscordText.BigText("done")}");
        }

        private async Task AltCommand(MessageCreateEventArgs e, string[] commandParts, DiscordUser user)
        {
            if (!CheckForOperation(e))
                return;

            try
            {
                Operation.SetAltRoles(_names.GetName(user), user.ID, commandParts);
                await UpdateOperationMessage(e.Channel);
            }
            catch (OpBotInvalidValueException ex)
            {
                await e.Channel.SendMessage($"Sorry {_names.GetName(e.Message.Author)}.\n {ex.Message}");
            }
        }


        private async Task RaidTimesCommand(MessageCreateEventArgs e)
        {
#if DEBUG
            const int messageLifetime = 180000; 
#else
            const int messageLifetime = 180000; // 3 minutes
#endif
            if (!CheckForOperation(e))
                return;

            List<TimeZoneTime> times = TimeZones.GetZoneTimes(Operation.Date);
            string timesMessage = TimeZones.ToString(times);
            StringBuilder messageText = new StringBuilder(timesMessage.Length + 80);
            messageText.Append(DiscordText.CodeBlock);
            messageText.Append(timesMessage);
            messageText.AppendLine(DiscordText.CodeBlock);
            messageText.Append(GetSelfDestructText(messageLifetime));
            Console.WriteLine($"Message length: {messageText.Length}");
            DiscordMessage message = await e.Channel.SendMessage(messageText.ToString());
            _messageDeleter.AddMessage(message, messageLifetime);
            await SafeDeleteMessage(e.Channel, e.Message);
        }

        private async Task DeleteNoteCommand(MessageCreateEventArgs e, string[] commandParts)
        {
            if (commandParts.Length != 2)
            {
                await e.Channel.SendMessage("Specify a note number or * for all notes");
                return;
            }
            if (commandParts[1] == "*")
            {
                Operation.ResetNotes();
            }
            else
            {
                int noteNumber;

                if (!int.TryParse(commandParts[1], out noteNumber) || noteNumber < 1 || noteNumber > Operation.NoteCount)
                {
                    await e.Channel.SendMessage("Specify a note number or * for all notes");
                    return;
                }
                Operation.DeleteNote(noteNumber - 1);
            }
            await UpdateOperationMessage(e.Channel);
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
                    && time.TotalHours <= 23)
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


        private async Task GroupFinderCommand(MessageCreateEventArgs e, string[] commandParts)
        {
            const int messageLifetime = 60000;
            int days = 7;
            if (commandParts.Length > 2)
            {
                await e.Channel.SendMessage($"Invalid GF command");
                return;
            }
            if (commandParts.Length == 2)
            {
                string dayString = commandParts[1];
                if (!int.TryParse(dayString, out days))
                {
                    await e.Channel.SendMessage($"{dayString} is not a number");
                    return;
                }
                if (days > 14) days = 14;
            }
            List<string> ops = GroupFinder.NextDays(days);
            DateTime dt = DateTime.Now.Date;
            StringBuilder msg = new StringBuilder(512);
            msg.AppendLine(DiscordText.BigText("group  finder"));
            msg.AppendLine();
            msg.Append("Operations for the next ");
            msg.Append(days);
            msg.AppendLine(" days are");
            msg.AppendLine(DiscordText.CodeBlock);
            foreach (string opCode in ops)
            {
                msg.Append(dt.ToString("ddd"));
                msg.Append(' ');
                msg.Append(opCode.PadRight(4));
                msg.AppendLine(Operation.GetFullName(opCode));
                dt = dt.AddDays(1);
            }
            msg.AppendLine(DiscordText.CodeBlock);
            msg.Append(GetSelfDestructText(messageLifetime));
            DiscordMessage message = await e.Channel.SendMessage(msg.ToString());
            _messageDeleter.AddMessage(message, messageLifetime);
            await SafeDeleteMessage(e.Channel, e.Message);
        }

        private async Task PurgeCommand(MessageCreateEventArgs e)
        {
            if (!CheckIsAdminUser(e, "PURGE"))
                return;

            var messages = await e.Channel.GetMessages();
            foreach (var message in messages)
            {
                if (Operation == null || message.ID != Operation.MessageId)
                {
                    try
                    {
                        await message.Delete();
                    }
                    catch (NotFoundException)
                    {
                        // we dont care if message is not there
                    }
                    catch (UnauthorizedException)
                    {
                        await e.Channel.SendMessage(NeedManagePermission("purge messages"));
                        break; // foreach
                    }
                    await Task.Delay(1500);
                }
            }
        }

        private bool CheckIsAdminUser(MessageCreateEventArgs e, string commandName)
        {
            if (!_adminUsers.IsAdmin(e.Message.Author.ID))
            {
                e.Channel.SendMessage($"{DiscordText.BigText("oops")}\n\n{_names.GetName(e.Message.Author)} you are not an administrator.\n\nYou need to be an administrator to use the *{commandName}* command.")
                    .GetAwaiter()
                    .GetResult();
                return false;
            }
            return true;
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
                Operation.AddNote(text);
            }
            await UpdateOperationMessage(e.Channel);
        }

        private async Task SignupCommand(MessageCreateEventArgs e, string command, DiscordUser user)
        {
            if (command.StartsWith("HEAL"))
                command = command.Substring(0, 4);

            if (!CheckForOperation(e))
                return;

            Operation.Signup(user.ID, _names.GetName(user), command);
            await UpdateOperationMessage(e.Channel);
        }

        private async Task CreateCommand(MessageCreateEventArgs e, string[] commandParts)
        {
            try
            {
                CreateCommandParameters ccp = CreateCommandParameters.Parse(commandParts);

                Operation newOperation = new Operation()
                {
                    Size = ccp.Size,
                    Mode = ccp.Mode,
                    Date = DateHelper.GetDateForNextOccuranceOfDay(ccp.Day) + ccp.Time,
                };
                newOperation.OperationName = ccp.OperationCode == "GF" ? GroupFinder.OperationOn(newOperation.Date) : ccp.OperationCode;

                if (Operation != null)
                    await UnpinPreviousOperation(e);

                Operation = newOperation;
                var text = Operation.GetOperationMessageText();
                var newOpMessage = await e.Channel.SendMessage(text);
                Operation.MessageId = newOpMessage.ID;
                await PinMessage(e, newOpMessage);
                _repository.Save(Operation);
            }
            catch (OpBotInvalidValueException opEx)
            {
                await e.Channel.SendMessage($"{DiscordText.NoEntry}\n\nHey {_names.GetName(e.Message.Author)}.\n\nI don't understand part of that create command.\n\n{opEx.Message}\n\nSo meat bag, try again and get it right this time or you will be terminated as an undesirable :stuck_out_tongue:.");
            }
        }

        private async Task UnpinPreviousOperation(MessageCreateEventArgs e)
        {
            try
            {
                var message = await e.Channel.GetMessage(Operation.MessageId);
                await UnpinMessage(e, message);
            }
            catch (NotFoundException)
            {
                // its valid to do nothing. someone might have deleted it.
            }
        }

        private async Task RepostCommand(MessageCreateEventArgs e)
        {
            if (!CheckForOperation(e))
                return;

            DiscordMessage previousOperationMessage;

            try
            {
                previousOperationMessage = await e.Channel.GetMessage(Operation.MessageId);
            }
            catch (NotFoundException)
            {
                previousOperationMessage = null;
            }
            DiscordMessage newOperationMessage = await e.Channel.SendMessage(Operation.GetOperationMessageText());
            Operation.MessageId = newOperationMessage.ID;
            await PinMessage(e, newOperationMessage);
            if (previousOperationMessage != null)
                await previousOperationMessage.Delete();
            _repository.Save(Operation);
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
                e.Channel.SendMessage($"Sorry {_names.GetName(e.Message.Author)}. I cannot do that as there is no current operation.")
                    .GetAwaiter()
                    .GetResult();
                return false;
            }
            return true;
        }

        private static string GetSelfDestructText(int lifetime)
        {
            int minutes = lifetime / 60000;
            string plural = minutes > 1 ? "s" : "";
            return $"{DiscordText.Stopwatch} Message will self destruct in {minutes} minute{plural}";
        }

        private async Task SafeDeleteMessage(DiscordChannel channel, DiscordMessage message)
        {
            try
            {
                await message.Delete();
            }
            catch (NotFoundException)
            {
                // do nothing. Its ok for message to have been already deleted
            }
            catch (UnauthorizedException)
            {
                await channel.SendMessage($"{DiscordText.Warning} Warning!\n\nMy diodes are hurting because I do not appear to have the \"Manage Messages\" permission. This is required for me to operate properly, please assign me a role that has the \"Manage Messages\" permission.");
            }
        }

        public void Dispose()
        {
            if (_messageDeleter != null)
            {
                _messageDeleter.Dispose();
            }
        }
    }
}
