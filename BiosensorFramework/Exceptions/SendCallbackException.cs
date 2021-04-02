using System;
using System.Runtime.Serialization;

namespace MMIVR.BiosensorFramework.Exceptions
{
    [Serializable]
    internal class SendCallbackException : Exception
    {
        public SendCallbackException()
        {
        }

        public SendCallbackException(string message) : base(message)
        {
        }

        public SendCallbackException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SendCallbackException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}