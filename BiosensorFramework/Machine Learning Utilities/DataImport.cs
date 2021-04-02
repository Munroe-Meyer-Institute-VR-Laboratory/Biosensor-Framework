using System;
using System.Collections.Generic;
using System.IO;

namespace MMIVR.BiosensorFramework.MachineLearningUtilities
{
    class DataImport
    {
        public static List<Tuple<string, List<double[]>, double[]>> LoadDataset(string DirectoryPath = @"C:\Users\Walker Arce\Documents\Business\Research\UNMC\Software\Python\MachineLearning\E4Inferencing\datasets\")
        {
            string[] files = Directory.GetFiles(DirectoryPath, "*.csv", SearchOption.AllDirectories);
            List<Tuple<string, List<double[]>, double[]>> Dataset = new List<Tuple<string, List<double[]>, double[]>>();
            foreach (string file in files)
            {
                using (StreamReader reader = new StreamReader(file))
                {
                    List<double[]> SubjectData = new List<double[]>();
                    double[] SubjectLabels = null;
                    int DataLines = 4;
                    while (!reader.EndOfStream)
                    {
                        if (DataLines == 4)
                        {
                            var line = reader.ReadLine();
                            double[] Acc = Array.ConvertAll(line.Split(','), double.Parse);
                            double[] AccX = new double[Acc.Length / 3],
                                AccY = new double[Acc.Length / 3],
                                AccZ = new double[Acc.Length / 3];
                            for (int i = 0; i < Acc.Length; i += 3)
                            {
                                AccX[i / 3] = Acc[i];
                                AccY[i / 3] = Acc[i + 1];
                                AccZ[i / 3] = Acc[i + 2];
                            }
                            SubjectData.Add(Acc);
                            SubjectData.Add(AccX);
                            SubjectData.Add(AccY);
                            SubjectData.Add(AccZ);
                            DataLines--;
                        }
                        if (DataLines < 4 && DataLines != 0)
                        {
                            var line = reader.ReadLine();
                            SubjectData.Add(Array.ConvertAll(line.Split(','), double.Parse));
                            DataLines--;
                        }
                        else
                        {
                            var line = reader.ReadLine();
                            SubjectLabels = Array.ConvertAll(line.Split(','), double.Parse);
                        }
                    }
                    Dataset.Add(Tuple.Create(Path.GetFileNameWithoutExtension(file), SubjectData, SubjectLabels));
                }
            }
            return Dataset;
        }
    }
}