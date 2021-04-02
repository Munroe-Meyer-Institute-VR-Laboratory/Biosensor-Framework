using System;
using System.Collections.Generic;

using MMIVR.BiosensorFramework.MachineLearningUtilities;

namespace MMIVR.BiosensorFramework.Extensions
{
    public static class ArrayExtensions
    {
        public static float[] ToFloat(this double[] array)
        {
            float[] RetArray = new float[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                RetArray[i] = (float)array[i];
            }
            return RetArray;
        }
        public static List<int> AllIndexesOf(this List<ExtractedMultiFeatures> array, int value)
        {
            List<int> Indices = new List<int>();
            for (int i = 0; i < array.Count; i++)
            {
                if (array[i].Result == value)
                {
                    Indices.Add(i);
                }
            }
            return Indices;
        }
        public static T[] GetSubArray<T>(this T[] array, int start, int end)
        {
            if (end > array.Length)
                end = array.Length;
            T[] RetArray = new T[end - start];
            int j = 0;
            for (int i = start; i < end; i++)
            {
                RetArray[j] = array[i];
                j++;
            }
            return RetArray;
        }

        private static Random rng = new Random();
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}