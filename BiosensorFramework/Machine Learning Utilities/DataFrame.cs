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
    /*public class StressData
    {
        // EDA Features
        public double EdaMean;
        public double EdaStD;
        public double EdaMin;
        public double EdaMax;
        public double EdaSlope;
        public double EdaDynamicRange;
        public double SclMean;
        public double SclStD;
        public double ScrStD;
        public double SclCorr;
        public double ScrPeaks;
        public double EdaMag;
        public double EdaDur;
        public double EdaAmp;
        public double ScrSlope;
        // ACCX Features
        public double AccXMean;
        public double AccXStD;
        public double AccXAbsInt;
        public double AccXPeakFreq;
        // ACCY Features
        public double AccYMean;
        public double AccYStD;
        public double AccYAbsInt;
        public double AccYPeakFreq;
        // ACCZ Features
        public double AccZMean;
        public double AccZStD;
        public double AccZAbsInt;
        public double AccZPeakFreq;
        // ACC3D Features
        public double Acc3DMean;
        public double Acc3DStD;
        public double Acc3DAbsInt;
        public double Acc3DPeakFreq;
        // TMP Features
        public double TmpMean;
        public double TmpStD;
        public double TmpMin;
        public double TmpMax;
        public double TmpDynamicRange;
        public double TmpSlope;
        // PPG Features
        public double HrMean;
        public double HrStD;
        public double HrvMean;
        public double HrvStD;
        public double NN50;
        public double pNN50;
        public double HrvRms;
        public double LFHFRatio;
        public double SumULF;
        public double SumLF;
        public double SumHF;
        public double SumUHF;
        public double RelPowULF;
        public double RelPowLF;
        public double RelPowHF;
        public double RelPowUHF;
        public double NormLF;
        public double NormHF;
        // Frame Label
        [ColumnName("Label")]
        public int Result;
    }*/
    /*public class StressPrediction
    {
        [ColumnName("PredictedLabel")]
        public bool Prediction { get; set; }
        public float Probability { get; set; }
        public float Score { get; set; }
    }*/
}
