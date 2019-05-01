// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MeetingYourMatch.Models.TrueSkill
{
    using System.Collections.Generic;
    using System.Linq;

    using global::MeetingYourMatch.Experiments;
    using global::MeetingYourMatch.Items;

    /// <summary>
    /// The model base.
    /// </summary>
    public abstract class ModelBase : IModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModelBase"/> class.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        protected ModelBase(IModelParameters parameters)
        {
            this.Parameters = (TrueSkillParameters)parameters;
        }

        /// <summary>
        /// Gets or sets the parameters.
        /// </summary>
        public TrueSkillParameters Parameters { get; set; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name
        {
            get
            {
                return this.GetType().Name;
            }
        }

        /// <summary>
        /// Trains the specified game.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <param name="players">The players.</param>
        /// <param name="priors">The priors.</param>
        /// <returns>The <see cref="Results"/>.</returns>
        public abstract Results Train(Game game, IList<string> players, Marginals priors);

        /// <summary>
        /// Trains the specified games.
        /// </summary>
        /// <param name="games">The games.</param>
        /// <param name="players">The players.</param>
        /// <param name="priors">The priors.</param>
        /// <returns>The <see cref="IList{Results}"/>.</returns>
        public IList<Results> Train(IList<Game> games, IList<string> players, Marginals priors)
        {
            var results = new List<Results> { new Results { Posteriors = priors } };
            foreach (var game in games)
            {
                results.Add(this.Train(game, players, results.Last().Posteriors));
            }

            return results;
        }

        /// <summary>
        /// Predicts the outcome of a game.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <param name="posteriors">The posteriors.</param>
        /// <returns>
        /// The <see cref="Prediction" />.
        /// </returns>
        public abstract Prediction PredictOutcome(Game game, Marginals posteriors);

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.Name + string.Format("[{0}]", this.Parameters);
        }
    }
}
