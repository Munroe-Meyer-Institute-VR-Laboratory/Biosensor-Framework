using System;
using System.Runtime.Serialization;

namespace MMIVR.BiosensorFramework.Exceptions
{
    [Serializable]
    internal class DeviceNotAvailableException : Exception
    {
        public DeviceNotAvailableException()
        {
        }

        public DeviceNotAvailableException(string message) : base(message)
        {
        }

        public DeviceNotAvailableException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DeviceNotAvailableException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}