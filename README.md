[![status](https://joss.theoj.org/papers/ce2098ed62af72faa9a4817dabdc34e3/status.svg)](https://joss.theoj.org/papers/ce2098ed62af72faa9a4817dabdc34e3) [![DOI](https://zenodo.org/badge/354086592.svg)](https://zenodo.org/badge/latestdoi/354086592)

## Biosensor Framework: A C# Library for Affective Computing
### Parties Involved
Institution: Munroe Meyer Institute in the University of Nebraska Medical Center<br>
Laboratory: Virtual Reality Laboratory<br>
Advisor: Dr. James Gehringer<br>
Developer: Walker Arce<br>

### Introduction
Developed in the Virtual Reality Laboratory at the Munroe Meyer Institute, this library provides an interface for interacting with biosensors and performing affective computing tasks on their output data streams.  Currently, it natively supports the [Empatica E4](https://www.empatica.com/research/e4/) and communicates through a TCP connection with their provided server.  

This library provides the following capabilities:
-	Connection to the [TCP client](https://github.com/empatica/ble-client-windows) that Empatica provides for connecting their devices to a user's PC
-	Parsing and packing of commands to communicate bidirectionally with the TCP client
-	Collection and delivery of biometric readings to an end user's software application
-	Extraction of literature supported feature vectors to allow characterization of the biological signal features
-	Microsoft.ML support for inferencing on the feature vectors natively in C# in both multi-class and binary classification tasks
-	Support for simulating a data stream for debugging and testing applications without an Empatica E4 device
-	Support for parsing open source Empatica E4 datasets and training machine learning models on them (i.e. [WESAD](https://archive.ics.uci.edu/ml/datasets/WESAD+%28Wearable+Stress+and+Affect+Detection%29))
-	Support for interfacing new sensors into the same pipeline
-	Contains models trained on WESAD dataset in the Microsoft.ML framework

### Motivation
For our lab to use biosensor data in our research projects, we needed an interface to pull in data from our Empatica E4, perform feature extraction, and return inferences from a trained model.  From our research, there was no existing all in one library to do this and major parts of this project did not exist.  Additionally, there was no Unity compatible way to process this data and perform actions from the output.  From a programming perspective, it is best if there was no string manipulation when interacting with the server, so there needed to be a true wrapper for the TCP client communications.  The development of the Biosensor Framework has allowed us to use these features reliably in all of our research projects.  There will be errors about the loaded dependencies, though the project should still run with those present.

### Dependencies

    .NETStandard 2.0
        LightGBM (>= 2.2.3)
        MathNet.Numerics (>= 4.15.0)
        Microsoft.ML (>= 1.5.5)
        Microsoft.ML.FastTree (>= 1.5.5)
        Microsoft.ML.LightGbm (>= 1.5.5)

### Installation
The library can be installed using the [NuGet package manager](https://www.nuget.org/packages/Biosensor-Framework/) and is listed publicly.  For Unity, downloading the package and importing it through the Unity UI is the best approach.  This includes all of the dependency libraries, a test script, a test scene, and an example output file.  This example performs the same tasks as Example2 and needs to be supplied the E4 Streaming Server exe path, an output path, and your API key for your device.

The following command can be used to install the package using the NuGet CLI:

`Install-Package Biosensor-Framework -Version 1.0.0`

### Unity Package
The included Unity package includes the required DLLs, a test script, and an example output file.  To install the package, clone this repository and open the target Unity project. 

Click on Assets -> Import Package -> Custom Package...

![image](https://user-images.githubusercontent.com/22334349/127775654-de8f4aeb-92c4-4d99-8c17-1b8f3b738e78.png)

Navigate to the [Unity Package](https://github.com/Munroe-Meyer-Institute-VR-Laboratory/Biosensor-Framework/tree/main/Unity%20Package) folder and click on the "BiosensorFramework_Unity.unitypackage" file.  When the file has loaded in, click "All" and "Import" to import the package into your project.

![image](https://user-images.githubusercontent.com/22334349/127775776-724fa58d-6365-4613-9268-a722b357a8b5.png)

### Testing Installation
Once the package has been installed, this repo can be cloned to get the test [datasets](https://github.com/Munroe-Meyer-Institute-VR-Laboratory/Biosensor-Framework/tree/main/Dataset) and the [trained models](https://github.com/Munroe-Meyer-Institute-VR-Laboratory/Biosensor-Framework/tree/main/Trained%20Models).  
Follow Example 1 to test the functionality of the Empatica E4 TCP server communication and window data gathering.  
Follow Example 2 to test the functionality of Microsoft.ML for affective computing tasks with a connected body-worn sensor.  
Follow Example 3 to demonstrate the functionality of Microsoft.ML for affective computing tasks using the WESAD dataset without a connected body-worn sensor. 

### Basic Usage
This repository has two example projects.  Example1 shows the basic usage to communicate with the server and pull data off of a biosensor.  Example2 expands Example1 by adding in Microsoft.ML inferencing.  Example2 will be used to explain the operation of the library.

```
        using System;
        using System.Diagnostics;
        using System.IO;
        using System.Threading;
        using Microsoft.ML;

        using MMIVR.BiosensorFramework.Biosensors.EmpaticaE4;
        using MMIVR.BiosensorFramework.MachineLearningUtilities;
        
        namespace Example2_ComputingWithMicrosoftML
        {
            class Program
            {
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
                public static string ModelDir = @"";
```
For each project, a client connection is established using the ServerClient class.  This handles the TCP initialization, command communication, and error handling.  Each device will have its own ServerClient instance, so Device1 is represented by a device name and a TCP connection.  Additionally, the API key and the path to the server executable need to be defined.  The API key can be found by following Empatica's directions from the Installation section.  The path to the server is found in the installation directory for the server (i.e. @"{installation path}\EmpaticaBLEServer.exe").

Next the Microsoft.ML variables are created.
```
        static void Main(string[] args)
        {
            mlContext = new MLContext();
            Train.RunBenchmarks(WesadDirectory, out ITransformer RegModel, out ITransformer MultiModel, out Model, ModelDir);
```
In this example, the models are trained on the WESAD data using the RunBenchmarks function.  The best performing models for each class are output.  For this example, the binary classification model (BinModel) is used.
```
            Console.WriteLine("E4 Console Interface - Press ENTER to begin the client");
            Console.ReadKey();

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
            var WindowData = Utilities.GrabWindow(Device1, @"C:\Readings.data");
            var pred = Predict.PredictWindow(mlContext, Model, WindowData);
            watch.Stop();
            Console.WriteLine("Time: {0} | Prediction: {1}", DateTime.Now, pred.Prediction);
            Console.WriteLine("Processing Time: {0}", watch.ElapsedMilliseconds);
        }
```
The data can be manipulated once the timer expires for the window size.  The data can be grabbed by the GrabWindow function, which also can be fed a filepath to write the readings to a data file.  Predictions can be done on the raw data using the PredictWindow function and a prediction is returned in a simple structure that contains the output bool.

### Class Documentation
[Biosensor Framework Documentation](https://github.com/Munroe-Meyer-Institute-VR-Laboratory/Biosensor-Framework/blob/main/BiosensorFramework/BiosensorFrameworkDoc.md)

### Paper Citation
Arce, W., Gehringer, J., (2021). Biosensor Framework: A C# Library for Affective Computing. Journal of Open Source Software, 6(64), 3455, https://doi.org/10.21105/joss.03455

### Contact
To address issues in the codebase, please create an issue in the repo and it will be addressed by a maintainer.  For collaboration inquiries, please address Dr. James Gehringer (james.gehringer@unmc.edu).  For technical questions, please address Walker Arce (walker.arce@unmc.edu).
