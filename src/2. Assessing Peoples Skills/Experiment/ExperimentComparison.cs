// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AssessingPeoplesSkills
{
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Research.Glo.ObjectModel;

    /// <summary>
    /// The experiment collection.
    /// </summary>
    public class ExperimentComparison
    {
        /// <summary>
        /// The experiments.
        /// </summary>
        private KeyedCollectionWithFunc<string, Experiment> experiments =
            new KeyedCollectionWithFunc<string, Experiment>(ia => ia.ModelName);

        /// <summary>
        /// Initializes a new instance of the <see cref="ExperimentComparison"/> class.
        /// </summary>
        public ExperimentComparison()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExperimentComparison"/> class.
        /// </summary>
        /// <param name="items">
        /// The items.
        /// </param>
        public ExperimentComparison(IEnumerable<Experiment> items)
        {
            this.experiments = new KeyedCollectionWithFunc<string, Experiment>(items, ia => ia.ModelName);
        }

        /// <summary>
        /// Gets or sets the experiments.
        /// </summary>
        public KeyedCollectionWithFunc<string, Experiment> Experiments
        {
            get
            {
                return this.experiments;
            }

            set
            {
                this.experiments = value;
            }
        }

        /// <summary>
        /// Gets the negative log probability of truth.
        /// </summary>
        /// <value>
        /// The negative log probability of truth.
        /// </value>
        public Dictionary<string, Dictionary<string, double>> NegativeLogProbabilityOfTruth
        {
            get
            {
                return this.Experiments.Where(ia => ia.Model.IsReal).OrderBy(ia => ia.Model.Index)
                           .ToDictionary(ia => ia.ModelName, ia => ia.Metrics.NegativeLogProbabilityOfTruthAsDictionary);
            }
        }

        /// <summary>
        /// Gets the negative log probability of truth per skill.
        /// </summary>
        /// <value>
        /// The negative log probability of truth per skill.
        /// </value>
        public Dictionary<string, Dictionary<string, double>> NegativeLogProbabilityOfTruthPerSkill
        {
            get
            {
                return this.Experiments.Where(ia => ia.Model.IsReal).OrderBy(ia => ia.Model.Index)
                           .ToDictionary(ia => ia.ModelName, ia => ia.Metrics.NegativeLogProbabilityOfTruthPerSkill);
            }
        }

        /// <summary>
        /// Gets the roc curves
        /// </summary>
#if NETFULL
        public Dictionary<string, System.Windows.Point[]> RocCurves
#else
        public Dictionary<string, MBMLCommon.Point[]> RocCurves
#endif
        {
            get
            {
                return
                    this.Experiments.Where(ia => !ia.Model.Name.StartsWith("Sample"))
                        .OrderBy(ia => ia.Model.Index)
                        .ToDictionary(
                            ia =>
                            string.Format("{0} (AUC={1})", ia.ModelName, ia.Metrics.AreaUnderCurve.ToString("P1")),
                            ia => 
                            ia.Metrics.ReceiverOperatingCharacteristicPoints
#if NETFULL
                            .Select(p => new System.Windows.Point(p.X, p.Y)).ToArray()
#endif
                            );
            }
        }
    }
}
