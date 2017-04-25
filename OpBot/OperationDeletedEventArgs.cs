using System;

namespace OpBot
{
    internal class OperationDeletedEventArgs : EventArgs
    {
        public ulong MessageId { get; set; }

        public OperationDeletedEventArgs(ulong messageId)
        {
            MessageId = messageId;
        }
    }
}