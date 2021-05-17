---
title: 'Biosensor Framework: A C# Library for Affective Computing'
tags:
  - C#
  - Affective Computing
  - Biometrics
  - Machine Learning
  - Unity
authors:
  - name: Walker Arce
    orcid: 0000-0003-0819-0710
    affiliation: 1
  - name: James Gehringer, PhD
    orcid: 0000-0003-3457-2288
    affiliation: 1
affiliations:
 - name: Virtual Reality Laboratory in the Munroe Meyer Institute at the University of Nebraska Medical Center
   index: 1
date: 15 May 2021
bibliography: paper.bib
---

# Summary

Developed in the Virtual Reality Laboratory at the Munroe Meyer Institute, the Biosensor Framework library provides an interface for interacting with biosensors and performing affective computing tasks on their output data streams.  Currently, it natively supports the Empatica E4 biosensor and communicates through a TCP connection with their provided server.  

This library provides the following capabilities:
- Connection to the [TCP client](https://github.com/empatica/ble-client-windows) that Empatica provides for connecting their devices to a user's PC
- Parsing and packing of commands to communicate bidirectionally with the TCP client
- Collection and delivery of biometric readings to an end user's software application
- Extraction of literature supported feature vectors to allow characterization of the biological signal features
- Microsoft.ML support for inferencing on the feature vectors natively in C# in both multi-class and binary classification tasks
- Support for simulating a data stream for debugging and testing applications without an Empatica E4 device
- Support for parsing open source Empatica E4 datasets and training machine learning models on them (i.e., WESAD [@schmidt2018introducing])
- Support for interfacing new sensors into the same pipeline
- Contains models trained on WESAD dataset in the Microsoft.ML framework

# Background

Biosensor Framework is a C# library that handles the full process of gathering biometric data from a body-worn sensor, transforming it into handcrafted feature vectors, and delivering an inferencing result in thirty-five lines of code.  Purpose built to handle the Empatica E4 biosensor, the Empatica provided TCP client is wrapped in a static C# class to provide convenient function calls and error handling to simplify code structure.  With the introduction of Microsoft.ML, the entire pipeline is handled in C# managed code and is fully compatible with the Unity game engine, allowing for affective computing to be easily introduced into existing virtual reality therapy tools.  The decoupled structure of the library also allows for new biosensors to be introduced into the pipeline, requiring only a single code file to be added to retrieve data in a list of doubles.  

Generally, affective computing is concerned with the physiological changes of the subject and how that relates to their internal state.  Most implementations of affective computing look at electrodermal activity (EDA), like a lie detector test, to measure the onset of stress before the subject may even be aware of it [@healey2000wearable; @healey2005detecting; @aqajari2020gsr; @kutt2018towards; @gjoreski2016continuous].   There is a growing body of literature that incorporates the use of multiple body sensors, primarily worn on the wrist, such as photoplethysmography (PPG), three axis acceleration (ACC), and skin temperature (TMP) [@kutt2018towards; gjoreski2016continuous; @zhou2017indexing; @van2019heartpy; @kleiman2019using; @kaczor2020objective; @choi2011development].  As these sensors are miniaturized, they can be easily incorporated into wrist-worn packages and can be fused with other sensor modalities, such as respiratory sensors, eye trackers, or body trackers, to estimate a subject’s emotional state more accurately.

Biosensor Framework incorporates the algorithms used in the WESAD dataset for computing feature vectors for all sensors on the Empatica E4: EDA, PPG, ACC, and TMP [-@schmidt2018introducing].  Additionally, the E4 provides estimations on the heart rate and inter-beat interval, as well as a button for manually tagging the data stream, which has been implemented in a function to mark relevant events.  To detrend the EDA signal, a time-varying, finite-impulse-response, high-pass filter is used [@tarvainen2002advanced].  For detecting major responses in the EDA, the method proposed by Healey is used [@healey2000wearable].  To perform the Fast Fourier Transform, the [DSPLib](https://www.codeproject.com/Articles/1107480/DSPLib-FFT-DFT-Fourier-Transform-Library-for-NET-6) project was incorporated to allow real-time computation of the signal spectra.  Finally, a stillness metric was added to compute a unitless measure of movement from the accelerometer signals [@chang2012wireless].

# Statement of need

In the application of virtual reality therapy tools for patients with autism spectrum disorder, the continual management of stress is of utmost importance [@kildahl2019identification; @bishop2015relationship].  These tools can introduce stressful situations such as navigating an airport, handling an airplane ride, or crossing a busy intersection, which can be difficult or impossible to simulate in a controlled clinical environment [@poyade2017using; @miller2020virtual; @saiano2015natural].  This stress can be monitored using biosensors and literature supported machine learning models, which gives the clinician an additional layer of information for tuning the environment for its intended therapeutic effect.  Additionally, the library can be used in applications for monitoring and managing ambulatory stress for at-risk patients, allowing another layer of information for medical personnel to assess the state of their patients [@kleiman2019using].

The most common tool to implement virtual reality therapy tools is the Unity game engine, which runs a C# interpreter based on the Mono compiler.  To reasonably use common open-source machine learning tools, such as Python, MATLAB, or R, would require the use of additional TCP servers to act as the intermediary between the virtual environment and the biosensor readings.  This adds an additional layer to the technology stack and, for Unity developers, requires the compilation and management of multiple projects. The Biosensor Framework library removes this additional layer and simplifies the machine learning down to simple function calls.

# Affect Classification Model Performance

We split the WESAD dataset into the binary classification and multi-class organization as specified in [-@schmidt2018introducing] and calculated the feature vectors accordingly.  We used a train-test split of 0.3 and trained using the standard Fit method built into the Microsoft.ML models.  The binary classification task was split into _stress vs. non-stress_ and the multi-class classification task was split into _baseline vs. stress vs. amusement_.  The regression tasks also used the latter dataset.  The metrics were calculated from the test fit and were automatically generated by Microsoft.ML.

Regression Model Metrics

|                            |     Fast Forest    |     Fast Tree    |     Fast Tree Tweedie    |     LBFGS    |
|----------------------------|--------------------|------------------|--------------------------|--------------|
|     Loss Function          |     19.54          |     11.34        |     12.27                |     37.11    |
|     Mean Absolute Error    |     28.33          |     16.64        |     16.63                |     45.89    |
|     Mean Squared Error     |     19.54          |     11.14        |     12.27                |     37.11    |
|     RMS Error              |     44.20          |     33.67        |     35.03                |     60.92    |
|     R Squared              |     65.17          |     **79.79**        |     78.12                |     26.11    |

Multi-Class Classification Metrics
|                          |     Fast Forest    |     Fast Tree    |     LBFGS Regression    |     LBFGS Max Entropy    |
|--------------------------|--------------------|------------------|-------------------------|--------------------------|
|     Log Loss             |     31.30          |     22.27        |     45.11               |     41.51                |
|     LL Reduction         |     68.51          |     77.59        |     51.82               |     55.67                |
|     Macro Acc            |     84.59          |     **93.78**        |     68.80               |     71.97                |
|     Micro Acc            |     89.06          |     **95.40**        |     84.76               |     85.40                |
|     Class 0 Precision    |     93.72          |     95.90        |     83.09               |     84.69                |
|     Class 0 Recall       |     93.36          |     97.82        |     95.85               |     94.54                |
|     Class 0 Loss         |     22.03          |     9.92         |     23.13               |     23.59                |
|     Class 1 Precision    |     83.98          |     98.34        |     91.46               |     90.56                |
|     Class 1 Recall       |     92.97          |     94.73        |     91.09               |     91.30                |
|     Class 1 Loss         |     25.96          |     27.68        |     42.37               |     34.16                |
|     Class 2 Precision    |     83.21          |     88.53        |     63.64               |     85.40                |
|     Class 2 Recall       |     67.26          |     88.79        |     19.44               |     30.09                |
|     Class 2 Loss         |     71.23          |     52.41        |     152.03              |     140.35               |

Binary Classification Metrics
|                           |     Fast Forest    |     Fast Tree    |
|---------------------------|--------------------|------------------|
|     Accuracy              |     92.35          |     **96.85**        |
|     AUPRC                 |     97.95          |     99.05        |
|     AURC                  |     97.39          |     98.88        |
|     F1 Score              |     92.86          |     97.06        |
|     Negative Precision    |     92.34          |     97.07        |
|     Negative Recall       |     91.19          |     96.17        |
|     Positive Precision    |     92.36          |     96.67        |
|     Positive Recall       |     93.37          |     97.46        |

For our evaluation of the machine learning framework, we focused on the binary classification task of _stress v. non-stress_.  Microsoft.ML has built in models that allow for quick training, evaluation, and deployment of machine learning applications.  The ones that we evaluated were the Fast Forest and Fast Tree models.  The models evaluated performed very well in the binary classification task and the Fast Tree outperformed the Fast Forest.  For all three classification tasks the Fast Tree outperformed all other models.  

# Acknowledgements

We would like to thank the Integrated Center for Autism Spectrum Disorder and the Munroe Meyer Guild at the Munroe Meyer Institute at the University of Nebraska Medical Center for supporting our work.

# References
