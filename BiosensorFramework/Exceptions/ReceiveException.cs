using System;
using System.Runtime.Serialization;

namespace MMIVR.BiosensorFramework.Exceptions
{
    [Serializable]
    internal class ReceiveException : Exception
    {
        public ReceiveException()
        {
        }

        public ReceiveException(string message) : base(message)
        {
        }

        public ReceiveException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ReceiveException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}