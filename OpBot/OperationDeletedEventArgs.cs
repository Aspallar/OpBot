using System;

namespace OpBot
{
    internal class OperationDeletedEventArgs : EventArgs
    {
        public ulong OperationMessageId { get; internal set; }

        public OperationDeletedEventArgs(ulong messageId)
        {
            OperationMessageId = messageId;
        }
    }
}