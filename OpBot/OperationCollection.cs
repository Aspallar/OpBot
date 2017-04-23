using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpBot
{
    internal class OperationCollection
    {
        public const int MaxOperations = 64;

        private int _operationCount = 0;
        private Operation[] _operations = new Operation[MaxOperations];

        public Operation Add(Operation operation)
        {
            lock (this)
            {
                int slot = GetNextOperationSlot();
                operation.Id = slot + 1;
                _operations[slot] = operation;
            }
            return operation;
        }

        public async Task<bool> Signup(int operationId, ulong userId, string userName, string role)
        {
            Operation op;
            lock (this)
            {
                if (operationId < 0 || operationId >= MaxOperations)
                    return false;

                op = (operationId == 0) ? GetDefaultOperation() : _operations[operationId-1];
                if (op == null)
                    return false;

                op.Signup(userId, userName, role);
            }
            await _operationUpdated.InvokeAsync(new OperationUpdatedEventArgs(op));
            return true;
        }

        private Operation GetDefaultOperation()
        {
            for (int k = 0; k < MaxOperations; k++)
            {
                if (_operations[k] != null)
                    return _operations[k];
            }
            return null;
        }

        public async Task Delete(int id)
        {
            ulong messageId;
            lock (this)
            {
                messageId = _operations[id].MessageId;
                _operations[id] = null;
            }
            if (messageId != 0)
                await _operationDeleted.InvokeAsync(new OperationDeletedEventArgs(messageId));
        }

        private int GetNextOperationSlot()
        {
            for (int k = 0; k < MaxOperations; k++)
            {
                if (_operations[k] == null)
                    return k;
            }
            // TODO: proper exception
            throw new Exception("The maximum amount of operations has been reached");
        }

        public event AsyncEventHandler<OperationDeletedEventArgs> OperationDeleted
        {
            add { this._operationDeleted.Register(value); }
            remove { this._operationDeleted.Unregister(value); }
        }
        private AsyncEvent<OperationDeletedEventArgs> _operationDeleted = new AsyncEvent<OperationDeletedEventArgs>();


        public event AsyncEventHandler<OperationUpdatedEventArgs> OperationUpdated
        {
            add { this._operationUpdated.Register(value); }
            remove { this._operationUpdated.Unregister(value); }
        }
        private AsyncEvent<OperationUpdatedEventArgs> _operationUpdated = new AsyncEvent<OperationUpdatedEventArgs>();
    }
}
