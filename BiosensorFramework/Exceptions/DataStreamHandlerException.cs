using System;
using System.Runtime.Serialization;

namespace MMIVR.BiosensorFramework.Exceptions
{
    [Serializable]
    internal class DataStreamHandlerException : Exception
    {
        private string v;
        private string message;

        public DataStreamHandlerException()
        {
        }

        public DataStreamHandlerException(string message) : base(message)
        {
        }

        public DataStreamHandlerException(string v, string message)
        {
            this.v = v;
            this.message = message;
        }

        public DataStreamHandlerException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DataStreamHandlerException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}