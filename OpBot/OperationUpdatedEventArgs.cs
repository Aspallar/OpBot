using System;

namespace OpBot
{
    internal class OperationUpdatedEventArgs : EventArgs
    {
        public Operation Operation { get; set; }

        public OperationUpdatedEventArgs(Operation operation)
        {
            Operation = operation;
        }
    }
}