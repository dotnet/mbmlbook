using Microsoft.ML.Probabilistic.Math;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MakingRecommendations
{
    /// <summary>
    /// The class is used to convert original data which is taken from MovieLens-for-education dataset
    /// into a format understandable for Infer.Net Recommender.
    /// </summary>
    class FeatureProcessor
    {
        /// <summary>
        /// The number of buckets in the movie year feature.
        /// </summary>
        public static readonly int MovieYearBucketCount = 17;

        /// <summary>
        /// The number of buckets in the movie genre feature.
        /// </summary>
        public static readonly int MovieGenreBucketCount = 19;

        /// <summary>
        /// All possible movie genres.
        /// </summary>
        private static readonly string[] MovieGenreNames =
        {
            "Action", "Adventure", "Animation", "Children", "Comedy", "Crime",
            "Documentary", "Drama", "Fantasy", "Film-Noir", "Horror", "IMAX",  "Musical",
            "Mystery", "Romance", "Sci-Fi", "Thriller", "War", "Western"
        };

        /// <summary>
        /// The movie years delimiting year intervals.
        /// </summary>
        private static readonly int[] MovieYears;

        /// <summary>
        /// The mapping between movie genre name and feature bucket.
        /// </summary>
        private static readonly Dictionary<string, int> MovieGenreBuckets;

        /// <summary>
        /// Initializes static members of the <see cref="FeatureProcessor"/> class.
        /// </summary>
        static FeatureProcessor()
        {
            // Movie years
            MovieYears = new int[MovieYearBucketCount];
            int bucket = 0;

            for (int i = 1900; i < 1980; i += 10)
            {
                MovieYears[bucket++] = i;
            }

            for (int i = 1980; i <= 2020; i += 5)
            {
                MovieYears[bucket++] = i;
            }

            // Movie genre buckets
            MovieGenreBuckets = new Dictionary<string, int>();
            for (int i = 0; i < MovieGenreNames.Length; ++i)
            {
                MovieGenreBuckets[MovieGenreNames[i]] = i;
            }
        }

        /// <summary>
        /// Computes the bucket values of the movie year feature.
        ///  Year interpolated between two adjacent buckets.
        /// Buckets chosen based on the plot of the data below to be one in every 10 years until 1980 and one in every 5 years after that.
        /// So 17 in total.
        /// </summary>
        /// <param name="year">The year to be placed into the feature vector.</param>
        /// <returns>The computed feature vector.</returns>
        private static double[] ComputeMovieYear(int year)
        {

            if (year < 1900 || year > 2020)
            {
                throw new ArgumentException("The movie release year should be between 1900 and 2020.");
            }

            var bucket = 0;

            while (year > MovieYears[bucket])
            {
                ++bucket;
            }

            var result = new double[MovieYearBucketCount]; // interpolate
            if (bucket > 0 && MovieYears[bucket] != year)
            {
                // set two values
                double years = MovieYears[bucket] - MovieYears[bucket - 1];

                double rvalue = (year - MovieYears[bucket - 1]) / years;
                double lvalue = 1 - rvalue;

                result[bucket - 1] = lvalue;
                result[bucket] = rvalue;
            }
            else
            {
                // set only one value
                result[bucket] = 1;
            }

            return result;
        }

        /// <summary>
        /// Computes the bucket values of the movie year genre.
        /// A bucket for each of the 19 genres. The value (1) is split across the number of genres a film is assigned. 
        /// </summary>
        /// <param name="genres">The genres of a movies which should be placed into the feature vector. </param>
        /// <returns>The number of feature buckets and the computed feature vector.</returns>
        public static double[] ComputeMovieGenre(string[] genres)
        {
            if (genres.Length < 1 && genres.Length > 3)
            {
                throw new ArgumentException($"Movies should have between 1 and 3 genres; given {genres.Length}.");
            }

            var value = 1.0 / genres.Length;

            var result = new double[MovieGenreBucketCount];
            result[MovieGenreBuckets[genres[0]]] = value;

            for (var i = 1; i < genres.Length; ++i)
            {
                result[MovieGenreBuckets[genres[i].Trim()]] = value;
            }

            return result;
        }

        /// <summary>
        /// Converts year and genres into the bucket vector.
        /// </summary>
        /// <param name="year">Movie year</param>
        /// <param name="genres">Movie genres</param>
        /// <returns></returns>
        public static Vector ProcessFeatures(int year, string[] genres)
        {
            var constantTerm = new[] {1.0};
            var yearBuckets = ComputeMovieYear(year);
            var genreBuckets = ComputeMovieGenre(genres);

            return DenseVector.FromArray(constantTerm.Concat(yearBuckets).Concat(genreBuckets).ToArray());
        }
    }
}
