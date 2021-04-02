using MathNet.Numerics.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;

using MMIVR.BiosensorFramework.DataProcessing;

namespace MMIVR.BiosensorFramework.InputPipeline
{
    class SignalProcessing
    {
        public static List<double> ProcessEdaSignal(double[] EdaSignal, double SamplingRate = 4.0)
        {
            List<double> Features;
            if (EdaSignal == null || EdaSignal.Length == 0)
                Features = new List<double>(new double[15]);
            else
            {
                double[] ScrSignal = TarvainenDetrending.RemoveTrend(EdaSignal, 1500, null);
                double[] SclSignal = TarvainenDetrending.GetResidual(EdaSignal, ScrSignal);
                double[] SummedFeatures = HealeyStressDetection.SumFeatures(HealeyStressDetection.GetPeakFeatures(EdaSignal, SamplingRate));
                Features = new List<double>
            {
                EdaSignal.Mean(),
                EdaSignal.StandardDeviation(),
                EdaSignal.Min(),
                EdaSignal.Max(),
                FeatureExtraction.SignalSlope(
                    EdaSignal, Array.ConvertAll(Enumerable.Range(0, EdaSignal.Length).ToArray(), Convert.ToDouble)),
                FeatureExtraction.SignalDynamicRange(EdaSignal),
                SclSignal.Mean(),
                SclSignal.StandardDeviation(),
                ScrSignal.StandardDeviation(),
                FeatureExtraction.SignalCorrelation(
                    SclSignal, Array.ConvertAll(Enumerable.Range(0, SclSignal.Length).ToArray(), Convert.ToDouble)),
                FeatureExtraction.FindLocalMaxima(ScrSignal, ScrSignal.Mean(), 4).Length,
                SummedFeatures[0],
                SummedFeatures[1],
                SummedFeatures[2],
                FeatureExtraction.SignalSlope(
                    ScrSignal, Array.ConvertAll(Enumerable.Range(0, ScrSignal.Length).ToArray(), Convert.ToDouble)),
            };
            }
            return Features;
        }
        public static List<double> ProcessAccSignal(double[] AccSignal, double SamplingRate = 32.0)
        {
            List<double> Features;
            if (AccSignal == null || AccSignal.Length == 0)
                Features = new List<double>(new double[4]);
            else
            {
                Features = new List<double>
            {
                AccSignal.Mean(),
                AccSignal.StandardDeviation(),
                FeatureExtraction.SignalAbsolute(FeatureExtraction.SignalIntegral(AccSignal)),
                FeatureExtraction.SignalPeakFrequency(AccSignal, SamplingRate),
            };
            }
            return Features;
        }
        public static List<double> ProcessTmpSignal(double[] TempSignal)
        {
            List<double> Features = null;
            if (TempSignal == null || TempSignal.Length == 0)
                Features = new List<double>(new double[6]);
            else
                Features = new List<double>
            {
                TempSignal.Mean(),
                TempSignal.StandardDeviation(),
                TempSignal.Min(),
                TempSignal.Max(),
                FeatureExtraction.SignalDynamicRange(TempSignal),
                FeatureExtraction.SignalSlope(
                    TempSignal, Array.ConvertAll(Enumerable.Range(0, TempSignal.Length).ToArray(), Convert.ToDouble))
            };

            return Features;
        }
        public static List<double> ProcessPpgSignal(double[] PpgSignal, double SamplingRate = 64.0, double Threshold = 3.5, int PeakSize = 3)
        {
            List<double> Features;
            if (PpgSignal.Length == 0 || PpgSignal == null)
                Features = new List<double>(new double[18]);
            else
            {
                double[] PpgDer = HealeyStressDetection.FiniteDifference(ref PpgSignal);
                int[] Beats = FeatureExtraction.FindLocalMaxima(PpgDer, Threshold, PeakSize);
                Tuple<double[], double[], int, double> PpgFeatures = BloodVolumePulse.GetHeartFeatures(Beats, SamplingRate);
                List<Tuple<double, double>> FreqBands = new List<Tuple<double, double>>()
            {
                Tuple.Create(0.01, 0.04),
                Tuple.Create(0.05, 0.15),
                Tuple.Create(0.15, 0.4),
                Tuple.Create(0.4, 1.0),
            };
                List<double[]> HrvFreqEner = FeatureExtraction.SignalFreqBandEnergies(PpgSignal, FreqBands, SamplingRate);
                List<double> FreqSummation = FeatureExtraction.SignalFreqSummation(HrvFreqEner);
                List<double> FreqPower = FeatureExtraction.SignalRelativePower(HrvFreqEner, 1024 / 2);
                Features = new List<double>
            {
                PpgFeatures.Item1.Mean(),
                PpgFeatures.Item1.StandardDeviation(),
                PpgFeatures.Item2.Mean(),
                PpgFeatures.Item2.StandardDeviation(),
                PpgFeatures.Item3,
                (double)PpgFeatures.Item4,
                FeatureExtraction.SignalRMS(PpgFeatures.Item2),
                FeatureExtraction.SignalFreqRatio(FreqSummation[1], FreqSummation[2]),
                FreqSummation[0],
                FreqSummation[1],
                FreqSummation[2],
                FreqSummation[3],
                FreqPower[0],
                FreqPower[1],
                FreqPower[2],
                FreqPower[3],
                FeatureExtraction.SignalFreqNormalize(HrvFreqEner[1]).Mean(),
                FeatureExtraction.SignalFreqNormalize(HrvFreqEner[2]).Mean(),
            };
            }
            return Features;
        }
    }
}