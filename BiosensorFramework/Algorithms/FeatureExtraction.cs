using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Statistics;
using MathNet.Numerics;
using MathNet.Numerics.Integration;
using MathNet.Numerics.LinearAlgebra;

using MMIVR.BiosensorFramework.DataProcessing.ThirdParty.DSPLib;
using MMIVR.BiosensorFramework.Extensions;
using System.Numerics;

namespace MMIVR.BiosensorFramework.DataProcessing
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
        /// <summary>
        /// Calculates the physical stillness index, specified in the paper referenced below.  
        /// Chang, Kang-Ming, et al. "A wireless accelerometer-based body posture stability detection system and its application for 
        ///     meditation practitioners." Sensors 12.12 (2012): 17620-17632.
        /// Calculates a metric of physical stillness from acceleration readings. 
        /// Uses a sliding window, root mean square filter to get the absolute magnitude of the overall acceleration in the readings.
        /// This method works with the absolute magnitude of accelerations and not three-axis readings, use PSIVector for those purposes.
        /// </summary>
        /// <param name="Accelerations"> List of acceleration readings </param>
        /// <param name="AverageIndex"> Output PSI, the average of the filtered output </param>
        /// <param name="MaxIndex"> Output PSI, the max value of the filtered output </param>
        /// <param name="WindowWidth"> The number of values to incorporate in the window on both sides </param>
        /// <returns> The output filtered list of RMS values, can be discarded in favor of 'out' variables </returns>
        public static List<double> PSI(List<double> Accelerations, out double AverageIndex, out double MaxIndex, int WindowWidth = 8)
        {
            // Calculate the mean of the readings
            double MeanAcceleration = Accelerations.Sum();
            MeanAcceleration /= Accelerations.Count;
            // Remove bias from each measurement
            Accelerations.ForEach(i => i -= MeanAcceleration);
            // Calculate sliding window RMS
            double FirstTerm = 1.0 / (2.0 * WindowWidth + 1.0);
            List<double> RMS = new List<double>();
            double Summation = 0.0;
            for (int i = WindowWidth; i < Accelerations.Count - WindowWidth; i++)
            {
                for (int j = -WindowWidth; j < WindowWidth; j++)
                {
                    Summation = SquareSummation(Accelerations[i + j]);
                }
                RMS.Add(RootMeanSquare(FirstTerm, Summation));
            }
            AverageIndex = RMS.Average();
            MaxIndex = RMS.Max();
            return RMS;
        }
        /// <summary>
        /// Not Implemented.
        /// Performs the calculation of the physical stillness index using a 3D acceleration vector
        /// </summary>
        /// <param name="Accelerations"> List of acceleration readings </param>
        /// <param name="AverageIndex"> Output PSI, the average of the filtered output </param>
        /// <param name="MaxIndex"> Output PSI, the max value of the filtered output </param>
        /// <param name="WindowWidth"> The number of values to incorporate in the window on both sides </param>
        /// <returns> The output filtered list of RMS values, can be discarded in favor of 'out' variables </returns>
        public static List<List<double>> PSIVector(List<List<double>> Accelerations, out double AverageIndex, out double MaxIndex, int WindowWidth = 8)
        {
            List<List<double>> RMS = new List<List<double>>();
            AverageIndex = 0;
            MaxIndex = 0;
            foreach (var acc in Accelerations)
            {
                RMS.Add(PSI(acc, out double Average1D, out double Max1D));
                AverageIndex += Average1D;
                MaxIndex += Max1D;
            }
            AverageIndex /= Accelerations.Count;
            MaxIndex /= Accelerations.Count;
            return RMS;
        }
        /// <summary>
        /// Calculates the root mean square of the input variables
        /// </summary>
        /// <param name="FirstTerm"> Calculated first term of form - 1/2n </param>
        /// <param name="Summation"> The square sum of the input values </param>
        /// <returns></returns>
        static double RootMeanSquare(double FirstTerm, double Summation)
        {
            return Math.Sqrt(FirstTerm * Summation);
        }
        /// <summary>
        /// Performs square of input values
        /// </summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        static double SquareSummation(double Value)
        {
            return Math.Pow(Value, 2);
        }
        /// <summary>
        /// Performs square summation of Vector3 variables
        /// </summary>
        /// <param name="Value"> Vector3 with x, y, and z values </param>
        /// <returns></returns>
        static double SquareSummation(Vector3 Value)
        {
            double SquareSum = 0.0;
            SquareSum += Math.Pow(Value.X, 2);
            SquareSum += Math.Pow(Value.Y, 2);
            SquareSum += Math.Pow(Value.Z, 2);
            return SquareSum;
        }
        /// <summary>
        /// Removes mean from each axis of the 3D vector
        /// </summary>
        /// <param name="AccelerationVector"> 3D acceleration vector </param>
        /// <param name="BiasX"> Mean of X axis </param>
        /// <param name="BiasY"> Mean of Y axis </param>
        /// <param name="BiasZ"> Mean of Z axis </param>
        /// <returns></returns>
        static Vector3 RemoveBias(Vector3 AccelerationVector, double BiasX, double BiasY, double BiasZ)
        {
            return new Vector3()
            {
                X = (float)(AccelerationVector.X - BiasX),
                Y = (float)(AccelerationVector.Y - BiasY),
                Z = (float)(AccelerationVector.Z - BiasZ)
            };
        }
    }
}