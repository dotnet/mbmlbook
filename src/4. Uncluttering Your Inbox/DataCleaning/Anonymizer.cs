// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.DataCleaning
{
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;
    
    /// <summary>
    /// The anonymize.
    /// </summary>
    public enum Anonymize
    {
        /// <summary>
        /// The do not anonymize.
        /// </summary>
        DoNotAnonymize,

        /// <summary>
        /// The anonymize by random names.
        /// </summary>
        AnonymizeByRandomNames,

        /// <summary>
        /// The anonymize by codes.
        /// </summary>
        AnonymizeByCodes
    }

    /// <summary>
    /// Helper class to anonymize data.
    /// </summary>
    public static class Anonymizer
    {
        /// <summary>
        /// Anonymize the identities.
        /// </summary>
        /// <param name="identities">The identities.</param>
        /// <param name="randomNames">The random names.</param>
        /// <param name="nameMapping">The name mapping.</param>
        internal static void AnonymizeIdentities(IList<ContactDetails> identities, IEnumerator<string> randomNames, IDictionary<string, string> nameMapping)
        {
            for (int i = 0; i < identities.Count; i++)
            {
                ContactDetails identity = identities[i];
                if (identity == User.UnknownContactDetails)
                {
                    continue;
                }

                string name = identity.Name.ToString();
                if (!nameMapping.ContainsKey(name))
                {
                    nameMapping[name] = randomNames.Current;
                }

                identity.AnonymizedName = new Uncertain<string> { Value = nameMapping[name], Probability = 1.0 };

                identity.AnonymizedEmail = new Uncertain<string>
                        {
                            Value = nameMapping[name].ToLower().Replace(".", string.Empty).Replace(' ', '.') + i + "@example.com",
                            Probability = 1.0
                        };
            }
        }

        /// <summary>
        /// Convert words to x.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>The <see cref="string"/></returns>
        internal static string WordsToX(string text)
        {
            text = Regex.Replace(text, "[a-z]", "x");
            text = Regex.Replace(text, "[A-Z]", "X");
            return Regex.Replace(text, "[0-9]", "x");
        }

        /// <summary>
        /// Calculates the MD5 hash.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The hash as a <see cref="string"/></returns>
        internal static string CalculateMd5Hash(string name)
        {
            MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(name);
            byte[] hash = md5.ComputeHash(inputBytes);

            StringBuilder sb = new StringBuilder();
            foreach (byte b in hash)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }
    }
}