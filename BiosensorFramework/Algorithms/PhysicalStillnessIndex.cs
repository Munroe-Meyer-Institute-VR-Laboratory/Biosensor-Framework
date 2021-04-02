using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

/// <summary>
/// Calculates the physical stillness index, specified in the paper referenced below.  
/// Chang, Kang-Ming, et al. "A wireless accelerometer-based body posture stability detection system and its application for 
///     meditation practitioners." Sensors 12.12 (2012): 17620-17632.
/// </summary>
namespace MMIVR.BiosensorFramework.DataProcessing
{
    public class PhysicalStillnessIndex
    {
        /// <summary>
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