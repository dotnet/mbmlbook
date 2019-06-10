// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MakingRecommendations
{
    /// <summary>
    /// A rating given by a specific user to a specific item.
    /// </summary>
    public class RatingTriple
    {
        public RatingTriple()
        {
            //An empty constructor for serialization
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RatingTriple"/> class.
        /// </summary>
        /// <param name="user">Id of a user who gave a rating</param>
        /// <param name="movie">A rated movie</param>
        /// <param name="rating">A rated movie</param>
        public RatingTriple(string user, Movie movie, double rating)
        {
            User = user;
            Movie = movie;
            Rating = rating;
        }

        /// <summary>
        /// Id of a user who gave a rating
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// A rated movie
        /// </summary>
        public Movie Movie { get; set; }

        /// <summary>
        /// Given rating
        /// </summary>
        public double Rating { get; set; }

        public override string ToString()
        {
            return $"{this.User}, {this.Movie}, {this.Rating}";
        }
    }
}
