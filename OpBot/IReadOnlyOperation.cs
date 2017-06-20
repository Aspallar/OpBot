using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpBot
{
    internal interface IReadOnlyOperation
    {
        int Id { get; }
        ulong MessageId { get; }
        string OperationName { get; }
        string GetOperationMessageText();
    }
}
