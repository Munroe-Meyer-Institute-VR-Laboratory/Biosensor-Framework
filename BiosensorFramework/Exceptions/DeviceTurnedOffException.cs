using System;
using System.Runtime.Serialization;

namespace MMIVR.BiosensorFramework.Exceptions
{
    [Serializable]
    internal class DeviceTurnedOffException : Exception
    {
        public DeviceTurnedOffException()
        {
        }

        public DeviceTurnedOffException(string message) : base(message)
        {
        }

        public DeviceTurnedOffException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DeviceTurnedOffException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}