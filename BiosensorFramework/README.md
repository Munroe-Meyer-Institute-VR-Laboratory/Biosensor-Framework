### Biosensor Framework: A C# Library for Affective Computing
C# library that handles the full process of gathering biometric data from a body-worn sensor, transforming it into handcrafted feature vectors, and delivering an inferencing result in thirty-five lines of code.

### Basic Usage

```
        public static ServerClient Device1;
        static ServerClient client;
        public static MLContext mlContext;
        public static ITransformer Model;
        public static DataViewSchema ModelSchema;
        // TODO: Fill these out with your own values
        public static string APIKey = "";
        public static string ServerPath = @"";
        public static string WesadDirectory = @"";
        public static string SessionOutputPath = @"";
```
For each project, a client connection is established using the ServerClient class.  This handles the TCP initialization, command communication, and error handling.  Each device will have its own ServerClient instance, so Device1 is represented by a device name and a TCP connection.  Additionally, the API key and the path to the server executable need to be defined.  The API key can be found by following Empatica's directions from the Installation section.  The path to the server is found in the installation directory for the server (i.e. @"{installation path}\EmpaticaBLEServer.exe").

Next the Microsoft.ML variables are created.
```
        static void Main(string[] args)
        {
            mlContext = new MLContext();
            Train.RunBenchmarks(WesadDirectory, out ITransformer RegModel, out ITransformer MultiModel, out Model);
            Console.ReadKey();
```
In this example, the models are trained on the WESAD data using the RunBenchmarks function.  The best performing models for each class are output.  For this example, the binary classification model (BinModel) is used.
```
            Console.WriteLine("E4 Console Interface - Press ENTER to begin the client");

            Console.WriteLine("Step 1 - Start Empatica server");
            Utilities.StartE4ServerGUI(ServerPath);
```
The E4 server is started through a Process call.  This will open the GUI for the server, if that is not desired, the command line variant of the command can be called.
```
            client = new ServerClient();
            Console.ReadKey();
            client.StartClient();
            Utilities.StartE4Server(ServerPath, APIKey);
```
The E4 server can also be started through the command line variant as shown.  
```
            Console.WriteLine("Step 2 - Getting connected E4 devices");
            Utilities.ListDiscoveredDevices(client);
            Console.ReadKey();
            Console.WriteLine("     Available Devices:");
            Utilities.AvailableDevices.ForEach(i => Console.WriteLine("     Device Name: {0}", i));
            Console.ReadKey();
```
Listing the connected devices will show all devices connected through the Bluetooth dongle.  These are managed internally.
```
            Device1 = new ServerClient();
            Device1.StartClient();
            Device1.DeviceName = Utilities.AvailableDevices[0];
            Utilities.ConnectDevice(Device1);
            Console.ReadKey();
```
To connect a device, it needs to be assigned one of the available devices.  Once that's done, the ConnectDevice function can be called and its TCP connection will be established.
```
            Utilities.SuspendStreaming(Device1);
```
Since there are configurations to be done on the device, the streaming needs to be suspended.
```
            Console.WriteLine("Step 3 - Adding biometric data streams");

            foreach (ServerClient.DeviceStreams stream in Enum.GetValues(typeof(ServerClient.DeviceStreams)))
            {
                Thread.Sleep(100);
                Console.WriteLine("     Adding new stream: " + stream.ToString());
                Utilities.SubscribeToStream(Device1, stream);
            }
```
Each available device stream is assigned, but any number of streams can be assigned.  
```
            Utilities.StartStreaming(Device1);
            var timer = Utilities.StartTimer(5);
            Utilities.WindowedDataReadyEvent += PullData;

            Console.WriteLine("ENTER to end program");
            Console.ReadKey();
        }
```
When the device has streaming restarted, it will begin collecting data as quickly as the server sends it.  The window size for this example is 5 seconds, but can be any length.  An internal timer will trigger an event at each expiration and then reset.  Ending the program can be done by pressing the 'Enter' key.
```
        private static void PullData()
        {
            var watch = Stopwatch.StartNew();
            var WindowData = Utilities.GrabWindow(Device1, Path.Combine(SessionOutputPath, "Readings.data"));
            var pred = Predict.PredictWindow(mlContext, Model, WindowData);
            watch.Stop();
            Console.WriteLine("Time: {0} | Prediction: {1}", DateTime.Now, pred.Prediction);
            Console.WriteLine("Processing Time: {0}", watch.ElapsedMilliseconds);
        }
```
The data can be manipulated once the timer expires for the window size.  The data can be grabbed by the GrabWindow function, which also can be fed a filepath to write the readings to a data file.  Predictions can be done on the raw data using the PredictWindow function and a prediction is returned in a simple structure that contains the output bool.

### Class Documentation
[Biosensor Framework Documentation](https://github.com/Munroe-Meyer-Institute-VR-Laboratory/Biosensor-Framework/blob/main/BiosensorFramework/BiosensorFrameworkDoc.md)

### Contact
To address issues in the codebase, please create an issue in the repo and it will be addressed by a maintainer.  For collaboration inquiries, please address Dr. James Gehringer (james.gehringer@unmc.edu).  For technical questions, please address Walker Arce (walker.arce@unmc.edu).
