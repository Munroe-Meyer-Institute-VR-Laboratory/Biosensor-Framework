using System;
using System.Runtime.Serialization;

namespace MMIVR.BiosensorFramework.Exceptions
{
    [Serializable]
    internal class DeviceNotConnectedException : Exception
    {
        public DeviceNotConnectedException()
        {
        }

        public DeviceNotConnectedException(string message) : base(message)
        {
        }

        public DeviceNotConnectedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DeviceNotConnectedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}