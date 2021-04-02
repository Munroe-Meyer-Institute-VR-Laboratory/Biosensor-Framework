using System;
using System.Runtime.Serialization;

namespace MMIVR.BiosensorFramework.Exceptions
{
    [Serializable]
    internal class HandleResponseException : Exception
    {
        private string v;
        private string response;
        private string message;

        public HandleResponseException()
        {
        }

        public HandleResponseException(string message) : base(message)
        {
        }

        public HandleResponseException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public HandleResponseException(string v, string response, string message)
        {
            this.v = v;
            this.response = response;
            this.message = message;
        }

        protected HandleResponseException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}