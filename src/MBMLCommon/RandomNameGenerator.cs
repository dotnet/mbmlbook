// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MBMLCommon
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;

    /// <summary>
    /// The random name generator. If MaxValues is not set, or is less than 1, then this will generate a (practically) endless stream
    /// </summary>
    public class RandomNameGenerator : IEnumerable<string>
    {
        /// <summary>
        /// The text info.
        /// </summary>
        private static readonly TextInfo TextInfo = new CultureInfo("en-GB", false).TextInfo;

        /// <summary>
        /// The male first names.
        /// </summary>
        private readonly IList<string> maleFirstNames;

        /// <summary>
        /// The female first names.
        /// </summary>
        private readonly IList<string> femaleFirstNames;

        /// <summary>
        /// The surnames.
        /// </summary>
        private readonly IList<string> surnames;

        /// <summary>
        /// Initializes a new instance of the <see cref="RandomNameGenerator"/> class.
        /// </summary>
        /// <param name="maleFirstNameFile">
        /// The male first name file.
        /// </param>
        /// <param name="femaleFirstNameFile">
        /// The female first name file.
        /// </param>
        /// <param name="surnameFile">
        /// The surname file.
        /// </param>
        /// <param name="seed">
        /// The seed.
        /// </param>
        /// <param name="maxItems">
        /// The max items.
        /// </param>
        /// <param name="firstNameOnly">
        /// The first Name Only.
        /// </param>
        public RandomNameGenerator(string maleFirstNameFile, string femaleFirstNameFile, string surnameFile, int seed = 0, int maxItems = int.MaxValue, bool firstNameOnly = false)
        {
            this.maleFirstNames = this.ReadTextFile(maleFirstNameFile);
            this.femaleFirstNames = this.ReadTextFile(femaleFirstNameFile);
            this.surnames = this.ReadTextFile(surnameFile);
            this.MaxItems = maxItems;
            this.Seed = seed;
            this.FirstNameOnly = firstNameOnly;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RandomNameGenerator"/> class.
        /// </summary>
        /// <param name="maleFirstNames">
        /// The male first names.
        /// </param>
        /// <param name="femaleFirstNames">
        /// The female first names.
        /// </param>
        /// <param name="surnames">
        /// The surnames.
        /// </param>
        /// <param name="seed">
        /// The seed.
        /// </param>
        /// <param name="maxItems">
        /// The max items.
        /// </param>
        /// <param name="firstNameOnly">
        /// The first Name Only.
        /// </param>
        public RandomNameGenerator(IList<string> maleFirstNames, IList<string> femaleFirstNames, IList<string> surnames, int seed = 0, int maxItems = int.MaxValue, bool firstNameOnly = false)
        {
            this.maleFirstNames = maleFirstNames;
            this.femaleFirstNames = femaleFirstNames;
            this.surnames = surnames;
            this.MaxItems = maxItems;
            this.Seed = seed;
            this.FirstNameOnly = firstNameOnly;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RandomNameGenerator"/> class.
        /// </summary>
        /// <param name="seed">
        /// The seed.
        /// </param>
        /// <param name="maxItems">
        /// The maximum items.
        /// </param>
        /// <param name="firstNameOnly">
        /// The first Name Only.
        /// </param>
        public RandomNameGenerator(int seed = 0, int maxItems = int.MaxValue, bool firstNameOnly = false)
            : this(Names.Male, Names.Female, Names.Surnames, seed, maxItems, firstNameOnly)
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether first name only.
        /// </summary>
        public bool FirstNameOnly { get; set; }

        /// <summary>
        /// Gets or sets the seed.
        /// </summary>
        public int Seed { get; set; }
        
        /// <summary>
        /// Gets or sets the max number of items.
        /// </summary>
        public int MaxItems { get; set; }

        /// <summary>
        /// The read text file.
        /// </summary>
        /// <param name="filename">
        /// The filename.
        /// </param>
        /// <returns>
        /// The list of strings
        /// </returns>
        public IList<string> ReadTextFile(string filename)
        {
            List<string> strings = new List<string>();
            using (StreamReader sr = new StreamReader(filename))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    if (line != null)
                    {
                        strings.Add(line.Trim());
                    }
                }
            }
            
            return strings;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<string> GetEnumerator()
        {
            Random random = new Random(this.Seed);

            HashSet<string> usedNames = new HashSet<string>();

            Func<IList<string>, string> randomStringFromList = ia => ia[random.Next(ia.Count)];

            if (this.MaxItems < 1)
            {
                this.MaxItems = int.MaxValue;
            }

            for (int i = 0; i < this.MaxItems; i++)
            {
                IList<string> list = random.Next(1) == 0 ? this.maleFirstNames : this.femaleFirstNames;

                string first = randomStringFromList(list);

                string fullName;

                if (this.FirstNameOnly)
                {
                    fullName = TextInfo.ToTitleCase(TextInfo.ToLower(first));
                }
                else
                {
                    // Add a middle initial 1 in 4 times
                    string initial = (random.Next(4) == 0) ? randomStringFromList(list)[0] + ". " : string.Empty;

                    string last = this.surnames[random.Next(this.surnames.Count)];

                    fullName = TextInfo.ToTitleCase(TextInfo.ToLower(first + " " + initial + last));
                }

                if (usedNames.Contains(fullName))
                {
                    // Name has been used, generate another one
                    continue;
                }

                usedNames.Add(fullName);

                yield return fullName;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}