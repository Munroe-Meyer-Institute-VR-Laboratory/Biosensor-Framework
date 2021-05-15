using Microsoft.ML.Data;

namespace MMIVR.BiosensorFramework.MachineLearningUtilities
{
    public class ExtractedFeatures
    {
        [VectorType(119)]
        public float[] StressFeatures;
    }
    public class ExtractedMultiFeatures
    {
        [VectorType(119)]
        public float[] StressFeatures;
        public uint Result;
    }
    public class ExtractedRegFeatures
    {
        [VectorType(119)]
        public float[] StressFeatures;
        public float Result;
    }
    public class ExtractedBinFeatures
    {
        [VectorType(119)]
        public float[] Features;
        public bool Label;
    }
    public class PredictionMultiResult
    {
        public uint Result;
    }
    public class PredictionRegResult
    {
        public float Result;
    }
    public class PredictionBinResult
    {
        [ColumnName("PredictedLabel")]
        public bool Prediction;
        public float Probability;
        public float Score;
    }
}
