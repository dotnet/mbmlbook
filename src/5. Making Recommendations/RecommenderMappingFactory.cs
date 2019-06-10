// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MakingRecommendations.Mappings;
using Microsoft.ML.Probabilistic.Learners;
using Microsoft.ML.Probabilistic.Learners.Mappings;
using Microsoft.ML.Probabilistic.Math;

namespace MakingRecommendations
{
    public class RecommenderMappingFactory
    {
        private readonly CsvMapping csvMapping;

        /// <summary>
        /// Initializes a new instance of the <see cref="RecommenderMappingFactory"/> class.
        /// </summary>
        /// <param name="movies"> A movies catalog which is used to get movie features </param>
        public RecommenderMappingFactory(IEnumerable<Movie> movies)
        {
            csvMapping = new CsvMapping(movies);
        }

        /// <summary>
        /// Converts a 10-star rating mapping to a binary (like/dislike) one. It allows to save order of mapping output.
        /// </summary>
        /// <param name="mapping"></param>
        /// <returns></returns>
        public RatingBinarizingMapping<SplitInstanceSource<string>, Movie> BinarizeMapping(IStarRatingRecommenderMapping<SplitInstanceSource<string>, RatingTriple, string, Movie, int, NoFeatureSource, Vector> mapping)
        {
            return new RatingBinarizingMapping<SplitInstanceSource<string>, Movie>(mapping);
        }

        /// <summary>
        /// Creates a new instance of binary (like/dislike) rating mapping which splits data between training and test sets.
        /// </summary>
        /// <param name="removeOccasionalColdItems"> While it is true a mapping removes all cold items from a test set. </param>
        /// <returns>A recommender mapping instance</returns>
        public RatingBinarizingMapping<SplitInstanceSource<string>, Movie> GetBinaryMapping(bool removeOccasionalColdItems)
        {
            return new RatingBinarizingMapping<SplitInstanceSource<string>, Movie>(GetStarsMapping(removeOccasionalColdItems));
        }

        /// <summary>
        /// Creates a new instance of 10-star rating mapping which splits data between training and test sets.
        /// </summary>
        /// <param name="removeOccasionalColdItems"> While it is true a mapping removes all cold items from a test set. </param>
        /// <returns>A recommender mapping instance</returns>
        public TrainTestSplittingStarRatingRecommenderMapping<string, RatingTriple, string, Movie, int, NoFeatureSource, Vector> GetStarsMapping(bool removeOccasionalColdItems)
        {
            return csvMapping.SplitToTrainTest(
                    trainingOnlyUserFraction: 0.0,
                    testUserRatingTrainingFraction: 0.7,
                    coldUserFraction: 0,
                    coldItemFraction: 0,
                    ignoredUserFraction: 0,
                    ignoredItemFraction: 0,
                    removeOccasionalColdItems: removeOccasionalColdItems);
        }
    }
}
