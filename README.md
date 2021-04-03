## Biosensor Framework: A C# Static Library for Generic Affective Computing
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
For our lab to use biosensor data in our research projects, we needed an interface to pull in data from our Empatica E4, perform feature extraction, and return inferences from a trained model.  From our research, there was no existing all in one library to do this and major parts of this project did not exist.  Additionally, there was no Unity compatible way to process this data and perform actions from the output.  From a programming perspective, it is best if there was no string manipulation when interacting with the server, so there needed to be a true wrapper for the TCP client communications.  The development of the Biosensor Framework has allowed us to use these features reliably in all of our research projects.

### Basic Usage

### Class Documentation

### Citation

### Contact
To address issues in the codebase, please create an issue in the repo and it will be addressed by a maintainer.  For collaboration inquiries, please address Dr. James Gehringer (james.gehringer@unmc.edu).  For technical questions, please address Walker Arce (walker.arce@unmc.edu).
