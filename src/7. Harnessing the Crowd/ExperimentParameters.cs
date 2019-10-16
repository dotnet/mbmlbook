// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarnessingTheCrowd
{
    /// <summary>
    ///     The model types.
    /// </summary>
    [Flags]
    public enum ModelTypes
    {
        /// <summary>
        ///     The majority vote model.
        /// </summary>
        MajorityVote = 0x01,

        /// <summary>
        ///     The honest worker model.
        /// </summary>
        HonestWorker = 0x02,

        /// <summary>
        ///     The biased worker model.
        /// </summary>
        BiasedWorker = 0x04,

        /// <summary>
        ///     The biased community model.
        /// </summary>
        BiasedCommunity = 0x08,

        /// <summary>
        ///     The biased community model with naive Bayes for words.
        /// </summary>
        BiasedCommunityWords = 0x10
    }

    /// <summary>
    ///     The experiment parameters.
    /// </summary>
    public class ExperimentParameters
    {
        /// <summary>
        ///     Gets or sets the experiment name prefix.
        /// </summary>
        public string ExperimentNamePrefix { get; set; } = string.Empty;

        /// <summary>
        ///     Gets or sets the random seed for the experiment.
        /// </summary>
        public int RandomSeed { get; set; } = 12347;

        /// <summary>
        ///     Gets or sets the fraction of gold labels to assign to the training set.
        ///     <see cref="UseGoldLabelsInTraining" /> determines whether they will actually
        ///     get used in training.
        /// </summary>
        public double FractionGoldLabelsReservedForTraining { get; set; } = 0.3;

        /// <summary>
        ///     Gets or sets a value indicating whether to use only tweets with gold labels in the training set.
        /// </summary>
        public bool UseOnlyTweetsWithGoldLabels { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether to use the training gold labels in training.
        /// </summary>
        public bool UseGoldLabelsInTraining { get; set; }

        /// <summary>
        ///     Gets or sets the number of tweets in the training set.
        /// </summary>
        public int NumberOfTrainingTweets { get; set; } = 10000;

        /// <summary>
        ///     Gets or sets the number of training set sizes to run for the progressive charts.
        /// </summary>
        public int NumDataSizes { get; set; } = 10;

        /// <summary>
        ///     Gets or sets a value that allows limiting of the total number of worker labels in the validation set.
        /// </summary>
        public int NumberOfValidationJudgments { get; set; } = int.MaxValue;

        /// <summary>
        ///     Gets or sets the sweep over communities in community models.
        /// </summary>
        public int[] NumberOfCommunitiesSweep { get; set; } = { 1, 2, 3 };

        /// <summary>
        ///     Gets or sets the maximum number of workers for whom to return results.
        /// </summary>
        public int MaximumNumberWorkers { get; set; } = 20;

        /// <summary>
        ///     Gets or sets the vocabulary threshold.
        /// </summary>
        public int? VocabularyThreshold { get; set; } = 10;

        /// <summary>
        ///     Balance the training set so there are an equal number of
        ///     tweets per majority-vote label.
        /// </summary>
        public bool UseBalancedTrainingSets { get; set; } = false;

        /// <summary>
        ///     Gets or sets the model types to run.
        /// </summary>
        public ModelTypes ModelTypes { get; set; } =
            ModelTypes.MajorityVote |
            ModelTypes.HonestWorker |
            ModelTypes.BiasedWorker |
            ModelTypes.BiasedCommunity;
    }
}
