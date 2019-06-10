// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MBMLCommon;
using Microsoft.ML.Probabilistic.Distributions;
using Microsoft.ML.Probabilistic.Models;
using Microsoft.Research.Glo;

namespace MakingRecommendations
{
#if NETFULL
    using MBMLViews.Views;
#endif

    /// <summary>
    /// Type of the experiment to run
    /// </summary>
    public enum ExperimentRunType
    {
        /// <summary>
        /// A very small experiment used for testing purposes: only 0 and 4 trait models are considered,
        /// recommender performs 30 iterations.
        /// </summary>
        TestRun,
        /// <summary>
        /// A fast, but representative experiment: 0, 1, 2, and 4 trait models are considered,
        /// recommender performs 30 iterations.
        /// </summary>
        FastRun,
        /// <summary>
        /// The complete experiment: 0, 1, 2, 4, 8, and 16 trait models are considered,
        /// recommender runs to convergence performing 200 iterations.
        /// </summary>
        FullRun
    }

    /// <summary>
    /// The program.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Gets the data path.
        /// </summary>
        public static readonly string DataPath = @"Data";

        /// <summary>
        /// Gets the path of the MovieLens for Education dataset.
        /// </summary>
        public static readonly string MleDataPath = Path.Combine(DataPath, "MovieLensForEducation");

        /// <summary>
        /// Gets the path of the ratings.csv.
        /// </summary>
        public static readonly string RatingsPath = Path.Combine(MleDataPath, "ratings.csv");

        /// <summary>
        /// Initialize UI.
        /// </summary>
        private static void InitializeUI()
        {
#if NETFULL
            // Initialize MBMLViews/Glo
            var types = new[] { typeof(Gaussian), typeof(IList<Gaussian>), typeof(GaussianView) };

            InferenceEngine.Visualizer = new Microsoft.ML.Probabilistic.Compiler.Visualizers.WindowsVisualizer();
#endif
        }

        /// <summary>
        /// Parses movies.csv into array of Movies.
        /// </summary>
        /// <returns></returns>
        private static Movie[] GetMovies()
        {
            var movies = File.ReadLines(Path.Combine(MleDataPath, "movies.csv"))
                .Select(str => str.Split(';'))
                .Select( x =>
                {
                    var name = x[1].Substring(0, x[1].Length - 7);
                    var year = Convert.ToInt32(x[1].Substring(x[1].Length - 5, 4));
                    var genres = x[2].Split('|');
                    return new Movie(int.Parse(x[0]), name, year, genres);
                });

            return movies.ToArray();
        }

        /// <summary>
        /// Produces plots of relations between ratings and their number. 
        /// </summary>
        /// <returns>A tuple of a rating list, a plot of the number of ratings given for each number of stars
        /// and a plot of the number of ratings given for each movie in the data set. </returns>
        private static (List<RatingTriple> ratings, SortedDictionary<string, double> ratingsToStarsDistribution,
            IOrderedEnumerable<int> rankToRatingsDistributions) PriorRatings(IEnumerable<Movie> movies)
        {
            var moviesDict = movies.ToDictionary(x => x.Id.ToString());

            // Despite input data already given to the form of 10-star ratings
            // it is interesting and convenient to watch original 5-star ratings.
            var ratings = File.ReadLines(RatingsPath)
                .Select(str => str.Split(','))
                .Where(x => x[0] != "I")
                .Select(x => new RatingTriple(x[0], moviesDict[x[1]], Convert.ToDouble(x[2]) / 2.0))
                .ToList();

            var ratingsToStarsDistribution = new SortedDictionary<string, double>(ratings.GroupBy(x => x.Rating).ToDictionary(x => x.Key.ToString(), x => (double)x.Count()));
            var rankToRatingsDistributions = ratings.GroupBy(x => x.Movie).Select(x => x.Count()).OrderByDescending(x => x);

            return (ratings, ratingsToStarsDistribution, rankToRatingsDistributions);
        }

