using System;
using System.Collections.Generic;
using System.Linq;

namespace OpBot
{
    internal class DefaultOperations
    {
        private Dictionary<ulong, int> _defaultOperations = new Dictionary<ulong, int>();

        public int this[ulong userId]
        {
            get
            {
                int operationId;
                lock (this)
                {
                    if (!_defaultOperations.TryGetValue(userId, out operationId))
                        operationId = 0;
                }
                return operationId;
            }
            set
            {
                lock (this)
                    _defaultOperations[userId] = value;

            }
        }
    }
}