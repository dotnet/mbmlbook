// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AssessingPeoplesSkills
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using MBMLViews;
    
    using AssessingPeoplesSkills.Models;
    using MBMLCommon;
#if NETFULL
    using Point = System.Windows.Point;
#else
    using Point = MBMLCommon.Point;
#endif

    using Microsoft.ML.Probabilistic.Distributions;

    /// <summary>
    /// Runs the inference for the various scenarios in the chapter
    /// </summary>
    public class ModelRunner
    {
        /// <summary>
        /// The data path.
        /// </summary>
        private const string DataPath = @"Data/";

        private Outputter outputter;

        public ModelRunner(Outputter outputter)
        {
            this.outputter = outputter;
        }

        /// <summary>
        /// The probability density function demo.
        /// </summary>
        public void ProbabilityDensityFunctionDemo()
        {
            var height = new Gaussian(1.84, 0.0001);

            outputter.Out(SteppedGaussian(height, new RealRange { Min = 1.8, Max = 1.9, Steps = 11 }).ToArray(),
                Contents.S6LearningTheGuessProbabilities.NumberedName,
                "PDF Demo",
                "HeightDiscrete");
            outputter.Out(new RealRange { Min = 1.8, Max = 1.9, Steps = 1000 }.Values.Select(x => new Point(x, Math.Exp(height.GetLogProb(x)))).ToArray(),
                Contents.S6LearningTheGuessProbabilities.NumberedName,
                "PDF Demo",
                "HeightContinuous");
            outputter.Out(height,
                Contents.S6LearningTheGuessProbabilities.NumberedName,
                "PDF Demo",
                "Height");

            Console.WriteLine($@"Area of shaded region in the continuous plot: {height.CumulativeDistributionFunction(1.845) - height.CumulativeDistributionFunction(1.835)}");
            
            //// Second method using integration of pdf
            // Func<double, double> pdf = x => Math.Exp(height.GetLogProb(x));
            // Console.WriteLine("Area of shaded region: {0}", pdf.Integrate(1.835, 1.845, 500));
        }

        /// <summary>
        /// Stepped gaussian.
        /// </summary>
        /// <param name="gaussian">The gaussian.</param>
        /// <param name="range">The range.</param>
        /// <returns>The points.</returns>
        private IEnumerable<Point> SteppedGaussian(Gaussian gaussian, RealRange range)
        {
            var steps = new List<Point>();

            double sum = 0.0;

            var xvals = range.Values.ToArray();
            for (int i = 0; i < range.Count; i++)
            {
                double x1 = xvals[i] - (range.StepSize / 2);
                double x2 = xvals[i] + (range.StepSize / 2);

                double d = gaussian.CumulativeDistributionFunction(x2) - gaussian.CumulativeDistributionFunction(x1);

                if (i == 0)
                {
                    x1 = range.Min;
                }

                if (i == range.Count - 1)
                {
                    x2 = range.Max;
                }

                steps.Add(new Point(x1, 0));
                steps.Add(new Point(x1, d));
                steps.Add(new Point(x2, d));
                steps.Add(new Point(x2, 0));
                sum += d;
            }

            Console.WriteLine($@"The sum of the probabilities in the stepped plot is {sum}");

            return steps;
        }

        /// <summary>
        /// Demo of Beta Distribution
        /// </summary>
        public void BetaDemo()
        {
            outputter.Out(new[] { new Beta(1, 1), new Beta(2, 2), new Beta(2, 5), new Beta(4, 10), new Beta(8, 20) }, Contents.S6LearningTheGuessProbabilities.NumberedName, "Betas");
        }


        Random rnd = new Random();
        public void BetaSelfAssessment()
        {
            double true_prob = 0.3;
            int N = 100;
            int countTrue = 30;// TrueCount(true_prob, N);

            int numSamples = 10000;

            var counts = new int[50];
            for (int i = 0; i < numSamples; i++)
            {
                double p;
                while (true)
                {
                    p = rnd.NextDouble();
                    int sample_count = TrueCount(p, N);
                    if (sample_count == countTrue) break;
                }

                // Discretise into bins
                int binNum = (int)Math.Floor(p * counts.Length);
                counts[binNum]++;
            }
            outputter.Out(counts, Contents.S6LearningTheGuessProbabilities.NumberedName, "BetaHistogram");
        }

        private int TrueCount(double p, int N)
        {
            int ct = 0;
            for (int j = 0; j < N; j++)
            {
                if (rnd.NextDouble() < p) ct++;
            }
            return ct;
        }

#region Inference
        /// <summary>
        /// Toy example the with 3 questions and 2 skills. Used to create first results table
        /// </summary>
        public void ToyWith3QuestionsAnd2Skills()
        {
            Experiment experiment = new Experiment
            {
                Inputs = LoadInputData("Toy3"),
                Model = new UnrolledModel
                {
                    Name = "ThreeQuestions",
                    ProbabilityOfGuess = 0.2,
                    ProbabilityOfNotMistake = 0.9,
                    ProbabilityOfSkillTrue = 0.5,
                    ShowFactorGraph = false
                }
            };

            AnnounceAndRun(experiment, Contents.S2TestingOutTheModel.NumberedName);

            outputter.Out(
                new Dictionary<string, object>
                {
                    { "IsCorrect1", experiment.Inputs.IsCorrect.Select(r => r[0]).ToArray() },
                    { "IsCorrect2", experiment.Inputs.IsCorrect.Select(r => r[1]).ToArray() },
                    { "IsCorrect3", experiment.Inputs.IsCorrect.Select(r => r[2]).ToArray() },
                    { "P(csharp)", experiment.Results.SkillsPosteriorMeans.Select(p => new Bernoulli(p[0])).ToArray() },
                    { "P(sql)", experiment.Results.SkillsPosteriorMeans.Select(p => new Bernoulli(p[1])).ToArray() }
                },
                Contents.S2TestingOutTheModel.NumberedName,
                "ThreeQuestionsResults");
        }

        /// <summary>
        /// Results for loopy belief propagation section
        /// </summary>
        public void LoopyExample()
        {
            string section = Contents.S3Loopiness.NumberedName;
            Inputs inputs = LoadInputData("Toy4");
            Experiment loopyExperiment = new Experiment
            {
                // version without plates
                Inputs = inputs,
                Model = new UnrolledModel
                {
                    Name = "Loopy",
                    ProbabilityOfGuess = 0.2,
                    ProbabilityOfNotMistake = 0.9,
                    ProbabilityOfSkillTrue = 0.5,
                    ShowFactorGraph = false
                }
            };
            AnnounceAndRun(loopyExperiment, section);
            Experiment exactLoopyExperiment = new Experiment
            {
                // exact results
                Inputs = inputs,
                Model = new UnrolledModel
                {
                    Name = "LoopyExact",
                    ProbabilityOfGuess = 0.2,
                    ProbabilityOfNotMistake = 0.9,
                    ProbabilityOfSkillTrue = 0.5,
                    ShowFactorGraph = false,
                    ExactInference = true
                }
            };
            AnnounceAndRun(exactLoopyExperiment, section);
            ExperimentComparison comparison = new ExperimentComparison(
                new[]
                {
                    loopyExperiment,
                    exactLoopyExperiment
                });

            //learningSkills.Run();

            var histories = ((UnrolledModel)loopyExperiment.Model).MessageHistories;

            // Show message progress for edges A, B, C, D
            var messageHistories = new Dictionary<string, object>
                                       {
                                           { "Iteration", new[] { 1, 2, 3, 4, 5 } },
                                           { "A", histories["sql_uses_B[1]"].ToArray() },
                                           { "B", histories["sql_uses_F[2]"].ToArray() },
                                           { "C", histories["csharp_uses_B[2]"].ToArray() },
                                           { "D", histories["csharp_uses_F[1]"].ToArray() }
                                       };
            
            outputter.Out(messageHistories, section, "Loop Messages");
            outputter.Out(comparison, section, "Comparison");
        }

        private void AnnounceAndRun(Experiment experiment, string section)
        {
            Console.WriteLine($"Running {experiment.ModelName}");
            outputter.Out(experiment, section, experiment.ModelName);
            experiment.Run();
        }

        /// <summary>
        /// Real data inference. Try with guess probability fixed and inferred
        /// </summary>
        /// <param name="inputs">The inputs.</param>
        /// <param name="name">The name.</param>
        public void RealDataInference()
        {
            // Define priors (fixed and learnt)
            const double ProbGuess = 0.2;
            const double ProbNotMistake = 0.9;
            const double ProbSkillTrue = 0.5;

            Inputs inputs = LoadInputData("InputData");

            Console.WriteLine($"\n{Contents.S4MovingToRealData.NumberedName}\n");
            outputter.Out(inputs, Contents.S4MovingToRealData.NumberedName, "Inputs");
            // Original model (point mass priors)
            Experiment originalExperiment = new Experiment
            {
                Inputs = inputs,
                Model = new NoisyAndModel
                {
                    Name = "Original",
                    ProbabilityOfGuess = ProbGuess,
                    ProbabilityOfNotMistake = ProbNotMistake,
                    ProbabilityOfSkillTrue = ProbSkillTrue,
                    ShowFactorGraph = false,
                    IsReal = true
                }
            };
            AnnounceAndRun(originalExperiment, Contents.S4MovingToRealData.NumberedName);

            Console.WriteLine($"\n{Contents.S5DiagnosingTheProblem.NumberedName}\n");

            // Sampled model using ground truth skills
            Experiment samleSkillsObservedExperiment = new Experiment
            {
                Inputs = originalExperiment.Model.SampleFromModel(
                            inputs, inputs.NumberOfPeople, new object[] { ProbSkillTrue, true }),
                Model = new NoisyAndModel
                {
                    Name = "SampleSkillsObserved",
                    ProbabilityOfGuess = ProbGuess,
                    ProbabilityOfNotMistake = ProbNotMistake,
                    ProbabilityOfSkillTrue = ProbSkillTrue,
                    IsReal = false
                }
            };
            AnnounceAndRun(samleSkillsObservedExperiment, Contents.S5DiagnosingTheProblem.NumberedName);

            // Sampled model using sampled skills
            Experiment sampleSkillsSampledExperiment = new Experiment
            {
                Inputs = originalExperiment.Model.SampleFromModel(
                            inputs, inputs.NumberOfPeople, new object[] { ProbSkillTrue, false }),
                Model = new NoisyAndModel
                {
                    Name = "SampleSkillsSampled",
                    ProbabilityOfGuess = ProbGuess,
                    ProbabilityOfNotMistake = ProbNotMistake,
                    ProbabilityOfSkillTrue = ProbSkillTrue,
                    IsReal = false
                }
            };
            AnnounceAndRun(sampleSkillsSampledExperiment, Contents.S5DiagnosingTheProblem.NumberedName);

            Console.WriteLine($"\n{Contents.S6LearningTheGuessProbabilities.NumberedName}\n");
            outputter.Out(inputs, Contents.S6LearningTheGuessProbabilities.NumberedName, "Inputs");

            Beta guessPrior = BetaFromMeanAndTotalCount(0.25, 10);
            outputter.Out(
                guessPrior,
                Contents.S6LearningTheGuessProbabilities.NumberedName,
                "Priors");
            
            // Random model
            Experiment randomExperiment = new Experiment
            {
                Inputs = inputs,
                Model = new RandomModel
                {
                    Name = "Random",
                    ProbabilityOfGuess = 0.5,
                    ProbabilityOfNotMistake = 0.5,
                    ProbabilityOfSkillTrue = 0.5,
                    Index = 8,
                    ShowFactorGraph = false,
                    IsReal = false
                }
            };
            AnnounceAndRun(randomExperiment, Contents.S6LearningTheGuessProbabilities.NumberedName);

            // Model with Beta prior over guess probabilities
            Experiment learnedExperiment = new Experiment
            {
                Inputs = inputs,
                Model = new LearnedNoisyAndModel
                {
                    Name = "Learned",
                    ProbabilityOfGuess = ProbGuess,
                    ProbabilityOfNotMistake = ProbNotMistake,
                    ProbabilityOfSkillTrue = ProbSkillTrue,
                    GuessPrior = guessPrior,
                    ShowFactorGraph = false,
                    IsReal = true
                }
            };
            AnnounceAndRun(learnedExperiment, Contents.S6LearningTheGuessProbabilities.NumberedName);

            Experiment perfectExperiment = new Experiment
            {
                FullyObserved = true,
                Inputs = inputs,
                Model = new NoisyAndModel { Name = "Perfect", Index = 9, IsReal = false }
            };
            AnnounceAndRun(perfectExperiment, Contents.S6LearningTheGuessProbabilities.NumberedName);

            ExperimentComparison comparison = new ExperimentComparison();
            comparison.Experiments.Add(randomExperiment);
            comparison.Experiments.Add(originalExperiment);
            comparison.Experiments.Add(samleSkillsObservedExperiment);
            comparison.Experiments.Add(sampleSkillsSampledExperiment);
            comparison.Experiments.Add(learnedExperiment);
            comparison.Experiments.Add(perfectExperiment);

            //Selected guess posteriors
            outputter.Out(
                learnedExperiment.Results.GuessPosteriors
                    .Select((ia, i) => new { ia, i })
                    .Where((ia, i) => i % 5 == 0)
                    .ToDictionary<dynamic, string, Beta>(x => "Question " + (x.i + 1), x => x.ia),
                Contents.S6LearningTheGuessProbabilities.NumberedName,
                "SelectedGuessPosteriors");

            outputter.Out(comparison, Contents.S6LearningTheGuessProbabilities.NumberedName, "Comparison");
        }
#endregion

#region BetaHelpers
        /// <summary>
        /// Betas from mean and total count.
        /// </summary>
        /// <param name="mean">The mean.</param>
        /// <param name="totalCount">The total count.</param>
        /// <returns>Beta distribution.</returns>
        private static Beta BetaFromMeanAndTotalCount(double mean, int totalCount)
        {
            return new Beta(mean * totalCount, (1 - mean) * totalCount);
        }
#endregion

#region File IO
        /// <summary>
        /// Loads the input data.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <returns>The input data.</returns>
        /// <exception cref="System.NullReferenceException">Failed to load input data
        /// or
        /// Failed to load quiz data</exception>
        public static Inputs LoadInputData(string filename)
        {
            var inputData = FileUtils.Load<Inputs>(DataPath, filename);

            if (inputData == null)
            {
                throw new NullReferenceException("Failed to load input data");
            }

            if (inputData.Quiz == null)
            {
                throw new NullReferenceException("Failed to load quiz data");
            }

            return inputData;
        }
#endregion
    }
}
