// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Text
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.ML.Probabilistic.Distributions;

    /// <summary>
    /// Represents a word in a vocabulary.  
    /// </summary>
    /// <remarks>
    /// A word object represents all capitalizations of the same word e.g. The, the, THE.
    /// However, different capitalizations are tracked as different variants of the word
    /// and separate counts of each are stored.
    /// </remarks>
    public class Word
    {
        /// <summary>
        /// The variants
        /// </summary>
        private List<WordVariant> variants;

        /// <summary>
        /// Initializes a new instance of the <see cref="Word"/> class.
        /// </summary>
        internal Word()
        {
            this.Variants = new List<WordVariant>();
            this.TokenCount = 1;
        }

        /// <summary>
        /// Gets the number of times the word has been used.
        /// </summary>
        public int Count { get; internal set; }
        
        /// <summary>
        /// Gets or sets the list of variants of this word.
        /// </summary>
        public List<WordVariant> Variants
        {
            get
            {
                return this.variants;
            }

            set
            {
                this.variants = value;
                this.Count = 0;
                if (this.variants == null)
                {
                    return;
                }

                this.Count = this.variants.Sum(ia => ia.Count);
            }
        }

        /// <summary>
        /// Gets or sets a user-defined weight
        /// </summary>
        public double Weight { get; set; }

        /// <summary>
        /// Gets or sets the gaussian weight.
        /// </summary>
        public Gaussian GaussianWeight { get; set; }

        /// <summary>
        /// Gets or sets the token count.
        /// </summary>
        public int TokenCount { get; set; }

        /// <summary>
        /// Gets the most common variant of the word
        /// </summary>
        public WordVariant MostCommonVariant
        {
            get
            {
                return this.Variants.Count == 0 ? null : this.Variants.OrderByDescending(v => v.Count).First();
            }
        }

        /// <summary>
        /// Gets or sets the log background probability.
        /// </summary>
        public double LogBackgroundProbability { get; set; }

        /// <summary>
        /// Gets a value indicating whether this word has fixed children
        /// </summary>
        internal bool HasFixedChildren { get; private set; }

        /// <summary>
        /// Gets a sparse histogram of the indices of the words that follows this one in the source text.
        /// </summary>
        internal Dictionary<string, ChildInfo> Children { get; private set; }

        /// <summary>
        /// Returns the variant of the word identified by the supplied string.
        /// If the variant doesn't exist then it will be created, unless readOnly was true
        /// in which case null will be returned.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <param name="readOnly">if set to <c>true</c> [read only].</param>
        /// <returns>The word variant</returns>
        public WordVariant LookupVariant(string str, bool readOnly)
        {
            if (!readOnly)
            {
                this.Count++;
            }

            foreach (var v in this.Variants.Where(v => v.Text.Equals(str)))
            {
                if (!readOnly)
                {
                    v.Count++;
                }

                return v;
            }

            if (readOnly)
            {
                return null;
            }

            WordVariant wv = new WordVariant { Text = str, Count = 1 };
            this.Variants.Add(wv);
            return wv;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            WordVariant wv = this.MostCommonVariant;
            return wv == null ? "<null>" : wv.Text;
        }

        /// <summary>
        /// The next word is.
        /// </summary>
        /// <param name="w">
        /// The w.
        /// </param>
        internal void NextWordIs(Word w)
        {
            if (this.HasFixedChildren)
            {
                return;
            }

            if (w == null)
            {
                return;
            }

            if (this.Children == null)
            {
                this.Children = new Dictionary<string, ChildInfo>();
            }

            string key = Vocabulary.ToNormalizedForm(w.ToString());

            if (!this.Children.ContainsKey(key))
            {
                this.Children[key] = new ChildInfo();
            }

            var cw = this.Children[key];
            cw.Count++;
            this.Children[key] = cw;
        }

        /// <summary>
        /// Fixes the children.
        /// </summary>
        /// <param name="wordsToAdd">
        /// The words to add.
        /// </param>
        /// <param name="minCount">
        /// The min count.
        /// </param>
        internal void FixChildren(List<Word> wordsToAdd, int minCount)
        {
            this.Count = 0;
            this.HasFixedChildren = true;
            if (this.Children == null)
            {
                return;
            }

            var ch = new Dictionary<string, ChildInfo>();
            foreach (var kvp in this.Children)
            {
                if (kvp.Value.Count < minCount)
                {
                    continue;
                }

                ChildInfo ci = kvp.Value;
                Word w = new Word { TokenCount = this.TokenCount + 1 };
                w.LookupVariant(this + " " + kvp.Key, false);
                wordsToAdd.Add(w);
                ci.ChildWord = w;
                ch[kvp.Key] = ci;
            }

            this.Children = ch;
        }

        /// <summary>
        /// The child info.
        /// </summary>
        public struct ChildInfo
        {
            /// <summary>
            /// Gets or sets the count.
            /// </summary>
            internal int Count { get; set; }

            /// <summary>
            /// Gets or sets the child word.
            /// </summary>
            internal Word ChildWord { get; set; }

            /// <summary>
            /// Returns a <see cref="System.String" /> that represents this instance.
            /// </summary>
            /// <returns>
            /// A <see cref="System.String" /> that represents this instance.
            /// </returns>
            public override string ToString()
            {
                if (this.ChildWord != null)
                {
                    return this.ChildWord + " (" + this.Count + ")";
                }

                return "Count=" + this.Count;
            }
        }
    }
}
