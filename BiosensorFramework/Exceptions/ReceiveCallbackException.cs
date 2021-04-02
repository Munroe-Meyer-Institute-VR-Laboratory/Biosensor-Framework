using System;
using System.Runtime.Serialization;

namespace MMIVR.BiosensorFramework.Exceptions
{
    [Serializable]
    internal class ReceiveCallbackException : Exception
    {
        public ReceiveCallbackException()
        {
        }

        public ReceiveCallbackException(string message) : base(message)
        {
        }

        public ReceiveCallbackException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ReceiveCallbackException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}