using DSharpPlus;

namespace OpBot
{
    internal class CommandProcessorConfig
    {
        public IAdminUser AdminUsers { get; set; }
        public DiscordClient Client { get; set; }
        public string CommandCharacters { get; internal set; }
        public NicknameList Names { get; set; }
        public ulong OpBotChannelId { get; set; }
        public ulong OpBotUserId { get;  set; }
        public OperationManager Ops { get; set; }
        public OperationRepository Repository { get; set; }
    }
}