// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace MakingRecommendations
{
    /// <summary>
    /// The struct which contains calculated metrics of an experiment.
    /// </summary>
    public struct MetricValues
    {
        public IDictionary<string, double> CorrectFractions { get; }
        public IDictionary<string, double> Ndcgs { get; }
        public IDictionary<string, double> Maes { get; }

        /// <summary>
        /// A constructor of the struct for experiments which not support MAE calculation.
        /// </summary>
        /// <param name="correctFractions">Fraction of correct predictions</param>
        /// <param name="ndcgs">Normalized Discounted Cumulative Gain values </param>
        public MetricValues(IDictionary<string, double> correctFractions, IDictionary<string, double> ndcgs)
        {
            CorrectFractions = correctFractions;
            Ndcgs = ndcgs;
            Maes = null;
        }

        /// <summary>
        /// A constructor of the struct for experiments which support MAE calculation.
        /// </summary>
        /// <param name="correctFractions">Fraction of correct predictions</param>
        /// <param name="ndcgs">Normalized Discounted Cumulative Gain values </param>
        public MetricValues(IDictionary<string, double> correctFractions, IDictionary<string, double> ndcgs, IDictionary<string, double> maes) : this(correctFractions, ndcgs)
        {
            Maes = maes;
        }
    }
}
