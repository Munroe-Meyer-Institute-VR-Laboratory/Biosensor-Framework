using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;

using MMIVR.BiosensorFramework.Extensions;
using static Microsoft.ML.DataOperationsCatalog;

namespace MMIVR.BiosensorFramework.MachineLearningUtilities
{
    /// <summary>
    /// Class to perform training of Microsoft.ML models.
    /// </summary>
    public class Train
    {
        /// <summary>
        /// Runs regression, multi-class, and binary classification tasks on the WESAD dataset and compares performance.  Returns the best performing model of each category.
        /// </summary>
        /// <param name="DirectoryPath">Path to directory with WESAD dataset.</param>
        /// <param name="BestRegModel">The best regression ITransformer model.</param>
        /// <param name="BestMultiModel">The best multi-class ITransformer model.</param>
        /// <param name="BestBinModel">The best binary ITransformer model.</param>
        /// <param name="ModelDir">The top directory to save the best models to. Default null.</param>
        public static void RunBenchmarks(string DirectoryPath, out ITransformer BestRegModel, out ITransformer BestMultiModel, out ITransformer BestBinModel, string ModelDir = null)
        {
            MLContext mlContext = new MLContext();

            DataImport.SchmidtDatasetPipeline(DirectoryPath, mlContext, 
                out TrainTestData MultiClass, out TrainTestData BinClass, out TrainTestData RegClass, 5, 0.3);

            List<ITransformer> RegMultiModels = BuildAndTrainRegressionModels(mlContext, RegClass.TrainSet);
            List<ITransformer> BinModels = BuildAndTrainBinClassModels(mlContext, BinClass.TrainSet);
            List<ITransformer> MultiModels = BuildAndTrainMultiClassModels(mlContext, MultiClass.TrainSet);

            double RegRSquared = 0;
            double BinAccuracy = 0;
            double MultiLogLoss = 1.0;

            BestRegModel = null;
            BestMultiModel = null;
            BestBinModel = null;

            DataViewSchema RegModelSchema = null;
            DataViewSchema MultiModelSchema = null;
            DataViewSchema BinModelSchema = null;

            Console.WriteLine("\nRegression Model Metrics");
            foreach (var model in RegMultiModels)
            {
                try
                {
                    IDataView testPred = model.Transform(RegClass.TestSet);
                    RegressionMetrics modelMetrics = mlContext.Regression.Evaluate(testPred);
                    PrintRegMetrics(modelMetrics);
                    if (modelMetrics.RSquared > RegRSquared)
                    {
                        RegModelSchema = testPred.Schema;
                        RegRSquared = modelMetrics.RSquared;
                        BestRegModel = model;
                    }
                }
                catch
                {
                    continue;
                }
            }

            Console.WriteLine("\nBinary Classification Model Metrics");
            foreach (var model in BinModels)
            {
                try
                {
                    IDataView testpred = model.Transform(BinClass.TestSet);
                    BinaryClassificationMetrics BinMetrics = mlContext.BinaryClassification.EvaluateNonCalibrated(testpred);
                    PrintBinMetrics(BinMetrics);
                    if (BinMetrics.Accuracy > BinAccuracy)
                    {
                        BinModelSchema = testpred.Schema;
                        BinAccuracy = BinMetrics.Accuracy;
                        BestBinModel = model;
                    }
                }
                catch
                {
                    continue;
                }
            }

            Console.WriteLine("\nMulti-Class Classification Model Metrics");
            foreach (var model in MultiModels)
            {
                 try
                 {
                     IDataView testpred = model.Transform(MultiClass.TestSet);
                     MulticlassClassificationMetrics MultiMetrics = mlContext.MulticlassClassification.Evaluate(testpred);
                     PrintMultiMetrics(MultiMetrics);
                     if (MultiMetrics.LogLoss < MultiLogLoss)
                     {
                         MultiModelSchema = testpred.Schema;
                         MultiLogLoss = MultiMetrics.LogLoss;
                         BestMultiModel = model;
                     }
                 }
                 catch (Exception e)
                 {
                    Console.WriteLine(e.Message); 
                    continue;
                 }
            }
            if (ModelDir != null)
            {
                mlContext.Model.Save(BestMultiModel, MultiModelSchema, Path.Combine(ModelDir, "MultiModel.zip"));
                mlContext.Model.Save(BestBinModel, BinModelSchema, Path.Combine(ModelDir, "BinModel.zip"));
                mlContext.Model.Save(BestRegModel, RegModelSchema, Path.Combine(ModelDir, "RegModel.zip"));
            }
        }
        /// <summary>
        /// Prints the performance metrics of the binary classification test to Console.
        /// </summary>
        /// <param name="metrics">The metrics from the test set.</param>
        public static void PrintBinMetrics(BinaryClassificationMetrics metrics)
        {
            Console.WriteLine("Accuracy: {0}" +
                "\nArea Under Precision Recall Curve: {1}" +
                "\nArea Under Roc Curve: {2}" +
                "\nF1 Score: {3}" +
                "\nNegative Precision: {4}" +
                "\nNegative Recall {5}" +
                "\nPositive Precision: {6}" +
                "\nPositive Recall: {7}\n",
                metrics.Accuracy, metrics.AreaUnderPrecisionRecallCurve, metrics.AreaUnderRocCurve, metrics.F1Score,
                metrics.NegativePrecision, metrics.NegativeRecall, metrics.PositivePrecision, metrics.PositiveRecall);
        }
        /// <summary>
        /// Prints the performance of the multi-class classification test to Console.
        /// </summary>
        /// <param name="metrics">The metrics from the test set.</param>
        public static void PrintMultiMetrics(MulticlassClassificationMetrics metrics)
        {
            Console.WriteLine("Log Loss: {0}" +
                "\nLog Loss Reduction: {1}" +
                "\nMacro Accuracy: {2}" +
                "\nMicro Accuracy: {3}" +
                "\nTop K Accuracy: {4}" +
                "\nCF Classes: {5}" +
                "\nTop K Accuracy: {6}",
                metrics.LogLoss, metrics.LogLossReduction, metrics.MacroAccuracy, metrics.MicroAccuracy, metrics.TopKAccuracy,
                metrics.TopKAccuracy, metrics.ConfusionMatrix.NumberOfClasses);
            for (int i = 0; i < metrics.ConfusionMatrix.PerClassPrecision.Count; i++)
            {
                Console.WriteLine(i + " Class Precision: " + metrics.ConfusionMatrix.PerClassPrecision[i]);
            }
            for (int i = 0; i < metrics.ConfusionMatrix.PerClassRecall.Count; i++)
            {
                Console.WriteLine(i + " Class Recall: " + metrics.ConfusionMatrix.PerClassRecall[i]);
            }
            for (int i = 0; i < metrics.PerClassLogLoss.Count; i++)
            {
                Console.WriteLine(i + " Loss: " + metrics.PerClassLogLoss[i]);
            }
            Console.WriteLine();
        }
        /// <summary>
        /// Prints the performance metrics of the regression classification test to Console.
        /// </summary>
        /// <param name="metrics">The metrics from the test set.</param>
        public static void PrintRegMetrics(RegressionMetrics metrics)
        {
            Console.WriteLine("Loss Function: " + metrics.LossFunction);
            Console.WriteLine("Mean Absolute Error: " + metrics.MeanAbsoluteError);
            Console.WriteLine("Mean Squared Error: " + metrics.MeanSquaredError);
            Console.WriteLine("Root Mean Squared Error: " + metrics.RootMeanSquaredError);
            Console.WriteLine("RSquared: " + metrics.RSquared);
            Console.WriteLine();
        }
        /// <summary>
        /// Trains multi-class models built-in to Microsoft.ML on the TrainingSet provided.
        /// </summary>
        /// <param name="mlContext">The Microsoft.ML context to perform operations in.</param>
        /// <param name="TrainingSet">The time-series dataset to train the models on.</param>
        /// <returns>List of models that can be used in performance benchmarks.</returns>
        public static List<ITransformer> BuildAndTrainMultiClassModels(MLContext mlContext, IDataView TrainingSet)
        {
            List<ITransformer> Models = new List<ITransformer>();
            var Pipeline = mlContext.Transforms.Conversion.MapValueToKey(nameof(ExtractedMultiFeatures.Result))
                .Append(mlContext.Transforms.CopyColumns("Label", "Result"))
                .Append(mlContext.Transforms.Concatenate("Features", "StressFeatures"));

            var APOPipeline = Pipeline.Append(mlContext.MulticlassClassification.Trainers.OneVersusAll(mlContext.BinaryClassification.Trainers.AveragedPerceptron()));
            var FFOPipeline = Pipeline.Append(mlContext.MulticlassClassification.Trainers.OneVersusAll(mlContext.BinaryClassification.Trainers.FastForest()));
            var FTOPipeline = Pipeline.Append(mlContext.MulticlassClassification.Trainers.OneVersusAll(mlContext.BinaryClassification.Trainers.FastTree()));
            var LBFGSPipeline = Pipeline.Append(mlContext.MulticlassClassification.Trainers.OneVersusAll(mlContext.BinaryClassification.Trainers.LbfgsLogisticRegression()));
            var LBFGSMEPipeline = Pipeline.Append(mlContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy());
            var LGBMPipeline = Pipeline.Append(mlContext.MulticlassClassification.Trainers.LightGbm());
            var LSVMOPipeline = Pipeline.Append(mlContext.MulticlassClassification.Trainers.OneVersusAll(mlContext.BinaryClassification.Trainers.LinearSvm()));
            var SdcaPipeline = Pipeline.Append(mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy());
            var SGDCOPipeline = Pipeline.Append(mlContext.MulticlassClassification.Trainers.OneVersusAll(mlContext.BinaryClassification.Trainers.SgdCalibrated()));
            //var SSGDLROPipeline = Pipeline.Append(mlContext.MulticlassClassification.Trainers.OneVersusAll(mlContext.BinaryClassification.Trainers.SymbolicSgdLogisticRegression()));
            var KPipeline = Pipeline.Append(mlContext.Clustering.Trainers.KMeans(numberOfClusters: 9));

            Models.Add(APOPipeline.Fit(TrainingSet));
            Models.Add(FFOPipeline.Fit(TrainingSet));
            Models.Add(FTOPipeline.Fit(TrainingSet));
            Models.Add(LBFGSPipeline.Fit(TrainingSet));
            Models.Add(LBFGSMEPipeline.Fit(TrainingSet));
            //Models.Add(LGBMPipeline.Fit(TrainingSet));
            Models.Add(LSVMOPipeline.Fit(TrainingSet));
            //Models.Add(SdcaPipeline.Fit(TrainingSet));
            //Models.Add(SGDCOPipeline.Fit(TrainingSet));
            //Models.Add(SSGDLROPipeline.Fit(TrainingSet));
            Models.Add(KPipeline.Fit(TrainingSet));

            return Models;
        }
        /// <summary>
        /// Trains binary classification models built-in to Microsoft.ML on the provided TrainingSet data.
        /// </summary>
        /// <param name="mlContext">The Microsoft.ML context to perform operations in.</param>
        /// <param name="TrainingSet">The time-series dataset to train the models on.</param>
        /// <returns>List of models that can be used in performance benchmarks.</returns>
        public static List<ITransformer> BuildAndTrainBinClassModels(MLContext mlContext, IDataView TrainingSet)
        {
            List<ITransformer> Models = new List<ITransformer>();
            var Pipeline = mlContext.Transforms.Concatenate("Features", "Features");

            //var APPipeline = Pipeline.Append(mlContext.BinaryClassification.Trainers.AveragedPerceptron());
            var FFPipeline = Pipeline.Append(mlContext.BinaryClassification.Trainers.FastForest());
            var FTPipeline = Pipeline.Append(mlContext.BinaryClassification.Trainers.FastTree());
            /*var LBFGSLRPipeline = Pipeline.Append(mlContext.BinaryClassification.Trainers.LbfgsLogisticRegression());
            var LGBMPipeline = Pipeline.Append(mlContext.BinaryClassification.Trainers.LightGbm());
            var LSVMPipeline = Pipeline.Append(mlContext.BinaryClassification.Trainers.LinearSvm());
            var SdcaLRPipeline = Pipeline.Append(mlContext.BinaryClassification.Trainers.SdcaLogisticRegression());
            var SGDCPipeline = Pipeline.Append(mlContext.BinaryClassification.Trainers.SgdCalibrated());
            var SSGDLRPipeline = Pipeline.Append(mlContext.BinaryClassification.Trainers.SymbolicSgdLogisticRegression());
            var KPipeline = Pipeline.Append(mlContext.Clustering.Trainers.KMeans(numberOfClusters: 9));*/

            //Models.Add(APPipeline.Fit(TrainingSet));
            Models.Add(FFPipeline.Fit(TrainingSet));
            Models.Add(FTPipeline.Fit(TrainingSet));
            //Models.Add(LBFGSLRPipeline.Fit(TrainingSet));
            //Models.Add(LGBMPipeline.Fit(TrainingSet));
            //Models.Add(LSVMPipeline.Fit(TrainingSet));
            //Models.Add(SdcaLRPipeline.Fit(TrainingSet));
            //Models.Add(SGDCPipeline.Fit(TrainingSet));
            //Models.Add(SSGDLRPipeline.Fit(TrainingSet));
            //Models.Add(KPipeline.Fit(TrainingSet));

            return Models;
        }
        /// <summary>
        /// Train regression classification models built-in to Microsoft.ML on the provided TrainingSet data.
        /// </summary>
        /// <param name="mlContext">The Microsoft.ML context to perform operations in.</param>
        /// <param name="TrainingSet">The time-series dataset to train the models on.</param>
        /// <returns>List of models that can be used in performance benchmarks.</returns>
        public static List<ITransformer> BuildAndTrainRegressionModels(MLContext mlContext, IDataView TrainingSet)
        {
            List<ITransformer> Models = new List<ITransformer>();
            var Pipeline = mlContext.Transforms.CopyColumns("Label", "Result")
                .Append(mlContext.Transforms.Concatenate("Features", "StressFeatures"));
            var FastForestPipeline = Pipeline.Append(mlContext.Regression.Trainers.FastForest());
            var FastTreePipeline = Pipeline.Append(mlContext.Regression.Trainers.FastTree());
            var FastTreeTweediePipeline = Pipeline.Append(mlContext.Regression.Trainers.FastTreeTweedie());
            var LBFGSPipeline = Pipeline.Append(mlContext.Regression.Trainers.LbfgsPoissonRegression());
            var LGBMPipeline = Pipeline.Append(mlContext.Regression.Trainers.LightGbm());
            /*var OLSOptions = new OlsTrainer.Options()
            {
                L2Regularization = 0.5f,
            };
            var OLSPipeline = Pipeline.Append(mlContext.Regression.Trainers.Ols(OLSOptions));
            */
            /*
            //TODO: These models have errors to be addressed
            var OGDOptions = new OnlineGradientDescentTrainer.Options()
            {
                NumberOfIterations = 100,
                ResetWeightsAfterXExamples = 10,
                L2Regularization = 0.25f,
                DecreaseLearningRate = true,
            };
            var OGDPipeline = Pipeline.Append(mlContext.Regression.Trainers.OnlineGradientDescent(OGDOptions));
            var SdcaPipeline = Pipeline.Append(mlContext.Regression.Trainers.Sdca());
            // TODO: Exploding/vanishing gradients causing this line to fail
            Models.Add(OGDPipeline.Fit(TrainingSet));
            // TODO: Update, Actual error preventing this from completing: https://github.com/dotnet/machinelearning-samples/issues/833
            Models.Add(SdcaPipeline.Fit(TrainingSet));
            */

            Models.Add(FastForestPipeline.Fit(TrainingSet));
            Models.Add(FastTreePipeline.Fit(TrainingSet));
            Models.Add(FastTreeTweediePipeline.Fit(TrainingSet));
            Models.Add(LBFGSPipeline.Fit(TrainingSet));
            //Models.Add(LGBMPipeline.Fit(TrainingSet));
            //Models.Add(OLSPipeline.Fit(TrainingSet));

            return Models;
        }
        /// <summary>
        /// Removes specified labels from the dataset.
        /// </summary>
        /// <param name="FeatureSet">The data to remove samples from.</param>
        /// <param name="LabelsToRemove">List of labels to remove from dataset.</param>
        /// <returns>Trimmed list.</returns>
        public static List<ExtractedMultiFeatures> TrimFeatureSet(List<ExtractedMultiFeatures> FeatureSet, List<int> LabelsToRemove)
        {
            for (int i = 0; i < LabelsToRemove.Count; i++)
            {
                List<int> DelLabels = FeatureSet.AllIndexesOf(LabelsToRemove[i]);
                // Ensure to remove from the highest index first for a list
                foreach (int DelLabel in DelLabels.OrderByDescending(v => v))
                {
                    FeatureSet.RemoveAt(DelLabel);
                }
            }
            return FeatureSet;
        }
        /// <summary>
        /// Converts multi-class feature set to binary class representation.
        /// </summary>
        /// <param name="FeatureSet">The feature set to convert.</param>
        /// <returns>Binary class representation of the input data.</returns>
        public static List<ExtractedBinFeatures> MultiToBin(List<ExtractedMultiFeatures> FeatureSet)
        {
            List<ExtractedBinFeatures> ConFeatures = new List<ExtractedBinFeatures>();
            for (int i = 0; i < FeatureSet.Count; i++)
            {
                if (FeatureSet[i].Result == 2)
                    ConFeatures.Add(new ExtractedBinFeatures()
                    {
                        Label = false,
                        Features = FeatureSet[i].Features
                    });
                else
                    ConFeatures.Add(new ExtractedBinFeatures()
                    {
                        Label = FeatureSet[i].Result == 1,
                        Features = FeatureSet[i].Features,
                    });
            }
            return ConFeatures;
        }
        /// <summary>
        /// Converts multi-class feature dataset to regression class feature dataset.
        /// </summary>
        /// <param name="FeatureSet">the feature set to convert.</param>
        /// <returns>Regression class representation of the input data.</returns>
        public static List<ExtractedRegFeatures> MultiToReg(List<ExtractedMultiFeatures> FeatureSet)
        {
            List<ExtractedRegFeatures> ConFeatures = new List<ExtractedRegFeatures>();
            for (int i = 0; i < FeatureSet.Count; i++)
            {
                ConFeatures.Add(new ExtractedRegFeatures()
                {
                    Result = FeatureSet[i].Result,
                    Features = FeatureSet[i].Features,
                });
            }
            return ConFeatures;
        }
    }
}
