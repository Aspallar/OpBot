using System;

namespace OpBot
{
    [Serializable]
    internal class OperationMember
    {
        public ulong UserId { get; set; }
        public string UserName { get; set; }
        public string PrimaryRole { get; set; }
    }
}
