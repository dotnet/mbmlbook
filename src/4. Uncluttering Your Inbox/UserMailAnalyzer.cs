// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox
{
    using System;
    using System.Collections.Generic;
    using UnclutteringYourInbox.DataCleaning;
    using UnclutteringYourInbox.Features;
    using UnclutteringYourInbox.Models;
    
    using MBMLViews.Views;
    using UnclutteringYourInbox.Views;

    /// <summary>
    /// The demonstrator.
    /// </summary>
    public class UserMailAnalyzer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserMailAnalyzer"/> class.
        /// </summary>
        public UserMailAnalyzer()
        {
            this.UserTrials = new Dictionary<FeatureSetType, ModelRunner.Trial>();
            
            this.Inbox = new InboxViewModel
                             {
                                 SortOrder = SortOrder.ByProbabilityOfReply,
                                 SortDirection = SortDirection.Descending,
                                 CutOff = 0.4,
                                 Trials = this.UserTrials
                             };
        }

        /// <summary>
        /// Gets or sets the end time.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets or sets the collector user.
        /// </summary>
        public User User { get; set; }

        /// <summary>
        /// Gets or sets the collector user trial.
        /// </summary>
        public Dictionary<FeatureSetType, ModelRunner.Trial> UserTrials { get; set; }
        
        /// <summary>
        /// Gets the inbox view model.
        /// </summary>
        public InboxViewModel Inbox { get; private set; }

        /// <summary>
        /// Gets or sets the months.
        /// </summary>
        public int Months { get; set; }

        /// <summary>
        /// Gets or sets the anonymize.
        /// </summary>
        public Anonymize Anonymize { get; set; }
        
        /// <summary>
        /// Loads the user from desktop search and runs the trial.
        /// </summary>
        public void LoadUserFromDesktopSearchAndRun(bool showFactorGraph, double thresholdAndNoiseVariance)
        {
            this.LoadUserFromDesktopSearch(this.Months, this.Anonymize);
            this.RunUserTrial(showFactorGraph, thresholdAndNoiseVariance);
        }

        /// <summary>
        /// Runs the collector user trial.
        /// </summary>
        public void RunUserTrial(bool showFactorGraph, double thresholdAndNoiseVariance)
        {
            if (this.User == null)
            {
                return;
            }

            double increment = (100 - this.Inbox.ReplyToModelProgress.PercentComplete) / this.UserTrials.Count;
            foreach (var userTrial in this.UserTrials)
            {
                this.Inbox.ReplyToModelProgress.Status = "Running " + userTrial.Key;
                
                if (userTrial.Key == FeatureSetType.WithRecipient2)
                {
                    userTrial.Value.Run<SparseReplyToModel2>(
                        ExperimentMode.Offline,
                        showFactorGraph,
                        thresholdAndNoiseVariance,
                        "ReplyTo");
                }
                else
                {
                    userTrial.Value.Run<SparseReplyToModel>(
                        ExperimentMode.Offline,
                        showFactorGraph,
                        thresholdAndNoiseVariance,
                        "ReplyTo");
                }

                if (userTrial.Value.ExperimentCollection.Experiments == null)
                {
                    // Experiment failed
                    continue;
                }

                userTrial.Value.UpdateProbabilityOfReply(InputMode.Validation, userTrial.Key);

                this.Inbox.ReplyToModelProgress.PercentComplete += increment;
            }

            this.Inbox.UpdateToReply();
            this.Inbox.UpdateErrorRates(this.User.CurrentFeatureSetType);
            this.Inbox.ReplyToModelProgress.Status = "Complete";
            this.Inbox.ReplyToModelProgress.PercentComplete = 100;
        }

        /// <summary>
        /// Loads the user from desktop search. 
        /// </summary>
        /// <param name="months">The months.</param>
        /// <param name="anonymizeUserData">The anonymize user data.</param>
        public void LoadUserFromDesktopSearch(int months, Anonymize anonymizeUserData)
        {
            this.Inbox.ReplyToModelProgress.Status = "Loading data";
            this.User = this.LoadInputsFromDesktopFetcher(months);
            if (this.User == null)
            {
                var message = string.Format("\n* Please re-run with more months than {0} months, e.g.: *\n", this.Months)
                              + string.Format(
                                  "{0} /m {1}\n",
                                  AppDomain.CurrentDomain.FriendlyName,
                                  Math.Min(this.Months + Program.DefaultMonths, Program.MaxMonths));
                throw new Exception(message);
            }

            this.Inbox.ReplyToModelProgress.PercentComplete = 25;

            this.User.Anonymize = anonymizeUserData;

            this.Inbox.ReplyToModelProgress.Status = "Computing Features";

            this.User.FeatureSetTypes = new[]
                {
                    FeatureSetType.Initial,
                    FeatureSetType.WithSubjectPrefix, 
                    FeatureSetType.WithRecipient,
                    FeatureSetType.WithRecipient2
                };

            this.User.CurrentFeatureSetType = FeatureSetType.Initial;
            this.UserTrials.Clear();

            foreach (var featureSetType in this.User.FeatureSetTypes)
            {
                var trial = new ModelRunner.Trial { Name = "CollectorUser" };
                this.UserTrials.Add(featureSetType, trial);
                trial.InputsCollection.Add(Inputs.FromUser(this.User, featureSetType));
                trial.TrainMessages = this.User.TrainMessages;
                trial.ValidationMessages = this.User.ValidationMessages;
                trial.TestMessages = this.User.TestMessages;
            }

            this.Inbox.ReplyToModelProgress.PercentComplete = 50;

            Console.WriteLine(Environment.NewLine + @"* Data collection complete *" + Environment.NewLine);

            this.Inbox.User = this.User;
        }

        /// <summary>
        /// Loads the inputs from desktop fetcher.
        /// </summary>
        /// <param name="months">The number of months to go back into the past.</param>
        /// <returns>
        /// The inputs
        /// </returns>
        private User LoadInputsFromDesktopFetcher(int months)
        {
            // Use desktop fetcher
            IList<string> errors;
            User user = DesktopSearchProvider.LoadUserData(this.EndTime, months, out errors);
            user.SetUpData(months, Program.TrainSetSize, Program.ValidationSetSize, Program.TestSetSize, out errors);

            if (errors.Count == 0)
            {
                return user;
            }

            // Here we could just return the partial files, or null, asking the user to expand the date range
            Console.WriteLine(string.Join("\n", errors));

            if (months < Program.MaxMonths)
            {
                return null;
            }

            Console.WriteLine(@"Continuing to save files anyway since the maximum number of months was entered.");
            return user;
        }
    }
}
