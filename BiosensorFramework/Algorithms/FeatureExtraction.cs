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
    /// <summary>
    /// Class with methods for extracted features from time-series data.
    /// </summary>
    public class FeatureExtraction
    {
        /// <summary>
        /// Returns the mean of the data.
        /// </summary>
        /// <param name="Signal">List of data.</param>
        /// <returns>Mean of the signal.</returns>
        public static double SignalMean(List<float> Signal)
        {
            return Signal.Mean();
        }
        /// <summary>
        /// Returns the standard deviation of the data.
        /// </summary>
        /// <param name="Signal">List of data.</param>
        /// <returns>Standard deviation of the signal.</returns>
        public static double SignalStandardDeviation(List<float> Signal)
        {
            return Signal.StandardDeviation();
        }
        /// <summary>
        /// Computes the integral of the signal using Simpson's Rule.
        /// </summary>
        /// <param name="Signal">Array of data.</param>
        /// <returns>The integral of the data.</returns>
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
        /// <summary>
        /// Returns the absolute value of the area under the curve.
        /// </summary>
        /// <param name="AUC">The area under the curve of the signal.</param>
        /// <returns>The absolute value of the AUC.</returns>
        public static double SignalAbsolute(double AUC)
        {
            return Math.Abs(AUC);
        }
        /// <summary>
        /// Returns the dynamic range of the signal (max / min).
        /// </summary>
        /// <param name="Signal">Array of data.</param>
        /// <returns>The dynamic range of the signal.</returns>
        public static double SignalDynamicRange(double[] Signal)
        {
            return Signal.Max() / Signal.Min();
        }
        /// <summary>
        /// Computes the root mean square of the signal.
        /// </summary>
        /// <param name="Signal">Array of data.</param>
        /// <returns>The root mean square of the signal.</returns>
        public static double SignalRMS(double[] Signal)
        {
            double FirstTerm = 1.0 / (2.0 * Signal.Length);
            Array.ForEach(Signal, x => x = (float)Math.Pow(x, 2));
            double SecondTerm = Signal.Sum();
            return Math.Sqrt(FirstTerm * SecondTerm);
        }
        /// <summary>
        /// Gets the signal min and max from the array of data.
        /// </summary>
        /// <param name="Signal">List of data.</param>
        /// <returns>Tuple with max and min.</returns>
        public static Tuple<double, double> SignalMinMax(List<float> Signal)
        {
            return Tuple.Create((double)Signal.Max(), (double)Signal.Min());
        }
        /// <summary>
        /// Returns the frequency ratio of the signal.
        /// </summary>
        /// <param name="LowFreq">The low frequency value.</param>
        /// <param name="HighFreq">The high frequency value.</param>
        /// <returns>Returns the ratio of the freqencies (low / high).</returns>
        public static double SignalFreqRatio(double LowFreq, double HighFreq)
        {
            return LowFreq / HighFreq;
        }
        /// <summary>
        /// Computes the peak frequency of the signal.
        /// </summary>
        /// <param name="Signal">Array of data.</param>
        /// <param name="SamplingRate">The sampling rate of the signal.</param>
        /// <returns>Peak frequency component of the signal.</returns>
        public static double SignalPeakFrequency(double[] Signal, double SamplingRate)
        {
            ProcessFFT.ProcessSignal(ref Signal, SamplingRate, out double[] FreqSpan, out _);
            return Math.Abs(FreqSpan.Max() * SamplingRate);
        }
        /// <summary>
        /// Computes the frequency band energies of the signal.
        /// </summary>
        /// <param name="Signal">Array of data.</param>
        /// <param name="Bands">The frequency bands to compute.</param>
        /// <param name="SamplingFrequency">The sampling rate of the signal.</param>
        /// <param name="Order">The filter order. Defaults to 5.</param>
        /// <returns>List of frequency band energies.</returns>
        public static List<double[]> SignalFreqBandEnergies(double[] Signal, List<Tuple<double, double>> Bands, double SamplingFrequency, int Order = 5)
        {
            List<double[]> SignalEnergies = new List<double[]>();
            foreach (Tuple<double, double> band in Bands)
            {
                BandpassFilterButterworthImplementation bp = new BandpassFilterButterworthImplementation(band.Item1, band.Item2, Order, SamplingFrequency);
                bp.compute(Signal);
                SignalEnergies.Add(Signal);
            }
            return SignalEnergies;
        }
        /// <summary>
        /// Computes the signal relative power from the frequency band energies.
        /// </summary>
        /// <param name="Energies">The frequency band energies.</param>
        /// <param name="Resolution">The resolution of the relative power.</param>
        /// <param name="Epsilon">Default 1e-7.</param>
        /// <returns>List of doubles for the signal relative power.</returns>
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
        /// <summary>
        /// Normalizes the frequency components.
        /// </summary>
        /// <param name="Freq">The signal band energies.</param>
        /// <returns>The normalized frequency components.</returns>
        public static double[] SignalFreqNormalize(double[] Freq)
        {
            double norm = Matrix<double>.Build.Dense(1, Freq.Length, Freq).FrobeniusNorm();
            Array.ForEach(Freq, x => x /= norm);
            return Freq;
        }
        /// <summary>
        /// The slope of the data.
        /// </summary>
        /// <param name="Signal">Array of data.</param>
        /// <param name="TimeLabels">Array of time labels for each data point.</param>
        /// <returns>The slope of the signal.</returns>
        public static double SignalSlope(double[] Signal, double[] TimeLabels)
        {
            Tuple<double, double> p = Fit.Line(Signal, TimeLabels);
            return p.Item2;
        }
        /// <summary>
        /// Calculates the signal p-percentile.
        /// </summary>
        /// <param name="Signal">Array of data.</param>
        /// <param name="percentile">The percentile to calculate.</param>
        /// <returns>The signal percentile.</returns>
        public static double SignalPercentile(double[] Signal, int percentile)
        {
            return Statistics.Percentile(Signal, percentile);
        }
        /// <summary>
        /// Computes the Pearson correlation coefficient of the signal.
        /// </summary>
        /// <param name="Signal">Array of data.</param>
        /// <param name="TimeLabels">The time labels for each data point.</param>
        /// <returns>The correlation coefficient of the data.</returns>
        public static double SignalCorrelation(double[] Signal, double[] TimeLabels)
        {
            return Correlation.Pearson(Signal, TimeLabels);
        }
        /// <summary>
        /// Sums the frequency components of the signal band energies.
        /// </summary>
        /// <param name="Freq">The signal band energies.</param>
        /// <returns>List of doubles with the summed signal band energies.</returns>
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
        /// <summary>
        /// Finds the local maxima of the signal.
        /// </summary>
        /// <param name="array">Array of data.</param>
        /// <param name="Threshold">The threshold for the local maxima.</param>
        /// <param name="Samples">The number of samples required to classify a maxima.</param>
        /// <returns></returns>
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
        /// <returns>Root mean square</returns>
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
        /// <returns>The squared Vector3.</returns>
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
        /// <returns>Vector3 with subtracted bias.</returns>
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