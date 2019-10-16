// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using Microsoft.ML.Probabilistic.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace HarnessingTheCrowd
{
    public static class Tokenizer
    {
        [Flags]
        public enum TokenizationOptions
        {
            None = 0x00,
            StripHtml = 0x01,
            StripNumbers = 0x02,
            StripUrls = 0x04,
            StripEmailAddresses = 0x08,
            StripUsernames = 0x10,
            StripMonetary = 0x20,
            All = 0xff
        }

        public const string PunctuationCharacters = " ^@$/#.-:&*+=[]?!(){},''\">_<;%\\";
        public const string PunctuationSpaceOnly = " ";

        /// <summary>
        /// Gets the tokens the has been preprocessed.
        /// </summary>
        /// <param name="doc">
        /// The document.
        /// </param>
        /// <returns>
        /// The  tokens in the document.
        /// </returns>
        public static List<string> GetTokensFromPreProcessedDoc(string doc)
        {
            return Tokenize(doc.ToLower(), TokenizationOptions.None, PunctuationSpaceOnly).ToList();
        }

        /// <summary>
        /// Tokenizes a string, returning its list of words.
        /// </summary>
        /// <param name="text">The document.</param>
        /// <param name="options">The tokenization options.</param>
        /// <param name="punctuationCharacters">The characters considered as punctuation.</param>
        /// <returns>The tokens.</returns>
        public static string[] Tokenize(
            string text,
            TokenizationOptions options = TokenizationOptions.All,
            string punctuationCharacters = PunctuationCharacters)
        {
            var left = "⋘";
            var right = "⋙";

            if ((options & TokenizationOptions.StripHtml) != 0)
            {
                text = Regex.Replace(text, "<[^<>]+>", string.Empty);
            }

            if ((options & TokenizationOptions.StripNumbers) != 0)
            {
                text = Regex.Replace(text, "[0-9]+", left + "number" + right);
            }

            if ((options & TokenizationOptions.StripUrls) != 0)
            {
                text = Regex.Replace(text, @"(http|https)://[^\s]*", left + "httpaddr" + right);
            }

            if ((options & TokenizationOptions.StripEmailAddresses) != 0)
            {
                text = Regex.Replace(text, @"[^\s]+@[^\s]+", left + "emailaddr" + right);
            }

            if ((options & TokenizationOptions.StripMonetary) != 0)
            {
                text = Regex.Replace(text, "[$]+", left + "dollar" + right);
            }

            if ((options & TokenizationOptions.StripUsernames) != 0)
            {
                text = Regex.Replace(text, @"@[^\s]+", left + "username" + right);
            }

            // Tokenize and also get rid of any punctuation
            var tokens = text.Split(punctuationCharacters.ToCharArray()).Select(
                token =>
                {
                    var result = token.Replace(left, "{");
                    result = result.Replace(right, "}");
                    return result;
                }).Where(str => !string.IsNullOrWhiteSpace(str)).ToArray();

            return tokens;
        }
    }
}
