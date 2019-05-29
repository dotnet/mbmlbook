// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Text
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// A set of words.
    /// </summary>
    /// <remarks>
    /// A vocabulary can be used to convert a text string into a sequence
    /// of integers indicating the index of each word into the vocabulary.
    /// </remarks>
    public class Vocabulary
    {
        /// <summary>
        /// The words lookup.
        /// </summary>
        private readonly Dictionary<string, Word> wordsLookup = new Dictionary<string, Word>();

        /// <summary>
        /// Gets or sets the words in this vocabulary
        /// </summary>
        public IList<Word> Words
        {
            get
            {
                return this.wordsLookup.Values.OrderByDescending(w => w.Count).ToList();
            }

            set
            {
                this.Clear();
                foreach (Word w in value)
                {
                    this.Add(w);
                }
            }
        }

        /// <summary>
        /// Gets the maximum number of tokens that make up a word.
        /// </summary>
        public int MaxTokenCount
        {
            get
            {
                return 1;
            }
        }

        /// <summary>
        /// Gets the number of words in this vocabulary
        /// </summary>
        public int WordCount
        {
            get
            {
                return this.wordsLookup.Count;
            }
        }

        /// <summary>
        /// Gets the total of the instance counts of all words in this vocabulary
        /// </summary>
        public int InstanceCount { get; private set; }

        /// <summary>
        /// Gets or sets the average word count per body of text
        /// </summary>
        public double AverageWordCountPerText { get; set; }

        /// <summary>
        /// Gets or sets the total number of messages 
        /// </summary>
        public double TotalNumberOfTexts { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the vocabulary is read only.  When not read only,
        /// looking up a new word will expand the vocabulary by creating an entry for that word.  
        /// Otherwise, the vocabulary is frozen and will not be modified by any operation.
        /// </summary>
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Creates a vocabulary from all words that occur at least 'threshold' times in the
        /// supplied enumerable of strings.
        /// </summary>
        /// <param name="texts">The texts.</param>
        /// <param name="threshold">The threshold.</param>
        /// <returns>The vocabulary</returns>
        public static Vocabulary FromTexts(IEnumerable<string> texts, int threshold)
        {
            Vocabulary v = new Vocabulary();
            const int MaxTokens = 1;
            double wordStringCount = 0.0;
            IEnumerable<string> enumerable = texts as string[] ?? texts.ToArray();
            for (int i = 0; i < MaxTokens; i++)
            {
                Vocabulary v1 = v; // prevent modified closure
                wordStringCount +=
                    enumerable.Select(s => WordSequence.FromString(v1, s)).Select(ws => (double)ws.WordStrings.Count).Sum();
                if (i < MaxTokens - 1)
                {
                    v.IncrementMaxTokenCount(threshold);
                }
            }

            double averageWordCountPerText = !enumerable.Any() ? 0.0 : wordStringCount / enumerable.Count();
            double ratio = averageWordCountPerText / v.Words.Count;
            if (threshold > 0)
            {
                v = v.Subvocabulary(w => w.Count >= threshold);
            }

            averageWordCountPerText = ratio * v.Words.Count; // This is an approximation
            v.ReadOnly = true;
            v.AverageWordCountPerText = averageWordCountPerText;
            v.TotalNumberOfTexts = enumerable.Count();
            return v;
        }

        /// <summary>
        /// Clears the vocabulary, so that it contains no words.
        /// </summary>
        public void Clear()
        {
            this.wordsLookup.Clear();
            this.InstanceCount = 0;
            this.AverageWordCountPerText = 0.0;
        }

        /// <summary>
        /// Looks up the Word object for a string containing a word.
        /// If the vocabulary is not read only, a new Word object will be created if necessary.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <returns>The word.</returns>
        public Word Lookup(string str)
        {
            string key = ToNormalizedForm(str);
            Word w;
            bool found = false;
            if (!this.wordsLookup.ContainsKey(key))
            {
                if (this.ReadOnly)
                {
                    return null;
                }

                w = new Word();
            }
            else
            {
                w = this.wordsLookup[key];
                found = true;
            }

            w.LookupVariant(str, this.ReadOnly);
            if (!found)
            {
                this.Add(w);
            }

            if (!this.ReadOnly)
            {
                this.InstanceCount++;
            }

            return w;
        }

        /// <summary>
        /// Takes a stream of word strings and converts it into a stream of Word objects.
        /// </summary>
        /// <param name="wordStrings">The strings</param>
        /// <returns>The words.</returns>
        public IEnumerable<Word> ToWords(IEnumerable<string> wordStrings)
        {
            Word lastWord = null;
            IEnumerator<string> ens = wordStrings.GetEnumerator();
            bool ready = true;
            while (true)
            {
                if (ready)
                {
                    if (!ens.MoveNext())
                    {
                        break;
                    }
                }

                ready = true;
                Word w = this.Lookup(ens.Current);
                if ((lastWord != null) && (!this.ReadOnly))
                {
                    lastWord.NextWordIs(w);
                }

                while ((w != null) && w.HasFixedChildren && (w.Children != null))
                {
                    // See if next word can be absorbed
                    if (!ens.MoveNext())
                    {
                        break;
                    }

                    // there are more words
                    string nextKey = ToNormalizedForm(ens.Current);
                    if (!w.Children.ContainsKey(nextKey))
                    {
                        ready = false;
                        break;
                    }

                    yield return w;
                    string text = w + " " + ens.Current;
                    w = w.Children[nextKey].ChildWord;
                    w.LookupVariant(text, this.ReadOnly);
                }

                lastWord = w;
                yield return w;
            }
        }

        /// <summary>
        /// Uses 'next word' counts associated with the current set of words, to
        /// extend the maximum word length by one token.
        /// </summary>
        /// <param name="minCount">The min count.</param>
        /// <returns>The count</returns>
        public int IncrementMaxTokenCount(int minCount)
        {
            var wordsToAdd = new List<Word>();
            this.InstanceCount = 0;
            foreach (Word w in this.wordsLookup.Values.Where(w => !w.HasFixedChildren))
            {
                w.FixChildren(wordsToAdd, minCount);
            }
            
            foreach (Word w in wordsToAdd)
            {
                this.Add(w);
            }

            return wordsToAdd.Count;
        }

        /// <summary>
        /// Returns a vocabulary containing the subset of the words in this
        /// vocabulary that satisfy the specified condition.
        /// </summary>
        /// <param name="condition">The condition that must hold for a word to be added to the new vocabulary</param>
        /// <returns>The vocabulary</returns>
        public Vocabulary Subvocabulary(Func<Word, bool> condition)
        {
            Vocabulary v = new Vocabulary();
            foreach (Word w in this.wordsLookup.Values.Where(condition))
            {
                v.Add(w);
            }

            return v;
        }
        
        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("Vocabulary[{0} words]", this.Words.Count);
        }
        
        /// <summary>
        /// Converts a string into its normalized form, so it can be looked up.
        /// </summary>
        /// <param name="str">The string</param>
        /// <returns>The normalized form</returns>
        internal static string ToNormalizedForm(string str)
        {
            return str.ToLowerInvariant().Trim();
        }
        
        /// <summary>
        /// Adds a word to the vocabulary
        /// </summary>
        /// <param name="w">The word</param>
        private void Add(Word w)
        {
            string key = ToNormalizedForm(w.MostCommonVariant.Text);
            this.wordsLookup[key] = w;
            this.InstanceCount += w.Count;
        }
    }
}