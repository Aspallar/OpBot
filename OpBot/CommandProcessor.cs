using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;

namespace OpBot
{
    internal class CommandProcessor : IDisposable
    {
        private readonly ulong _opBotUserId;
        private readonly NicknameList _names;
        private readonly OperationRepository _repository;
        private readonly IAdminUser _adminUsers;
        private readonly MessageDeleter _messageDeleter;
        private readonly DiscordClient _client;
        private readonly ulong _opBotChannelId;
        private readonly OperationManager _ops;
        private readonly DefaultOperations _defaultOperations;

        public CommandProcessor(CommandProcessorConfig config)
        {
            _opBotUserId = config.OpBotUserId;
            _names = config.Names;
            _repository = config.Repository;
            _adminUsers = config.AdminUsers;
            _client = config.Client;
            _opBotChannelId = config.OpBotChannelId;
            _messageDeleter = new MessageDeleter();
            _defaultOperations = new DefaultOperations();
            _ops = config.Ops;
            _ops.OperationDeleted += OperationDeleted;
            _ops.OperationUpdated += OperationUpdated;
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
                await SendError(e, "There were too many mentions in that command.\n");
                return;
            }

            try
            {
                ParsedCommand cmd = new ParsedCommand(e, _defaultOperations[e.Message.Author.ID], _opBotUserId);

                if (cmd.CommandParts.Length == 0)
                {
                    await ShowInstructions(e);
                }
                else
                {
                    if ("CREATE".StartsWith(cmd.Command))
                    {
                        await CreateCommand(e, cmd);
                    }
                    else if (cmd.Command == "TANK" || cmd.Command == "DPS" || cmd.Command == "HEAL" || cmd.Command == "HEALZ" || cmd.Command == "HEALS")
                    {
                        await SignupCommand(e, cmd);
                    }
                    else if (cmd.Command == "ALT" || cmd.Command == "RESERVE")
                    {
                        await AltCommand(e, cmd);
                    }
                    else if (cmd.Command == "ADDNOTE")
                    {
                        await AddNoteCommand(e, cmd);
                    }
                    else if (cmd.Command == "DELNOTE")
                    {
                        await DeleteNoteCommand(e, cmd);
                    }
                    else if (cmd.Command == "REMOVE")
                    {
                        await RemoveCommand(e, cmd);
                    }
                    else if (cmd.Command == "VER" || cmd.Command == "VERSION")
                    {
                        await VersionCommand(e);
                    }
                    else if (cmd.Command == "GF")
                    {
                        await GroupFinderCommand(e, cmd.CommandParts);
                    }
                    else if (cmd.Command == "REPOST")
                    {
                        await RepostCommand(e);
                    }
                    else if (cmd.Command == "RAIDTIMES")
                    {
                        await RaidTimesCommand(e, cmd);
                    }
                    else if (cmd.Command == "EDIT")
                    {
                        await EditCommand(e, cmd);
                    }
                    else if (cmd.Command == "CLOSE")
                    {
                        await CloseCommand(e, cmd);
                    }
                    else if (cmd.Command == "BIGTEXT")
                    {
                        await BigTextCommand(e, cmd);
                    }
                    else if (cmd.Command == "OFFLINE")
                    {
                        await OfflineCommand(e, cmd.CommandParts);
                    }
                    else if (cmd.Command == "BACK" || cmd.Command == "BK")
                    {
                        await BackCommand(e, cmd.CommandParts);
                    }
                    else if (cmd.Command == "LIST")
                    {
                        await ListCommand(e);
                    }
                    else if (cmd.Command == "OP" || cmd.Command == "SETOP")
                    {
                        await SetOperationCommand(e, cmd);
                    }
                    else if (cmd.Command == "PURGE")
                    {
                        await PurgeCommand(e);
                    }
                    else
                    {
                        await SendError(e, $"That is not a command that I recognize.");
                    }
                }
            }
            catch (CommandParseException ex)
            {
                await SendError(e, ex.Message);
            }

        }

        private async Task SetOperationCommand(MessageCreateEventArgs e, ParsedCommand cmd)
        {
            int operationId;
            
            if (cmd.CommandParts.Length != 2
                || !int.TryParse(cmd.CommandParts[1], out operationId)
                || operationId < 1
                || operationId > OperationManager.MaxOperations
                || !_ops.IsActiveOperation(operationId))
            {
                await SendError(e, "That's an invalid set operation command.");
                return;
            }
            _defaultOperations[e.Message.Author.ID] = operationId;
        }

