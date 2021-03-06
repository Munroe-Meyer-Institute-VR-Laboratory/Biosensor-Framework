using System;
using System.Collections.Generic;
using System.Linq;

namespace MMIVR.BiosensorFramework.DataProcessing
{
    class BloodVolumePulse
    {
        /// <summary>
        /// Computes the features of the PPG signal.  Returns the heartrate, RR intervals, NN50 metric, pNN50 metric.
        /// </summary>
        /// <param name="Beats">Indices of heart beats in PPG signal.</param>
        /// <param name="SamplingRate">The sensor sampling rate.</param>
        /// <returns></returns>
        public static Tuple<double[], double[], int, double> GetHeartFeatures(int[] Beats, double SamplingRate)
        {
            List<double> RRIntervals = new List<double>();
            List<double> HR = new List<double>();
            double SamplesPerSecond = SamplingRate * 60.0;
            for (int i = 1; i < Beats.Length; i++)
            {
                RRIntervals.Add(Beats[i] - Beats[i - 1]);
                HR.Add(SamplesPerSecond / RRIntervals.Last());
            }
            int NN50 = 0;
            // Get value of 50 ms interval in samples
            double Interval = SamplingRate * 0.05;
            for (int i = 1; i < RRIntervals.Count - 1; i++)
            {
                if (Math.Abs(RRIntervals[i] - RRIntervals[i - 1]) > Interval)
                {
                    NN50++;
                }
            }
            double pNN50 = (double)NN50 / RRIntervals.Count;
            return Tuple.Create(HR.ToArray(), RRIntervals.ToArray(), NN50, pNN50);
        }
    }
}