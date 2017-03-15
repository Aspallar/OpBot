using System;

namespace OpBot
{
    [Serializable]
    public class OpbotInvalidValueException : Exception
    {
        public OpbotInvalidValueException() { }
        public OpbotInvalidValueException(string message) : base(message) { }
        public OpbotInvalidValueException(string message, Exception inner) : base(message, inner) { }
        protected OpbotInvalidValueException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }    

}
