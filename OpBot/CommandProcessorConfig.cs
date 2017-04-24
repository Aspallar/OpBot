using DSharpPlus;

namespace OpBot
{
    internal class CommandProcessorConfig
    {
        public IAdminUser AdminUsers { get; set; }
        public DiscordClient Client { get; set; }
        public NicknameList Names { get; set; }
        public ulong OpBotChannelId { get; set; }
        public ulong OpBotUserId { get;  set; }
        public OperationCollection Ops { get; internal set; }
        public OperationRepository Repository { get; set; }
    }
}