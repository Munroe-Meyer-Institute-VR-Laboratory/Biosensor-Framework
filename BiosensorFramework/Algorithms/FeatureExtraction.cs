using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Statistics;
using MathNet.Numerics;
using MathNet.Numerics.Integration;
using MathNet.Numerics.LinearAlgebra;

using MMIVR.DataProcessing.ThirdParty.DSPLib;
using MMIVR.Extensions;

namespace MMIVR.DataProcessing
{
    class FeatureExtraction
    {
        public static double SignalMean(List<float> Signal)
        {
            return Signal.Mean();
        }
        public static double SignalStandardDeviation(List<float> Signal)
        {
            return Signal.StandardDeviation();
        }
        public static double SignalIntegral(double[] Signal)
        {
            int Partitions = Signal.Length;
            if (Signal.Length % 2 != 0)
                Partitions += 1;
            Func<double, double> f = x =>
            {
                return Signal[(int)x];
            };
            return SimpsonRule.IntegrateComposite(f, 0, Signal.Length - 1, Partitions);
        }
        public static double SignalAbsolute(double AUC)
        {
            return Math.Abs(AUC);
        }
        public static double SignalDynamicRange(double[] Signal)
        {
            return Signal.Max() / Signal.Min();
        }
        public static double SignalRMS(double[] Signal)
        {
            double FirstTerm = 1.0 / (2.0 * Signal.Length);
            Array.ForEach(Signal, x => x = (float)Math.Pow(x, 2));
            double SecondTerm = Signal.Sum();
            return Math.Sqrt(FirstTerm * SecondTerm);
        }
        public static Tuple<double, double> SignalMinMax(List<float> Signal)
        {
            return Tuple.Create((double)Signal.Max(), (double)Signal.Min());
        }
        public static double SignalFreqRatio(double LowFreq, double HighFreq)
        {
            return LowFreq / HighFreq;
        }
        public static double SignalPeakFrequency(double[] Signal, double SamplingRate)
        {
            ProcessFFT.ProcessSignal(ref Signal, SamplingRate, out double[] FreqSpan, out _);
            return Math.Abs(FreqSpan.Max() * SamplingRate);
        }
        public static List<double[]> SignalFreqBandEnergies(double[] Signal, List<Tuple<double, double>> Bands, double SamplingFrequency, int Order = 5)
        {
            List<double[]> SignalEnergies = new List<double[]>();
            foreach (Tuple<double, double> band in Bands)
            {
                BandpassFilterButterworthImplementation bp = new BandpassFilterButterworthImplementation(band.Item1, band.Item2, Order, SamplingFrequency);
                bp.compute(Signal);
                SignalEnergies.Add(Signal);
                /*double[] bp = FirCoefficients.BandPass(SamplingFrequency, band.Item1, band.Item2, HalfOrder);
                OnlineFirFilter filter = new OnlineFirFilter(bp);
                SignalEnergies.Add(filter.ProcessSamples(Signal));*/
            }
            return SignalEnergies;
        }
        public static List<double> SignalRelativePower(List<double[]> Energies, double Resolution, double Epsilon = 1e-7)
        {
            List<double> RelativePowers = new List<double>();
            foreach (double[] energy in Energies)
            {
                Array.ForEach(energy, x => x /= Resolution);
                RelativePowers.Add(20 * Math.Log10(energy.Max() + Epsilon));
            }
            return RelativePowers;
        }
        public static double[] SignalFreqNormalize(double[] Freq)
        {
            double norm = Matrix<double>.Build.Dense(1, Freq.Length, Freq).FrobeniusNorm();
            Array.ForEach(Freq, x => x /= norm);
            return Freq;
        }
        public static double SignalSlope(double[] Signal, double[] TimeLabels)
        {
            Tuple<double, double> p = Fit.Line(Signal, TimeLabels);
            return p.Item2;
        }
        public static double SignalPercentile(double[] Signal, int percentile)
        {
            return Statistics.Percentile(Signal, percentile);
        }
        public static double SignalCorrelation(double[] Signal, double[] TimeLabels)
        {
            return Correlation.Pearson(Signal, TimeLabels);
        }
        public static List<double> SignalFreqSummation(List<double[]> Freq)
        {
            List<double> FreqSum = new List<double>();
            foreach (double[] freq in Freq)
            {
                Array.ForEach(freq, x => x *= x);
                FreqSum.Add(freq.Sum());
            }
            return FreqSum;
        }
        public static int[] FindLocalMaxima(double[] array, double Threshold, int Samples)
        {
            List<int> Maxima = new List<int>();
            List<int> Peak = new List<int>();
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i] >= Threshold)
                    Peak.Add(i);
                else
                {
                    if (Peak.Count > Samples)
                    {
                        Maxima.Add(Array.IndexOf(array, array.GetSubArray(Peak[0], Peak.Last()).Max()));
                    }
                    Peak = new List<int>();
                }
            }
            return Maxima.ToArray();
        }
    }
}