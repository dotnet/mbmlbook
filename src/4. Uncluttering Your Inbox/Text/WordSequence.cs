// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Text
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A sequence of words which can be used as a parsed representation of a string.
    /// </summary>
    public class WordSequence
    {
        /// <summary>
        /// The word matcher
        /// </summary>
        private static readonly Regex WordMatcher = new Regex(
            @"[\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Lm}\p{Nd}]+([.'][\p{Ll}\p{Lu}\p{Lt}\p{Lo}\p{Lm}\p{Nd}]+)*",
            RegexOptions.Compiled);

        /// <summary>
        /// The text.
        /// </summary>
        private string text = string.Empty;

        /// <summary>
        /// The vocabulary.
        /// </summary>
        private Vocabulary vocabulary;

        /// <summary>
        /// Prevents a default instance of the <see cref="WordSequence"/> class from being created.
        /// </summary>
        private WordSequence()
        {
        }

        /// <summary>
        /// Gets or sets the text used to construct the word sequence.
        /// </summary>
        public string Text
        {
            get
            {
                return this.text;
            }

            set
            {
                this.text = value;
                this.SetFromString(this.text);
            }
        }

        /// <summary>
        /// Gets or sets the vocabulary of words to use when parsing this text.
        /// </summary>
        public Vocabulary Vocabulary
        {
            get { return this.vocabulary; }
            set { this.vocabulary = value; }
        }

        /// <summary>
        /// Gets the words in the text, as a list of strings.
        /// </summary>
        public IList<string> WordStrings
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets the words.
        /// </summary>
        public IList<Word> Words
        {
            get;
            internal set;
        }

        /// <summary>
        /// Word sequence from string.
        /// </summary>
        /// <param name="v">The v.</param>
        /// <param name="s">The s.</param>
        /// <returns>The word sequence</returns>
        public static WordSequence FromString(Vocabulary v, string s)
        {
            return new WordSequence { Vocabulary = v, Text = s };
        }

        /// <summary>
        /// Parses a string to determine the individual words in the string, which
        /// are returned in order as a string array. 
        /// </summary>
        /// <param name="s">The string to parse</param>
        /// <returns>The array of words in the original string</returns>
        public static string[] ParseIntoWordStrings(string s)
        {
            if (s == null)
            {
                return new string[0];
            }

            // normalize apostrophes
            s = s.Replace('’', '\'');
            s = s.Replace('`', '\'');

            // find words
            MatchCollection matches = WordMatcher.Matches(s);
            string[] ws = new string[matches.Count];
            for (int i = 0; i < ws.Length; i++)
            {
                ws[i] = matches[i].Value;
            }

            return ws;
        }

        /// <summary>
        /// Set this instance from a string
        /// </summary>
        /// <param name="s">
        /// The s.
        /// </param>
        public void SetFromString(string s)
        {
            this.WordStrings = ParseIntoWordStrings(s);
            this.Words = this.vocabulary.ToWords(this.WordStrings).ToList();
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("WordSequence[{0} words]", this.Words.Count);
        }
    }
}