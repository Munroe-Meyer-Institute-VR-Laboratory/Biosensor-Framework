using System;
using System.Collections.Generic;
using Microsoft.ML;

using MMIVR.BiosensorFramework.Extensions;
using MMIVR.BiosensorFramework.InputPipeline;

namespace MMIVR.BiosensorFramework.MachineLearningUtilities
{
    /// <summary>
    /// Class for performing predictions on Microsoft.ML models.
    /// </summary>
    public class Predict
    {
        /// <summary>
        /// Takes the readings from the windowed data, extracts the features, and runs it through a prediction pipeline.
        /// </summary>
        /// <param name="mlContext">Microsoft ML context for operations to be performed in.</param>
        /// <param name="Model">The loaded model for operations to be performed on.</param>
        /// <param name="WindowReadings">Packaged List of List of sensor readings.</param>
        public static PredictionBinResult PredictWindow(MLContext mlContext, ITransformer Model, List<List<double>> WindowReadings)
        {
            List<double> Features = new List<double>();
            // Since ACC readings can be jagged, i.e. not exactly five seconds of readings, we need to handle
            // the end index case.  It adds processing time but prevents runtime errors.
            // If we calculate the number of windows that are present, we can append all of the missing samples after
            // the processing is complete.
            // By checking this, we truncate the readings at the end of the array, regardless of how close it is to
            // a full second of readings.  This was attempted to be handled by the '?' operator in the Range operator
            // but it would not be implemented in this case since are only dealing with full windows.
            // TODO: Incorporate readings at the end of the array that are not a full window, the code is there, but the 
            // check on line 32 needs to be updated to round from midpoint, i.e. 4.45 -> 4 and 4.55 -> 5 
            // So, actually, we had to create a new 'range' operator because Unity 2019.2.16f doesn't support C# 8.0
            // and this new extension method, 'GetSubArray', handles end cases by adjusting the end value to the end of the array
            // By adding in the rounding function, it essentially cuts out a window if there is less than half of the window present
            int WindowCount = (int)Math.Round(WindowReadings[0].ToArray().Length / 96.0, 0, MidpointRounding.AwayFromZero);
            WindowCount = WindowCount > 5 ? 5 : WindowCount;
            for (int i = 0; i < WindowCount; i++)
            {
                Features.AddRange(SignalProcessing.ProcessAccSignal(WindowReadings[0].ToArray().GetSubArray(i * 96, (i + 1) * 96)));
                Features.AddRange(SignalProcessing.ProcessAccSignal(WindowReadings[1].ToArray().GetSubArray(i * 32, (i + 1) * 32)));
                Features.AddRange(SignalProcessing.ProcessAccSignal(WindowReadings[2].ToArray().GetSubArray(i * 32, (i + 1) * 32)));
                Features.AddRange(SignalProcessing.ProcessAccSignal(WindowReadings[3].ToArray().GetSubArray(i * 32, (i + 1) * 32)));
            }
            for (int i = 0; i < (5 - WindowCount) * 4; i++)
            {
                Features.AddRange(new double[4]);
            }
            Features.AddRange(SignalProcessing.ProcessPpgSignal(WindowReadings[4].ToArray()));
            Features.AddRange(SignalProcessing.ProcessEdaSignal(WindowReadings[5].ToArray()));
            Features.AddRange(SignalProcessing.ProcessTmpSignal(WindowReadings[6].ToArray()));

            ExtractedBinFeatures WindowFeatures = new ExtractedBinFeatures()
            {
                Features = Features.ToArray().ToFloat(),
            };
            PredictionBinResult Prediction = MakeBinPrediction(mlContext, WindowFeatures, Model);
            return Prediction;
        }
        /// <summary>
        /// Performs a prediction on a dataset for multi-class models.
        /// </summary>
        /// <param name="mlContext">Microsoft ML context for operations to be performed in.</param>
        /// <param name="LiveData">The extracted features packaged in the ExtractedMultiFeatures class.</param>
        /// <param name="Model">The model to perform inferencing with.</param>
        /// <returns></returns>
        public static PredictionMultiResult MakeMultiPrediction(MLContext mlContext, ExtractedMultiFeatures LiveData, ITransformer Model)
        {
            return mlContext.Model.CreatePredictionEngine<ExtractedMultiFeatures, PredictionMultiResult>(Model).Predict(LiveData);
        }
        /// <summary>
        /// Performs a prediction on a dataset for binary classification models.
        /// </summary>
        /// <param name="mlContext">Microsoft ML context for operations to be performed in.</param>
        /// <param name="LiveData">The extracted features packaged in the ExtractedMultiFeatures class.</param>
        /// <param name="Model">The model to perform inferencing with.</param>
        /// <returns></returns>
        public static PredictionBinResult MakeBinPrediction(MLContext mlContext, ExtractedBinFeatures LiveData, ITransformer Model)
        {
            return mlContext.Model.CreatePredictionEngine<ExtractedBinFeatures, PredictionBinResult>(Model).Predict(LiveData);
        }
        /// <summary>
        /// Performs a prediction on a dataset for regression classification models.
        /// </summary>
        /// <param name="mlContext">Microsoft ML context for operations to be performed in.</param>
        /// <param name="LiveData">The extracted features packaged in the ExtractedMultiFeatures class.</param>
        /// <param name="Model">The model to perform inferencing with.</param>
        /// <returns></returns>
        public static PredictionRegResult MakeRegPrediction(MLContext mlContext, IDataView LiveData, ITransformer Model)
        {
            return mlContext.Model.CreatePredictionEngine<IDataView, PredictionRegResult>(Model).Predict(LiveData);
        }
    }
}
