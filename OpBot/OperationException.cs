using System;

namespace OpBot
{
    [Serializable]
    public class OpBotInvalidValueException : Exception
    {
        public OpBotInvalidValueException() { }
        public OpBotInvalidValueException(string message) : base(message) { }
        public OpBotInvalidValueException(string message, Exception inner) : base(message, inner) { }
        protected OpBotInvalidValueException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }    

}
