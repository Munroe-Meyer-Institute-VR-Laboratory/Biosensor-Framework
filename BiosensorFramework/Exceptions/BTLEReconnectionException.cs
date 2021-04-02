using System;
using System.Runtime.Serialization;

namespace MMIVR.BiosensorFramework.Exceptions
{
    [Serializable]
    internal class BTLEReconnectionException : Exception
    {
        public BTLEReconnectionException()
        {
        }

        public BTLEReconnectionException(string message) : base(message)
        {
        }

        public BTLEReconnectionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected BTLEReconnectionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}