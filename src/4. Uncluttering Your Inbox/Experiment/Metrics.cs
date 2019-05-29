// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;

    using MBMLViews;

    using Microsoft.ML.Probabilistic.Learners.Mappings;

    using Evaluator = Microsoft.ML.Probabilistic.Learners.ClassifierEvaluator<Inputs.DataSet, Inputs.Instance, object, bool>;
    using InferMetrics = Microsoft.ML.Probabilistic.Learners.Metrics;
    using Microsoft.Research.Glo.Object;
    using Microsoft.ML.Probabilistic.Collections;
#if NETFULL
    using MBMLViews.Views;
    using Point = System.Windows.Point;
#else
    using Point = MBMLCommon.Point;
#endif

    /// <summary>
    /// The metrics.
    /// </summary>
    public class Metrics
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Metrics"/> class. 
        /// </summary>
        public Metrics()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Metrics" /> class.
        /// </summary>
        /// <param name="inputs">The inputs.</param>
        /// <param name="results">The results.</param>
        /// <param name="mode">The mode.</param>
        public Metrics(Inputs inputs, Results results, ExperimentMode mode)
        {
            this.UserName = inputs.UserName;

            if (results.Validation != null)
            {
                this.Validation = new MetricsSet(inputs.Validation, results.Validation, inputs.UserName, mode);
            }

            if (results.Test != null)
            {
                this.Test = new MetricsSet(inputs.Test, results.Test, inputs.UserName, mode);
            }
        }

        /// <summary>
        /// Gets or sets the user name.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the validation metrics.
        /// </summary>
        public MetricsSet Validation { get; set; }

        /// <summary>
        /// Gets or sets the test metrics.
        /// </summary>
        public MetricsSet Test { get; set; }

        /// <summary>
        /// Gets the user summary.
        /// </summary>
        public Summary UserSummary
        {
            get
            {
                return new Summary
                {
                    UserName = this.UserName,
                    AveragePrecisionValidation = this.Validation == null ? null : new Percentage(this.Validation.AveragePrecision),
                    AveragePrecisionTest = this.Test == null ? null : new Percentage( this.Test.AveragePrecision),
                    AreaUnderCurveValidation = this.Validation == null ? null : new Percentage ( this.Validation.AreaUnderCurve ),
                    AreaUnderCurveTest = this.Test == null ? null : new Percentage ( this.Test.AreaUnderCurve ),
                    ValidationReplyCount = this.Validation == null ? double.NaN : this.Validation.ReplyCount,
                    TestReplyCount = this.Test == null ? double.NaN : this.Test.ReplyCount
                };
            }
        }

        /// <summary>
        /// The summary.
        /// </summary>
        public struct Summary
        {
            /// <summary>
            /// Gets or sets the user name.
            /// </summary>
            public string UserName { get; set; }

            /// <summary>
            /// Gets or sets the average precision validation.
            /// </summary>
            public Percentage AveragePrecisionValidation { get; set; }

            /// <summary>
            /// Gets or sets the average precision test.
            /// </summary>
            public Percentage AveragePrecisionTest { get; set; }

            /// <summary>
            /// Gets or sets the area under curve Validation.
            /// </summary>
            public Percentage AreaUnderCurveValidation { get; set; }

            /// <summary>
            /// Gets or sets the area under curve test.
            /// </summary>
            public Percentage AreaUnderCurveTest { get; set; }

            /// <summary>
            /// Gets or sets the reply count.
            /// </summary>
            public double ValidationReplyCount { get; set; }

            /// <summary>
            /// Gets or sets the test reply count.
            /// </summary>
            public double TestReplyCount { get; set; }

            /// <summary>
            /// Gets the average.
            /// </summary>
            /// <param name="summaries">The summaries.</param>
            /// <returns>The <see cref="MetricsSet.SetSummary"/></returns>
            public static Summary GetAverage(IList<Summary> summaries)
            {
                return new Summary
                           {
                               UserName = "Average",
                               AveragePrecisionValidation =
                                   new Percentage (summaries.Average(ia => ia.AveragePrecisionValidation.Value) ),
                               AveragePrecisionTest =
                                   new Percentage (summaries.Average(ia => ia.AveragePrecisionTest.Value) ),
                               AreaUnderCurveValidation =
                                   new Percentage (summaries.Average(ia => ia.AreaUnderCurveValidation.Value) ),
                               AreaUnderCurveTest =
                                   new Percentage (summaries.Average(ia => ia.AreaUnderCurveTest.Value) ),
                               ValidationReplyCount = summaries.Average(ia => ia.ValidationReplyCount),
                               TestReplyCount = summaries.Average(ia => ia.TestReplyCount)
                           };
            }
        }

        /// <summary>
        /// The metrics set.
        /// </summary>
        public class MetricsSet
        {
            /// <summary>
            /// The bins for the calibration curve.
            /// </summary>
            private const int Bins = 11;

            /// <summary>
            /// The min bin instance count.
            /// </summary>
            private const int MinBinInstanceCount = 10;

            /// <summary>
            /// The evaluator.
            /// </summary>
            private static readonly Evaluator Evaluator = new Evaluator(new EvaluatorMapping(InstanceSource));

            /// <summary>
            /// Initializes a new instance of the <see cref="MetricsSet"/> class.
            /// </summary>
            public MetricsSet()
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="MetricsSet" /> class.
            /// </summary>
            /// <param name="inputsSet">The inputs set.</param>
            /// <param name="resultsSet">The results set.</param>
            /// <param name="userName">Name of the user.</param>
            /// <param name="mode">The mode.</param>
            public MetricsSet(Inputs.DataSet inputsSet, Results.ResultsSet resultsSet, string userName, ExperimentMode mode)
            {
                this.UserName = userName;

                this.DataSetSize = inputsSet.Instances.Count;

                var instances = inputsSet.Instances;

                var instanceScores =
                    instances.Zip(resultsSet.IsRepliedProbTrue, (x, y) => new KeyValuePair<Inputs.Instance, double>(x, y)).ToArray();

                var positiveInstances = inputsSet.PositiveInstances;

                this.FractionPositive = (double)positiveInstances.Count / instances.Count;

                var calibrationCurve =
                    Evaluator.CalibrationCurve(true, inputsSet, resultsSet.PredictionDicts, Bins, MinBinInstanceCount)
                        .Select(ia => new Point(ia.First, ia.Second));

                var precisionRecallCurve = InferMetrics.PrecisionRecallCurve(positiveInstances, instanceScores).ToList();

                this.AreaUnderCurve = InferMetrics.AreaUnderRocCurve(positiveInstances, instanceScores);
                this.AveragePrecision = ComputeAveragePrecision(precisionRecallCurve, 0.1, 0.9);
                this.CalibrationError = Math.Sqrt(calibrationCurve.Select(ia => Math.Pow(ia.X - ia.Y, 2)).Average());

                if (mode != ExperimentMode.Online && mode != ExperimentMode.Incremental)
                {
                    this.CalibrationCurve = calibrationCurve.ToArray();

                    // Save some memory by not calculating these in online mode
                    this.PrecisionRecallCurve = precisionRecallCurve.Select(ia => new Point(ia.First, ia.Second)).ToArray();

                    this.PrecisionRecallRandomCurve = new[] { new Point(0.0, this.FractionPositive), new Point(1.0, this.FractionPositive) };

                    this.RocCurve =
                        InferMetrics.ReceiverOperatingCharacteristicCurve(instances.Where(ia => ia.Label), instanceScores)
                            .Select(ia => new Point(ia.First, ia.Second))
                            .ToArray();
                }

                this.ReplyCount = inputsSet.PositiveInstances.Count;
                this.ReplyFraction = (double)this.ReplyCount / inputsSet.Count;
            }

            /// <summary>
            /// Gets or sets the precision recall curve.
            /// </summary>
            public Point[] PrecisionRecallCurve { get; set; }

            /// <summary>
            /// Gets or sets the precision recall random curve.
            /// </summary>
            public Point[] PrecisionRecallRandomCurve { get; set; }

            /// <summary>
            /// Gets or sets the fraction positive.
            /// </summary>
            public double FractionPositive { get; set; }

            /// <summary>
            /// Gets or sets the average precision.
            /// </summary>
            public double AveragePrecision { get; set; }

            /// <summary>
            /// Gets or sets the calibration curve.
            /// </summary>
            public Point[] CalibrationCurve { get; set; }

            /// <summary>
            /// Gets or sets the calibration error.
            /// </summary>
            public double CalibrationError { get; set; }

            /// <summary>
            /// Gets or sets the roc curve.
            /// </summary>
            public Point[] RocCurve { get; set; }

            /// <summary>
            /// Gets or sets the area under curve.
            /// </summary>
            public double AreaUnderCurve { get; set; }

            /// <summary>
            /// Gets or sets the user name.
            /// </summary>
            public string UserName { get; set; }

            /// <summary>
            /// Gets or sets the reply count.
            /// </summary>
            public int ReplyCount { get; set; }

            /// <summary>
            /// Gets or sets the reply fraction.
            /// </summary>
            public double ReplyFraction { get; set; }

            /// <summary>
            /// Gets or sets the data set size.
            /// </summary>
            public int DataSetSize { get; set; }

            /// <summary>
            /// Gets the metric summary for this user.
            /// </summary>
            public SetSummary UserSetSummary
            {
                get
                {
                    return new SetSummary
                               {
                                   UserName = this.UserName,
                                   AveragePrecision = new Percentage( this.AveragePrecision ),
                                   AreaUnderCurve = new Percentage ( this.AreaUnderCurve )
                               };
                }
            }

            /// <summary>
            /// The instance source.
            /// </summary>
            /// <param name="dataSet">The data set.</param>
            /// <returns>
            /// The <see cref="IEnumerable{Instance}" />.
            /// </returns>
            private static IEnumerable<Inputs.Instance> InstanceSource(Inputs.DataSet dataSet)
            {
                return dataSet.Instances;
            }
            
            /// <summary>
            /// Computes the average precision.
            /// </summary>
            /// <param name="precisionRecallCurve">The precision recall curve.</param>
            /// <param name="lowerBound">The lower bound.</param>
            /// <param name="upperBound">The upper bound.</param>
            /// <returns>The average precision.</returns>
            private static double ComputeAveragePrecision(IEnumerable<Pair<double, double>> precisionRecallCurve, double lowerBound = 0.0, double upperBound = 1.0)
            {
                if (lowerBound < 0.0 || lowerBound > 1.0)
                {
                    throw new ArgumentOutOfRangeException("lowerBound", @"should be in the range [0,1]");
                }

                if (upperBound < 0.0 || upperBound > 1.0)
                {
                    throw new ArgumentOutOfRangeException("upperBound", @"should be in the range [0,1]");
                }

                if (lowerBound > upperBound)
                {
                    double tmp = lowerBound;
                    lowerBound = upperBound;
                    upperBound = tmp;
                }

                var inRange = precisionRecallCurve.Where(ia => ia.First > lowerBound && ia.First < upperBound).ToList();

                double x1 = inRange[0].First;
                double x2 = inRange.Last().First;

                double area = inRange.Select(ia => new MBMLCommon.Point(ia.First, ia.Second)).Integrate();

                return area / (x2 - x1);
            }

            /// <summary>
            /// The summary.
            /// </summary>
            public struct SetSummary
            {
                /// <summary>
                /// Gets or sets the user name.
                /// </summary>
                public string UserName { get; set; }

                /// <summary>
                /// Gets or sets the average precision.
                /// </summary>
                public Percentage AveragePrecision { get; set; }

                /// <summary>
                /// Gets or sets the area under curve.
                /// </summary>
                public Percentage AreaUnderCurve { get; set; }

                /// <summary>
                /// Gets the average.
                /// </summary>
                /// <param name="summaries">The summaries.</param>
                /// <returns>The <see cref="SetSummary"/></returns>
                public static SetSummary GetAverage(IList<SetSummary> summaries)
                {
                    return new SetSummary
                               {
                                   UserName = "Average",
                                   AveragePrecision =
                                       new Percentage (summaries.Average(ia => ia.AveragePrecision.Value) ),
                                   AreaUnderCurve =
                                       new Percentage (summaries.Average(ia => ia.AreaUnderCurve.Value) )
                               };
                }
            }
        }
        
        #region Evaluator Mapping
        /// <summary>
        /// The evaluator mapping. Required to use Infer.NET metrics
        /// </summary>
        public class EvaluatorMapping : IClassifierEvaluatorMapping<Inputs.DataSet, Inputs.Instance, object, bool>
        {
            /// <summary>
            /// The instance source getter
            /// </summary>
            private readonly Func<Inputs.DataSet, IEnumerable<Inputs.Instance>> instanceSourceGetter;

            /// <summary>
            /// Initializes a new instance of the <see cref="EvaluatorMapping"/> class.
            /// </summary>
            /// <param name="instanceSourceGetter">The instance source getter.</param>
            public EvaluatorMapping(Func<Inputs.DataSet, IEnumerable<Inputs.Instance>> instanceSourceGetter)
            {
                this.instanceSourceGetter = instanceSourceGetter;
            }

            /// <summary>
            /// Gets the instances.
            /// </summary>
            /// <param name="instanceSource">The instance source.</param>
            /// <returns>
            /// "The IEnumerable{Instance}."
            /// </returns>
            public IEnumerable<Inputs.Instance> GetInstances(Inputs.DataSet instanceSource)
            {
                return this.instanceSourceGetter(instanceSource);
            }

            /// <summary>
            /// Gets the label.
            /// </summary>
            /// <param name="instance">The instance.</param>
            /// <param name="instanceSource">The instance source.</param>
            /// <param name="labelSource">The label source.</param>
            /// <returns>
            /// "The System.Boolean."
            /// </returns>
            public bool GetLabel(Inputs.Instance instance, Inputs.DataSet instanceSource = null, object labelSource = null)
            {
                return instance.Label;
            }

            /// <summary>
            /// Gets the class labels.
            /// </summary>
            /// <param name="instanceSource">The instance source.</param>
            /// <param name="labelSource">The label source.</param>
            /// <returns>
            /// "The IEnumerable{System.Boolean}."
            /// </returns>
            public IEnumerable<bool> GetClassLabels(Inputs.DataSet instanceSource = null, object labelSource = null)
            {
                return new[] { true, false };
            }
        }
        #endregion
    }
}
