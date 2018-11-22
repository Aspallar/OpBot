using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpBot
{
    [Serializable]
    internal class OperationManager
    {
        private static ILog log = LogManager.GetLogger(typeof(OperationManager));

        public const int MaxOperations = 14;  // if more than this needed then guild needs a proper raid planner, not discord

        private Operation[] _operations = new Operation[MaxOperations];

        [NonSerialized]
        private bool _started = false;

        public void Start()
        {
            lock (this)
            {
                if (_started)
                    return;
                _started = true;
            }
            Task.Run(AutoCloseExpiredOperations);
        }

        private async Task AutoCloseExpiredOperations()
        {
            const int shortPeriod = 5000; // 5 seconds
#if DEBUG
            const int period = 10000;
#else
            const int period = 600000; // 10 minutes
#endif
            while (true)
            {
                DateTime now = DateTime.Now.ToUniversalTime();
                ulong messageId = 0;
                int opIndex;
                lock (this)
                {
                    opIndex = Array.FindIndex(_operations, x => x != null && x.Date < now);
                    if (opIndex != -1)
                    {
                        messageId = _operations[opIndex].MessageId;
                        _operations[opIndex] = null;
                    }
                }
                int delayLength;
                if (messageId > 0)
                {
                    delayLength = shortPeriod;
                    await _operationDeleted.InvokeAsync(new OperationDeletedEventArgs(messageId));
                    log.Info($"Autoclosed operation [{opIndex}] [{messageId}]");
                }
                else
                {
                    delayLength = period;
                }
                await Task.Delay(delayLength);
            }
        }

        public IReadOnlyOperation Add(Operation operation)
        {
            lock (this)
            {
                int slot = GetNextOperationSlot();
                operation.Id = slot + 1;
                _operations[slot] = operation;
            }
            return operation;
        }

        public bool GetOperationDate(int operationId, out DateTime operationDate)
        {
            lock (this)
            {
                Operation op = GetOperation(operationId);
                if (op == null)
                {
                    operationDate = new DateTime();
                    return false;
                }
                else
                {
                    operationDate = op.Date;
                    return true;
                }
            }
        }

        public async Task<bool> Signup(int operationId, ulong userId, string userName, string role)
        {
            Operation op;
            lock (this)
            {
                op = GetOperation(operationId);
                if (op == null)
                    return false;
                op.Signup(userId, userName, role);
            }
            await _operationUpdated.InvokeAsync(new OperationUpdatedEventArgs(op));
            return true;
        }

        public async Task<bool> RemoveSignup(int operationId, ulong userId)
        {
            Operation op;
            lock (this)
            {
                op = GetOperation(operationId);
                if (op == null)
                    return false;
                op.Remove(userId);
            }
            await _operationUpdated.InvokeAsync(new OperationUpdatedEventArgs(op));
            return true;
        }

        public async Task<bool> SetOperationRoles(int operationId, string name, ulong userId, string[] roles)
        {
            Operation op;
            lock (this)
            {
                op = GetOperation(operationId);
                if (op == null)
                    return false;
                op.SetAltRoles(name, userId, roles);
            }
            await _operationUpdated.InvokeAsync(new OperationUpdatedEventArgs(op));
            return true;
        }

        public async Task<bool> UpdateOperation(int operationId, OperationParameters opParams)
        {
            Operation op;
            lock (this)
            {
                op = GetOperation(operationId);
                if (op == null)
                    return false;

                if (opParams.HasOperationCode)
                {
                    op.OperationName = opParams.OperationCode;
                }
                if (opParams.HasTime)
                {
                    op.Date = op.Date.Date + opParams.Time;
                }
                if (opParams.HasMode)
                {
                    op.Mode = opParams.Mode;
                }
                if (opParams.HasSize)
                {
                    op.Size = opParams.Size;
                }
                if (opParams.HasSide)
                {
                    op.Side = opParams.Side;
                }
                if (opParams.HasDay)
                {
                    DateTime newDate = DateHelper.GetDateForNextOccuranceOfDay(opParams.Day);
                    op.Date = newDate + op.Date.TimeOfDay;
                }
            }
            await _operationUpdated.InvokeAsync(new OperationUpdatedEventArgs(op));
            return true;
        }

        public async Task<bool> AddOperationNote(int operationId, string noteText)
        {
            Operation op;
            lock (this)
            {
                op = GetOperation(operationId);
                if (op == null)
                    return false;
                op.AddNote(noteText);
            }
            await _operationUpdated.InvokeAsync(new OperationUpdatedEventArgs(op));
            return true;
        }

        internal bool IsActiveOperation(int operationId)
        {
            lock (this)
                return _operations[operationId - 1] != null;
        }

        public async Task<bool> DeleteOperationNote(int operationId, int noteIndex)
        {
            Operation op;
            lock (this)
            {
                op = GetOperation(operationId);
                if (op == null)
                    return false;
                if (noteIndex == -1)
                    op.ResetNotes();
                else
                    op.DeleteNote(noteIndex);
            }
            await _operationUpdated.InvokeAsync(new OperationUpdatedEventArgs(op));
            return true;
        }


        public async Task<bool> Delete(int id)
        {
            ulong messageId = 0;
            int index = id - 1;
            lock (this)
            {
                if (index < 0 || index >= _operations.Length || _operations[index] == null)
                    return false;
                messageId = _operations[index].MessageId;
                _operations[index] = null;
            }
            if (messageId != 0)
                await _operationDeleted.InvokeAsync(new OperationDeletedEventArgs(messageId));
            return true;
        }

        public bool IsOperationMessage(ulong messageId)
        {
            lock (this)
            {
                return _operations.Any(x => x != null && x.MessageId == messageId);
            }
        }

        public string GetSummary()
        {
            StringBuilder sb = new StringBuilder(512);
            lock (this)
            {
                foreach (Operation op in _operations)
                {
                    if (op != null)
                    {
                        sb.Append(DiscordText.BigText(op.Id));
                        sb.Append(' ');
                        sb.Append(op.OperationName);
                        sb.AppendLine(op.Date.ToString(" (dddd)"));
                    }
                }
            }
            return sb.ToString();
        }

        private Operation GetOperation(int operationId)
        {
            if (operationId < 0 || operationId >= MaxOperations)
                return null;

            return (operationId == 0) ? GetDefaultOperation() : _operations[operationId - 1];
        }

        private int GetNextOperationSlot()
        {
            int slot = Array.FindIndex(_operations, x => x == null);
            if (slot == -1)
                throw new OperationException($"The maximum amount of operations, {MaxOperations}, has been reached.");
            return slot;
        }

        private Operation GetDefaultOperation()
        {
            return _operations.Where(x => x != null).FirstOrDefault();
        }

        public void WireUp()
        {
            _operationDeleted = new AsyncEvent<OperationDeletedEventArgs>();
            _operationUpdated = new AsyncEvent<OperationUpdatedEventArgs>();
        }

        public event AsyncEventHandler<OperationDeletedEventArgs> OperationDeleted
        {
            add { this._operationDeleted.Register(value); }
            remove { this._operationDeleted.Unregister(value); }
        }
        [NonSerialized]
        private AsyncEvent<OperationDeletedEventArgs> _operationDeleted = new AsyncEvent<OperationDeletedEventArgs>();

        public IReadOnlyOperation[] GetOperationsByDateDesc()
        {
            lock (this)
            {
                return _operations.Where(x => x != null).OrderByDescending(x => x.Date).ToArray();
            }
        }

        public event AsyncEventHandler<OperationUpdatedEventArgs> OperationUpdated
        {
            add { this._operationUpdated.Register(value); }
            remove { this._operationUpdated.Unregister(value); }
        }
        [NonSerialized]
        private AsyncEvent<OperationUpdatedEventArgs> _operationUpdated = new AsyncEvent<OperationUpdatedEventArgs>();

        internal ulong UpdateMessageId(int id, ulong newMessageID)
        {
            lock (this)
            {
                int index = id - 1;
                if (_operations[index] == null)
                    return 0;
                ulong messageId = _operations[index].MessageId;
                _operations[index].MessageId = newMessageID;
                return messageId;
            }
        }
    }
}
