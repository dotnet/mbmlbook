// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AssessingPeoplesSkills
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MBMLCommon;

    using MBMLViews;

    using AssessingPeoplesSkills.Models;

    using Microsoft.ML.Probabilistic.Learners;

    using InferMetrics = Microsoft.ML.Probabilistic.Learners.Metrics;
    using InstanceScoreEnumerable = System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<Instance, double>>;

    /// <summary>
    /// Metrics for assessing the Experiment
    /// </summary>
    [Serializable]
    public class Metrics
    {
        /// <summary>
        /// Gets or sets the inputs.
        /// </summary>
        /// <value>
        /// The inputs.
        /// </value>
        public Inputs Inputs { get; set; }

        /// <summary>
        /// Gets or sets the model.
        /// </summary>
        /// <value>
        /// The model.
        /// </value>
        public NoisyAndModel Model { get; set; }

        /// <summary>
        /// Gets or sets the results.
        /// </summary>
        /// <value>
        /// The results.
        /// </value>
        public Results Results { get; set; }

        /// <summary>
        /// Gets the stated skills mean frequency per person.
        /// </summary>
        /// <value>
        /// The stated skills mean frequency per person.
        /// </value>
        public double[] StatedSkillsMeanFrequencyPerPerson => Inputs?.StatedSkills.Average();

        /// <summary>
        /// Gets the stated skills mean frequency per skill.
        /// </summary>
        /// <value>
        /// The stated skills mean frequency per skill.
        /// </value>
        public Dictionary<string, double> StatedSkillsMeanFrequencyPerSkill => Inputs != null ? GetAverageOfMetricBySkill(Inputs.StatedSkills) : null;

        /// <summary>
        /// Gets the stated skills mean frequency.
        /// </summary>
        /// <value>
        /// The stated skills mean frequency.
        /// </value>
        public double StatedSkillsMeanFrequency
        {
            get
            {
                return StatedSkillsMeanFrequencyPerSkill?.Values.Average() ?? double.NaN;
            }
        }

        /// <summary>
        /// Gets the log probability of truth per person and skill.
        /// </summary>
        /// <value>
        /// The log probability of truth per person and skill.
        /// </value>
        public double[][] LogProbabilityOfTruthPerPersonAndSkill => Results?.SkillsPosteriors?.GetLogProbabilityOfTruth(Inputs.StatedSkills);

        /// <summary>
        /// Gets the log probability of truth per person.
        /// </summary>
        /// <value>
        /// The log probability of truth per person.
        /// </value>
        public double[] LogProbabilityOfTruthPerPerson => LogProbabilityOfTruthPerPersonAndSkill?.Average();

        /// <summary>
        /// Gets the log probability of truth per skill.
        /// </summary>
        /// <value>
        /// The log probability of truth per skill.
        /// </value>
        public Dictionary<string, double> LogProbabilityOfTruthPerSkill => GetAverageOfMetricBySkill(LogProbabilityOfTruthPerPersonAndSkill);

        /// <summary>
        /// Gets the negative log probability of truth.
        /// </summary>
        /// <value>
        /// The negative log probability of truth.
        /// </value>
        public double NegativeLogProbabilityOfTruth =>
            NegativeLogProbabilityOfTruthPerPerson?.Average() ?? double.NaN;

        /// <summary>
        /// Gets the negative log probability of truth as array.
        /// Used to force ChartView to treat it as a series
        /// </summary>
        /// <value>
        /// The negative log probability of truth as array.
        /// </value>
        public double[] NegativeLogProbabilityOfTruthAsArray =>
            NegativeLogProbabilityOfTruthPerPerson == null
                ? null
                : new[] { NegativeLogProbabilityOfTruthPerPerson.Average() };

        /// <summary>
        /// Gets the negative log probability of truth as dictionary.
        /// </summary>
        /// <value>
        /// The negative log probability of truth as dictionary.
        /// </value>
        public Dictionary<string, double> NegativeLogProbabilityOfTruthAsDictionary =>
            NegativeLogProbabilityOfTruthPerPerson == null
                ? null
                : new Dictionary<string, double>
                {
                    {
                        string.Empty,
                        NegativeLogProbabilityOfTruthPerPerson.Average()
                    }
                };

        /// <summary>
        /// Gets the negative log probability of truth per person and skill.
        /// </summary>
        /// <value>
        /// The negative log probability of truth per person and skill.
        /// </value>
        public double[][] NegativeLogProbabilityOfTruthPerPersonAndSkill
        {
            get
            {
                if (Results == null || Results.SkillsPosteriors == null)
                {
                    return null;
                }

                return
                    Results.SkillsPosteriors.GetLogProbabilityOfTruth(Inputs.StatedSkills)
                           .Select(ia => ia.Select(inner => -inner).ToArray())
                           .ToArray();
            }
        }
        
        /// <summary>
        /// Gets the negative log probability of truth per person.
        /// </summary>
        /// <value>
        /// The negative log probability of truth per person.
        /// </value>
        public double[] NegativeLogProbabilityOfTruthPerPerson => NegativeLogProbabilityOfTruthPerPersonAndSkill.Average();

        /// <summary>
        /// Gets the negative log probability of truth per skill.
        /// </summary>
        /// <value>
        /// The negative log probability of truth per skill.
        /// </value>
        public Dictionary<string, double> NegativeLogProbabilityOfTruthPerSkill => GetAverageOfMetricBySkill(NegativeLogProbabilityOfTruthPerPersonAndSkill);

        /// <summary>
        /// Gets the log probability of truth.
        /// </summary>
        /// <value>
        /// The log probability of truth.
        /// </value>
        public double LogProbabilityOfTruth =>
            LogProbabilityOfTruthPerPerson?.Average() ?? double.NaN;

        /// <summary>
        /// Gets the log probability of truth as array.
        /// </summary>
        /// <value>
        /// The log probability of truth as array.
        /// </value>
        public double[] LogProbabilityOfTruthAsArray =>
            LogProbabilityOfTruthPerPerson == null
                ? null
                : new[] { LogProbabilityOfTruthPerPerson.Average() };

        /// <summary>
        /// Gets the log probability of truth as dictionary.
        /// </summary>
        /// <value>
        /// The log probability of truth as dictionary.
        /// </value>
        public Dictionary<string, double> LogProbabilityOfTruthAsDictionary =>
            LogProbabilityOfTruthPerPerson == null
                ? null
                : new Dictionary<string, double>
                {
                    { string.Empty, LogProbabilityOfTruthPerPerson.Average() }
                };

        /// <summary>
        /// Gets the log probability of truth by probability guess.
        /// </summary>
        /// <value>
        /// The log probability of truth by probability guess.
        /// </value>
        public Point LogProbabilityOfTruthByProbGuess =>
            Model == null
                ? new Point(double.NaN, double.NaN)
                : new Point(Model.ProbabilityOfGuess, LogProbabilityOfTruth);

        /// <summary>
        /// Gets the log probability of true is correct per person and question.
        /// </summary>
        /// <value>
        /// The log probability of true is correct per person and question.
        /// </value>
        public double[][] LogProbabilityOfTrueIsCorrectPerPersonAndQuestion
        {
            get
            {
                if (Results == null || Results.IsCorrectPosteriors == null)
                {
                    return null;
                }

                return
                    Results.IsCorrectPosteriors.GetLogProbabilityOfTruth(Inputs.IsCorrect)
                           .Select(ia => ia.Select(inner => inner).ToArray())
                           .ToArray();
            }
        }

        /// <summary>
        /// Gets the negative log probability of true is correct per person and question.
        /// </summary>
        /// <value>
        /// The negative log probability of true is correct per person and question.
        /// </value>
        public double[][] NegativeLogProbabilityOfTrueIsCorrectPerPersonAndQuestion
        {
            get
            {
                if (Results == null || Results.IsCorrectPosteriors == null)
                {
                    return null;
                }

                return
                    Results.IsCorrectPosteriors.GetLogProbabilityOfTruth(Inputs.IsCorrect)
                           .Select(ia => ia.Select(inner => -inner).ToArray())
                           .ToArray();
            }
        }

        /// <summary>
        /// Gets the log probability of true is correct per person.
        /// </summary>
        /// <value>
        /// The log probability of true is correct per person.
        /// </value>
        public double[] LogProbabilityOfTrueIsCorrectPerPerson => LogProbabilityOfTrueIsCorrectPerPersonAndQuestion.Average();

        /// <summary>
        /// Gets the negative log probability of true is correct per skill.
        /// </summary>
        /// <value>
        /// The log probability of true is correct per skill.
        /// </value>
        public Dictionary<string, double> LogProbabilityOfTrueIsCorrectPerSkill => GetAverageOfMetricBySkill(LogProbabilityOfTrueIsCorrectPerPersonAndQuestion);

        /// <summary>
        /// Gets the negative log probability of true is correct per skill.
        /// </summary>
        /// <value>
        /// The negative log probability of true is correct per skill.
        /// </value>
        public Dictionary<string, double> NegativeLogProbabilityOfTrueIsCorrectPerSkill => GetAverageOfMetricBySkill(NegativeLogProbabilityOfTrueIsCorrectPerPersonAndQuestion);

        /// <summary>
        /// Gets the log probability of true is correct.
        /// </summary>
        /// <value>
        /// The log probability of true is correct.
        /// </value>
        public double LogProbabilityOfTrueIsCorrect =>
            LogProbabilityOfTrueIsCorrectPerPerson?.Average() ?? double.NaN;

        /// <summary>
        /// Gets the log probability of true is correct by probability guess.
        /// </summary>
        /// <value>
        /// The log probability of true is correct by probability guess.
        /// </value>
        public Point LogProbabilityOfTrueIsCorrectByProbGuess =>
            Model == null
                ? new Point(double.NaN, double.NaN)
                : new Point(Model.ProbabilityOfGuess, LogProbabilityOfTrueIsCorrect);

        /// <summary>
        /// Gets the negative log probability of true is correct.
        /// </summary>
        /// <value>
        /// The negative log probability of true is correct.
        /// </value>
        public double NegativeLogProbabilityOfTrueIsCorrect =>
            -LogProbabilityOfTrueIsCorrectPerPerson?.Average() ?? double.NaN;

        /// <summary>
        /// Gets the negative log probability of true is correct as dictionary.
        /// </summary>
        /// <value>
        /// The negative log probability of true is correct as dictionary.
        /// </value>
        public Dictionary<string, double> NegativeLogProbabilityOfTrueIsCorrectAsDictionary => new Dictionary<string, double> { { string.Empty, NegativeLogProbabilityOfTrueIsCorrect } };

        /// <summary>
        /// Gets the probability of truth per person and skill.
        /// </summary>
        /// <value>
        /// The probability of truth per person and skill.
        /// </value>
        public double[][] ProbabilityOfTruthPerPersonAndSkill
        {
            get
            {
                return LogProbabilityOfTruthPerPersonAndSkill?.Select(ia => ia.Select(Math.Exp).ToArray()).ToArray();
            }
        }

        /// <summary>
        /// Gets the probability of truth per person.
        /// </summary>
        /// <value>
        /// The probability of truth per person.
        /// </value>
        public double[] ProbabilityOfTruthPerPerson => ProbabilityOfTruthPerPersonAndSkill?.Average();

        /// <summary>
        /// Gets the probability of truth per skill.
        /// </summary>
        /// <value>
        /// The probability of truth per skill.
        /// </value>
        public Dictionary<string, double> ProbabilityOfTruthPerSkill => GetAverageOfMetricBySkill(ProbabilityOfTruthPerPersonAndSkill);

        /// <summary>
        /// Gets the probability of truth.
        /// </summary>
        /// <value>
        /// The probability of truth.
        /// </value>
        public double ProbabilityOfTruth => ProbabilityOfTruthPerPerson?.Average() ?? double.NaN;

        /// <summary>
        /// Gets the skills mean squared error per person.
        /// </summary>
        /// <value>
        /// The skills mean squared error per person.
        /// </value>
        public double[] MeanSquaredErrorPerPerson => Results?.SkillsPosteriorMeans?.MeanSquaredError(Inputs.StatedSkills);

        /// <summary>
        /// Gets the skills mean squared error per skill.
        /// </summary>
        /// <value>
        /// The skills mean squared error per skill.
        /// </value>
        public Dictionary<string, double> MeanSquaredErrorPerSkill 
        { 
            get 
            {
                if (Results?.SkillsPosteriorMeans == null || Inputs?.StatedSkills == null || Inputs.Quiz?.SkillShortNames == null)
                {
                    return null;
                }

                double[] meanSquaredError = Results.SkillsPosteriorMeans.MeanSquaredError(Inputs.StatedSkills, 1);
                return Inputs.Quiz.SkillShortNames.Select((ia, i) => new { ia, i }).ToDictionary(x => x.ia, x => meanSquaredError[x.i]); 
            } 
        }

        /// <summary>
        /// Gets the skills mean squared error.
        /// </summary>
        /// <value>
        /// The skills mean squared error.
        /// </value>
        public double MeanSquaredError => MeanSquaredErrorPerPerson?.Average() ?? double.NaN;

        /// <summary>
        /// Gets the fraction correct.
        /// </summary>
        /// <value>
        /// The fraction correct.
        /// </value>
        public double FractionCorrect => FractionCorrectPerPerson?.Average() ?? double.NaN;

        /// <summary>
        /// Gets the fraction correct responses per question.
        /// </summary>
        /// <value>
        /// The fraction correct.
        /// </value>
        public double[] FractionCorrectPerPerson => Inputs?.IsCorrect.Average();

        /// <summary>
        /// Gets the fraction correct per question.
        /// </summary>
        /// <value>
        /// The fraction correct per question.
        /// </value>
        public double[] FractionCorrectPerQuestion => Inputs?.IsCorrect.Average(dimension: 1);

        /// <summary>
        /// Gets the fraction correct per skill.
        /// </summary>
        /// <value>
        /// The fraction correct per skill.
        /// </value>
        public Dictionary<string, double> FractionCorrectPerSkill => Inputs == null ? null : GetAverageOfMetricBySkill(Inputs.IsCorrect);

        /// <summary>
        /// Gets the expected fraction correct per person.
        /// </summary>
        /// <value>
        /// The expected fraction correct per person.
        /// </value>
        public double[] ExpectedFractionCorrectPerPerson
        {
            get
            {
                if (Inputs?.StatedSkills == null || Inputs.Quiz?.SkillsQuestionsMask == null)
                {
                    return null;
                }

                double[] expected = new double[Inputs.Quiz.NumberOfQuestions];

                for (int i = 0; i < Inputs.NumberOfPeople; i++)
                {
                    for (int j = 0; j < Inputs.Quiz.NumberOfQuestions; j++)
                    {
                        bool[] skillsForQuestion = Inputs.StatedSkills[i].Where((item, index) => Inputs.Quiz.SkillsQuestionsMask[index][j]).ToArray();
                        bool hasAllSkills = skillsForQuestion.All(ia => ia);

                        expected[j] += (hasAllSkills ? Model.ProbabilityOfNotMistake : Model.ProbabilityOfGuess) / Inputs.NumberOfPeople;
                    }
                }

                return expected;
            }
        }

        /// <summary>
        /// Gets the expected and true fraction correct as dictionary.
        /// </summary>
        /// <value>
        /// The expected and true fraction correct.
        /// </value>
        public Dictionary<string, double[]> ExpectedAndTrueFractionCorrect =>
            new Dictionary<string, double[]>
            {
                { "Predicted", ExpectedFractionCorrectPerPerson },
                { "Actual", FractionCorrectPerQuestion }
            };

        /// <summary>
        /// Gets the fraction correct when has all skills.
        /// </summary>
        /// <value>
        /// The fraction correct when has all skills.
        /// </value>
        public double FractionCorrectWhenHasAllSkills => FractionCorrectPerQuestionWhenHasAllSkills?.Average() ?? double.NaN;

        /// <summary>
        /// Gets the fraction correct when not all skills.
        /// </summary>
        /// <value>
        /// The fraction correct when not all skills.
        /// </value>
        public double FractionCorrectWhenNotAllSkills => FractionCorrectPerQuestionWhenNotAllSkills?.Average() ?? double.NaN;

        /// <summary>
        /// Gets the fraction correct per question when has all skills.
        /// </summary>
        /// <value>
        /// The fraction correct per question when has all skills.
        /// </value>
        public double[] FractionCorrectPerQuestionWhenHasAllSkills
        {
            get
            {
                if (Inputs?.Quiz == null)
                {
                    return null;
                }

                double[] fraction = new double[Inputs.Quiz.NumberOfQuestions];
                for (int i = 0; i < Inputs.Quiz.NumberOfQuestions; i++)
                {
                    List<bool> responses = new List<bool>();
                    for (int j = 0; j < Inputs.NumberOfPeople; j++)
                    {
                        if (Inputs.HasAllSkills(j, i))
                        {
                            responses.Add(Inputs.IsCorrect[j][i]);
                        }
                    }

                    fraction[i] = ((double)responses.Count(ia => ia)) / responses.Count;
                }

                return fraction;
            }
        }

        /// <summary>
        /// Gets the fraction correct per question when not all skills.
        /// </summary>
        /// <value>
        /// The fraction correct per question when not all skills.
        /// </value>
        public double[] FractionCorrectPerQuestionWhenNotAllSkills
        {
            get
            {
                if (Inputs == null || Inputs.Quiz == null)
                {
                    return null;
                }

                double[] fraction = new double[Inputs.Quiz.NumberOfQuestions];
                for (int i = 0; i < Inputs.Quiz.NumberOfQuestions; i++)
                {
                    List<bool> responses = new List<bool>();
                    for (int j = 0; j < Inputs.NumberOfPeople; j++)
                    {
                        if (!Inputs.HasAllSkills(j, i))
                        {
                            responses.Add(Inputs.IsCorrect[j][i]);
                        }
                    }

                    fraction[i] = ((double)responses.Count(ia => ia)) / responses.Count;
                }

                return fraction;
            }
        }

        /// <summary>
        /// Gets the fraction correct per question when has all skills binned.
        /// </summary>
        /// <value>
        /// The fraction correct per question when has all skills binned.
        /// </value>
        public int[] FractionCorrectPerQuestionWhenHasAllSkillsBinned => FractionCorrectPerQuestionWhenHasAllSkills?.Bin(10, 0.0, 1.0);

        /// <summary>
        /// Gets the fraction correct per question when not all skills binned.
        /// </summary>
        /// <value>
        /// The fraction correct per question when not all skills binned.
        /// </value>
        public int[] FractionCorrectPerQuestionWhenNotAllSkillsBinned =>
            FractionCorrectPerQuestionWhenNotAllSkills?.Bin(10, 0.0, 1.0);

        /// <summary>
        /// Gets the instances.
        /// </summary>
        public IEnumerable<Instance> Instances
        {
            get
            {
                if (Inputs?.StatedSkills != null && Results?.SkillsPosteriorMeans != null)
                { 
                    bool[] measurements = Inputs.StatedSkills.Flatten();
                    double[] predictions = Results.SkillsPosteriorMeans.Flatten();

                    for (int i = 0; i < measurements.Length; i++)
                    {
                        bool m = measurements[i];
                        double p = predictions[i];
                        yield return new Instance {Index = i, Measurement = m, Prediction = p};
                    }
                }
            }
        }

        /// <summary>
        /// Gets the positive instances.
        /// </summary>
        public IEnumerable<Instance> PositiveInstances
        {
            get
            {
                return Instances.Where(ia => ia.Measurement);
            }
        }

        /// <summary>
        /// Gets the instance scores.
        /// </summary>
        public InstanceScoreEnumerable InstanceScores
        {
            get
            {
                return Instances.Select(ia => new KeyValuePair<Instance, double>(ia, ia.Prediction));
            }
        }

        /// <summary>
        /// Gets the predictions for the positive class only. Required for Infer.NET metrics
        /// </summary>
        public IEnumerable<Dictionary<bool, double>> PositivePredictions
        {
            get
            {
                return Instances.Select(ia => new Dictionary<bool, double> { { true, ia.Measurement ? ia.Prediction : 1 - ia.Prediction } });
            }
        }

        /// <summary>
        /// Gets the receiver operating characteristic points (new version using AS code).
        /// </summary>
        public Point[] ReceiverOperatingCharacteristicPoints
        {
            get
            {
                if (PositiveInstances.Any())
                {
                    return
                        InferMetrics.ReceiverOperatingCharacteristicCurve(PositiveInstances, InstanceScores)
                            .Select(ia => new Point(ia.First, ia.Second))
                            .ToArray();
                }
                return new Point[0];
            }
        }
        
        /// <summary>
        /// Gets the area under curve (new version using AS code).
        /// </summary>
        public double AreaUnderCurve
        {
            get
            {
                if (PositiveInstances.Any())
                {
                    return InferMetrics.AreaUnderRocCurve(PositiveInstances, InstanceScores);
                }
                return 0;
            }
        }

        /// <summary>
        /// Gets the skills posterior histogram.
        /// </summary>
        /// <value>
        /// The skills posterior histogram.
        /// </value>
        public int[] SkillsPosteriorHistogram => Results?.SkillsPosteriorMeans?.Bin(10);

        /// <summary>
        /// Gets the calibration curve.
        /// </summary>
        public Point[] CalibrationCurve
        {
            get
            {
                var evaluatorMapping = new EvaluatorMapping();

                var evaluator = new ClassifierEvaluator<IEnumerable<Instance>, Instance, InstanceScoreEnumerable, bool>(evaluatorMapping);

                const int Bins = 5;

                return
                    evaluator.CalibrationCurve(true, Instances, PositivePredictions, Bins)
                             .Select(ia => new Point(ia.First, ia.Second))
                             .ToArray();
            }
        }

        /// <summary>
        /// Gets the average of metric by skill.
        /// </summary>
        /// <param name="metric">The metric.</param>
        /// <returns>The average of the metric keyed by skill</returns>
        private Dictionary<string, double> GetAverageOfMetricBySkill(bool[][] metric)
        {
            if (metric == null)
            {
                return null;
            }

            bool[][] metricTransposed = metric.Transpose();
            return Inputs.Quiz.SkillShortNames.Select((ia, i) => new { ia, i }).ToDictionary(x => x.ia, x => metricTransposed[x.i].Average());
        }

        /// <summary>
        /// Gets the average of metric by skill.
        /// </summary>
        /// <param name="metric">The metric.</param>
        /// <returns>The average of the metric keyed by skill</returns>
        private Dictionary<string, double> GetAverageOfMetricBySkill(double[][] metric)
        {
            if (metric == null)
            {
                return null;
            }

            double[][] metricTransposed = metric.Transpose();
            return Inputs.Quiz.SkillShortNames.Select((ia, i) => new { ia, i }).ToDictionary(x => x.ia, x => metricTransposed[x.i].Average());
        }
    }
}
