using System;
using System.Collections.Generic;
using System.Linq;

using MMIVR.BiosensorFramework.DataProcessing.ThirdParty.DSPLib;

namespace MMIVR.BiosensorFramework.DataProcessing
{
    static class HealeyStressDetection
    {
        /// <summary>
        /// Uses the approximate first derivative of the GSR signal to find the magnitude, duration, and amplitude of the peaks in the 
        /// signal.
        /// </summary>
        /// Healey, Jennifer Anne. Wearable and automotive systems for affect recognition from physiology. Diss. 
        ///     Massachusetts Institute of Technology, 2000.
        /// <param name="Signal"></param>
        /// <param name="SamplingRate"></param>
        /// <param name="CutoffFreq"></param>
        /// <param name="Threshold"></param>
        public static List<double[]> GetPeakFeatures(double[] Signal, double SamplingRate, double CutoffFreq = 5.0, double Threshold = 0.02325)
        {
            // Create low pass filter of 4 Hz cutoff
            LowpassFilterButterworthImplementation lp = new LowpassFilterButterworthImplementation(CutoffFreq, 4, SamplingRate);
            // Filter out HF noise
            lp.Compute(Signal);
            // Calculate finite difference (approximate derivative) of filtered signal
            double[] SignalDer = FiniteDifference(ref Signal);
            // Get indices in signal that are above the threshold
            List<List<int>> Peaks = FindAbove(SignalDer, Threshold, SamplingRate);
            // Get indices of the positive and minus zero crossings
            Tuple<int[], int[]> ZeroCrossings = FindZeroCrossings(SignalDer);
            // Construct peaks using Peaks and ZeroCrossings
            List<int[]> FinalPeaks = ConstructPeaks(Peaks, ZeroCrossings);
            // Handle overlapping peaks that don't zero cross
            FinalPeaks = MergeOverlappingPeaks(FinalPeaks);
            // Get magnitude, duration, and amplitude of peaks
            List<double[]> PeakFeatures = new List<double[]>();
            foreach (int[] Peak in FinalPeaks)
            {
                double[] PeakFeature = new double[3];
                // Magnitude
                PeakFeature[0] = Signal.GetSubArray(Peak[0], Peak.Last()).Max() - Signal[Peak[0]];
                // Duration (number of samples / Fs = # sec)
                PeakFeature[1] = Peak.Length;
                // Amplitude (triangle rule)
                PeakFeature[2] = 0.5 * (PeakFeature[0] * PeakFeature[1]);
                PeakFeatures.Add(PeakFeature);
            }
            return PeakFeatures;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="PeakFeatures"> Output of the GetPeakFeatures function </param>
        /// <returns></returns>
        public static double[] SumFeatures(List<double[]> PeakFeatures)
        {
            double[] SummedFeatures = new double[3];
            foreach (double[] Peak in PeakFeatures)
            {
                SummedFeatures[0] += Peak[0];
                SummedFeatures[1] += Peak[1];
                SummedFeatures[2] += Peak[2];
            }
            return SummedFeatures;
        }
        /// <summary>
        /// Returns indices above a threshold, removes zero elements
        /// </summary>
        /// <param name="array"></param>
        /// <param name="Threshold"></param>
        /// <returns></returns>
        public static List<List<int>> FindAbove(double[] array, double Threshold, double SamplingRate)
        {
            List<List<int>> Peaks = new List<List<int>>();
            List<int> Peak = new List<int>();
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] > Threshold)
                    Peak.Add(i);
                else
                {
                    if (Peak.Count >= SamplingRate)
                        Peaks.Add(Peak);
                    Peak = new List<int>();
                }
            }
            return Peaks;
        }
        /// <summary>
        /// Find zero crossings for two cases:
        ///     1. Goes from below zero to above zero
        ///     2. Goes from above zero to below zero
        /// </summary>
        /// <param name="array"> Array to find the indices of zero crossings in </param>
        /// <returns> A tuple of the form: Item1 is case one and Item2 is case twos </returns>
        private static Tuple<int[], int[]> FindZeroCrossings(double[] array)
        {
            List<int> ZeroPlusCrossing = new List<int>();
            List<int> ZeroMinusCrossing = new List<int>();
            for (int i = 0; i < array.Length - 1; i++)
            {
                if (array[i] < 0 && array[i + 1] > 0)
                    ZeroPlusCrossing.Add(i);
                else if (array[i] > 0 && array[i + 1] < 0)
                    ZeroMinusCrossing.Add(i);
            }
            return Tuple.Create(ZeroPlusCrossing.ToArray(), ZeroMinusCrossing.ToArray());
        }
        /// <summary>
        /// Takes the zero crossings and peaks and reconstructs the index range for the peaks.
        /// </summary>
        /// <param name="Peaks"> List of List of indices that represent the indices above threshold </param>
        /// <param name="ZeroCrossings"> List of indices where zero crossings happen </param>
        /// <returns></returns>
        private static List<int[]> ConstructPeaks(List<List<int>> Peaks, Tuple<int[], int[]> ZeroCrossings)
        {
            List<int[]> ConstructedPeaks = new List<int[]>();
            foreach (List<int> Peak in Peaks)
            {
                int StartIndex = FindNearestPlus(Peak[0], ZeroCrossings.Item1);
                if (StartIndex == -1)
                    StartIndex = Peak[0];
                int EndIndex = FindNearestMinus(Peak.Last(), ZeroCrossings.Item2);
                if (EndIndex == -1)
                    EndIndex = Peak.Last();
                ConstructedPeaks.Add(Enumerable.Range(StartIndex, EndIndex - StartIndex).ToArray());
            }
            return ConstructedPeaks;
        }
        /// <summary>
        /// Finds the nearest index for zero crossings that go from above zero to below zero
        /// </summary>
        /// <param name="PeakStart"> Peak starting index </param>
        /// <param name="ZeroCrossings"> Array of indices that represent the zero crossing points </param>
        /// <returns> int, the index in ZeroCrossings that is closest to PeakStart </returns>
        private static int FindNearestMinus(int PeakStart, int[] ZeroCrossings)
        {
            int NearestIndex = -1;
            for (int i = ZeroCrossings.Length - 1; i >= 0; i--)
            {
                if (ZeroCrossings[i] - PeakStart >= 0)
                    NearestIndex = ZeroCrossings[i];
                else
                    break;
            }
            return NearestIndex;
        }
        /// <summary>
        /// Finds the nearest index for zero crossings that go from below zero to above zero
        /// </summary>
        /// <param name="PeakStart"> Peak starting index </param>
        /// <param name="ZeroCrossings"> Array of indices that represent the zero crossing points </param>
        /// <returns> int, the index in ZeroCrossings that is closest to PeakStart </returns>
        private static int FindNearestPlus(int PeakStart, int[] ZeroCrossings)
        {
            int NearestIndex = -1;
            for (int i = 0; i < ZeroCrossings.Length; i++)
            {
                if (PeakStart - ZeroCrossings[i] >= 0)
                    NearestIndex = ZeroCrossings[i];
                else
                    break;
            }
            return NearestIndex;
        }
        /// <summary>
        /// If reconstructed peaks are overlapping in their indices, which can happen if a peak does not zero cross before the next peak,
        /// then remove the copies.
        /// </summary>
        /// <param name="Peaks"> List of indices for the reconstructed peaks </param>
        /// <returns> List with any overlapping peaks removed </returns>
        private static List<int[]> MergeOverlappingPeaks(List<int[]> Peaks)
        {
            List<int[]> MergedPeaks = new List<int[]>();
            for (int i = 0; i < Peaks.Count; i++)
            {
                if (i == (Peaks.Count - 1))
                {
                    MergedPeaks.Add(Peaks[i]);
                    continue;
                }
                if (!Enumerable.SequenceEqual(Peaks[i], Peaks[i + 1]))
                    MergedPeaks.Add(Peaks[i]);
            }
            return MergedPeaks;
        }
        /// <summary>
        /// Calculates the differences between adjacent elements, equating to the approximate first derivative
        /// Algorithm: Y = [X(1)-X(0), X(2)-X(1), ..., X(m + 1)-X(m)]
        /// Source: https://www.mathworks.com/help/matlab/ref/diff.html?s_tid=srchtitle
        /// </summary>
        /// <param name="Signal"></param>
        /// <returns></returns>
        public static double[] FiniteDifference(ref double[] Signal)
        {
            double[] SignalDer = new double[Signal.Length - 1];
            for (int i = 0; i < SignalDer.Length; i++)
                SignalDer[i] = Signal[i + 1] - Signal[i];
            return SignalDer;
        }
    }
}