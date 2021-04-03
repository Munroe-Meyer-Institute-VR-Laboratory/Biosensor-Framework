﻿using Microsoft.ML;
using System;
using System.Collections.Generic;
using System.IO;
using static Microsoft.ML.DataOperationsCatalog;
using MathNet.Numerics.Statistics;

using MMIVR.BiosensorFramework.Extensions;
using MMIVR.BiosensorFramework.InputPipeline;

namespace MMIVR.BiosensorFramework.MachineLearningUtilities
{
    class DataImport
    {
        public static List<Tuple<string, List<double[]>, double[]>> LoadDataset(string DirectoryPath = @"C:\Users\Walker Arce\Documents\Business\Research\UNMC\Software\Python\MachineLearning\E4Inferencing\datasets\")
        {
            string[] files = Directory.GetFiles(DirectoryPath, "*.csv", SearchOption.AllDirectories);
            List<Tuple<string, List<double[]>, double[]>> Dataset = new List<Tuple<string, List<double[]>, double[]>>();
            foreach (string file in files)
            {
                using (StreamReader reader = new StreamReader(file))
                {
                    List<double[]> SubjectData = new List<double[]>();
                    double[] SubjectLabels = null;
                    int DataLines = 4;
                    while (!reader.EndOfStream)
                    {
                        if (DataLines == 4)
                        {
                            var line = reader.ReadLine();
                            double[] Acc = Array.ConvertAll(line.Split(','), double.Parse);
                            double[] AccX = new double[Acc.Length / 3],
                                AccY = new double[Acc.Length / 3],
                                AccZ = new double[Acc.Length / 3];
                            for (int i = 0; i < Acc.Length; i += 3)
                            {
                                AccX[i / 3] = Acc[i];
                                AccY[i / 3] = Acc[i + 1];
                                AccZ[i / 3] = Acc[i + 2];
                            }
                            SubjectData.Add(Acc);
                            SubjectData.Add(AccX);
                            SubjectData.Add(AccY);
                            SubjectData.Add(AccZ);
                            DataLines--;
                        }
                        if (DataLines < 4 && DataLines != 0)
                        {
                            var line = reader.ReadLine();
                            SubjectData.Add(Array.ConvertAll(line.Split(','), double.Parse));
                            DataLines--;
                        }
                        else
                        {
                            var line = reader.ReadLine();
                            SubjectLabels = Array.ConvertAll(line.Split(','), double.Parse);
                        }
                    }
                    Dataset.Add(Tuple.Create(Path.GetFileNameWithoutExtension(file), SubjectData, SubjectLabels));
                }
            }
            return Dataset;
        }
        /// <summary>
        /// Pipeline implemented to process the WESAD dataset.
        /// </summary>
        /// Schmidt, Philip, et al. "Introducing wesad, a multimodal dataset for wearable stress and affect detection." 
        ///     Proceedings of the 20th ACM International Conference on Multimodal Interaction. 2018.
        /// <param name="WindowSize"></param>
        public static void SchmidtDatasetPipeline(MLContext mlContext, out TrainTestData MultiClass, out TrainTestData BinClass, out TrainTestData RegClass, int WindowSize = 5, double TrainTestRatio = 0.1)
        {
            List<ExtractedMultiFeatures> MultiFeatureSet = new List<ExtractedMultiFeatures>();
            List<Tuple<string, List<double[]>, double[]>> Datasets = DataImport.LoadDataset();
            foreach (Tuple<string, List<double[]>, double[]> Dataset in Datasets)
            {
                int NumberOfSamples = Dataset.Item2[1].Length / 32;
                for (int i = 0; i < NumberOfSamples - WindowSize; i += WindowSize)
                {
                    List<double> SubjectFeatures = new List<double>();
                    for (int j = 0; j < WindowSize; j++)
                    {
                        SubjectFeatures.AddRange(SignalProcessing.ProcessAccSignal(Dataset.Item2[0].GetSubArray(j * 96, (j + 1) * 96)));
                        SubjectFeatures.AddRange(SignalProcessing.ProcessAccSignal(Dataset.Item2[1].GetSubArray(j * 32, (j + 1) * 32)));
                        SubjectFeatures.AddRange(SignalProcessing.ProcessAccSignal(Dataset.Item2[2].GetSubArray(j * 32, (j + 1) * 32)));
                        SubjectFeatures.AddRange(SignalProcessing.ProcessAccSignal(Dataset.Item2[3].GetSubArray(j * 32, (j + 1) * 32)));
                    }
                    SubjectFeatures.AddRange(SignalProcessing.ProcessPpgSignal(Dataset.Item2[4].GetSubArray(i * 64, (i + WindowSize) * 64)));
                    SubjectFeatures.AddRange(SignalProcessing.ProcessEdaSignal(Dataset.Item2[5].GetSubArray(i * 4, (i + WindowSize) * 4)));
                    SubjectFeatures.AddRange(SignalProcessing.ProcessTmpSignal(Dataset.Item2[6].GetSubArray(i * 4, (i + WindowSize) * 4)));

                    MultiFeatureSet.Add(new ExtractedMultiFeatures()
                    {
                        StressFeatures = SubjectFeatures.ToArray().ToFloat(),
                        Result = (uint)Dataset.Item3.GetSubArray(i * 700, (i + WindowSize) * 700).Mean(),
                    });

                    if (i + WindowSize > NumberOfSamples)
                    {
                        WindowSize = NumberOfSamples - i;
                    }
                }
            }
            MultiFeatureSet = Train.TrimFeatureSet(MultiFeatureSet, new List<int>() { 0, 4, 5, 6, 7 });
            List<ExtractedBinFeatures> BinFeatureSet = Train.MultiToBin(MultiFeatureSet);
            List<ExtractedRegFeatures> RegFeatureSet = Train.MultiToReg(MultiFeatureSet);
            IDataView MultiClassView = mlContext.Data.LoadFromEnumerable(MultiFeatureSet);
            IDataView BinClassView = mlContext.Data.LoadFromEnumerable(BinFeatureSet);
            IDataView RegClassView = mlContext.Data.LoadFromEnumerable(RegFeatureSet);
            MultiClass = mlContext.Data.TrainTestSplit(MultiClassView, TrainTestRatio);
            BinClass = mlContext.Data.TrainTestSplit(BinClassView, TrainTestRatio);
            RegClass = mlContext.Data.TrainTestSplit(RegClassView, TrainTestRatio);
        }
    }
}