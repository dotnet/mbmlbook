// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.ML.Probabilistic.Distributions;
using Microsoft.ML.Probabilistic.Learners;
using Microsoft.ML.Probabilistic.Learners.Mappings;
using Microsoft.ML.Probabilistic.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MakingRecommendations
{
    class ModelRunner
    {
        /// <summary>
        /// Gets the path of the ratings.csv.
        /// </summary>
        public string RatingsPath { get; set; }

        /// <summary>
        /// Recommender should take items in the same order each time.
        /// This is archived by restarting a random number generator. 
        /// </summary>
        private static int RandomSeed { get; } = 1984;

        /// <summary>
        /// This number is used  to switch off certain parameters of the model.
        /// This is achieved by setting their prior variance to zero.
        /// However, zero is not used in order to avoid numerical instabilities in inference.
        /// </summary>
        private static double EpsilonPriorVariance { get; } = 1E-200;

        /// <summary>
        /// An instance of factory producing recommender mappings.
        /// </summary>
        public RecommenderMappingFactory RecommenderMappingFactory { get; }

        /// <summary>
        /// Number of inference iterations that recommender will perform.
        /// </summary>
        public int IterationCount { get; set; } = 200;

        public ModelRunner(RecommenderMappingFactory recommenderMappingFactory, string ratingsPath)
        {
            RatingsPath = ratingsPath;
            RecommenderMappingFactory = recommenderMappingFactory;
        }

        /// <summary>
        /// Creates a new default instance of Matchbox recommender 
        /// </summary>
        /// <param name="dataMapping">A mapping to convert raw data to a format clear to the program </param>
        /// <param name="traitCount">Number of item traits </param>
        /// <returns>An instance of a recommender</returns>
        public IMatchboxRecommender<SplitInstanceSource<string>, string, Movie, IDictionary<int, double>, NoFeatureSource>
            GetRecommender(
            IStarRatingRecommenderMapping<SplitInstanceSource<string>, RatingTriple, string, Movie, int, NoFeatureSource, Vector> dataMapping,
            int traitCount)
        {
            // In this chapter Infer.NET Learner is used to make recommendations
            // You can find its source code here:
            // https://github.com/dotnet/infer/tree/master/src/Learners/Recommender
            // This recommender has a model that matches the one described in the book.
            // Its source code is situated here:
            // https://github.com/dotnet/infer/blob/master/src/Learners/RecommenderModels/Models.cs
            var recommender = MatchboxRecommender.Create(dataMapping);

            if (traitCount == 0)
            {
                //workaround to make recommender work with 0 traits
                var training = recommender.Settings.Training;
                var countField = training.GetType().GetField("traitCount", BindingFlags.NonPublic | BindingFlags.Instance);
                countField.SetValue(training, 0);
            }
            else
            {
                recommender.Settings.Training.TraitCount = traitCount;
            }
            // This value allows to normalize the sum of trait affinity variances to 1.0. 
            var traitVariance = 1.0 / Math.Sqrt(traitCount);

            recommender.Settings.Training.IterationCount = IterationCount;
            recommender.Settings.Training.BatchCount = 1;
            recommender.Settings.Training.UseUserFeatures = false;
            recommender.Settings.Training.UseItemFeatures = false;
            recommender.Settings.Training.Advanced.AffinityNoiseVariance = 1.0;
            recommender.Settings.Training.Advanced.UserThresholdNoiseVariance = EpsilonPriorVariance;
            recommender.Settings.Training.Advanced.ItemTraitVariance = traitVariance;
            recommender.Settings.Training.Advanced.ItemBiasVariance = 1.0;
            recommender.Settings.Training.Advanced.UserTraitVariance = traitVariance;
            recommender.Settings.Training.Advanced.UserBiasVariance = 1.0;

            recommender.Settings.Prediction.SetPredictionLossFunction(LossFunction.ZeroOne);

            return recommender;
        }

        /// <summary>
        /// Extracts like probability from predictions dictionary 
        /// </summary>
        /// <param name="predictions">Distribution of ratings given for each movie by each user. </param>
        /// <returns>Array of probabilities. The first dimension represents users, the second dimension represents movies. </returns>
        private static double[][] GetLikeProbability(
            IDictionary<string, IDictionary<Movie, IDictionary<int, double>>> predictions)
        {
            var allPredictions = 
                predictions.Select(userWithPredictionList =>
                    userWithPredictionList.Value.Select(itemPrediction =>
                        {
                            var prediction = itemPrediction.Value;
                            var likeProbability = prediction[2];
                            return likeProbability;
                        }
                    )
            );
                        
            return GetJaggedDoubles(allPredictions);
        }

        /// <summary>
        /// Predictions based on like/dislike input data
        /// </summary>
        /// <param name="traitsCounts"> Number of item traits </param>
        /// <returns>A tuple of probability of like and metrics </returns>
        public (Dictionary<string, double[][]> likeProbability, MetricValues metricValues) PredictionsOnBinaryData(
            IList<int> traitsCounts
        )
        {
            var starRatingTrainTestSplittingMapping = RecommenderMappingFactory.GetStarsMapping(true);
            var binaryRatingTrainTestSplittingMapping = RecommenderMappingFactory.BinarizeMapping(starRatingTrainTestSplittingMapping);

            var trainSource = SplitInstanceSource.Training(RatingsPath);
            var testSource = SplitInstanceSource.Test(RatingsPath);

            var binaryRatingEvaluator  = new RecommenderEvaluator<SplitInstanceSource<string>, string, Movie, int, int, IDictionary<int, double>>(binaryRatingTrainTestSplittingMapping.ForEvaluation());
            var starsRatingEvaluator  = new RecommenderEvaluator<SplitInstanceSource<string>, string, Movie, int, int, IDictionary<int, double>>(starRatingTrainTestSplittingMapping.ForEvaluation());

            var correctFractions = new Dictionary<string, double>();
            var ndcgs = new Dictionary<string, double>();
            var likeProbability = new Dictionary<string, double[][]>();

            foreach (var traitCount in traitsCounts)
            {
                Console.WriteLine($"Running metrics calculation for binarized data and a model with {traitCount} traits.");

                Rand.Restart(RandomSeed);

                var recommender = GetRecommender(binaryRatingTrainTestSplittingMapping, traitCount);

                recommender.Settings.Training.Advanced.UserThresholdPriorVariance = EpsilonPriorVariance;

                recommender.Train(trainSource);

                var predictions = recommender.Predict(testSource);

                likeProbability.Add(traitCount.ToString(), GetLikeProbability(recommender.PredictDistribution(testSource)));

                var correctFraction = 1.0 - binaryRatingEvaluator.RatingPredictionMetric(testSource, predictions, Metrics.ZeroOneError);
                correctFractions.Add(traitCount.ToString(), correctFraction);

                var itemRecommendationsForEvaluation = starsRatingEvaluator.RecommendRatedItems(recommender, testSource, 5, 5);
                var ndcg = starsRatingEvaluator.ItemRecommendationMetric(testSource, itemRecommendationsForEvaluation, Metrics.Ndcg);
                ndcgs.Add(traitCount.ToString(), ndcg);
            }

            return (likeProbability, new MetricValues(correctFractions, ndcgs)); 
        }

        /// <summary>
        /// Takes test data from data source and represents it in the form of jagged array.
        /// The first dimension represents users, the second dimension represents movies.
        /// </summary>
        /// <param name="mapping">A mapping to convert ratings to a scale used exactly in the current experiment. </param>
        /// <returns></returns>
        public double[][] GetGroundTruth(
            IStarRatingRecommenderMapping<SplitInstanceSource<string>, RatingTriple, string, Movie, int, NoFeatureSource, Vector> mapping
            )
        {
            Rand.Restart(RandomSeed);

            var testSource = SplitInstanceSource.Test(RatingsPath);

            var mappingForEvaluation = mapping.ForEvaluation();
            var users = mappingForEvaluation.GetUsers(testSource);
            var ratings = users.Select(u =>
                mappingForEvaluation.GetItemsRatedByUser(testSource, u)
                    .Select(m => (double)mappingForEvaluation.GetRating(testSource, u, m)));
            var groundTruthArray = GetJaggedDoubles(ratings);

            return groundTruthArray;
        }

        /// <summary>
        /// Converts distribution of 10-star ratings given to movies by users into binary (like/dislike) form
        /// </summary>
        /// <param name="distributions">Original 10-star rating distributions.
        /// Each internal dictionary keeps probability of one or the other rating given for a movie by a user.</param>
        /// <returns>A dictionary of dictionaries containing user ratings.
        /// 1 means "dislike", 2 means "like".</returns>
        public static Dictionary<string, IDictionary<Movie, int>> 
            BinarizePredictions(IDictionary<string, IDictionary<Movie, IDictionary<int, double>>> distributions)
        {
            var binarizedPredictions = new Dictionary<string, IDictionary<Movie, int>>();

            foreach (var user in distributions)
            {
                var moviesRatings = new Dictionary<Movie, int>();
                foreach (var movie in user.Value)
                {
                    var dislikeProbability = 0.0;
                    var likeProbability = 0.0;

                    foreach (var probability in movie.Value)
                    {
                        if (probability.Key < 6)
                        {
                            dislikeProbability += probability.Value;
                        }
                        else
                        {
                            likeProbability += probability.Value;
                        }
                    }

                    moviesRatings.Add(movie.Key, (dislikeProbability > likeProbability) ? 1 : 2);
                }
                binarizedPredictions.Add(user.Key, moviesRatings);
            }

            return binarizedPredictions;
        }

        /// <summary>
        /// Removes first and last infinity thresholds, converts a list to a dictionary and makes point masses more visible.
        /// </summary>
        /// <param name="posteriorDistribution">Original list of posterior distributions</param>
        /// <returns>A beautified dictionary of distributions</returns>
        public static IDictionary<string, Gaussian> BeautifyPosteriorDistribution(List<Gaussian> posteriorDistribution)
        {
            var distributionsDict = 
                posteriorDistribution.GetRange(1, posteriorDistribution.Count - 2)
                    .Select((s, i) => new { s, i }).ToDictionary(x =>
                    {
                        var stars = (x.i / 2.0) + 1;
                        return stars.ToString() + " " + "star" + (stars.Equals(1) ? "" : "s");
                    }, x =>
                    {
                        // Point mass cannot be visualized that's why make it a little bit "thicker".
                        if (x.s.IsPointMass)
                        {
                            x.s.SetMeanAndPrecision(x.s.Point, 100000);
                        }
                        return x.s;
                    });
            return distributionsDict;
        }

        /// <summary>
        /// Predictions based on 10-star rating input data
        /// </summary>
        /// <param name="traitsCounts"> Number of item traits </param>
        /// <returns>A tuple of probability of thresholds posterior distributions, most probable ratings and metrics </returns>
        public (Dictionary<string, IDictionary<string, Gaussian>> posteriorDistributionsOfThresholds, Dictionary<string, double[][]> mostProbableRatings, MetricValues metricValues) PredictionsOnStarRatings(
                IList<int> traitsCounts
            )
        {
            var starRatingTrainTestSplittingMapping = RecommenderMappingFactory.GetStarsMapping(true);
            var binaryRatingTrainTestSplittingMapping = RecommenderMappingFactory.BinarizeMapping(starRatingTrainTestSplittingMapping);

            var trainSource = SplitInstanceSource.Training(RatingsPath);
            var testSource = SplitInstanceSource.Test(RatingsPath);

            var binaryRatingEvaluator  = new RecommenderEvaluator<SplitInstanceSource<string>, string, Movie, int, int, IDictionary<int, double>>(binaryRatingTrainTestSplittingMapping.ForEvaluation());
            var starsRatingEvaluator  = new RecommenderEvaluator<SplitInstanceSource<string>, string, Movie, int, int, IDictionary<int, double>>(starRatingTrainTestSplittingMapping.ForEvaluation());

            var correctFractions = new Dictionary<string, double>();
            var ndcgs = new Dictionary<string, double>();
            var maes = new Dictionary<string, double>();

            var mostProbableRatings = new Dictionary<string, double[][]>();
            var posteriorDistributionsOfThresholds = new Dictionary<string, IDictionary<string, Gaussian>>();

            foreach (var traitCount in traitsCounts)
            {
                Console.WriteLine($"Running metrics calculation for 10-star data and a model with {traitCount} traits.");

                Rand.Restart(RandomSeed);

                var recommender = GetRecommender(starRatingTrainTestSplittingMapping, traitCount);

                recommender.Settings.Training.UseSharedUserThresholds = true;
                recommender.Settings.Training.Advanced.UserThresholdPriorVariance = 10;

                recommender.Train(trainSource);

                var distributions = recommender.PredictDistribution(testSource);

                var predictions = recommender.Predict(testSource);

                mostProbableRatings.Add(traitCount.ToString(),
                    GetJaggedDoubles(predictions.Select(userRating =>
                        userRating.Value.Select(movieRating => (double) movieRating.Value))));

                var posteriorDistributionOfThresholds = recommender.GetPosteriorDistributions().Users.First().Value.Thresholds.ToList();
                var posteriorDistributionOfThresholdsDict = BeautifyPosteriorDistribution(posteriorDistributionOfThresholds);

                posteriorDistributionsOfThresholds.Add(traitCount.ToString(), posteriorDistributionOfThresholdsDict);

                var binarizedPredictions = BinarizePredictions(distributions);

                var correctFraction = 1.0 - binaryRatingEvaluator.RatingPredictionMetric(testSource, binarizedPredictions, Metrics.ZeroOneError);
                correctFractions.Add(traitCount.ToString(), correctFraction);

                var itemRecommendationsForEvaluation = starsRatingEvaluator.RecommendRatedItems(recommender, testSource, 5, 5);
                var ndcg = starsRatingEvaluator.ItemRecommendationMetric(testSource, itemRecommendationsForEvaluation, Metrics.Ndcg);
                ndcgs.Add(traitCount.ToString(), ndcg);
                var mae = starsRatingEvaluator.RatingPredictionMetric(testSource, predictions, Metrics.AbsoluteError);
                //Divide maes by 2 to convert 10-star rating to 5-star rating
                maes.Add(traitCount.ToString(), mae / 2.0);
            }

            return (posteriorDistributionsOfThresholds, mostProbableRatings,
                new MetricValues(correctFractions, ndcgs, maes));
        }

        /// <summary>
        /// Predictions based on 10-star rating input data
        /// </summary>
        /// <returns>A tuple of probability of thresholds posterior distributions, most probable ratings and metrics </returns>
        public Dictionary<string, double> GetRatingsNumToMaeOnStarsPredictions()
        {
            var starRatingTrainTestSplittingMapping = RecommenderMappingFactory.GetStarsMapping(false);

            var trainSource = SplitInstanceSource.Training(RatingsPath);
            var testSource = SplitInstanceSource.Test(RatingsPath);

            Console.WriteLine(
                $"Calculation of mean absolute error for movies with different numbers of ratings in the training set, for a model with 16 traits.");

            Rand.Restart(RandomSeed);

            var recommender = GetRecommender(starRatingTrainTestSplittingMapping, 16);

            recommender.Settings.Training.UseSharedUserThresholds = true;
            recommender.Settings.Training.Advanced.UserThresholdPriorVariance = 10;

            recommender.Train(trainSource);

            var distributions = recommender.PredictDistribution(testSource);

            var predictionError = PredictionError(testSource, starRatingTrainTestSplittingMapping, distributions);
            var ratingsNumToMae = CreateItemPopularityPredictions(trainSource, starRatingTrainTestSplittingMapping, predictionError);

            return ratingsNumToMae;
        }

        /// <summary>
        /// Predictions based on 10-star rating input data and features
        /// </summary>
        /// <param name="traitsCounts"> Number of item traits </param>
        /// <returns> Metrics </returns>
        public MetricValues PredictionsOnDataWithFeatures(IList<int> traitsCounts)
        {
            var starRatingTrainTestSplittingMapping = RecommenderMappingFactory.GetStarsMapping(true);
            var binaryRatingTrainTestSplittingMapping = RecommenderMappingFactory.BinarizeMapping(starRatingTrainTestSplittingMapping);

            var trainSource = SplitInstanceSource.Training(RatingsPath);
            var testSource = SplitInstanceSource.Test(RatingsPath);

            var binaryRatingEvaluator  = new RecommenderEvaluator<SplitInstanceSource<string>, string, Movie, int, int, IDictionary<int, double>>(binaryRatingTrainTestSplittingMapping.ForEvaluation());
            var starsRatingEvaluator  = new RecommenderEvaluator<SplitInstanceSource<string>, string, Movie, int, int, IDictionary<int, double>>(starRatingTrainTestSplittingMapping.ForEvaluation());

            var correctFractions = new Dictionary<string, double>();
            var ndcgs = new Dictionary<string, double>();
            var maes = new Dictionary<string, double>();

            foreach (var traitCount in traitsCounts)
            {
                Console.WriteLine($"Running metrics calculation for data with features and a model with {traitCount} traits.");

                Rand.Restart(RandomSeed);

                var recommender = GetRecommender(starRatingTrainTestSplittingMapping, traitCount);

                recommender.Settings.Training.UseItemFeatures = true;
                recommender.Settings.Training.UseSharedUserThresholds = true;
                recommender.Settings.Training.Advanced.UserThresholdPriorVariance = 10;

                recommender.Train(trainSource);

                var distribution = recommender.PredictDistribution(testSource);

                var binarizedPredictions = BinarizePredictions(distribution);

                var predictions = recommender.Predict(testSource);

                var correctFraction = 1.0 - binaryRatingEvaluator.RatingPredictionMetric(testSource, binarizedPredictions, Metrics.ZeroOneError);
                correctFractions.Add(traitCount.ToString(), correctFraction);

                var itemRecommendationsForEvaluation = starsRatingEvaluator.RecommendRatedItems(recommender, testSource, 5, 5);
                var ndcg = starsRatingEvaluator.ItemRecommendationMetric(testSource, itemRecommendationsForEvaluation, Metrics.Ndcg);
                ndcgs.Add(traitCount.ToString(), ndcg);
                var mae = starsRatingEvaluator.RatingPredictionMetric(testSource, predictions, Metrics.AbsoluteError);
                //Divide maes by 2 to convert 10-star rating to 5-star rating
                maes.Add(traitCount.ToString(), mae / 2.0);
            }

            return new MetricValues(correctFractions, ndcgs, maes);
        }

        /// <summary>
        /// Calculates MAE on 10-star rating input data with feature info
        /// </summary>
        /// <returns> MAE of movies grouped by number of ratings given for them </returns>
        public Dictionary<string, double> GetRatingsToMaeOnFeaturePredictions()
        {
            var starRatingTrainTestSplittingMapping = RecommenderMappingFactory.GetStarsMapping(false);

            var trainSource = SplitInstanceSource.Training(RatingsPath);
            var testSource = SplitInstanceSource.Test(RatingsPath);

            Console.WriteLine($"Calculation of mean absolute error for movies with different numbers of ratings in the training set for data with feature info.");

            Rand.Restart(RandomSeed);

            var recommender = GetRecommender(starRatingTrainTestSplittingMapping, 16);

            recommender.Settings.Training.UseItemFeatures = true;
            recommender.Settings.Training.UseSharedUserThresholds = true;
            recommender.Settings.Training.Advanced.UserThresholdPriorVariance = 10;

            recommender.Train(trainSource);

            var distribution = recommender.PredictDistribution(testSource);

            var predictionError = PredictionError(testSource, starRatingTrainTestSplittingMapping, distribution);
            var ratingsNumToMae = CreateItemPopularityPredictions(trainSource, starRatingTrainTestSplittingMapping, predictionError);

            return ratingsNumToMae;
        }

        /// <summary>
        /// Calculates absolute error between prediction and ground truth
        /// </summary>
        /// <param name="testSource">A source of ratings made by users</param>
        /// <param name="mapping">A mapping to convert source ratings into experiment specific ones</param>
        /// <param name="predictions">Ratings which are predicted by the recommender</param>
        /// <returns></returns>
        private static Dictionary<Movie, List<double>> PredictionError(
            SplitInstanceSource<string> testSource,
            IStarRatingRecommenderMapping<SplitInstanceSource<string>, RatingTriple, string, Movie, int, NoFeatureSource, Vector> mapping,
            IDictionary<string, IDictionary<Movie, IDictionary<int, double>>> predictions)
        {
            var evMapping = mapping.ForEvaluation();

            var allErrors = new Dictionary<Movie, List<double>>();

            foreach (var userWithPredictionList in predictions)
            {
                foreach (var itemPrediction in userWithPredictionList.Value)
                {
                    var prediction = itemPrediction.Value;
                    var groundTruth = evMapping.GetRating(testSource, userWithPredictionList.Key, itemPrediction.Key);
                    var predictedRating = prediction.Aggregate((l, r) => l.Value > r.Value ? l : r).Key; 
                    var error = Math.Abs(groundTruth - predictedRating);
                    if (allErrors.TryGetValue(itemPrediction.Key, out var values))
                    {
                        values.Add(error);
                    }
                    else
                    {
                        allErrors.Add(itemPrediction.Key, new List<double>() { error });
                    }
                }
            }

            return allErrors;
        }

        /// <summary>
        /// Converts enumerable of enumerables of double into jagged array of doubles.
        /// </summary>
        /// <param name="list"></param>
        /// <returns>Jagged array of doubles </returns>
        private static double[][] GetJaggedDoubles(IEnumerable<IEnumerable<double>> list)
        {
            const int UserCount = 25;
            const int MaxItemCount = 35;
            const int MinItemCount = 6;
            return list
                .Where(x => x.Count() >= MinItemCount)
                .Take(UserCount)
                .Select(x => x.Take(MaxItemCount).ToArray())
                .ToArray();
        }


        #region ItemPopularityPredictions

        /// <summary>
        /// Calculates MAE of items placed to different buckets according to Number ofrating given to them. 
        /// </summary>
        /// <param name="trainSource">An instance source</param>
        /// <param name="mapping">Mapping which converts input data to RatingTriples</param>
        /// <param name="predictionErrors">Absolute errors of predictions</param>
        /// <returns></returns>
        private static Dictionary<string, double> CreateItemPopularityPredictions(
            SplitInstanceSource<string> trainSource,
            IRecommenderMapping<SplitInstanceSource<string>, RatingTriple, string, Movie, NoFeatureSource, Vector> mapping,
            Dictionary<Movie, List<double>> predictionErrors)
        {
            Rand.Restart(RandomSeed);

            const int BucketCount = 4;

            var trainingSetCounts = mapping.GetInstances(trainSource)
                .GroupBy(i => i.Movie)
                .ToDictionary(x => x.Key, x => x.Count());

            var orderedItems = predictionErrors
                .Keys
                .Select(x => new { Item = x, Count = GetItemPopularityCount(x, trainingSetCounts) })
                .OrderBy(x => x.Count)
                .Select(x => x.Item)
                .ToArray();

            var result = new Dictionary<string, double>();

            for (var bucket = 0; bucket < BucketCount; ++bucket)
            {
                var items = GetFixedItems(bucket, orderedItems, trainingSetCounts);
                var bucketName = GetBucketName(items, trainingSetCounts);

                var mae = ComputeMae(items, predictionErrors) / 2.0;
                result.Add(bucketName, mae);
            }

            return result;
        }

        /// <summary>
        /// Gets items placed to the one or the other bucket 
        /// </summary>
        /// <typeparam name="T">A type of items</typeparam>
        /// <param name="bucket">Number of bucket</param>
        /// <param name="items">All items</param>
        /// <param name="counts">Number of item ratings</param>
        /// <returns></returns>
        private static IList<T> GetFixedItems<T>(int bucket, T[] items, IDictionary<T, int> counts)
        {
            switch (bucket)
            {
                case 0: return items.Where(i => GetItemPopularityCount(i, counts) == 0).ToList();
                case 1: return items.Where(i => GetItemPopularityCount(i, counts) == 1).ToList();
                case 2: return items.Where(i => { var c = GetItemPopularityCount(i, counts); return c >= 2 && c <= 7; }).ToList();
                case 3: return items.Where(i => GetItemPopularityCount(i, counts) >= 8).ToList();
                default: throw new Exception("Only 4 fixed buckets supported.");
            }
        }

        /// <summary>
        /// Generates bucket name by items in it
        /// </summary>
        /// <typeparam name="T">A type of items</typeparam>
        /// <param name="items">All items</param>
        /// <param name="counts">Number of item ratings</param>
        /// <returns></returns>
        private static string GetBucketName<T>(IList<T> items, IDictionary<T, int> counts)
        {
            var first = GetItemPopularityCount(items.First(), counts);
            var last = GetItemPopularityCount(items.Last(), counts);

            var ratings = first.Equals(last) ? $"{first} rating" + (first != 1 ? "s" : string.Empty) : $"{first} - {last} ratings";

            return $"{ratings} ({items.Count:n0})";
        }

        /// <summary>
        /// Takes errors of specified items and calculates MAE
        /// </summary>
        /// <param name="items">Items to calculate</param>
        /// <param name="predictionErrors">Prediction errors for all items</param>
        /// <returns></returns>
        private static double ComputeMae(IList<Movie> items, IDictionary<Movie, List<double>> predictionErrors)
        {
            return items
                .Where(predictionErrors.ContainsKey)
                .SelectMany(i => predictionErrors[i])
                .Average();
        }

        /// <summary>
        /// Returns number of ratings given to an item
        /// </summary>
        /// <typeparam name="T">Type of item</typeparam>
        /// <param name="item">Specified item</param>
        /// <param name="counts">Number ofratings for each item</param>
        /// <returns></returns>
        private static int GetItemPopularityCount<T>(T item, IDictionary<T, int> counts)
        {
            return counts.ContainsKey(item) ? counts[item] : 0;
        }

        #endregion
    }
}
