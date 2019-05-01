// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MeetingYourMatch.Experiments
{
    using System;
    using System.Collections.Generic;

    using Microsoft.Research.Glo.ObjectModel;

    using Microsoft.ML.Probabilistic.Collections;

    using MeetingYourMatch.Items;

    /// <summary>
    /// The experiment collection.
    /// </summary>
    /// <typeparam name="TExperiment">The type of the experiment.</typeparam>
    /// <typeparam name="TGame">The type of the game.</typeparam>
    public class ExperimentComparison<TExperiment, TGame>
        where TExperiment : Experiment
        where TGame : Game
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExperimentComparison{TExperiment,TGame}"/> class. 
        /// </summary>
        public ExperimentComparison()
        {
            this.Experiments = new KeyedCollectionWithFunc<string, TExperiment>(ia => ia.Name);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExperimentComparison{TExperiment,TGame}"/> class. 
        /// </summary>
        /// <param name="experiments">
        /// The experiments.
        /// </param>
        public ExperimentComparison(IEnumerable<TExperiment> experiments)
            : this()
        {
            if (experiments != null)
            {
                this.Experiments.AddRange(experiments);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExperimentComparison{TExperiment,TGame}"/> class. 
        /// </summary>
        /// <param name="experiments">
        /// The experiments.
        /// </param>
        public ExperimentComparison(params TExperiment[] experiments)
            : this()
        {
            if (experiments != null)
            {
                this.Experiments.AddRange(experiments);
            }
        }

        /// <summary>
        /// Gets or sets the experiments.
        /// </summary>
        public KeyedCollectionWithFunc<string, TExperiment> Experiments { get; set; }

        /// <summary>
        /// Gets or sets the truth.
        /// </summary>
        public Dictionary<string, List<double>> Truth { get; set; }

        private void AnnounceExperiment(string name)
        {
            Console.WriteLine($"Running " + name);
        }
        /// <summary>
        /// Runs all experiments.
        /// </summary>
        /// <param name="inputs">The inputs.</param>
        /// <param name="verbose">if set to <c>true</c> [verbose].</param>
        public void AnnounceAndRunAll(Inputs<TGame> inputs, bool verbose = false)
        {
            foreach (var experiment in this.Experiments)
            {
                AnnounceExperiment(experiment.Name);
                experiment.Run(inputs.Games, inputs.Games.Count, verbose);
            }
        }

        /// <summary>
        /// Runs all experiments.
        /// </summary>
        /// <param name="games">The games.</param>
        /// <param name="count">The count.</param>
        /// <param name="verbose">if set to <c>true</c> [verbose].</param>
        public void AnnounceAndRunAll(IList<TGame> games, int count, bool verbose = false)
        {
            foreach (var experiment in this.Experiments)
            {
                AnnounceExperiment(experiment.Name);
                experiment.Run(games, count, verbose);
            }
        }
}
}
