using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Linq;

namespace MMIVR.BiosensorFramework.DataProcessing
{
    static class TarvainenDetrending
    {
        /// <summary>
        /// A time-varying finite-impulse-response high-pass filter for detrending
        /// If using this in a published study, cite:
        ///     Tarvainen, Mika P., Perttu O. Ranta-Aho, and Pasi A.Karjalainen. "An advanced detrending method with
        ///         application to HRV analysis." IEEE Transactions on Biomedical Engineering 49.2 (2002): 172-175.
        /// </summary>
        /// <param name="InputSignal"> Original EDA signal </param>
        /// <param name="Lambda"> The 'smoothing' factor of the filter </param>
        /// <param name="Filter"> Coefficients for the 0, 1, 2 diagonals in the second derivative matrix.  Can be null, will default to standard value </param>
        /// <returns> Filtered signal without a trend, double[] </returns>
        public static double[] RemoveTrend(double[] InputSignal, int Lambda, double[] Filter)
        {
            // Setup our needed variables from inputs
            Matrix<double> InputMatrix = Matrix<double>.Build.Dense(1, InputSignal.Length, InputSignal);
            int[] diags = new int[] { 0, 1, 2 };
            // If filter not set, set it to default
            Filter = Filter ?? new double[] { 1.0, -2.0, 1.0 };
            // Get our identity matrix
            DenseMatrix InputIdentity = DenseMatrix.CreateIdentity(InputSignal.Length);
            // Create our base diagonal matrix
            Matrix<double> D2 = Matrix<double>.Build.Dense(InputSignal.Length, InputSignal.Length - 2);
            // Set our diagonals to the filter coefficients
            foreach (int diag in diags)
            {
                for (int i = 0; i < D2.ColumnCount; i++)
                {
                    D2[i + diag, i] = Filter[diag];
                }
            }
            // Create filter
            Matrix<double> D2T = D2.Multiply(D2.Transpose());
            Matrix<double> LD2T = D2T.Multiply(Math.Pow(Lambda, 2));
            Matrix<double> Inverted = InputIdentity.Add(LD2T).Inverse();
            // Stationary signal with trend removed
            return InputIdentity.Subtract(Inverted).Multiply(InputMatrix.Transpose()).ToColumnArrays()[0];
        }
        /// <summary>
        /// Removes the stationary signal from the EDA signal and returns the residual signal
        /// </summary>
        /// <param name="TrendedSignal"> Original EDA signal </param>
        /// <param name="DetrendedSignal"> Output of RemoveTrend function </param>
        /// <returns></returns>
        public static double[] GetResidual(double[] TrendedSignal, double[] DetrendedSignal)
        {
            return TrendedSignal.Select((elem, index) => elem - DetrendedSignal[index]).ToArray();
        }
    }
}