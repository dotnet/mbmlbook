// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MeetingYourMatch.Models
{
    using System.Collections.Generic;

    using Items;

    /// <summary>
    /// The model base.
    /// </summary>
    public interface IModel
    {   
        /// <summary>
        /// Gets the name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Trains the specified game.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <param name="players">The players.</param>
        /// <param name="priors">The priors.</param>
        /// <returns>The <see cref="Results"/>.</returns>
        Results Train(Game game, IList<string> players, Marginals priors);

        /// <summary>
        /// Trains the specified games.
        /// </summary>
        /// <param name="games">The games.</param>
        /// <param name="players">The players.</param>
        /// <param name="priors">The priors.</param>
        /// <returns>The <see cref="IList{Results}"/>.</returns>
        IList<Results> Train(IList<Game> games, IList<string> players, Marginals priors);

        /// <summary>
        /// Predicts the outcome of a game.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <param name="posteriors">The posteriors.</param>
        /// <returns>
        /// The <see cref="Prediction" />.
        /// </returns>
        Prediction PredictOutcome(Game game, Marginals posteriors);
    }

    /// <summary>
    /// The HasParameters interface.
    /// </summary>
    /// <typeparam name="TParameters">The type of the parameters.</typeparam>
    public interface IHasParameters<TParameters>
    {
        /// <summary>
        /// Gets or sets the parameters.
        /// </summary>
        TParameters Parameters { get; set; }
    }
}