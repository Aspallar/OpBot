namespace OpBot
{
    internal class CommandProcessorConfig
    {
        public NicknameList Names { get; set; }
        public ulong OpBotUserId { get;  set; }
        public Operation Operation { get; set; }
        public OperationRepository Repository { get; internal set; }
    }
}