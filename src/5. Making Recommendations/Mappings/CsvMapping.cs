// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ML.Probabilistic.Learners;
using Microsoft.ML.Probabilistic.Learners.Mappings;
using Microsoft.ML.Probabilistic.Math;

namespace MakingRecommendations.Mappings
{
    /// <summary>
    /// A mapping used to convert source files into <see cref="RatingTriple"/> and get Users, Movies and Ratings from them.
    /// </summary>
    [Serializable]
    class CsvMapping : IStarRatingRecommenderMapping
    <string, RatingTriple, string, Movie, int, NoFeatureSource, Vector>
    {
        private Dictionary<int, Movie> idToMovie;

        /// <summary>
        /// Initializes a new instance of the <see cref="CsvMapping"/> class.
        /// </summary>
        /// <param name="movies">A catalog of movies which used to get movie info and features </param>
        public CsvMapping(IEnumerable<Movie> movies)
        {
            idToMovie = movies.ToDictionary(m => m.Id);
        }

        /// <summary>
        /// Retrieves a list of instances from a given instance source.
        /// </summary>
        /// <param name="instanceSource">The source to retrieve instances from.</param>
        /// <returns>The list of retrieved instances.</returns>
        public IEnumerable<RatingTriple> GetInstances(string instanceSource)
        {
            var ratings = new List<Tuple<string, int, double>>();

            foreach (var line in File.ReadLines(instanceSource))
            {                
                string[] split = line.Split(',');
                //Each row describes a triple 'UserId,ItemId,Rating'.
                ratings.Add(Tuple.Create(split[0], int.Parse(split[1]), double.Parse(split[2])));
            }
            return ratings.Select(t => new RatingTriple(t.Item1, idToMovie[t.Item2], t.Item3));
        }

        /// <summary>
        /// Extracts a user from a given instance.
        /// </summary>
        /// <param name="instanceSource">The parameter is not used.</param>
        /// <param name="instance">The instance to extract user from.</param>
        /// <returns>The extracted user.</returns>
        public string GetUser(string instanceSource, RatingTriple instance)
        { return instance.User; }

        /// <summary>
        /// Extracts an item from a given instance.
        /// </summary>
        /// <param name="instanceSource">The parameter is not used.</param>
        /// <param name="instance">The instance to extract item from.</param>
        /// <returns>The extracted item.</returns>
        public Movie GetItem(string instanceSource, RatingTriple instance)
        { return instance.Movie; }

        /// <summary>
        /// Extracts a rating from a given instance.
        /// </summary>
        /// <param name="instanceSource">The parameter is not used.</param>
        /// <param name="instance">The instance to extract rating from. All ratings are integer. </param>
        /// <returns>The extracted rating.</returns>
        public int GetRating(string instanceSource, RatingTriple instance)
        { return Convert.ToInt32(instance.Rating); }

        /// <summary>
        /// Provides the object describing how ratings provided by the instance source map to stars.
        /// </summary>
        /// <param name="instanceSource">The instance source.</param>
        /// <returns>The object describing how ratings provided by the instance source map to stars.</returns>
        public IStarRatingInfo<int> GetRatingInfo(string instanceSource)
        { return new StarRatingInfo(1, 10); }

        /// <summary>
        /// User features are not used in the MBML Book
        /// </summary>
        public Vector GetUserFeatures(NoFeatureSource featureSource, string user)
        { throw new NotImplementedException(); }

        /// <summary>
        /// Provides a vector of features for a given item.
        /// </summary>
        /// <param name="featureSource">The parameter is not used.</param>
        /// <param name="item">The item to provide features for.</param>
        /// <returns>The feature vector for <paramref name="item"/>.</returns>
        public Vector GetItemFeatures(NoFeatureSource featureSource, Movie item)
        {
            return FeatureProcessor.ProcessFeatures(item.Year, item.Genres);
        }
    }
}