        /// <summary>
        /// Forces ModelRunner to run experiments, takes its results and show them via outputter.  
        /// </summary>
        /// <param name="outputter">A container for experiments output.</param>
        /// <param name="experimentRunType">
        /// When set to <see cref="ExperimentRunType.FullRun"/>, inference is run to convergence, which gives the metrics shown in the book.
        /// When set to <see cref="ExperimentRunType.FastRun"/>, the number of iterations in inference is reduced to improve execution time, while still achieving reasonable accuracy numbers, and some of the trait counts are omitted.
        /// When set to <see cref="ExperimentRunType.TestRun"/>, the number of iterations in inference is reduced still, and even more of the trait counts are omitted.
        /// </param>
        public static void RunExperiments(Outputter outputter, ExperimentRunType experimentRunType)
        {
            // List containing numbers of traits to use in experiments. A separate set of experiments will be run for each number in the list. 
            var traitCounts =
                experimentRunType == ExperimentRunType.FullRun ? new int[] { 0, 1, 2, 4, 8, 16 }
                : experimentRunType == ExperimentRunType.FastRun ? new int[] { 0, 1, 2, 4 }
                : new int[] { 0, 4 }; // experimentRunType == ExperimentRunType.TestRun
            var movies = GetMovies();
            var recommenderMappingFactory = new RecommenderMappingFactory(movies);

            var modelRunner = new ModelRunner(recommenderMappingFactory, RatingsPath)
            {
                IterationCount = experimentRunType == ExperimentRunType.FullRun ? 200 : 30
            };

            #region Section3

            Console.WriteLine($"\n{Contents.S3TrainingOurRecommender.NumberedName}.\n");

            var (ratings, ratingsToStarsDistribution, rankToRatingsDistributions) = PriorRatings(movies);
            outputter.Out(ratings, Contents.S3TrainingOurRecommender.NumberedName, "Ratings");
            outputter.Out(ratingsToStarsDistribution,
                Contents.S3TrainingOurRecommender.NumberedName, "The number of ratings given for each possible number of stars");

            #endregion
            
            #region Section4

            Console.WriteLine($"\n{Contents.S4OurFirstRecommendations.NumberedName}.\n");

            outputter.Out(modelRunner.GetGroundTruth(recommenderMappingFactory.GetBinaryMapping(true)),
                Contents.S4OurFirstRecommendations.NumberedName, "Ground truth");

            var (predictions, metricsOfPredictionsOnBinary) = modelRunner.PredictionsOnBinaryData(traitCounts);

            outputter.Out(predictions, Contents.S4OurFirstRecommendations.NumberedName, "Predictions");

            outputter.Out(metricsOfPredictionsOnBinary.CorrectFractions,
                Contents.S4OurFirstRecommendations.NumberedName,
                "Fraction of predictions correct");

            outputter.Out(metricsOfPredictionsOnBinary.Ndcgs,
                Contents.S4OurFirstRecommendations.NumberedName,
                "Average NDCG@5");

            #endregion
            
            #region Section5

            Console.WriteLine($"\n{Contents.S5ModellingStarRatings.NumberedName}.\n");

            outputter.Out(modelRunner.GetGroundTruth(recommenderMappingFactory.GetStarsMapping(true)),
                Contents.S5ModellingStarRatings.NumberedName, "Ground truth");

            var (posteriorDistributionsOfThresholds, predictionsOnStars, metricsOfPredictionsWithStars) =
                modelRunner.PredictionsOnStarRatings(traitCounts);

            var ratingsNumToMaeStars = modelRunner.GetRatingsNumToMaeOnStarsPredictions();

            outputter.Out(posteriorDistributionsOfThresholds, Contents.S5ModellingStarRatings.NumberedName, "Posterior distributions for star ratings thresholds");

            outputter.Out(predictionsOnStars, Contents.S5ModellingStarRatings.NumberedName, "Predictions");

            var traitsToCorrectFractionSection5 = new Dictionary<string, IDictionary<string, double>>() {
                    { "Initial", metricsOfPredictionsOnBinary.CorrectFractions },
                    { "With stars", metricsOfPredictionsWithStars.CorrectFractions }
                };

            var traitCountToMaeSection5 = new Dictionary<string, IDictionary<string, double>>() {
                    { "Initial", metricsOfPredictionsOnBinary.Ndcgs },
                    { "With stars", metricsOfPredictionsWithStars.Ndcgs }
                };

            outputter.Out(traitsToCorrectFractionSection5, Contents.S5ModellingStarRatings.NumberedName,
                "Fraction of predictions correct");

            outputter.Out(traitCountToMaeSection5, Contents.S5ModellingStarRatings.NumberedName,
                "Average NDCG@5");

            outputter.Out(metricsOfPredictionsWithStars.Maes, Contents.S5ModellingStarRatings.NumberedName,
                "Mean absolute error (MAE)");

            #endregion
    
            #region Section6

            Console.WriteLine($"\n{Contents.S6AnotherColdStartProblem.NumberedName}.\n");

            outputter.Out(rankToRatingsDistributions,
                Contents.S6AnotherColdStartProblem.NumberedName, "The number of ratings given for each movie in the data set as a whole. ");
            
            var metricsOfPredictionsWithFeatures = modelRunner.PredictionsOnDataWithFeatures(traitCounts);

            var ratingsNumToMaeFeatures = modelRunner.GetRatingsToMaeOnFeaturePredictions();
            
            outputter.Out(ratingsNumToMaeStars, Contents.S6AnotherColdStartProblem.NumberedName,
                "MAE for movies with different numbers of ratings.");

            var ratingsNumToMae = new Dictionary<string, Dictionary<string, double>>
                {
                    { "With stars", ratingsNumToMaeStars },
                    { "With stars and features",  ratingsNumToMaeFeatures }
                };

            outputter.Out(ratingsNumToMae, Contents.S6AnotherColdStartProblem.NumberedName,
                "MAE for movies with different numbers of ratings. A model including feature information.");

            var traitCountToMae = new Dictionary<string, IDictionary<string, double>>() {
                    { "With stars", metricsOfPredictionsWithStars.Maes },
                    { "With stars and features", metricsOfPredictionsWithFeatures.Maes },
                };

            outputter.Out(traitCountToMae, Contents.S6AnotherColdStartProblem.NumberedName,
                "Mean absolute error (MAE)");

            var traitCountToNdcg = new Dictionary<string, IDictionary<string, double>>() {
                    { "Initial", metricsOfPredictionsOnBinary.Ndcgs },
                    { "With stars", metricsOfPredictionsWithStars.Ndcgs },
                    { "With stars and features", metricsOfPredictionsWithFeatures.Ndcgs },
                };
                
            outputter.Out(traitCountToNdcg, Contents.S6AnotherColdStartProblem.NumberedName,
                "Average NDCG@5");
                
            #endregion
    
            Console.WriteLine("\nCompleted all experiments.");
        }

        /// <summary>
        /// The main.
        /// </summary>
        public static void Main(string[] args)
        {
            InitializeUI();
            Outputter outputter = Outputter.GetOutputter(Contents.ChapterName);
            // To get results exactly like in the book set it to FullRun.
            ExperimentRunType runType = ExperimentRunType.FastRun;

            try
            {
                RunExperiments(outputter, runType);
            }
            catch (Exception e)
            {
                Console.WriteLine($"\nAn unhandled exception was thrown:\n{e}");
            }
            finally
            {
                if (args.Length == 1)
                {
                    Console.WriteLine("\n\nSaving outputs...");
                    outputter.SaveOutputAsProducedFlattening(args[0]);
                    Console.WriteLine("Done saving.");
                }
            }
        }
    }
}
