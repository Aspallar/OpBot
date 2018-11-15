using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using NeoSmart.AsyncLock;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;

namespace OpBot
{
    internal class CommandProcessor : IDisposable
    {
        private readonly ulong _opBotUserId;
        private readonly NicknameList _names;
        private readonly OperationRepository _repository;
        private readonly IAdminUser _adminUsers;
        private MessageDeleter _messageDeleter;
        private readonly DiscordClient _client;
        private readonly ulong _opBotChannelId;
        private readonly OperationManager _ops;
        private readonly DefaultOperations _defaultOperations;
        private SwtorAvailablePoll _swtorAvailablePoll;
        private AsyncLock _asyncLock;
        private AlertMembers _alertMembers;
        private string _commandCharacters;

        public CommandProcessor(CommandProcessorConfig config)
        {
            _commandCharacters = config.CommandCharacters;
            _opBotUserId = config.OpBotUserId;
            _names = config.Names;
            _repository = config.Repository;
            _adminUsers = config.AdminUsers;
            _client = config.Client;
            _opBotChannelId = config.OpBotChannelId;
            _messageDeleter = new MessageDeleter();
            _defaultOperations = new DefaultOperations();
            _asyncLock = new AsyncLock();
            _alertMembers = new AlertMembers();
            _ops = config.Ops;
            _ops.OperationDeleted += OperationClosed;
            _ops.OperationUpdated += OperationUpdated;
        }


        public bool IsCommand(MessageCreateEventArgs e)
        {
            return e.Message.Content.Length > 0 &&
                (_commandCharacters.IndexOf(e.Message.Content[0]) >= 0
                || e.Message.MentionedUsers.Any(x => x.Id == _opBotUserId));
        }

