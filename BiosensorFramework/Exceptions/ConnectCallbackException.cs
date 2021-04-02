using System;
using System.Runtime.Serialization;

namespace MMIVR.BiosensorFramework.Exceptions
{
    [Serializable]
    internal class ConnectCallbackException : Exception
    {
        public ConnectCallbackException()
        {
        }

        public ConnectCallbackException(string message) : base(message)
        {
        }

        public ConnectCallbackException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ConnectCallbackException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}