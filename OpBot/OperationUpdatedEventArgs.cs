using System;

namespace OpBot
{
    internal class OperationUpdatedEventArgs : EventArgs
    {
        public IReadOnlyOperation Operation { get; set; }

        public OperationUpdatedEventArgs(IReadOnlyOperation operation)
        {
            Operation = operation;
        }
    }
}