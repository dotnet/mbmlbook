// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.DataCleaning
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The sequential name generator.
    /// </summary>
    public class HashCodeNameGenerator
    {
        /// <summary>
        /// The format
        /// </summary>
        private readonly string format;

        /// <summary>
        /// The hash dictionary - to check for collisions.
        /// </summary>
        private readonly Dictionary<string, string> hashDict = new Dictionary<string, string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="HashCodeNameGenerator" /> class.
        /// </summary>
        /// <param name="format">The format.</param>
        public HashCodeNameGenerator(string format)
        {
            this.format = format;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>The <see cref="string"/>.</returns>
        public string GetValue(string id)
        {
            int code = Math.Abs(id.GetHashCode()) % 0xFFFFFFF;

            string hash = string.Format(this.format, code.ToString("X"));

            if (this.hashDict.ContainsKey(hash))
            {
                if (this.hashDict[hash] != id)
                {
                    Console.WriteLine(@"Hash collision for {0} between {1} and {2}", hash, this.hashDict[hash], id);
                }
            }
            else
            {
                this.hashDict[hash] = id;
            }

            return hash;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="identity">The identity.</param>
        /// <returns>The <see cref="string"/>.</returns>
        public string GetValue(ContactDetails identity)
        {
            string email = identity.Email.Value ?? identity.Name.Value;

            int code = Math.Abs(email.GetHashCode()) % 0xFFFFFFF;

            string hash = string.Format(this.format, code.ToString("X"));

            if (this.hashDict.ContainsKey(hash))
            {
                if (this.hashDict[hash] != email)
                {
                    Console.WriteLine(@"Hash collision for {0} between {1} and {2}", hash, this.hashDict[hash], email);
                }
            }
            else
            {
                this.hashDict[hash] = email;
            }

            return hash;
        }
    }
}