        private async Task ListCommand(MessageCreateEventArgs e)
        {
            string text = _ops.GetSummary();
            if (string.IsNullOrEmpty(text))
                text = "There are no active operations to list.";
            await e.Message.Respond(text);
        }

        private async Task OperationDeleted(OperationDeletedEventArgs e)
        {
            DiscordChannel channel = null;
            await _repository.SaveAsync(_ops);
            try
            {
                channel = await _client.GetChannel(_opBotChannelId);
                DiscordMessage message = await channel.GetMessage(e.MessageId);
                await message.Edit($"{DiscordText.NoEntry} {DiscordText.BigText("closed")}  {message.Content}");
                await message.Unpin();
            }
            catch (NotFoundException)
            {
                // do nothing
            }
            catch (UnauthorizedException)
            {
                await channel.SendMessage("Unable to perform unpin. I need the 'Manage Messages' permission to do so.");
            }
        }

        private async Task OperationUpdated(OperationUpdatedEventArgs e)
        {
            DiscordChannel channel = null;
            await _repository.SaveAsync(_ops);
            try
            {
                channel = await _client.GetChannel(_opBotChannelId);
                DiscordMessage message = await channel.GetMessage(e.Operation.MessageId);
                await message.Edit(e.Operation.GetOperationMessageText());
            }
            catch (NotFoundException)
            {
                if (channel != null)
                    await SendError(channel, "I was unable to update the operation message. Someone appears to have deleted the operation");
            }
        }


