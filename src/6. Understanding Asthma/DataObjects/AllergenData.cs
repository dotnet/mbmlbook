// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
namespace UnderstandingAsthma
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.ML.Probabilistic.Utilities;

    public class AllergenData
    {
        public int?[][][] SkinTestData;
        public int?[][][] IgeTestData;
        public string[] OutcomeIndexToOutcomeName;
        public Dictionary<string, int> OutcomeNameToOutcomeIndex;
        public int?[][] Outcomes;
        public int[][] DataCountAllergenYear;
        public int[][] DataCountAllergenTest;
        public int[][][] DataCountAllergenTestYear;
        public int[] DataCountChild;
        public int[][] PositiveCountAllergenYear;
        public int[][] PositiveCountAllergenTest;
        public int[] PositiveCountChild;
        public int[] IndicesToIncludedChildren;
        public int[] OutcomeCount;
        public int[] PositiveOutcomeCount;

        public AllergenData()
        {
        }

        private struct ColumnDetail
        {
            public short allergen;
            public short year;
            public short test;
            public short outcome;
        }

        public static List<string> Tests = new List<string>() { "Skin", "IgE" };
        public static List<string> AllergensInFile = new List<string>() { "Mite", "Cat", "Dog", "Pollen", "Mould", "Milk", "Egg", "Peanut" };
        public static List<string> Years = new List<string>() { "1", "3", "5", "8" };
        public static int NumTests = Tests.Count;
        public static int NumYears = Years.Count;

        public int TotalDataCount
        {
            get
            {
                if (this.DataCountChild != null)
                {
                    return DataCountChild.Sum();
                }
                else
                {
                    return 0;
                }
            }
        }

        public int NumChildren
        {
            get
            {
                if (this.DataCountChild != null)
                {
                    return this.DataCountChild.Length;
                }
                else
                {
                    return 0;
                }
            }
        }

        public List<string> Allergens
        {
            get; private set;
        }

        public int NumAllergens
        {
            get
            {
                return Allergens == null ? 0 : Allergens.Count;
            }
        }

        public int NumOutcomes
        {
            get
            {
                return OutcomeIndexToOutcomeName == null ? 0 : OutcomeIndexToOutcomeName.Length;
            }
        }


        public void LoadDataFromTabDelimitedFile(string fileName)
        {
            Dictionary<ColumnDetail, int> detailToIndex = new Dictionary<ColumnDetail, int>();
            ColumnDetail[] indexToDetail = null;
            this.OutcomeNameToOutcomeIndex = new Dictionary<string, int>();
            var outcomeIndexToOutcomeName = new List<string>();
            List<Dictionary<int, bool>> children = new List<Dictionary<int, bool>>();
            this.Allergens = AllergensInFile;
            using (var reader = new StreamReader(fileName))
            {
                // Read the header
                string str = reader.ReadLine();
                string[] columns = str.Split('\t');
                indexToDetail = new ColumnDetail[columns.Length];
                ColumnDetail detail;
                for (int i = 0; i < columns.Length; i++)
                {
                    string col = columns[i];
                    string[] parts = col.Split('_');
                    if (parts.Length == 1)
                    {
                        short outcomeIndex = (short)this.OutcomeNameToOutcomeIndex.Count;
                        // Take this as an outcome
                        detail = new ColumnDetail
                        {
                            test = -1,
                            allergen = -1,
                            year = -1,
                            outcome = outcomeIndex
                        };

                        this.OutcomeNameToOutcomeIndex[parts[0]] = outcomeIndex;
                        outcomeIndexToOutcomeName.Add(parts[0]);
                    }
                    else
                    {
                        int testX = Tests.FindIndex(t => t == parts[0]);
                        int allergenX = AllergensInFile.FindIndex(a => a == parts[1]);
                        int yearX = Years.FindIndex(y => y == parts[2]);

                        if (testX < 0 || allergenX < 0 || yearX < 0)
                        {
                            throw new FileFormatException("Header not as expected");
                        }

                        detail = new ColumnDetail
                        {
                            test = (short)testX,
                            allergen = (short)allergenX,
                            year = (short)yearX,
                            outcome = -1
                        };
                    }

                    detailToIndex[detail] = i;
                    indexToDetail[i] = detail;
                }

                // Now read in each child
                while ((str = reader.ReadLine()) != null)
                {
                    Dictionary<int, bool> child = new Dictionary<int, bool>();
                    columns = str.Split('\t');
                    for (int i = 0; i < columns.Length; i++)
                    {
                        int yesno;
                        if (int.TryParse(columns[i], out yesno))
                        {
                            child[i] = yesno > 0;
                        }
                    }

                    children.Add(child);
                }
            }

            // Now fill in the data structure.
            int numTests = Tests.Count;
            int numAllergens = Allergens.Count;
            int numYears = Years.Count;
            int numChildren = children.Count;
            this.OutcomeIndexToOutcomeName = outcomeIndexToOutcomeName.ToArray();
            int numOutcomes = this.OutcomeIndexToOutcomeName.Length;

            int childX = 0;
            this.SkinTestData = Util.ArrayInit(numYears, y => Util.ArrayInit(numChildren, n => Util.ArrayInit(numAllergens, a => (int?)null)));
            this.IgeTestData = Util.ArrayInit(numYears, y => Util.ArrayInit(numChildren, n => Util.ArrayInit(numAllergens, a => (int?)null)));
            this.Outcomes = Util.ArrayInit(numOutcomes, y => Util.ArrayInit(numChildren, n => (int?)null));

            this.IndicesToIncludedChildren = Util.ArrayInit(numChildren, i => i);

            foreach (var child in children)
            {
                foreach (var kvp in child)
                {
                    var detail = indexToDetail[kvp.Key];

                    if (detail.outcome >= 0)
                    {
                        this.Outcomes[detail.outcome][childX] = kvp.Value ? 1 : 0;
                    }
                    else if (detail.test == 0)
                    {
                        this.SkinTestData[detail.year][childX][detail.allergen] = kvp.Value ? 1 : 0;
                    }
                    else
                    {
                        this.IgeTestData[detail.year][childX][detail.allergen] = kvp.Value ? 1 : 0;
                    }
                }

                childX++;
            }

            setStatisticsFromData();
        }

        private void setStatisticsFromData()
        {
            int numTests = AllergenData.NumTests;
            int numAllergens = this.NumAllergens;
            int numYears = AllergenData.NumYears;
            int numChildren = this.SkinTestData[0].GetLength(0);
            int numOutcomes = this.NumOutcomes;

            this.DataCountAllergenYear = Util.ArrayInit(numAllergens, a => Util.ArrayInit(numYears, y => 0));
            this.DataCountAllergenTest = Util.ArrayInit(numAllergens, a => Util.ArrayInit(numTests, t => 0));
            this.DataCountAllergenTestYear = Util.ArrayInit(numAllergens, a => Util.ArrayInit(numTests, t => Util.ArrayInit(numYears, y => 0)));
            this.DataCountChild = Util.ArrayInit(numChildren, n => 0);
            this.PositiveCountAllergenYear = Util.ArrayInit(numAllergens, a => Util.ArrayInit(numYears, y => 0));
            this.PositiveCountAllergenTest = Util.ArrayInit(numAllergens, a => Util.ArrayInit(numTests, t => 0));
            this.PositiveCountChild = Util.ArrayInit(numChildren, n => 0);
            this.OutcomeCount = Util.ArrayInit(numOutcomes, o => 0);
            this.PositiveOutcomeCount = Util.ArrayInit(numOutcomes, o => 0);

            for (int n = 0; n < NumChildren; n++)
            {
                for (int y = 0; y < NumYears; y++)
                {
                    for (int a = 0; a < NumAllergens; a++)
                    {
                        int? skinVal = this.SkinTestData[y][n][a];
                        if (skinVal != null)
                        {
                            this.DataCountChild[n]++;
                            this.DataCountAllergenYear[a][y]++;
                            this.DataCountAllergenTest[a][0]++;
                            this.DataCountAllergenTestYear[a][0][y]++;
                            if (skinVal.Value > 0)
                            {
                                this.PositiveCountAllergenYear[a][y]++;
                                this.PositiveCountAllergenTest[a][0]++;
                                this.PositiveCountChild[n]++;

                            }
                        }

                        int? igeValue = this.IgeTestData[y][n][a];
                        if (igeValue != null)
                        {
                            this.DataCountChild[n]++;
                            this.DataCountAllergenYear[a][y]++;
                            this.DataCountAllergenTest[a][1]++;
                            this.DataCountAllergenTestYear[a][1][y]++;
                            if (igeValue.Value > 0)
                            {
                                this.PositiveCountAllergenYear[a][y]++;
                                this.PositiveCountAllergenTest[a][1]++;
                                this.PositiveCountChild[n]++;

                            }
                        }
                    }
                }

                for (int o = 0; o < NumOutcomes; o++)
                {
                    int? outcome = this.Outcomes[o][n];
                    if (outcome != null)
                    {
                        this.OutcomeCount[o]++;
                        if (outcome.Value > 0)
                        {
                            this.PositiveOutcomeCount[o]++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Constructs a new AllergenData limited to the specific children.
        /// </summary>
        /// <param name="srcData">The source data.</param>
        /// <param name="indices">Indices of the children.</param>
        public AllergenData(AllergenData srcData, int[] indices = null)
        {
            this.IndicesToIncludedChildren = indices == null ? Util.ArrayInit(srcData.NumChildren, i => i) : (int[])indices.Clone();
            int numChildren = this.IndicesToIncludedChildren.Length;
            this.Allergens = srcData.Allergens;
            this.SkinTestData = Util.ArrayInit(AllergenData.NumYears, y => Util.ArrayInit(numChildren, n => Util.ArrayInit(srcData.NumAllergens, a => (int?)null)));
            this.IgeTestData = Util.ArrayInit(AllergenData.NumYears, y => Util.ArrayInit(numChildren, n => Util.ArrayInit(srcData.NumAllergens, a => (int?)null)));

            for (int n = 0; n < numChildren; n++)
            {
                int nSrc = this.IndicesToIncludedChildren[n];
                for (int a = 0; a < srcData.NumAllergens; a++)
                {
                    for (int y = 0; y < AllergenData.NumYears; y++)
                    {

                        this.SkinTestData[y][n][a] = srcData.SkinTestData[y][nSrc][a];
                        this.IgeTestData[y][n][a] = srcData.IgeTestData[y][nSrc][a];
                    }
                }
            }

            setStatisticsFromData();
        }
        
        public static AllergenData WithAllergensRemoved(AllergenData srcData, List<string> allergensToRemove)
        {
            AllergenData result = new AllergenData();
            result.Allergens = AllergensInFile.Except(allergensToRemove).ToList();
            int[] indexMapper = AllergensInFile.Select(str => result.Allergens.IndexOf(str)).ToArray();

            result.SkinTestData = Util.ArrayInit(AllergenData.NumYears, y => Util.ArrayInit(srcData.NumChildren, n => Util.ArrayInit(result.NumAllergens, a => (int?)null)));
            result.IgeTestData = Util.ArrayInit(AllergenData.NumYears, y => Util.ArrayInit(srcData.NumChildren, n => Util.ArrayInit(result.NumAllergens, a => (int?)null)));

            for (int n = 0; n < srcData.NumChildren; n++)
            {
                for (int a = 0; a < srcData.NumAllergens; a++)
                {
                    int mappedAllergenIndex = indexMapper[a];
                    if (mappedAllergenIndex < 0)
                    {
                        continue;
                    }

                    for (int y = 0; y < AllergenData.NumYears; y++)
                    {

                        result.SkinTestData[y][n][mappedAllergenIndex] = srcData.SkinTestData[y][n][a];
                        result.IgeTestData[y][n][mappedAllergenIndex] = srcData.IgeTestData[y][n][a];
                    }
                }
            }

            result.setStatisticsFromData();
            return result;
        }
    }
}
