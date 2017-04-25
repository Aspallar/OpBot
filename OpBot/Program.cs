using DSharpPlus;
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
        private OperationCollection _ops;

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

            _client = new DiscordClient(new DiscordConfig()
            {
                Token = Properties.Settings.Default.OpBotToken,
                TokenType = TokenType.Bot,
                DiscordBranch = Branch.Stable,
                LogLevel = LogLevel.Debug,
                UseInternalLogHandler = true,
                AutoReconnect = true,
            });

            _commandProcessor = new CommandProcessor(new CommandProcessorConfig()
            {
                OpBotUserId = Properties.Settings.Default.OpBotUserId,
                OpBotChannelId = Properties.Settings.Default.OpBotChannel,
                Names = _names,
                Repository = operationRepository,
                AdminUsers = admins,
                Client = _client,
                Ops = _ops,
            });

            _client.MessageCreated += Client_MessageCreated;
            _client.GuildAvailable += Client_GuildAvailable;
            _client.GuildMemberAdd += Client_GuildMemberAdd;
            _client.GuildMemberUpdate += Client_GuildMemberUpdate;
            _client.Ready += Client_Ready;

            try
            {
                await _client.Connect();
            }
            catch (UnauthorizedException)
            {
                Console.WriteLine("Authorization Failure. OpBot is unable to log in.");
                return;
            }

            Console.ReadKey();
            await _client.Disconnect();

            //operationRepository.Save(_commandProcessor.Operation);

            _client.Dispose();
        }

        private async Task Client_Ready()
        {
            await _client.UpdateStatus("Star Wars: The Old Republic");
        }

        private async Task Client_MessageCreated(MessageCreateEventArgs e)
        {
            if (e.Message.Author.IsBot)
                return;

            if (e.Channel.ID != Properties.Settings.Default.OpBotChannel)
                return;

            if (_commandProcessor.IsCommand(e))
                await _commandProcessor.Execute(e);
        }


        private async Task Client_GuildMemberAdd(GuildMemberAddEventArgs e)
        {
            _names.Add(e.Member);
            ulong greetingsChannelId = Properties.Settings.Default.GreetingsChannelId;
            await Greeting.Greet(greetingsChannelId, _client, _names.GetName(e.Member.User), _guildName);
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        private async Task Client_GuildAvailable(GuildCreateEventArgs e)
        {
            Console.WriteLine(nameof(Client_GuildAvailable));
            _guildName = e.Guild.Name;
            _names.RemoveAll(x => true);
            _names.Add(e.Guild.Members);
            _ops.Start();
        }

        private async Task Client_GuildMemberUpdate(GuildMemberUpdateEventArgs e)
        {
            _names.Update(e.User.ID, e.NickName);
        }

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

    }
}
