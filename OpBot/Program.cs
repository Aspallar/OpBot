using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using DSharpPlus.Net.WebSocket;
using log4net;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OpBot
{
    internal class Program
    {
        private static ILog log = LogManager.GetLogger(typeof(Program));

        static void Main(string[] args)
        {
            Logging.Configure("Logging.xml");
#if !DEBUG
            try
            {
#endif
                new Program().Run(args).GetAwaiter().GetResult();
#if !DEBUG
            }
            catch (Exception ex)
            {
                log.Fatal(ex.ToString());
            }
#endif            
        }

        private DiscordClient _client;
        private NicknameList _names = new NicknameList();
        private CommandProcessor _commandProcessor;
        private string _guildName = string.Empty;
        private OperationManager _ops;
        private DevTracker _devTracker;
        private CancellationTokenSource _stopApplication;
        private BotStatus _botStatus;

        public async Task Run(string[] args)
        {
            log.Info($"OpBot {OpBotUtils.GetVersionText()}");
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
                LogLevel = LogLevel.Info,
                UseInternalLogHandler = true,
                AutoReconnect = true,
            });
            _client.SetWebSocketClient<WebSocket4NetClient>();

            _stopApplication = new CancellationTokenSource();

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
                StopApplication = _stopApplication,
            });

            _client.MessageCreated += Client_MessageCreated;
            _client.GuildAvailable += Client_GuildAvailable;

            _client.GuildMemberAdded += Client_GuildMemberAdded;
            _client.GuildMemberUpdated += Client_GuildMemberUpdated;
            _client.Ready += Client_Ready;

            _botStatus = new BotStatus(_client);

            // this used to throw an UnauthorizedException when login failed, in DSharpPlus 3.2.3 it
            // now throws System.Exception and even if it is caught the DSharpPlus is unstable and will
            // crash out, so no point in catching it, only option is to let the app bomb out.
            await _client.ConnectAsync();

            Console.CancelKeyPress += Console_CancelKeyPress;
            try { await Task.Delay(-1, _stopApplication.Token); } catch (TaskCanceledException) { }

            if (_devTracker != null)
            {
                try { await _devTracker.Stop(); } catch (TaskCanceledException) { }
                _devTracker.Dispose();
            }
            await _client.DisconnectAsync();
            _client.Dispose();
        }

        private void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            if (e.SpecialKey == ConsoleSpecialKey.ControlC)
            {
                log.Info("Ctrl-C pressed, stopping Bot.");
                e.Cancel = true;
                _stopApplication.Cancel();
            }
        }

        private Task Client_Ready(ReadyEventArgs e)
        {
            log.Info("Client Ready");
            _botStatus.Start();
            return Task.CompletedTask;
        }

        private async Task Client_MessageCreated(MessageCreateEventArgs e)
        {
            if (!e.Message.Author.IsBot
                && e.Channel.Id == Properties.Settings.Default.OpBotChannel
                && _commandProcessor.IsCommand(e))
                    await _commandProcessor.Execute(e);
        }

        private async Task Client_GuildMemberAdded(GuildMemberAddEventArgs e)
        {
            _names.Add(e.Member);
            ulong greetingsChannelId = Properties.Settings.Default.GreetingsChannelId;
            await Greeting.Greet(greetingsChannelId, _client, _names.GetName(e.Member), _guildName);
        }

        private async Task Client_GuildAvailable(GuildCreateEventArgs e)
        {
            log.Info($"{e.Guild.Name} guild available ({e.Guild.MemberCount} members)");
            _guildName = e.Guild.Name;
            _names.RemoveAll(x => true);
            _names.Add(e.Guild.Members);
            _ops.Start();
            if (_devTracker != null)
                await StartDevTracker();
        }

        private async Task StartDevTracker()
        {
            log.Info("Starting DevTracker");
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
                log.Error($"Cannot find devtracker channel {devTrackerChannelId}. Devtracker disabled.");
            }
        }

        private Task Client_GuildMemberUpdated(GuildMemberUpdateEventArgs e)
        {
            _names.Update(e.Member.Id, e.Member.Nickname);
            return Task.CompletedTask;
        }

    }
}