        private async Task VersionCommand(MessageCreateEventArgs e)
        {
            string text = "Version: " + DiscordText.BigText(OpBotUtils.GetVersionText()) + "\nBETA";
            await e.Channel.SendMessage(text);
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
                    await SendError(e, $"{commandParts[1]} is not a valid time.");
                    return;
                }
                text = $"I will be back around {commandParts[1]} (UTC)";
            }
            else
            {
                int minutes;
                if (!int.TryParse(commandParts[1], out minutes))
                {
                    await SendError(e, $"{commandParts[1]} is not a valid number of minutes.");
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

        private async Task BigTextCommand(MessageCreateEventArgs e, ParsedCommand cmd)
        {
            if (!CheckIsAdminUser(e, "BIGTEXT"))
                return;

            await SafeDeleteMessage(e);

            for (int k = 1; k < cmd.CommandParts.Length; k++)
            {
                DiscordMessage message = await e.Channel.SendMessage(DiscordText.BigText(cmd.CommandParts[k]));
                if (!cmd.IsPermanent)
                    _messageDeleter.AddMessage(message, 30000);
            }
        }

        private async Task CloseCommand(MessageCreateEventArgs e, ParsedCommand cmd)
        {
            int operationId = cmd.OperationId;
            if (operationId == 0)
            {
                if (cmd.CommandParts.Length < 2)
                {
                    await SendError(e, "You must specify an operation to deactivate.");
                    return;
                }
                if (!int.TryParse(cmd.CommandParts[1], out operationId) || operationId < 0)
                {
                    await SendError(e, $"{cmd.CommandParts[1]} is not a valid operation number");
                    return;
                }
            }
            bool success = await _ops.Delete(operationId);
            if (!success)
                await SendOperationErrorMessage(e, operationId);
            else
                await e.Channel.SendMessage($"Operation {DiscordText.BigText(operationId)} closed {DiscordText.OkHand}.");
        }

        private async Task AltCommand(MessageCreateEventArgs e, ParsedCommand cmd)
        {
            try
            {
                bool success = await _ops.SetOperationRoles(cmd.OperationId, _names.GetName(cmd.User), cmd.User.ID, cmd.CommandParts);
                if (!success)
                    await SendOperationErrorMessage(e, cmd.OperationId);
            }
            catch (OpBotInvalidValueException ex)
            {
                await SendError(e, ex.Message);
            }
        }


        private async Task RaidTimesCommand(MessageCreateEventArgs e, ParsedCommand cmd)
        {
#if DEBUG
            const int messageLifetime = 180000; 
#else
            const int messageLifetime = 180000; // 3 minutes
#endif
            DateTime operationDate;
            if (!_ops.GetOperationDate(cmd.OperationId, out operationDate))
            {
                await SendError(e, "There are no active operations to display raid time for.");
                return;
            }

            List<TimeZoneTime> times = TimeZones.GetZoneTimes(operationDate);
            string timesMessage = TimeZones.ToString(times);
            StringBuilder messageText = new StringBuilder(timesMessage.Length + 80);
            messageText.Append(DiscordText.CodeBlock);
            messageText.Append(timesMessage);
            messageText.AppendLine(DiscordText.CodeBlock);
            messageText.Append(GetSelfDestructText(messageLifetime));
            Console.WriteLine($"Message length: {messageText.Length}");
            DiscordMessage message = await e.Channel.SendMessage(messageText.ToString());
            _messageDeleter.AddMessage(message, messageLifetime);
            await SafeDeleteMessage(e);
        }

        private async Task DeleteNoteCommand(MessageCreateEventArgs e, ParsedCommand cmd)
        {
            if (cmd.CommandParts.Length != 2)
            {
                await SendError(e, "You should specify only a note number, or * for all notes.");
                return;
            }
            int noteNumber;
            if (cmd.CommandParts[1] == "*")
            {
                noteNumber = 0;
            }
            else if (!int.TryParse(cmd.CommandParts[1], out noteNumber) || noteNumber < 1)
            {
                await SendError(e, $"{cmd.CommandParts[1]} is not a valid note number.");
                return;
            }

            bool success = await _ops.DeleteOperationNote(cmd.OperationId, noteNumber - 1);
            if (!success)
                await SendOperationErrorMessage(e, cmd.OperationId);
        }

        private async Task EditCommand(MessageCreateEventArgs e, ParsedCommand cmd)
        {
            if (cmd.CommandParts.Length == 1)
            {
                await SendError(e, "What do you want me to edit?");
                return;
            }

            try
            {
                OperationParameters opParams = OperationParameters.Parse(cmd.CommandParts);
                bool success = await _ops.UpdateOperation(cmd.OperationId, opParams);
                if (!success)
                    await SendOperationErrorMessage(e, cmd.OperationId);
            }
            catch (OpBotInvalidValueException ex)
            {
                await SendError(e, $"That is an invalid edit command\n\n.{ex.Message}");
            }
        }


        private async Task GroupFinderCommand(MessageCreateEventArgs e, string[] commandParts)
        {
            const int messageLifetime = 60000;
            int days = 7;
            if (commandParts.Length > 2)
            {
                await SendError(e, $"That is too many parameters for a GF command.");
                return;
            }
            if (commandParts.Length == 2)
            {
                string dayString = commandParts[1];
                if (!int.TryParse(dayString, out days))
                {
                    await SendError(e, $"{dayString} is not a number.");
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
            await SafeDeleteMessage(e);
        }

        private async Task PurgeCommand(MessageCreateEventArgs e)
        {
            if (!CheckIsAdminUser(e, "PURGE"))
                return;

            var messages = await e.Channel.GetMessages();
            foreach (var message in messages)
            {
                if (!_ops.IsOperationMessage(message.ID))
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
                        await SendError(e, NeedManagePermission("purge messages"));
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
                SendError(e, $"You are not an administrator.\n\nYou need to be an administrator to use the *{commandName}* command.")
                    .GetAwaiter()
                    .GetResult();
                return false;
            }
            return true;
        }

        private async Task RemoveCommand(MessageCreateEventArgs e, ParsedCommand cmd)
        {
            bool success = await _ops.RemoveSignup(cmd.OperationId, cmd.User.ID);
            if (!success)
                await SendOperationErrorMessage(e, cmd.OperationId);
        }

        private async Task AddNoteCommand(MessageCreateEventArgs e, ParsedCommand cmd)
        {
            if (cmd.CommandParts.Length > 1)
            {
                string noteText = $"{string.Join(" ", cmd.CommandParts, 1, cmd.CommandParts.Length - 1)} *({_names.GetName(e.Message.Author)})*";
                bool success = await _ops.AddOperationNote(cmd.OperationId, noteText);
                if (!success)
                    await SendOperationErrorMessage(e, cmd.OperationId);
            }
        }

        private async Task SignupCommand(MessageCreateEventArgs e, ParsedCommand cmd)
        {
            string role = cmd.Command.StartsWith("HEAL") ? cmd.Command.Substring(0, 4) : cmd.Command;

            bool success = await _ops.Signup(cmd.OperationId, cmd.User.ID, _names.GetName(cmd.User), role);

            if (!success)
                await SendOperationErrorMessage(e, cmd.OperationId);
        }

        private async Task SendOperationErrorMessage(MessageCreateEventArgs e, int operationId)
        {
            string text = (operationId == 0) ? "There are no active operations" : $"Operation {DiscordText.BigText(operationId)} does not exist";
            await SendError(e, text);
        }

        private async Task CreateCommand(MessageCreateEventArgs e, ParsedCommand cmd)
        {
            try
            {
                OperationParameters opParams = OperationParameters.ParseWithDefault(cmd.CommandParts);

                Operation newOperation = new Operation()
                {
                    Size = opParams.Size,
                    Mode = opParams.Mode,
                    Date = DateHelper.GetDateForNextOccuranceOfDay(opParams.Day) + opParams.Time,
                };
                newOperation.OperationName = opParams.OperationCode == "GF" ? GroupFinder.OperationOn(newOperation.Date) : opParams.OperationCode;

                DiscordMessage newOpMessage = await e.Channel.SendMessage("Creating event...");
                newOperation.MessageId = newOpMessage.ID;

                string text = _ops.Add(newOperation).GetOperationMessageText();
                await newOpMessage.Edit(text);
                await PinMessage(e, newOpMessage);
                await _repository.SaveAsync(_ops);
            }
            catch (OperationException ex)
            {
                await SendError(e, ex.Message);
            }
            catch (OpBotInvalidValueException opEx)
            {
                await SendError(e, $"I don't understand part of that create command.\n\n{opEx.Message}\n\nSo meat bag, try again and get it right this time or you will be terminated as an undesirable {DiscordText.StuckOutTongue}.");
            }
        }

        private async Task RepostCommand(MessageCreateEventArgs e)
        {
            await e.Message.Respond("Sorry the repost command is not implemenetd in this version");
            //if (!CheckForOperation(e))
            //    return;

            //DiscordMessage previousOperationMessage;

            //try
            //{
            //    previousOperationMessage = await e.Channel.GetMessage(Operation.MessageId);
            //}
            //catch (NotFoundException)
            //{
            //    previousOperationMessage = null;
            //}
            //DiscordMessage newOperationMessage = await e.Channel.SendMessage(Operation.GetOperationMessageText());
            //Operation.MessageId = newOperationMessage.ID;
            //await PinMessage(e, newOperationMessage);
            //if (previousOperationMessage != null)
            //    await previousOperationMessage.Delete();
            //_repository.Save(Operation);
        }


        private async Task PinMessage(MessageCreateEventArgs e, DiscordMessage message)
        {
            try
            {
                await message.Pin();
            }
            catch (UnauthorizedException)
            {
                await SendError(e, NeedManagePermission("pin the operation"));
            }
        }

        private static string NeedManagePermission(string actionText)
        {
            return $"I am unable to {actionText} as I do not appear to have the necessary 'Manage Messages' permission.";
        }


        private static string GetSelfDestructText(int lifetime)
        {
            int minutes = lifetime / 60000;
            string plural = minutes > 1 ? "s" : "";
            return $"{DiscordText.Stopwatch} Message will self destruct in {minutes} minute{plural}";
        }

        private async Task SafeDeleteMessage(MessageCreateEventArgs e)
        {
            try
            {
                await e.Message.Delete();
            }
            catch (NotFoundException)
            {
                // do nothing. Its ok for message to have been already deleted
            }
            catch (UnauthorizedException)
            {
                await SendError(e, NeedManagePermission("delete message"));
            }
        }

        private async Task SendError(DiscordChannel channel, string errorText)
        {
            ErrorEmbed errorEmbed = new ErrorEmbed(errorText);
            await channel.SendMessage("", embed: errorEmbed);
        }

        private async Task SendError(MessageCreateEventArgs e, string errorText)
        {
            errorText = $"I'm very sorry {_names.GetName(e.Message.Author)} but...\n" + errorText;
            await SendError(e.Channel, errorText);
        }


        private async Task ShowInstructions(MessageCreateEventArgs e)
        {
            DiscordEmbed instructionEmbed = new DiscordEmbed()
            {
                Color = 0xad1313,
                Title = "List of Commands",
                Url = Constants.InstrucionUrl,
                Thumbnail = new DiscordEmbedThumbnail()
                {
                    Url = "https://raw.githubusercontent.com/wiki/Aspallar/OpBot/images/2-128.png",
                },
                Description = $"Hey {_names.GetName(e.Message.Author)}.\n\nI manage operations and other events here.\n\n You can view a full list of my commands by clicking on the title above."
            };
            await e.Message.Respond("", embed: instructionEmbed);
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
