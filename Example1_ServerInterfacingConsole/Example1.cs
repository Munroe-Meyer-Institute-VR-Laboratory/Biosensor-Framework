using System;
using System.Threading;

using MMIVR.BiosensorFramework.Biosensors.EmpaticaE4;

namespace Example1_ServerInterfacingConsole
{
    class Program
    {
        public static ServerClient Device1;
        static ServerClient client;
        // TODO: Fill these out with your own values
        public static string APIKey = "";
        public static string ServerPath = "";

        static void Main(string[] args)
        {
            Console.WriteLine("E4 Console Interface - Press ENTER to begin the client");

            Console.WriteLine("Step 1 - Start Empatica server");
            Utilities.StartE4ServerGUI(ServerPath);

            client = new ServerClient();
            Console.ReadKey();
            client.StartClient();
            Utilities.StartE4Server(ServerPath, APIKey);

            Console.WriteLine("Step 2 - Getting connected E4 devices");
            Utilities.ListDiscoveredDevices(client);
            Console.ReadKey();

            Console.WriteLine("     Available Devices:");
            Utilities.AvailableDevices.ForEach(i => Console.WriteLine("     Device Name: {0}", i));
            Console.ReadKey();

            Device1 = new ServerClient();
            Device1.StartClient();
            Device1.DeviceName = Utilities.AvailableDevices[0];
            Utilities.ConnectDevice(Device1);
            Console.ReadKey();

            Utilities.SuspendStreaming(Device1);
            Console.WriteLine("Step 3 - Adding biometric data streams");

            foreach (ServerClient.DeviceStreams stream in Enum.GetValues(typeof(ServerClient.DeviceStreams)))
            {
                Thread.Sleep(100);
                Console.WriteLine("     Adding new stream: " + stream.ToString());
                Utilities.SubscribeToStream(Device1, stream);
            }

            Utilities.StartStreaming(Device1);
            var timer = Utilities.StartTimer(5);
            Utilities.WindowedDataReadyEvent += PullData;

            Console.WriteLine("ENTER to end program");
            Console.ReadKey();
        }
        private static void PullData()
        {
            var WindowData = Utilities.GrabWindow(Device1, @"C:\Readings.data");
            foreach (ServerClient.DeviceStreams data in Enum.GetValues(typeof(ServerClient.DeviceStreams)))
            {
                Console.WriteLine("Writing {0} data from last window:", data.ToString());
                for (int i = 0; i < WindowData[(int)data].Count; i++)
                {
                    Console.WriteLine("     {0}", WindowData[(int)data][i]);
                }
            }
        }
    }
}
