using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using DSharpPlus.Net.WebSocket;
using System;
using System.Threading.Tasks;

namespace OpBot
{
    internal class Program
    {
        static void Main(string[] args) => new Program().Run(args).GetAwaiter().GetResult();

        private DiscordClient _client;
        private NicknameList _names = new NicknameList();
        private CommandProcessor _commandProcessor;
        private string _guildName = string.Empty;
        private OperationManager _ops;
        private DevTracker _devTracker;

        public async Task Run(string[] args)
        {
            Console.WriteLine(OpBotUtils.GetVersionText());
            OperationRepository operationRepository = new OperationRepository(Properties.Settings.Default.OperationFile);
            IAdminUser admins;

            try
            {
                admins = new AdminUsers(Properties.Settings.Default.AdminUsers);
            }
            catch (FormatException)
            {
                Console.WriteLine("There are invalid AdminUsers entries in config file.");
                return;
            }

            _ops = operationRepository.Get();

            if (Properties.Settings.Default.devTrackerChannel != 0)
                _devTracker = new DevTracker();

            _client = new DiscordClient(new DiscordConfiguration()
            {
                Token = Properties.Settings.Default.OpBotToken,
                TokenType = TokenType.Bot,
                //DiscordBranch = Branch.Stable,
                LogLevel = LogLevel.Debug,
                UseInternalLogHandler = true,
                AutoReconnect = true,
            });
            _client.SetWebSocketClient<WebSocket4NetClient>();


            _commandProcessor = new CommandProcessor(new CommandProcessorConfig()
            {
                OpBotUserId = Properties.Settings.Default.OpBotUserId,
                OpBotChannelId = Properties.Settings.Default.OpBotChannel,
                Names = _names,
                Repository = operationRepository,
                AdminUsers = admins,
                Client = _client,
                Ops = _ops,
                CommandCharacters = Properties.Settings.Default.CommandChars,
            });

            _client.MessageCreated += Client_MessageCreated;
            _client.GuildAvailable += Client_GuildAvailable;

            _client.GuildMemberAdded += Client_GuildMemberAdded;
            _client.GuildMemberUpdated += Client_GuildMemberUpdated;
            _client.Ready += Client_Ready;


            try
            {
                await _client.ConnectAsync();
            }
            catch (UnauthorizedException)
            {
                Console.WriteLine("Authorization Failure. OpBot is unable to log in.");
                return;
            }

            Console.ReadKey();

            if (_devTracker != null)
            {
                try { await _devTracker.Stop(); } catch (TaskCanceledException) { }
                _devTracker.Dispose();
            }
            await _client.DisconnectAsync();
            _client.Dispose();
        }

        private async Task Client_Ready(ReadyEventArgs e)
        {
            await _client.UpdateStatusAsync(new DiscordGame("Star Wars: The Old Republic"));
        }

        private async Task Client_MessageCreated(MessageCreateEventArgs e)
        {
            if (e.Message.Author.IsBot)
                return;

            if (e.Channel.Id != Properties.Settings.Default.OpBotChannel)
                return;

            if (_commandProcessor.IsCommand(e))
                await _commandProcessor.Execute(e);
        }


        private async Task Client_GuildMemberAdded(GuildMemberAddEventArgs e)
        {
            // TODO: implement Client_GuildMemberAdded
            //_names.Add(e.Member);
            //ulong greetingsChannelId = Properties.Settings.Default.GreetingsChannelId;
            //await Greeting.Greet(greetingsChannelId, _client, _names.GetName(e.Member.User), _guildName);

            _names.Add(e.Member);
            ulong greetingsChannelId = Properties.Settings.Default.GreetingsChannelId;
            await Greeting.Greet(greetingsChannelId, _client, _names.GetName(e.Member), _guildName);
        }

        private async Task Client_GuildAvailable(GuildCreateEventArgs e)
        {
            _guildName = e.Guild.Name;
            _names.RemoveAll(x => true);
            _names.Add(e.Guild.Members);
            _ops.Start();
            if (_devTracker != null)
                await StartDevTracker();
        }

        private async Task StartDevTracker()
        {
            System.Diagnostics.Debug.Assert(_devTracker != null);
            ulong devTrackerChannelId = Properties.Settings.Default.devTrackerChannel;
            try
            {
                _devTracker.Start(await _client.GetChannelAsync(devTrackerChannelId));
            }
            catch (NotFoundException)
            {
                _devTracker.Dispose();
                _devTracker = null;
                Console.WriteLine($"Warning: Cannot find devtracker channel {devTrackerChannelId}. Devtracker disabled.");
            }
        }

        private Task Client_GuildMemberUpdated(GuildMemberUpdateEventArgs e)
        {
            _names.Update(e.Member.Id, e.Member.Nickname);
            return Task.Delay(0);
        }

    }
}
