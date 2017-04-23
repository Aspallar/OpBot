using System;
using System.Runtime.Serialization;

namespace OpBot
{
    [Serializable]
    internal class CommandParseException : Exception
    {
        public CommandParseException()
        {
        }

        public CommandParseException(string message) : base(message)
        {
        }

        public CommandParseException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected CommandParseException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}