# Biosensor Framework: A C# Static Library for Generic Affective Computing

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
