using Microsoft.ML;
using System;
using System.Collections.Generic;
using System.IO;
using static Microsoft.ML.DataOperationsCatalog;
using MathNet.Numerics.Statistics;

using MMIVR.BiosensorFramework.Extensions;
using MMIVR.BiosensorFramework.InputPipeline;
using System.Linq;

namespace MMIVR.BiosensorFramework.MachineLearningUtilities
{
    /// <summary>
    /// Class for importing data from CSV files and txt files.
    /// </summary>
    public class DataImport
    {
        /// <summary>
        /// The organization of the imported data.
        /// </summary>
        enum CollectedData { Acc3D, GSR, BVP, TMP, IBI, BAT, TAG, TIMESTAMPS, DATE }
        /// <summary>
        /// Loads a TXT file of stored readings.
        /// </summary>
        /// <param name="Filepath">The absolute filepath to the file.</param>
        /// <param name="WindowSize">The window size, in seconds, for collecting data. Defaults to 5 seconds.</param>
        /// <returns>List of Tuples containing data and tags.</returns>
        public static List<Tuple<double[], int>> LoadFile(string Filepath, int WindowSize = 5)
        {
            List<Tuple<double[], int>> DataFeatures = new List<Tuple<double[], int>>();

            List<string> lines = new List<string>();
            List<double[]> ExtractedData = new List<double[]>();
            string Date = "";

            // Opening the file for reading 
            using (StreamReader stream = File.OpenText(Filepath))
            {
                string temp = "";
                while ((temp = stream.ReadLine()) != null)
                {
                    lines.Add(temp);
                }
            }
            foreach (string line in lines)
            {
                List<string> tempList = line.Split(new char[] { ',' }).ToList();
                tempList.RemoveAt(tempList.Count - 1);
                ExtractedData.Add(tempList.Select(x => double.Parse(x)).ToArray());
            }
            Date = lines.Last();

            int[] TagLocations = ExtractedData[(int)CollectedData.TAG].Select(x => (int)(x / 64)).ToArray();
            ExtractedData[0].ToArray().SplitAcc3D(out double[] AccX, out double[] AccY, out double[] AccZ);

            int NumberOfSamples = ExtractedData[0].Length / 96;
            int[] Tags = new int[NumberOfSamples];
            int Tag = 0;

            for (int i = 0; i < NumberOfSamples; i++)
            {
                for (int j = 0; j < ExtractedData[(int)CollectedData.TAG].Length; j++)
                {
                    if (i >= ExtractedData[(int)CollectedData.TAG][j])
                    {
                        Tag = 1;
                    }
                }
                Tags[i] = Tag;
            }

            for (int i = 0; i < NumberOfSamples - WindowSize; i += WindowSize)
            {
                List<double> ComputedFeatures = new List<double>();
                for (int j = 0; j < WindowSize; j++)
                {
                    ComputedFeatures.AddRange(SignalProcessing.ProcessAccSignal(ExtractedData[(int)CollectedData.Acc3D].GetSubArray(j * 96, (j + 1) * 96)));
                    ComputedFeatures.AddRange(SignalProcessing.ProcessAccSignal(AccX.GetSubArray(j * 32, (j + 1) * 32)));
                    ComputedFeatures.AddRange(SignalProcessing.ProcessAccSignal(AccY.GetSubArray(j * 32, (j + 1) * 32)));
                    ComputedFeatures.AddRange(SignalProcessing.ProcessAccSignal(AccZ.GetSubArray(j * 32, (j + 1) * 32)));
                }
                ComputedFeatures.AddRange(SignalProcessing.ProcessPpgSignal(ExtractedData[(int)CollectedData.BVP].GetSubArray(i * 64, (i + WindowSize) * 64)));
                ComputedFeatures.AddRange(SignalProcessing.ProcessEdaSignal(ExtractedData[(int)CollectedData.GSR].GetSubArray(i * 4, (i + WindowSize) * 4)));
                ComputedFeatures.AddRange(SignalProcessing.ProcessTmpSignal(ExtractedData[(int)CollectedData.TMP].GetSubArray(i * 4, (i + WindowSize) * 4)));

                DataFeatures.Add(Tuple.Create(ComputedFeatures.ToArray(), Tags[i]));

                if (i + WindowSize > NumberOfSamples)
                {
                    WindowSize = NumberOfSamples - i;
                }
            }
            return DataFeatures;
        }
        /// <summary>
        /// Parses and transforms the data collected from a biosensor from a txt file.  To facilitate training the Fast Forest classifier used, the WESAD data is loaded in 
        /// first to allow retraining.
        /// </summary>
        /// <param name="WesadDirectory">The top directory of the WESAD dataset.</param>
        /// <param name="DirectoryPath">The top directory with the data.</param>
        /// <param name="SearchPattern">The search pattern to find the relevant files in the DirectoryPath.</param>
        /// <param name="WindowSize">The window size, in seconds, for collecting data. Defaults to 5 seconds.</param>
        /// <returns>List of Tuples with data and tags.</returns>
        public static List<Tuple<double[], int>> LoadCollectedDataset(string WesadDirectory, string DirectoryPath, string SearchPattern, int WindowSize = 5)
        {
            string[] files = Directory.GetFiles(DirectoryPath, SearchPattern, SearchOption.TopDirectoryOnly);
            List<Tuple<double[], int>> DataFeatures = new List<Tuple<double[], int>>();

            List<Tuple<string, List<double[]>, double[]>> Datasets = LoadDataset(WesadDirectory);
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

                    DataFeatures.Add(Tuple.Create(SubjectFeatures.ToArray(), (int)Dataset.Item3.GetSubArray(i * 700, (i + WindowSize) * 700).Mean()));

                    if (i + WindowSize > NumberOfSamples)
                    {
                        WindowSize = NumberOfSamples - i;
                    }
                }
            }

