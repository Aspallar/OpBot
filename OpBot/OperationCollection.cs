﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpBot
{
    internal class OperationCollection
    {
        public const int MaxOperations = 64;

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
                return _operations.Any(x => x.MessageId == messageId);
            }
        }

        private Operation GetOperation(int operationId)
        {
            if (operationId < 0 || operationId >= MaxOperations)
                return null;

            return (operationId == 0) ? GetDefaultOperation() : _operations[operationId - 1];
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

        private Operation GetDefaultOperation()
        {
            for (int k = 0; k < MaxOperations; k++)
            {
                if (_operations[k] != null)
                    return _operations[k];
            }
            return null;
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