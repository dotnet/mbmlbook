// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.Probabilistic.Learners;
using Microsoft.ML.Probabilistic.Learners.Mappings;
using Microsoft.ML.Probabilistic.Math;

namespace MakingRecommendations.Mappings
{
    /// <summary>
    /// A decorator for <see cref="IStarRatingRecommenderMapping{TInstanceSource,TInstance,TUser,TItem,TRating,TFeatureSource,TFeatureValues}"/>
    /// which converts 10-star ratings to binary (like/dislike) corresponding to ratings 1 and 2.
    /// In doing so, it provides exactly the same order of instances as in the decorated mapping.
    /// </summary>
    /// <typeparam name="TInstanceSource">The type of a source of instances.</typeparam>
    /// <typeparam name="TItem">The type of an item.</typeparam>
    [Serializable]
    public class RatingBinarizingMapping<TInstanceSource, TItem> : IStarRatingRecommenderMapping
        <TInstanceSource, RatingTriple, string, TItem, int, NoFeatureSource, Vector>
    {
        /// <summary>
        /// A decorated mapping.
        /// </summary>
        private readonly IStarRatingRecommenderMapping<TInstanceSource, RatingTriple, string, TItem, int, NoFeatureSource, Vector> mapping;

        /// <summary>
        /// A mapping constructor
        /// </summary>
        /// <param name="mapping">A 10-star rating which will be converted to like/dislike rating mapping. </param>
        public RatingBinarizingMapping(IStarRatingRecommenderMapping<TInstanceSource, RatingTriple, string, TItem, int, NoFeatureSource, Vector> mapping)
        {
            this.mapping = mapping;
        }

        /// <summary>
        /// A decorator takes all instances via decorated mapping.
        /// It considers that all ratings more than 5 mean "like". 
        /// </summary>
        public IEnumerable<RatingTriple> GetInstances(TInstanceSource instanceSource)
        {
            var ratingInfo = mapping.GetRatingInfo(instanceSource);

            //It is required that rating should be 10-star.
            if ((ratingInfo.MinStarRating != 1) || (ratingInfo.MaxStarRating != 10))
            {
                throw new Exception("Only 10-star ratings are supported.");
            }

            var instances = mapping.GetInstances(instanceSource);

            return instances.Select(instance =>
                        new RatingTriple(instance.User, instance.Movie, instance.Rating < 6 ? 1 : 2)
                   );           
        }

        /// <summary>
        /// Extracts a user from a given instance.
        /// </summary>
        /// <param name="instanceSource">The parameter is not used.</param>
        /// <param name="instance">The instance to extract user from.</param>
        /// <returns>The extracted user.</returns>
        public string GetUser(TInstanceSource instanceSource, RatingTriple instance)
        { return mapping.GetUser(instanceSource, instance); }

        /// <summary>
        /// Extracts an item from a given instance.
        /// </summary>
        /// <param name="instanceSource">The parameter is not used.</param>
        /// <param name="instance">The instance to extract item from.</param>
        /// <returns>The extracted item.</returns>
        public TItem GetItem(TInstanceSource instanceSource, RatingTriple instance)
        { return mapping.GetItem(instanceSource, instance); }

        /// <summary>
        /// Extracts a rating from a given instance.
        /// </summary>
        /// <param name="instanceSource">The parameter is not used.</param>
        /// <param name="instance">The instance to extract rating from. All ratings are integer. </param>
        /// <returns>The extracted rating.</returns>
        public int GetRating(TInstanceSource instanceSource, RatingTriple instance)
        { return mapping.GetRating(instanceSource, instance); }

        /// <summary>
        /// Provides the object describing how ratings provided by the instance source map to stars.
        /// </summary>
        /// <param name="instanceSource">The instance source.</param>
        /// <returns>The object describing how ratings provided by the instance source map to stars.</returns>
        public IStarRatingInfo<int> GetRatingInfo(TInstanceSource instanceSource)
        { return new StarRatingInfo(1, 2); }

        /// <summary>
        /// User features are not used in the MBML Book
        /// </summary>
        public Vector GetUserFeatures(NoFeatureSource featureSource, string user)
        { return mapping.GetUserFeatures(featureSource, user); }

        /// <summary>
        /// Provides a vector of features for a given item.
        /// </summary>
        /// <param name="featureSource">The parameter is not used.</param>
        /// <param name="item">The item to provide features for.</param>
        /// <returns>The feature vector for <paramref name="item"/>.</returns>
        public Vector GetItemFeatures(NoFeatureSource featureSource, TItem item)
        { return mapping.GetItemFeatures(featureSource, item); }

    }
}
