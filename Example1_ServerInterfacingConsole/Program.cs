using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.ML;

using MMIVR.BiosensorFramework.Biosensors.EmpaticaE4;
using MMIVR.BiosensorFramework.MachineLearningUtilities;

namespace Example1_ServerInterfacingConsole
{
    class Program
    {
        public static ServerClient Device1;
        static ServerClient client;
        public static MLContext mlContext;
        public static ITransformer Model;
        public static DataViewSchema ModelSchema;
        public static string APIKey = "";

        static void Main(string[] args)
        {
            //mlContext = new MLContext();
            //Train.RunBenchmarks(out ITransformer RegModel, out ITransformer MultiModel, out ITransformer BinModel);
            //Console.ReadKey();
            Console.WriteLine("E4 Console Interface - Press ENTER to begin the client");

            Console.WriteLine("Step 1 - Start Empatica server");
            Utilities.StartE4ServerGUI();

            client = new ServerClient();
            Console.ReadKey();
            client.StartClient();
            Utilities.StartE4Server(APIKey);

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
            var watch = Stopwatch.StartNew();
            var WindowData = Utilities.GrabWindow(Device1, @"C:\Users\Walker Arce\Documents\Business\Research\UNMC\Software\CSharp\E4-Inferencing\ConsoleInterfacing\E4ServerInterfacing\Saved Readings\Readings.e4");
            var pred = Predict.PredictWindow(mlContext, Model, WindowData);
            watch.Stop();
            Console.WriteLine("Time: {0} | Prediction: {1}", DateTime.Now, pred.Prediction);
            Console.WriteLine("Processing Time: {0}", watch.ElapsedMilliseconds);
        }
    }
}
