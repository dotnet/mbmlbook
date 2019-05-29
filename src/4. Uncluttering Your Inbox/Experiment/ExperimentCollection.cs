// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows;

    using MBMLViews;

    using Microsoft.ML.Probabilistic.Distributions;
#if NETFULL
    using Point = System.Windows.Point;
#else
    using Point = MBMLCommon.Point;
#endif

    /// <summary>
    /// The inputs collection.
    /// </summary>
    [Serializable]
    public class ExperimentCollection
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the experiments.
        /// </summary>
        public List<Experiment> Experiments { get; set; }

        /// <summary>
        /// Gets the online experiments.
        /// </summary>
        public List<Experiment> OnlineExperiments
        {
            get
            {
                if (this.Experiments == null || this.Experiments.Count == 0)
                {
                    return null;
                }
                
                var experiments = this.Experiments.Where(ia => ia.Mode == ExperimentMode.Online || ia.Mode == ExperimentMode.Incremental).ToList();
                
                ////if (this.ReferenceExperiments != null && this.ReferenceExperiments.Count > 0)
                ////{
                ////    experiments.AddRange(this.ReferenceExperiments);
                ////}

                return experiments;
            }
        }

        #region Superset of experiments
        /// <summary>
        /// Gets the online metrics.
        /// </summary>
        /// <value>
        /// All metrics.
        /// </value>
        [Browsable(false)]
        public List<List<Metrics.MetricsSet>> OnlineMetrics
        {
            get
            {
                if (this.OnlineExperiments == null || this.OnlineExperiments.Count == 0)
                {
                    return null;
                }

                int count = this.OnlineExperiments.Min(ia => ia.Metrics.Count);

                var allmetrics = new List<List<Metrics.MetricsSet>>();
                for (int i = 0; i < count; i++)
                {
                    allmetrics.Add(this.OnlineExperiments.Select(ia => ia.Metrics[i].Validation).ToList());
                }

                return allmetrics;
            }
        }

        /// <summary>
        /// Gets all overall metrics with average.
        /// </summary>
        /// <value>
        /// All overall metrics with average.
        /// </value>
        [Browsable(false)]
        public List<Dictionary<string, object>> AllOverallMetricsWithAverage
        {
            get
            {
                if (this.OnlineMetrics == null || this.OnlineMetrics.Count == 0)
                {
                    return null;
                }

                var functions = new Dictionary<string, Func<Metrics.MetricsSet, object>>
                                    {
                                        { "Average Precision", ia => new Bernoulli(ia.AveragePrecision) },
                                        { "Area Under Curve", ia => new Bernoulli(ia.AreaUnderCurve) },
                                        { "Calibration Error", ia => new Bernoulli(ia.CalibrationError) },
                                    };

                var aggregates = new Dictionary<string, Func<IList<object>, object>>
                    {
                        { "Average", ia => new Bernoulli(ia.Select(b => ((Bernoulli)b).GetProbTrue()).Average()) },
                    };

                return
                    this.OnlineMetrics.Select(
                        m => m.ToDictionaryForTable(ia => ia.UserName, functions, false, aggregates, string.Empty)).ToList();
            }
        }

        /// <summary>
        /// Gets the average data set size.
        /// </summary>
        public double AverageDataSetSize
        {
            get
            {
                return this.ValidationMetrics == null || this.ValidationMetrics.Count == 0
                           ? double.NaN
                           : this.ValidationMetrics.Average(ia => ia.DataSetSize);
            }
        }

        /// <summary>
        /// Gets the average precision online.
        /// </summary>
        public Dictionary<string, Point[]> AveragePrecisionOnline
        {
            get
            {
                if (this.OnlineExperiments == null || this.OnlineExperiments.Count == 0)
                {
                    return null;
                }
                
                return this.OnlineExperiments.ToDictionary(
                    ia => ia.UserName,
                    ia => ia.Metrics.Select((m, i) => new Point((i + 1) * ia.BatchSize, m.Validation.AveragePrecision)).ToArray());
            }
        }

        /// <summary>
        /// Gets the average train timing curve.
        /// </summary>
        public Point[] AverageTrainTimingCurve
        {
            get
            {
                if (this.OnlineExperiments == null || this.OnlineExperiments.Count == 0)
                {
                    return null;
                }

                int maxBatches = this.OnlineExperiments.Max(ia => ia.Metrics.Count);
                int maxBatchSize = this.OnlineExperiments.Max(ia => ia.BatchSize);

                var averages = new List<Point>();

                for (int i = 0; i < maxBatches; i++)
                {
                    averages.Add(
                        new Point(
                            (i + 1) * maxBatchSize,
                            this.OnlineExperiments.Where(ia => ia.TrainTimings.Count > i)
                                .Average(ia => ia.TrainTimings[i].TotalSeconds)));
                }

                return averages.ToArray();
            }
        }

        /// <summary>
        /// Gets the average precision online average.
        /// </summary>
        public Point[] AveragePrecisionOnlineAverage
        {
            get
            {
                if (this.OnlineExperiments == null || this.OnlineExperiments.Count == 0)
                {
                    return null;
                }

                int maxBatches = this.OnlineExperiments.Max(ia => ia.Metrics.Count);
                int maxBatchSize = this.OnlineExperiments.Max(ia => ia.BatchSize);

                var averages = new List<Point>();

                for (int i = 0; i < maxBatches; i++)
                {
                    averages.Add(
                        new Point(
                            (i + 1) * maxBatchSize,
                            this.OnlineExperiments.Where(ia => ia.Metrics.Count > i)
                                .Average(ia => ia.Metrics[i].Validation.AveragePrecision)));
                }

                return averages.ToArray();
            }
        }

        /// <summary>
        /// Gets the area under curve online average.
        /// </summary>
        public Point[] AreaUnderCurveOnlineAverage
        {
            get
            {
                if (this.OnlineExperiments == null || this.OnlineExperiments.Count == 0)
                {
                    return null;
                }

                int maxBatches = this.OnlineExperiments.Max(ia => ia.Metrics.Count);
                int maxBatchSize = this.OnlineExperiments.Max(ia => ia.BatchSize);

                var averages = new List<Point>();

                for (int i = 0; i < maxBatches; i++)
                {
                    averages.Add(
                        new Point(
                            (i + 1) * maxBatchSize,
                            this.OnlineExperiments.Where(ia => ia.Metrics.Count > i)
                                .Average(ia => ia.Metrics[i].Validation.AreaUnderCurve)));
                }

                return averages.ToArray();
            }
        }

        /// <summary>
        /// Gets the area under curve online.
        /// </summary>
        public Dictionary<string, Point[]> AreaUnderCurveOnline
        {
            get
            {
                if (this.OnlineExperiments == null || this.OnlineExperiments.Count == 0)
                {
                    return null;
                }

                return this.OnlineExperiments.ToDictionary(
                    ia => ia.UserName,
                    ia => ia.Metrics.Select((m, i) => new Point((i + 1) * ia.BatchSize, m.Validation.AreaUnderCurve)).ToArray());
            }
        }

        #endregion

        /// <summary>
        /// Gets the single metrics sets (Validation).
        /// </summary>
        public IList<Metrics.MetricsSet> ValidationMetrics
        {
            get
            {
                return this.Experiments == null
                           ? null
                           : (from experiment in this.Experiments
                              where experiment.Metrics != null && experiment.Metrics.Count > 0
                              select experiment.Metrics.Last().Validation).ToList();
            }
        }

        /// <summary>
        /// Gets the test metrics.
        /// </summary>
        public IList<Metrics.MetricsSet> TestMetrics
        {
            get
            {
                return this.Experiments == null
                           ? null
                           : (from experiment in this.Experiments
                              where experiment.Metrics != null && experiment.Metrics.Count > 0
                              select experiment.Metrics.Last().Test).ToList();
            }
        }

        /// <summary>
        /// Gets the overall metrics.
        /// </summary>
        /// <value>
        /// The overall metrics.
        /// </value>
        public IList<Metrics.MetricsSet.SetSummary> OverallMetricsWithAverage
        {
            get
            {
                if (this.Experiments == null)
                {
                    return null;
                }

                var summaries = this.ValidationMetrics.Select(ia => ia.UserSetSummary).ToList();
                summaries.Add(Metrics.MetricsSet.SetSummary.GetAverage(summaries));
                return summaries.ToArray();
            }
        }

        /// <summary>
        /// Gets the overall metrics with test and average.
        /// </summary>
        public IList<Metrics.Summary> OverallMetricsWithTestAndAverage
        {
            get
            {
                if (this.Experiments == null)
                {
                    return null;
                }

                var metrics = (from experiment in this.Experiments
                              where experiment.Metrics != null && experiment.Metrics.Count > 0
                              select experiment.Metrics.Last()).ToList();

                var summaries = metrics.Select(ia => ia.UserSummary).ToList();
                summaries.Add(Metrics.Summary.GetAverage(summaries));

                return summaries.ToArray();
            }
        }

        /// <summary>
        /// Gets the average precision.
        /// </summary>
        /// <value>
        /// The average precision.
        /// </value>
        public Dictionary<string, object> AveragePrecision
        {
            get
            {
                if (this.Experiments == null)
                {
                    return null;
                }

                var functions = new Dictionary<string, Func<Metrics.MetricsSet, object>>
                                    {
                                        { "Average Precision", ia => new Bernoulli(ia.AveragePrecision) }
                                    };

                var aggregates = new Dictionary<string, Func<IList<object>, object>>
                                     {
                                         {
                                             "Average",
                                             ia =>
                                             new Bernoulli(
                                                 ia.Select(b => ((Bernoulli)b).GetProbTrue())
                                                 .Average())
                                         }
                                     };

                return this.ValidationMetrics.ToDictionaryForTable(ia => ia.UserName, functions, false, aggregates, string.Empty);
            }
        }

        /// <summary>
        /// Gets the roc curves.
        /// </summary>
        /// <value>
        /// The roc curves.
        /// </value>
        public Dictionary<string, Point[]> RocCurves
        {
            get
            {
                if (this.Experiments == null)
                {
                    return null;
                }

                var rocCurves = this.GetExperimentMetric(
                    ia => string.Format("{0} (AUC={1})", ia.UserName, ia.AreaUnderCurve.ToString("P1")),
                    ia => ia.RocCurve);

                rocCurves["Random (AUC=50.0%)"] = new[] { new Point(0.0, 0.0), new Point(1.0, 1.0) };

                return rocCurves;
            }
        }

        /// <summary>
        /// Gets the precision recall curves.
        /// </summary>
        /// <value>
        /// The precision recall curves.
        /// </value>
        public Dictionary<string, Point[]> PrecisionRecallCurves
        {
            get
            {
                return this.Experiments == null
                           ? null
                           : this.GetExperimentMetric(
                               ia => string.Format("{0} (AP={1}, reply={2})", ia.UserName, ia.AveragePrecision.ToString("P1"), ia.ReplyFraction.ToString("P1")),
                               ia => ia.PrecisionRecallCurve);
            }
        }

        /// <summary>
        /// Gets the precision recall test curves.
        /// </summary>
        public Dictionary<string, Point[]> PrecisionRecallTestCurves
        {
            get
            {
                if (this.TestMetrics == null || this.TestMetrics.Count == 0)
                {
                    return null;
                }

                Func<Metrics.MetricsSet, string> keyFunc =
                    ia =>
                    string.Format(
                        "{0} (AP={1}, reply={2})",
                        ia.UserName,
                        ia.AveragePrecision.ToString("P1"),
                        ia.ReplyFraction.ToString("P1"));

                Func<Metrics.MetricsSet, Point[]> valueFunc = ia => ia.PrecisionRecallCurve;

                return this.TestMetrics.ToDictionary(keyFunc, valueFunc);
            }
        }

        /// <summary>
        /// Gets the precision recall random curves.
        /// </summary>
        public Dictionary<string, Point[]> PrecisionRecallRandomCurves
        {
            get
            {
                return this.Experiments == null
                           ? null
                           : this.GetExperimentMetric(
                               ia => string.Format("{0} (AP={1}, reply={2})", ia.UserName, ia.AveragePrecision.ToString("N2"), ia.ReplyFraction.ToString("P1")),
                               ia => ia.PrecisionRecallRandomCurve);
            }
        }

        /// <summary>
        /// Gets the calibration curves.
        /// </summary>
        /// <value>
        /// The calibration curves.
        /// </value>
        public Dictionary<string, Point[]> CalibrationCurves
        {
            get
            {
                if (this.Experiments == null)
                {
                    return null;
                }

                var calibrationCurves =
                    this.GetExperimentMetric(
                        ia => string.Format("{0} (RMSE={1})", ia.UserName, ia.CalibrationError.ToString("N3")),
                        ia => ia.CalibrationCurve).ToList();

                calibrationCurves.Insert(0, new KeyValuePair<string, Point[]>("Perfect calibration", new[] { new Point(0, 0), new Point(1, 1) }));

                return calibrationCurves.ToDictionary(ia => ia.Key, ia => ia.Value);
            }
        }

        /// <summary>
        /// Gets the calibration curves with average.
        /// </summary>
        public Dictionary<string, Point[]> CalibrationCurvesWithAverage
        {
            get
            {
                if (this.CalibrationCurves == null)
                {
                    return null;
                }

                var calibrationCurves = this.CalibrationCurves.Where(ia => ia.Key != "Perfect calibration").ToList();
                var average = this.GetAverageCalibrationCurve(calibrationCurves);
                calibrationCurves.Add(average);
                calibrationCurves.Insert(0, new KeyValuePair<string, Point[]>("Perfect calibration", new[] { new Point(0, 0), new Point(1, 1) }));

                return calibrationCurves.ToDictionary(ia => ia.Key, ia => ia.Value);
            }
        }

        /// <summary>
        /// Gets the average calibration curve.
        /// </summary>
        public Dictionary<string, Point[]> AverageCalibrationCurve
        {
            get
            {
                if (this.CalibrationCurves == null)
                {
                    return null;
                }

                var calibrationCurves = this.CalibrationCurves.Where(ia => ia.Key != "Perfect calibration").ToList();

                var average = this.GetAverageCalibrationCurve(calibrationCurves);

                var averageCalibrationCurves = new Dictionary<string, Point[]>
                                                   {
                                                       { "Perfect calibration", new[] { new Point(0, 0), new Point(1, 1) } },
                                                       { average.Key, average.Value }
                                                   };
                
                return averageCalibrationCurves;
            }
        }

        /// <summary>
        /// Gets the shared posterior means.
        /// </summary>
        public Dictionary<string, Dictionary<string, double>> SharedPosteriorMeans
        {
            get
            {
                return this.Experiments == null
                           ? null
                           : this.Experiments.Where(ia => ia.Mode == ExperimentMode.Offline)
                                 .ToDictionary(ia => ia.UserName, ia => ia.Results[0].Posteriors.SharedMeans);
            }
        }

        /// <summary>
        /// Adds the specified experiment.
        /// </summary>
        /// <param name="experiment">The experiment.</param>
        public void Add(Experiment experiment)
        {
            if (this.Experiments == null)
            {
                this.Experiments = new List<Experiment>();
            }

            this.Experiments.Add(experiment);
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            if (this.Experiments != null)
            {
                this.Experiments.Clear();
            }
        }

        /// <summary>
        /// Gets the average calibration curve.
        /// </summary>
        /// <param name="calibrationCurves">The calibration curves.</param>
        /// <returns>The average calibration curve.</returns>
        private KeyValuePair<string, Point[]> GetAverageCalibrationCurve(IEnumerable<KeyValuePair<string, Point[]>> calibrationCurves)
        {
            Func<double, double> closePoints =
                x => calibrationCurves.SelectMany(ia => ia.Value.Where(p => Math.Abs(p.X - x) < double.Epsilon).Select(p => p.Y)).Average();
            
            var xvalues = calibrationCurves.SelectMany(ia => ia.Value.Select(p => p.X)).Distinct().OrderBy(ia => ia);
            var average = xvalues.Select(x => new Point(x, closePoints(x))).ToArray();

            var error = this.ValidationMetrics.Average(ia => ia.CalibrationError);

            return new KeyValuePair<string, Point[]>(string.Format("Average (RMSE={0})", error.ToString("N3")), average);
        }

        /// <summary>
        /// Gets the experiment metric.
        /// </summary>
        /// <typeparam name="TMetric">The type of the metric.</typeparam>
        /// <param name="keyFunc">The key function.</param>
        /// <param name="valueFunc">The value function.</param>
        /// <returns>
        /// The <see cref="Dictionary{TKey,TValue}" />
        /// </returns>
        private Dictionary<string, TMetric> GetExperimentMetric<TMetric>(Func<Metrics.MetricsSet, string> keyFunc, Func<Metrics.MetricsSet, TMetric> valueFunc)
        {
            return this.ValidationMetrics == null ? null : this.ValidationMetrics.ToDictionary(keyFunc, valueFunc);
        }
    }
}