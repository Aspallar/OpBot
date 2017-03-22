namespace OpBot
{
    internal class CommandProcessorConfig
    {
        public IAdminUser AdminUsers { get; internal set; }
        public NicknameList Names { get; set; }
        public ulong OpBotUserId { get;  set; }
        public Operation Operation { get; set; }
        public OperationRepository Repository { get; internal set; }
    }
}