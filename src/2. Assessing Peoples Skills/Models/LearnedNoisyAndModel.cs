// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AssessingPeoplesSkills.Models
{
    using System.Linq;

    using Microsoft.ML.Probabilistic.Distributions;
    using Microsoft.ML.Probabilistic.Models;

    /// <summary>
    /// Model with learned guess probability using Beta prior
    /// </summary>
    public class LearnedNoisyAndModel : NoisyAndModel
    {
        #region Public interface
        
        /// <summary>
        /// Gets or sets the guess prior.
        /// </summary>
        /// <value>
        /// The guess prior.
        /// </value>
        public Beta GuessPrior { get; set; }
        
        /// <summary>
        /// Gets the model index.
        /// </summary>
        public override byte Index
        {
            get { return (byte)(base.Index + 1); }
        }

        /// <summary>
        /// Gets the priors.
        /// </summary>
        protected override string Priors
        {
            get
            {
                return "Guess: " + this.GuessPrior.ToString().Split('[')[0];
            }
        }

        #endregion
        
        /// <summary>
        /// The to string.
        /// </summary>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public override string ToString()
        {
            return this.Name + ", " + this.Priors;
        }

        /// <summary>
        /// Does the inference.
        /// </summary>
        /// <param name="results">The results.</param>
        public override void DoInference(ref Results results)
        {
            base.DoInference(ref results);

            this.InferProbabilityOfGuess(ref results);
        }

        /// <summary>
        /// Sets the observed arrays.
        /// </summary>
        /// <param name="inputs">The inputs.</param>
        protected override void SetObservedArrays(Inputs inputs)
        {
            this.probabilityOfNotMistake.ClearObservedValue();
            this.probabilityOfNotMistake.ObservedValue = Enumerable.Repeat(this.ProbabilityOfNotMistake, this.NumberOfQuestions).ToArray();
            
            this.probabilityOfSkillTrue.ClearObservedValue();
            this.probabilityOfSkillTrue.ObservedValue = Enumerable.Repeat(this.ProbabilityOfSkillTrue, this.NumberOfSkills).ToArray();
        }

        /// <summary>
        /// Constructs the noisy factor.
        /// </summary>
        protected override void ConstructNoisyFactor()
        {
            using (Variable.ForEach(this.Questions))
            {
                this.probabilityOfGuess[this.Questions] = Variable.Random(this.GuessPrior);
            }

            base.ConstructNoisyFactor();
        }

        /// <summary>
        /// Infers the probability of guess.
        /// </summary>
        /// <param name="results">The results.</param>
        private void InferProbabilityOfGuess(ref Results results)
        {
            results.GuessPosteriors = this.Engine.Infer<Beta[]>(this.probabilityOfGuess);
        }
    }
}