            foreach (string file in files)
            {
                List<string> lines = new List<string>();
                List<double[]> ExtractedData = new List<double[]>();
                string Date = "";

                // Opening the file for reading 
                using (StreamReader stream = File.OpenText(file))
                {
                    string temp = "";
                    while ((temp = stream.ReadLine()) != null)
                    {
                        lines.Add(temp);
                    }
                }
                foreach (string line in lines)
                {
                    List<string> tempList = line.Split(new char[] { ',' }).ToList();
                    tempList.RemoveAt(tempList.Count - 1);
                    ExtractedData.Add(tempList.Select(x => double.Parse(x)).ToArray());
                }
                Date = lines.Last();

                int[] TagLocations = ExtractedData[(int)CollectedData.TAG].Select(x => (int)(x / 64)).ToArray();
                ExtractedData[0].ToArray().SplitAcc3D(out double[] AccX, out double[] AccY, out double[] AccZ);

                int NumberOfSamples = ExtractedData[0].Length / 96;
                int[] Tags = new int[NumberOfSamples];
                int Tag = 0;

                for (int i = 0; i < NumberOfSamples; i++)
                {
                    for (int j = 0; j < ExtractedData[(int)CollectedData.TAG].Length; j++)
                    {
                        if (i >= ExtractedData[(int)CollectedData.TAG][j])
                        {
                            Tag = 1;
                        }
                    }
                    Tags[i] = Tag;
                }

                for (int i = 0; i < NumberOfSamples - WindowSize; i += WindowSize)
                {
                    List<double> ComputedFeatures = new List<double>();
                    for (int j = 0; j < WindowSize; j++)
                    {
                        ComputedFeatures.AddRange(SignalProcessing.ProcessAccSignal(ExtractedData[(int)CollectedData.Acc3D].GetSubArray(j * 96, (j + 1) * 96)));
                        ComputedFeatures.AddRange(SignalProcessing.ProcessAccSignal(AccX.GetSubArray(j * 32, (j + 1) * 32)));
                        ComputedFeatures.AddRange(SignalProcessing.ProcessAccSignal(AccY.GetSubArray(j * 32, (j + 1) * 32)));
                        ComputedFeatures.AddRange(SignalProcessing.ProcessAccSignal(AccZ.GetSubArray(j * 32, (j + 1) * 32)));
                    }
                    ComputedFeatures.AddRange(SignalProcessing.ProcessPpgSignal(ExtractedData[(int)CollectedData.BVP].GetSubArray(i * 64, (i + WindowSize) * 64)));
                    ComputedFeatures.AddRange(SignalProcessing.ProcessEdaSignal(ExtractedData[(int)CollectedData.GSR].GetSubArray(i * 4, (i + WindowSize) * 4)));
                    ComputedFeatures.AddRange(SignalProcessing.ProcessTmpSignal(ExtractedData[(int)CollectedData.TMP].GetSubArray(i * 4, (i + WindowSize) * 4)));

                    DataFeatures.Add(Tuple.Create(ComputedFeatures.ToArray(), Tags[i]));

                    if (i + WindowSize > NumberOfSamples)
                    {
                        WindowSize = NumberOfSamples - i;
                    }
                }
            }
            return DataFeatures;
        }
        /// <summary>
        /// Loads in a CSV datasets from the filesystem from the WESAD dataset.
        /// </summary>
        /// <param name="DirectoryPath">The top directory with the data.</param>
        /// <returns>List of Tuples with Subject ID, sensor data, and tags.</returns>
        public static List<Tuple<string, List<double[]>, double[]>> LoadDataset(string DirectoryPath)
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
        /// Schmidt, Philip, et al. "Introducing wesad, a multimodal dataset for wearable stress and affect detection." 
        ///     Proceedings of the 20th ACM International Conference on Multimodal Interaction. 2018.
        /// </summary>
        /// <param name="DirectoryPath">The top directory with the data.</param>
        /// <param name="mlContext">The Microsoft.ML context.</param>
        /// <param name="MultiClass">The returned TrainTestData for the multi-class models.</param>
        /// <param name="BinClass">The returned TrainTestData for the binary models.</param>
        /// <param name="RegClass">The returned TrainTestData for the regression models.</param>
        /// <param name="WindowSize">The window size, in seconds, for collecting data. Defaults to 5 seconds.</param>
        /// <param name="TrainTestRatio">The percentage of the data that is train and test. Default is 0.1.</param>
        public static void SchmidtDatasetPipeline(string DirectoryPath, MLContext mlContext, out TrainTestData MultiClass, out TrainTestData BinClass, out TrainTestData RegClass, int WindowSize = 5, double TrainTestRatio = 0.1)
        {
            List<ExtractedMultiFeatures> MultiFeatureSet = new List<ExtractedMultiFeatures>();
            List<Tuple<string, List<double[]>, double[]>> Datasets = LoadDataset(DirectoryPath);
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
                        Features = SubjectFeatures.ToArray().ToFloat(),
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
        /// <summary>
        /// Converts raw List of Tuples into a TrainTestData set for Microsoft.ML for binary classification training.
        /// </summary>
        /// <param name="mlContext">The Microsoft.ML context.</param>
        /// <param name="RawFeatures">The data and tags loaded in.</param>
        /// <param name="TrainTestRatio">The percentage of the data that is train and test. Default is 0.1.</param>
        /// <returns>The TrainTestData to be used in training a Microsoft.ML model.</returns>
        public static TrainTestData ConvertRawToBin(MLContext mlContext, List<Tuple<double[], int>> RawFeatures, double TrainTestRatio = 0.1)
        {
            List<ExtractedBinFeatures> BinFeatureSet = new List<ExtractedBinFeatures>();
            foreach (Tuple<double[], int> RawFeature in RawFeatures)
            {
                BinFeatureSet.Add(new ExtractedBinFeatures()
                {
                    Features = RawFeature.Item1.ToFloat(),
                    Label = RawFeature.Item2 > 0
                });
            }
            IDataView BinClassView = mlContext.Data.LoadFromEnumerable(BinFeatureSet);
            return mlContext.Data.TrainTestSplit(BinClassView, TrainTestRatio);
        }
        /// <summary>
        /// Converts raw List of Tuples into a TrainTestData set for Microsoft.ML multi-class training.
        /// </summary>
        /// <param name="mlContext">The Microsoft.ML context.</param>
        /// <param name="RawFeatures">The data and tags loaded in.</param>
        /// <param name="TrainTestRatio">The percentage of the data that is train and test. Default is 0.1.</param>
        /// <returns>The TrainTestData to be used in training a Microsoft.ML model.</returns>
        public static TrainTestData ConvertRawToMulti(MLContext mlContext, List<Tuple<double[], int>> RawFeatures, double TrainTestRatio = 0.1)
        {
            throw new NotImplementedException();
        }
    }
}