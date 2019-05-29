// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Serialization;

    using Microsoft.ML.Probabilistic.Distributions;

    using GaussianCollection = System.Collections.Generic.Dictionary<string, Microsoft.ML.Probabilistic.Distributions.Gaussian>;

    /// <summary>
    /// The results.
    /// </summary>
    public class Results
    {
        /// <summary>
        /// Gets or sets the train.
        /// </summary>
        [XmlIgnore]
        public ResultsSet Train { get; set; }

        /// <summary>
        /// Gets or sets the validation.
        /// </summary>
        public ResultsSet Validation { get; set; }

        /// <summary>
        /// Gets or sets the test.
        /// </summary>
        public ResultsSet Test { get; set; }

        /// <summary>
        /// Gets or sets the posteriors.
        /// </summary>
        public Posteriors Posteriors { get; set; }

        /// <summary>
        /// Gets or sets the community posteriors.
        /// </summary>
        public CommunityPosteriors CommunityPosteriors { get; set; }

        /// <summary>
        /// The results set.
        /// </summary>
        public class ResultsSet
        {
            /// <summary>
            /// Gets or sets the is replied to.
            /// </summary>
            public Bernoulli[] IsRepliedTo { get; set; }

            /// <summary>
            /// Gets the is replied log probability of truth.
            /// </summary>
            public double[] IsRepliedLogProbTrue
            {
                get
                {
                    return this.IsRepliedTo == null ? null : this.IsRepliedTo.Select(ia => ia.GetLogProbTrue()).ToArray();
                }
            }

            /// <summary>
            /// Gets the is replied probability true.
            /// </summary>
            public double[] IsRepliedProbTrue
            {
                get
                {
                    return this.IsRepliedTo == null ? null : this.IsRepliedLogProbTrue.Select(Math.Exp).ToArray();
                }
            }

            /// <summary>
            /// Gets the predictions.
            /// </summary>
            public IList<bool> Predictions
            {
                get
                {
                    return this.IsRepliedProbTrue.Select(ia => ia > 0.5).ToArray();
                }
            }
            
            /// <summary>
            /// Gets the prediction dictionaries. Used in calculating calibration curves.
            /// </summary>
            public IEnumerable<IDictionary<bool, double>> PredictionDicts
            {
                get
                {
                    return this.IsRepliedProbTrue.Select(p => new Dictionary<bool, double> { { true, p }, { false, 1 - p } });
                }
            }
        }
    }
}