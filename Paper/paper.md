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
    orcid: 0000-0000-0000-0000
    affiliation: 1
affiliations:
 - name: Virtual Reality Laboratory in the Munroe Meyer Institute at the University of Nebraska Medical Center
   index: 1
date: 11 April 2021
bibliography: paper.bib
---

# Summary

Developed in the Virtual Reality Laboratory at the Munroe Meyer Institute, the Biosensor Framework library provides an interface for interacting with biosensors and performing affective computing tasks on their output data streams.  Currently, it natively supports the Empatica E4 [1] and communicates through a TCP connection with their provided server.  

This library provides the following capabilities:
- Connection to the TCP client that Empatica provides [2] for connecting their devices to a user's PC
- Parsing and packing of commands to communicate bidirectionally with the TCP client
- Collection and delivery of biometric readings to an end user's software application
- Extraction of literature supported feature vectors to allow characterization of the biological signal features
- Microsoft.ML [3] support for inferencing on the feature vectors natively in C# in both multi-class and binary classification tasks
- Support for simulating a data stream for debugging and testing applications without an Empatica E4 device
- Support for parsing open source Empatica E4 datasets and training machine learning models on them (i.e., WESAD [4])
- Support for interfacing new sensors into the same pipeline
- Contains models trained on WESAD dataset in the Microsoft.ML framework

# Background

Biosensor Framework is a C# library that handles the full process of gathering biometric data from a body-worn sensor, transforming it into handcrafted feature vectors, and delivering an inferencing result in twenty-two lines of code.  Purpose built to handle the Empatica E4 biosensor, the Empatica provided TCP client is wrapped in a static C# class to provide convenient function calls and error handling to simplify code structure.  With the introduction of Microsoft.ML, the entire pipeline is handled in C# managed code and is fully compatible with the Unity game engine [5], allowing for affective computing to be easily introduced into existing virtual reality therapy tools.  The decoupled structure of the library also allows for new biosensors to be introduced into the pipeline, requiring only a single code file to be added to retrieve data in a list of doubles.  

Generally, affective computing is concerned with the physiological changes of the subject and how that relates to their internal state.  Most implementations of affective computing look at electrodermal activity (EDA), like a lie detector test, to measure the onset of stress before the subject may even be aware of it [6 – 10].   There is a growing body of literature that incorporates the use of multiple body sensors, primarily worn on the wrist, such as photoplethysmography (PPG), three axis acceleration (ACC), and skin temperature (TMP) [9 – 15].  As these sensors are miniaturized, they can be easily incorporated into wrist-worn packages and can be fused with other sensor modalities, such as respiratory sensors, eye trackers, or body trackers, to estimate a subject’s emotional state more accurately.

Biosensor Framework incorporates the algorithms used in the WESAD dataset for computing feature vectors for all sensors on the Empatica E4: EDA, PPG, ACC, and TMP [4].  Additionally, the E4 provides estimations on the heart rate and inter-beat interval, as well as a button for manually tagging the data stream, which has been implemented in a function to mark relevant events.  To detrend the EDA signal, a time-varying, finite-impulse-response, high-pass filter is used [16].  For detecting major responses in the EDA, the method proposed by Healey is used [6].  To perform the Fast Fourier Transform, the DSPLib project was incorporated to allow real-time computation of the signal spectra [17].   

# Statement of need

In the application of virtual reality therapy tools for patients with autism spectrum disorder, the continual management of stress is of utmost importance [19] [20].  These tools can introduce stressful situations such as navigating an airport, handling an airplane ride, or crossing a busy intersection, which can be difficult or impossible to simulate in a controlled clinical environment [21 – 23].  This stress can be monitored using biosensors and literature supported machine learning models, which gives the clinician an additional layer of information for tuning the environment for its intended therapeutic effect.  Additionally, the library can be used in applications for monitoring and managing ambulatory stress for at-risk patients, allowing another layer of information for medical personnel to assess the state of their patients [13].

The most common tool to implement virtual reality therapy tools is the Unity game engine, which runs a C# interpreter based on the Mono compiler.  To reasonably use common open-source machine learning tools, such as Python, MATLAB, or R, would require the use of additional TCP servers to act as the intermediary between the virtual environment and the biosensor readings.  This adds an additional layer to the technology stack and, for Unity developers, requires the compilation and management of multiple projects. The Biosensor Framework library removes this additional layer and simplifies the machine learning down to simple function calls.

# Affect Classification Model Performance

We split the WESAD dataset into the binary classification and multi-class organization as specified in [4] and calculated the feature vectors accordingly.  We used a train-test split of 0.3 and trained using the standard Fit method built into the Microsoft.ML models.  The binary classification task was split into stress vs. non-stress and the multi-class classification task was split into baseline vs. stress vs. amusement.  The regression tasks also used the latter dataset.  The metrics were calculated from the test fit and were automatically generated by Microsoft.ML.

For our evaluation of the machine learning framework, we focused on the binary classification task of stress v. non-stress.  Microsoft.ML has built in models that allow for quick training, evaluation, and deployment of machine learning applications.  The ones that we evaluated were the Fast Forest and Fast Tree models.  The models evaluated performed very well in the binary classification task and the Fast Tree outperformed the Fast Forest.  For all three classification tasks the Fast Tree outperformed all other models.  

# Citations

Citations to entries in paper.bib should be in
[rMarkdown](http://rmarkdown.rstudio.com/authoring_bibliographies_and_citations.html)
format.

If you want to cite a software repository URL (e.g. something on GitHub without a preferred
citation) then you can do it with the example BibTeX entry below for @fidgit.

For a quick reference, the following citation commands can be used:
- `@author:2001`  ->  "Author et al. (2001)"
- `[@author:2001]` -> "(Author et al., 2001)"
- `[@author1:2001; @author2:2001]` -> "(Author1 et al., 2001; Author2 et al., 2002)"

# Acknowledgements

We would like to thank the Integrated Center for Autism Spectrum Disorder and the Munroe Meyer Guild at the Munroe Meyer Institute at the University of Nebraska Medical Center for supporting our work.

# References