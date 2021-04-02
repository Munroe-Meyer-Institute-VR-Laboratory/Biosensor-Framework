using System;
using System.Runtime.Serialization;

namespace MMIVR.BiosensorFramework.Exceptions
{
    [Serializable]
    internal class DataNotSupportedException : Exception
    {
        public DataNotSupportedException()
        {
        }

        public DataNotSupportedException(string message) : base(message)
        {
        }

        public DataNotSupportedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DataNotSupportedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}