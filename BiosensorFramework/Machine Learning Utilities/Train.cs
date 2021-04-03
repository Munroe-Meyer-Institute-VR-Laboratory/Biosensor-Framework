using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;

using MMIVR.BiosensorFramework.Extensions;
using static Microsoft.ML.DataOperationsCatalog;

namespace MMIVR.BiosensorFramework.MachineLearningUtilities
{
    public class Train
    {
        public static void RunBenchmarks(out ITransformer BestRegModel, out ITransformer BestMultiModel, out ITransformer BestBinModel)
        {
            MLContext mlContext = new MLContext();

            DataImport.SchmidtDatasetPipeline(mlContext, out TrainTestData MultiClass, out TrainTestData BinClass, out TrainTestData RegClass);

            var RegMultiModels = BuildAndTrainRegressionModels(mlContext, RegClass.TrainSet);
            var BinModels = BuildAndTrainBinClassModels(mlContext, BinClass.TrainSet);
            var MultiModels = BuildAndTrainMultiClassModels(mlContext, MultiClass.TrainSet);

            double RegRSquared = 0;
            double BinAccuracy = 0;
            double MultiLogLoss = 1.0;

            BestRegModel = null;
            BestMultiModel = null;
            BestBinModel = null;

            DataViewSchema RegModelSchema = null;
            DataViewSchema MultiModelSchema = null;
            DataViewSchema BinModelSchema = null;

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
                 catch
                 {
                     continue;
                 }
            }
            mlContext.Model.Save(BestMultiModel, MultiModelSchema, @"C:\MultiModel.zip");
            mlContext.Model.Save(BestBinModel, BinModelSchema, @"C:\BinModel.zip");
            mlContext.Model.Save(BestRegModel, RegModelSchema, @"C:\RegModel.zip");
        }
        public static void PrintBinMetrics(BinaryClassificationMetrics metrics)
        {
            Console.WriteLine("Accuracy: {0}" +
                "\nArea Under Precision Recall Curve: {1}" +
                "\nArea Under Roc Curve: {2}" +
                "\nF1 Score: {3}" +
                "\nNegative Precision: {4}" +
                "\nNegative Recall {5}" +
                "\nPositive Precision: {6}" +
                "\nPositive Recall: {7}",
                metrics.Accuracy, metrics.AreaUnderPrecisionRecallCurve, metrics.AreaUnderRocCurve, metrics.F1Score,
                metrics.NegativePrecision, metrics.NegativeRecall, metrics.PositivePrecision, metrics.PositiveRecall);
        }
        public static void PrintMultiMetrics(MulticlassClassificationMetrics metrics)
        {
            Console.WriteLine("Log Loss: {0}" +
                "\nLog Loss Reduction: {1}" +
                "\nMacro Accuracy: {2}" +
                "\nMicro Accuracy: {3}" +
                "\nTop K Accuracy: {4}",
                metrics.LogLoss, metrics.LogLossReduction, metrics.MacroAccuracy, metrics.MicroAccuracy, metrics.TopKAccuracy);
        }
        public static void PrintRegMetrics(RegressionMetrics metrics)
        {
            Console.WriteLine("Mean Absolute Error: " + metrics.MeanAbsoluteError);
            Console.WriteLine("Mean Squared Error: " + metrics.MeanSquaredError);
            Console.WriteLine("Root Mean Squared Error: " + metrics.RootMeanSquaredError);
            Console.WriteLine("RSquared: " + metrics.RSquared);
        }
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
        public static List<ExtractedBinFeatures> MultiToBin(List<ExtractedMultiFeatures> FeatureSet)
        {
            List<ExtractedBinFeatures> ConFeatures = new List<ExtractedBinFeatures>();
            for (int i = 0; i < FeatureSet.Count; i++)
            {
                if (FeatureSet[i].Result == 2)
                    ConFeatures.Add(new ExtractedBinFeatures()
                    {
                        Label = false,
                        Features = FeatureSet[i].StressFeatures
                    });
                else
                    ConFeatures.Add(new ExtractedBinFeatures()
                    {
                        Label = FeatureSet[i].Result == 1,
                        Features = FeatureSet[i].StressFeatures,
                    });
            }
            return ConFeatures;
        }
        public static List<ExtractedRegFeatures> MultiToReg(List<ExtractedMultiFeatures> FeatureSet)
        {
            List<ExtractedRegFeatures> ConFeatures = new List<ExtractedRegFeatures>();
            for (int i = 0; i < FeatureSet.Count; i++)
            {
                ConFeatures.Add(new ExtractedRegFeatures()
                {
                    Result = FeatureSet[i].Result,
                    StressFeatures = FeatureSet[i].StressFeatures,
                });
            }
            return ConFeatures;
        }
    }
}
