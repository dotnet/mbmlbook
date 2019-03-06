// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AssessingPeoplesSkills
{
    using System.Collections.Generic;

    using Microsoft.ML.Probabilistic.Learners.Mappings;

    /// <summary>
    /// The evaluator mapping. Required to use Infer.NET metrics
    /// </summary>
    public class EvaluatorMapping :
        IClassifierEvaluatorMapping<IEnumerable<Instance>, Instance, IEnumerable<KeyValuePair<Instance, double>>, bool>
    {
        /// <summary>
        /// Gets the instances.
        /// </summary>
        /// <param name="instanceSource">The instance source.</param>
        /// <returns>
        /// "The IEnumerable{Instance}."
        /// </returns>
        public IEnumerable<Instance> GetInstances(IEnumerable<Instance> instanceSource)
        {
            return instanceSource;
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
        public bool GetLabel(Instance instance, IEnumerable<Instance> instanceSource = null, IEnumerable<KeyValuePair<Instance, double>> labelSource = null)
        {
            return instance.Measurement;
        }

        /// <summary>
        /// Gets the class labels.
        /// </summary>
        /// <param name="instanceSource">The instance source.</param>
        /// <param name="labelSource">The label source.</param>
        /// <returns>
        /// "The IEnumerable{System.Boolean}."
        /// </returns>
        public IEnumerable<bool> GetClassLabels(IEnumerable<Instance> instanceSource = null, IEnumerable<KeyValuePair<Instance, double>> labelSource = null)
        {
            return new[] { true, false };
        }
    }
}