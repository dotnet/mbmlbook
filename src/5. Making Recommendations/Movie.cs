// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.ML.Probabilistic.Math;

namespace MakingRecommendations
{
    /// <summary>
    /// A MovieLens dataset movie.
    /// </summary>
    public class Movie
    {
        public Movie()
        {
            //empty constructor for serialization
        }

        public Movie(int id, string name, int year, string[] genres)
        {
            Id = id;
            Name = name;
            Year = year;
            Genres = genres;
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public int Year { get; set; }
        public string[] Genres { get; set; }

        /// <summary>
        /// Determines whether the specified <see cref="Object"/> is equal to this item.
        /// </summary>
        /// <param name="obj">The object to compare with this item.</param>
        /// <returns>True if <paramref name="obj"/> is <see cref="Movie"/> and has the same <see cref="Id"/>, false otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return Id.Equals(((Movie)obj).Id);
        }

        /// <summary>
        /// Gets the hash code of this item, which is based entirely on its <see cref="Id"/>.
        /// </summary>
        /// <returns>A value of the hash code.</returns>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return $"{this.Name} ({this.Year})";
        }
    }
}
