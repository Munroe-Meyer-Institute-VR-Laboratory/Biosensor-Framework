using Microsoft.ML.Data;

namespace MMIVR.BiosensorFramework.MachineLearningUtilities
{
    /// <summary>
    /// Class to hold extracted features for inferencing.
    /// </summary>
    public class ExtractedFeatures
    {
        /// <summary>
        /// Array to store the hand crafted features.
        /// </summary>
        [VectorType(119)]
        public float[] StressFeatures;
    }
    /// <summary>
    /// Class to hold extracted features for training a multi-class model.
    /// </summary>
    public class ExtractedMultiFeatures
    {
        /// <summary>
        /// Array to store the hand crafted features.
        /// </summary>
        [VectorType(119)]
        public float[] StressFeatures;
        /// <summary>
        /// The label for the stress features.
        /// </summary>
        public uint Result;
    }
    /// <summary>
    /// Class to hold extracted features for training a regression model.
    /// </summary>
    public class ExtractedRegFeatures
    {
        /// <summary>
        /// Array to store the hand crafted features.
        /// </summary>
        [VectorType(119)]
        public float[] StressFeatures;
        /// <summary>
        /// The label for the stress features.
        /// </summary>
        public float Result;
    }
    /// <summary>
    /// Class to hold extracted features for training a binary model.
    /// </summary>
    public class ExtractedBinFeatures
    {
        /// <summary>
        /// Array to store the hand crafted features.
        /// </summary>
        [VectorType(119)]
        public float[] Features;
        /// <summary>
        /// The label for the stress features.
        /// </summary>
        public bool Label;
    }
    /// <summary>
    /// Class to hold multi-class prediction result.
    /// </summary>
    public class PredictionMultiResult
    {
        /// <summary>
        /// The label of the inferenced data.
        /// </summary>
        public uint Result;
    }
    /// <summary>
    /// Class to hold regression prediction result.
    /// </summary>
    public class PredictionRegResult
    {
        /// <summary>
        /// The label of the inferenced data.
        /// </summary>
        public float Result;
    }
    /// <summary>
    /// Class to hold binary prediction result.
    /// </summary>
    public class PredictionBinResult
    {
        /// <summary>
        /// The label of the inferenced data.
        /// </summary>
        [ColumnName("PredictedLabel")]
        public bool Prediction;
        /// <summary>
        /// The probability of the predicted label.
        /// </summary>
        public float Probability;
        /// <summary>
        /// The score of the predicted label.
        /// </summary>
        public float Score;
    }
}
