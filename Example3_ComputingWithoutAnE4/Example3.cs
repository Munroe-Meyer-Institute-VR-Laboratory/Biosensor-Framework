using System;
using System.Collections.Generic;
using System.IO;
using MathNet.Numerics.Statistics;
using Microsoft.ML;

using MMIVR.BiosensorFramework.Extensions;
using MMIVR.BiosensorFramework.InputPipeline;
using MMIVR.BiosensorFramework.MachineLearningUtilities;

/// <summary>
/// Example 3 - Computing Without An E4
/// This example is meant to show how to use the core library for affective computing tasks without the body-worn sensor.
/// 
/// The Virtual Reality Laboratory in the Munroe Meyer Institute at the University of Nebraska Medical Center
/// Author: Walker Arce
/// 
/// Copy the 'Dataset' folder from the main repo folder to the 'Biosensor-Framework\Example3_ComputingWithoutAnE4\bin\x64\{Release or Debug}'
/// Copy the BinModel.zip trained model to a new folder called 'Models' in the same bin folder
/// For more information on running this example, visit the repository
/// https://github.com/Munroe-Meyer-Institute-VR-Laboratory/Biosensor-Framework
/// </summary>
namespace Example3_ComputingWithoutAnE4
{
    class Program
    {
        public static MLContext mlContext;
        public static ITransformer Model;
        public static DataViewSchema ModelSchema;
        public static int WindowSize = 5;
        // Output and input filepaths
        public static string WesadDirectory = Path.Combine(Environment.CurrentDirectory, "Dataset");
        public static string ModelDir = Path.Combine(Environment.CurrentDirectory, "Models");

        static void Main(string[] args)
        {
            mlContext = new MLContext();
            Model = mlContext.Model.Load(Path.Combine(ModelDir, "BinModel.zip"), out ModelSchema);
            List<Tuple<string, List<double[]>, double[]>> ParsedDataset = DataImport.LoadDataset(WesadDirectory);
            List<ExtractedMultiFeatures> MultiFeatureSet = new List<ExtractedMultiFeatures>();

            foreach (Tuple<string, List<double[]>, double[]> Dataset in ParsedDataset)
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

            for (int i = 0; i < BinFeatureSet.Count; i++)
            {
                PredictionBinResult Prediction = Predict.MakeBinPrediction(mlContext, BinFeatureSet[i], Model);
                Console.WriteLine("Window {0} of {1} - Prediction Result: {2}", i + 1, BinFeatureSet.Count, Prediction.Prediction);
            }

            Console.ReadLine();
        }
    }
}