        public async Task Execute(MessageCreateEventArgs e)
        {
            try
            {
                ParsedCommand cmd = new ParsedCommand(e, _defaultOperations[e.Message.Author.Id], _opBotUserId, _commandCharacters);

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
                        await RepostCommand(e, cmd);
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
                    else if (cmd.Command == "MONITOR")
                    {
                        await MonitorCommand(e, cmd);
                    }
                    else if (cmd.Command == "MSG" || cmd.Command == "MESSAGE")
                    {
                        await MessageCommand(e, cmd);
                    }
                    else if (cmd.Command == "PURGE")
                    {
                        await PurgeCommand(e);
                    }
                    else if (cmd.Command == "ALERTME")
                    {
                        await AlertMeCommand(e, cmd);
                    }
                    else if (cmd.Command == "LISTALERTS")
                    {
                        await ListAlertsCommand(e, cmd);
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

        private async Task ListAlertsCommand(MessageCreateEventArgs e, ParsedCommand cmd)
        {
            if (!CheckIsAdminUser(e, "LISTALERTS"))
                return;

            await e.Message.RespondAsync("A list of alert recipients is being prepared. It will be PM'd to you when complete.");

            ulong[] alertRecipients = _alertMembers.GetRecipients();
            StringBuilder message = new StringBuilder();
            foreach (ulong userId in alertRecipients)
            {
                try
                {
                    DiscordMember member = await e.Guild.GetMemberAsync(userId);
                    message.Append(userId);
                    message.Append(' ');
                    message.AppendLine(_names.GetName(member));
                }
                catch (NotFoundException)
                {
                    message.AppendLine(userId.ToString());
                }
                await Task.Delay(100);
            }
            if (message.Length == 0)
                message.AppendLine("There are no registered alert recipents");

            DiscordMember author = await e.Guild.GetMemberAsync(e.Message.Author.Id);
            await author.SendMessageAsync(message.ToString());
        }

        private async Task AlertMeCommand(MessageCreateEventArgs e, ParsedCommand cmd)
        {
            ulong userId = e.Message.Author.Id;
            if (!_adminUsers.IsAdmin(userId) && cmd.User.Id != userId)
            {
                await SendError(e, "You must be an adminstrator to turn alerts on/off for another user.");
                return;
            }

            AlertMembers.AlertStates newState = await _alertMembers.Toggle(cmd.User.Id);
            string onOff = newState == AlertMembers.AlertStates.On ? "ON" : "OFF";
            await e.Message.RespondAsync($"PM alerts turned {onOff} for {_names.GetName(e.Message.Author)}.");

        }

        private async Task MessageCommand(MessageCreateEventArgs e, ParsedCommand cmd)
        {
            if (!CheckIsAdminUser(e, "MESSAGE") || cmd.CommandParts.Length == 1)
                return;
            await Task.Delay(100);
            await e.Message.DeleteAsync();
            await Task.Delay(100);
            await e.Channel.SendMessageAsync(string.Join(" ", cmd.CommandParts, 1, cmd.CommandParts.Length - 1));
        }

        private async Task MonitorCommand(MessageCreateEventArgs e, ParsedCommand cmd)
        {
            int numParts = cmd.CommandParts.Length;

            if (numParts > 2 || (numParts == 2 && cmd.CommandParts[1].ToUpperInvariant() != "STOP"))
            {
                await SendError(e, "There are invalid arguments in the monitor command.");
                return;
            }

            if (numParts != 2)
                await StartTwitterPoll(e);
            else
                await StopTwittorPoll(e);
        }

        private async Task StartTwitterPoll(MessageCreateEventArgs e)
        {
            bool alreadyStarted = false;
            using (await _asyncLock.LockAsync())
            {
                alreadyStarted = _swtorAvailablePoll != null;
                if (!alreadyStarted)
                {
                    _swtorAvailablePoll = new SwtorAvailablePoll();
                    _swtorAvailablePoll.ServersAvailable += SwtorAvailablePoll_ServersAvailable;
                    _swtorAvailablePoll.Start(e.Channel);
                }
            }
            if (alreadyStarted)
                await SendError(e, "I am already monitoring twitter");
            else
                await e.Message.RespondAsync($"{DiscordText.OkHand} Monitoring twitter for 'servers are available' tweet....");
        }

        private async Task StopTwittorPoll(MessageCreateEventArgs e)
        {
            bool alreadyStopped = false;
            using (await _asyncLock.LockAsync())
            {
                alreadyStopped = _swtorAvailablePoll == null;
                if (!alreadyStopped)
                    await EndTwitterPoll();
            }
            if (alreadyStopped)
                await SendError(e, "I am not montoring twitter!\nI cannot stop doing something I'm not doing! Stupid meat-bag.");
            else
                await e.Message.RespondAsync($"{DiscordText.OkHand} Stopped.");
        }

        private async Task EndTwitterPoll()
        {
            try { await _swtorAvailablePoll.Stop(); } catch (TaskCanceledException) { }
            _swtorAvailablePoll.Dispose();
            _swtorAvailablePoll = null;
        }

        private async Task SwtorAvailablePoll_ServersAvailable(ServersAvailableEventArgs e)
        {
            using (await _asyncLock.LockAsync())
            {
                if (_swtorAvailablePoll != null)
                    await EndTwitterPoll();
            }
            if (e.Expired)
                await SendError(e.Channel, "I have stopped monitoring twitter because it has taken too long for the tweet to happen.");
            else
                await e.Channel.SendMessageAsync($"{DiscordText.BigText("servers\navailable")}\nAccording to twitter it looks like the servers might be back up and running. Can't say for sure though, you meat-bags can be very imprecise in your tweets.");
        }

        private async Task SetOperationCommand(MessageCreateEventArgs e, ParsedCommand cmd)
        {
            int operationId;

            if (cmd.CommandParts.Length < 2)
            {
                await SendError(e, "You must supply an operation number.");
                return;
            }

            if (cmd.CommandParts.Length != 2)
            {
                await SendError(e, "Too many parameters supplied. Only specify the operation number.");
                return;
            }

            if (!int.TryParse(cmd.CommandParts[1], out operationId)
                || operationId < 1
                || operationId > OperationManager.MaxOperations
                || !_ops.IsActiveOperation(operationId))
            {
                await SendError(e, $"{cmd.CommandParts[1]} is not a valid operation number.");
                return;
            }

            _defaultOperations[e.Message.Author.Id] = operationId;
        }

        private async Task ListCommand(MessageCreateEventArgs e)
        {
            string text = _ops.GetSummary();
            if (string.IsNullOrEmpty(text))
                text = "There are no active operations to list.";
            await e.Message.RespondAsync(text);
        }

        private async Task OperationClosed(OperationDeletedEventArgs e)
        {
            DiscordChannel channel = null;
            await _repository.SaveAsync(_ops);
            try
            {
                channel = await _client.GetChannelAsync(_opBotChannelId);
                DiscordMessage message = await channel.GetMessageAsync(e.MessageId);
                await message.ModifyAsync($"{DiscordText.NoEntry} {DiscordText.BigText("closed")}  {message.Content}");
                await message.UnpinAsync();
            }
            catch (NotFoundException)
            {
                // do nothing
            }
            catch (UnauthorizedException)
            {
                await channel.SendMessageAsync("Unable to perform unpin. I need the 'Manage Messages' permission to do so.");
            }
        }

        private async Task OperationUpdated(OperationUpdatedEventArgs e)
        {
            DiscordChannel channel = null;
            await _repository.SaveAsync(_ops);
            try
            {
                channel = await _client.GetChannelAsync(_opBotChannelId);
                DiscordMessage message = await channel.GetMessageAsync(e.Operation.MessageId);
                await message.ModifyAsync(e.Operation.GetOperationMessageText());
            }
            catch (NotFoundException)
            {
                if (channel != null)
                    await SendError(channel, "I was unable to update the operation message. Someone appears to have deleted the operation");
            }
        }


        private async Task VersionCommand(MessageCreateEventArgs e)
        {
            string text = "Version: " + DiscordText.BigText(OpBotUtils.GetVersionText());
            await e.Channel.SendMessageAsync(text);
        }

        private async Task BackCommand(MessageCreateEventArgs e, string[] commandParts)
        {
            if (!CheckIsAdminUser(e, "BACK"))
                return;

            await e.Channel.SendMessageAsync($"{DiscordText.BigText("I  AM  BACK")}\n\nI am back online and awaiting your commands.");
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
            await e.Channel.SendMessageAsync(fullText);
        }

        private async Task BigTextCommand(MessageCreateEventArgs e, ParsedCommand cmd)
        {
            if (!CheckIsAdminUser(e, "BIGTEXT"))
                return;

            await SafeDeleteMessage(e);

            for (int k = 1; k < cmd.CommandParts.Length; k++)
            {
                DiscordMessage message = await e.Channel.SendMessageAsync(DiscordText.BigText(cmd.CommandParts[k]));
                if (!cmd.IsPermanent)
                    _messageDeleter.AddMessage(message, 30000);
            }
        }

        private async Task CloseCommand(MessageCreateEventArgs e, ParsedCommand cmd)
        {
            int operationId = cmd.OperationId;
            if (cmd.CommandParts.Length < 2)
            {
                await SendError(e, "You must specify an operation to deactivate.");
                return;
            }
            if (cmd.CommandParts.Length > 2)
            {
                await SendError(e, "That's too many parameters for an close command.");
                return;
            }
            if (!int.TryParse(cmd.CommandParts[1], out operationId) || operationId < 0)
            {
                await SendError(e, $"{cmd.CommandParts[1]} is not a valid operation number");
                return;
            }
            bool success = await _ops.Delete(operationId);
            if (!success)
                await SendOperationErrorMessage(e, operationId);
            else
                await e.Channel.SendMessageAsync($"Operation {DiscordText.BigText(operationId)} closed {DiscordText.OkHand}.");
        }

        private async Task AltCommand(MessageCreateEventArgs e, ParsedCommand cmd)
        {
            try
            {
                bool success = await _ops.SetOperationRoles(cmd.OperationId, _names.GetName(cmd.User), cmd.User.Id, cmd.CommandParts);
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
            DiscordMessage message = await e.Channel.SendMessageAsync(messageText.ToString());
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
            DiscordMessage message = await e.Channel.SendMessageAsync(msg.ToString());
            _messageDeleter.AddMessage(message, messageLifetime);
            await SafeDeleteMessage(e);
        }

        private async Task PurgeCommand(MessageCreateEventArgs e)
        {
            if (!CheckIsAdminUser(e, "PURGE"))
                return;

            var messages = await e.Channel.GetMessagesAsync();
            foreach (var message in messages)
            {
                if (!_ops.IsOperationMessage(message.Id))
                {
                    try
                    {
                        await message.DeleteAsync();
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
            if (!_adminUsers.IsAdmin(e.Message.Author.Id))
            {
                Task.Run(async () => {
                    await SendError(e, $"You are not an administrator.\n\nYou need to be an administrator to use the *{commandName}* command.");
                });
                return false;
            }
            return true;
        }

        private async Task RemoveCommand(MessageCreateEventArgs e, ParsedCommand cmd)
        {
            bool success = await _ops.RemoveSignup(cmd.OperationId, cmd.User.Id);
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

            bool success = await _ops.Signup(cmd.OperationId, cmd.User.Id, _names.GetName(cmd.User), role);

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

                DiscordMessage newOpMessage = await e.Channel.SendMessageAsync("Creating event...");
                newOperation.MessageId = newOpMessage.Id;

                string text = _ops.Add(newOperation).GetOperationMessageText();
                await newOpMessage.ModifyAsync(text);
                await PinMessage(e, newOpMessage);
                await _repository.SaveAsync(_ops);
                SendAlerts(e, newOperation);
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

        private void SendAlerts(MessageCreateEventArgs e, Operation newOperation)
        {
            string userName = _names.GetName(e.Message.Author);
            DiscordGuild guild = e.Guild;
            Task.Run(async () => await _alertMembers.SendAlerts(guild, newOperation, userName));
        }

        private async Task RepostCommand(MessageCreateEventArgs e, ParsedCommand cmd)
        {
            IReadOnlyOperation[] ops = _ops.GetOperationsByDateDesc();
            for (int k = 0; k < ops.Length; k++)
            {
                IReadOnlyOperation op = ops[k];
                DiscordMessage newMessage = await e.Channel.SendMessageAsync(op.GetOperationMessageText());
                ulong oldMessageId = _ops.UpdateMessageId(op.Id, newMessage.Id);
                DiscordMessage oldMessage = await e.Channel.GetMessageAsync(oldMessageId);
                try { await oldMessage.DeleteAsync(); } catch (NotFoundException) { }
                await Task.Delay(250);
                await PinMessage(e, newMessage);
                if (k != ops.Length - 1)
                    await Task.Delay(3000);
            }
        }


        private async Task PinMessage(MessageCreateEventArgs e, DiscordMessage message)
        {
            try
            {
                await message.PinAsync();
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
                await e.Message.DeleteAsync();
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
            DiscordEmbedBuilder errorEmbed = new DiscordEmbedBuilder()
            {
                Color = new DiscordColor("ad1313"),
                Title = "Error",
                Url = Constants.InstrucionUrl,
                ThumbnailUrl = "https://raw.githubusercontent.com/wiki/Aspallar/OpBot/images/2-128.png",
                Description = errorText
            };
            await channel.SendMessageAsync("", embed: errorEmbed.Build());
        }

        private async Task SendError(MessageCreateEventArgs e, string errorText)
        {
            errorText = $"I'm very sorry {_names.GetName(e.Message.Author)} but...\n" + errorText;
            await SendError(e.Channel, errorText);
        }


        private async Task ShowInstructions(MessageCreateEventArgs e)
        {
            await Task.CompletedTask;

            DiscordEmbedBuilder instructionEmbed = new DiscordEmbedBuilder()
            {
                Color = new DiscordColor("ad1313"),
                Title = "List of Commands",
                Url = Constants.InstrucionUrl,
                ThumbnailUrl = "https://raw.githubusercontent.com/wiki/Aspallar/OpBot/images/2-128.png",
                Description = $"Hey {_names.GetName(e.Message.Author)}.\n\nI manage operations and other events here.\n\n You can view a full list of my commands by clicking on the title above."
            };
            await e.Message.RespondAsync("", embed: instructionEmbed.Build());
        }


        public void Dispose()
        {
            if (_messageDeleter != null)
            {
                _messageDeleter.Dispose();
                _messageDeleter = null;
            }
        }
    }
}
