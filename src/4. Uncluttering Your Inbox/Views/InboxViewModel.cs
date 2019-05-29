// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Views
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;

    using UnclutteringYourInbox.Features;

    using Microsoft.Research.Glo;

    using MBMLViews.Annotations;
    using MBMLViews.Views;

    /// <summary>
    /// The sort order.
    /// </summary>
    public enum SortOrder
    {
        /// <summary>
        /// Order by date.
        /// </summary>
        ByDate,

        /// <summary>
        /// Order by probability of reply.
        /// </summary>
        ByProbabilityOfReply
    }

    /// <summary>
    /// Provides the data model for an action-oriented view of mailbox data.
    /// </summary>
    public class InboxViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Conversations the user is likely to want to reply to.
        /// </summary>
        private IEnumerable<Conversation> toReply;

        /// <summary>
        /// The reply to model progress.
        /// </summary>
        private Progress replyToModelProgress = new Progress { Status = "Model not yet run" };

        /// <summary>
        /// The sort order.
        /// </summary>
        private SortOrder sortOrder;

        /// <summary>
        /// The direction.
        /// </summary>
        private SortDirection sortDirection;

        /// <summary>
        /// The error rates.
        /// </summary>
        private string errorRates;

        /// <summary>
        /// The cut off.
        /// </summary>
        private double cutOff = double.NaN;

        /// <summary> Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the order.
        /// </summary>
        public SortOrder SortOrder
        {
            get
            {
                return this.sortOrder;
            }

            set
            {
                this.sortOrder = value;
                this.OrderByDateIsEnabled = value == SortOrder.ByProbabilityOfReply;
                this.OrderByProbabilityOfReplyIsEnabled = value == SortOrder.ByDate;

                if (this.PropertyChanged != null)
                {
                    this.OnPropertyChanged("OrderByDateIsEnabled");
                    this.OnPropertyChanged("OrderByProbabilityOfReplyIsEnabled");
                }
            }
        }

        /// <summary>
        /// Gets or sets the sort direction.
        /// </summary>
        public SortDirection SortDirection
        {
            get
            {
                return this.sortDirection;
            }

            set
            {
                this.sortDirection = value;
                this.UpArrowIsEnabled = value == SortDirection.Descending;
                this.DownArrowIsEnabled = value == SortDirection.Ascending;

                if (this.PropertyChanged != null)
                {
                    this.OnPropertyChanged("UpArrowIsEnabled");
                    this.OnPropertyChanged("DownArrowIsEnabled");
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether order by date is enabled.
        /// </summary>
        public bool OrderByDateIsEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether order by probability of reply is enabled.
        /// </summary>
        public bool OrderByProbabilityOfReplyIsEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether up arrow is enabled.
        /// </summary>
        public bool UpArrowIsEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether down arrow is enabled.
        /// </summary>
        public bool DownArrowIsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the to reply.
        /// </summary>
        public IEnumerable<Conversation> ToReply
        {
            get
            {
                return this.toReply;
            }

            set
            {
                this.toReply = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the reply to model progress.
        /// </summary>
        public Progress ReplyToModelProgress
        {
            get
            {
                return this.replyToModelProgress;
            }

            set
            {
                this.replyToModelProgress = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the user.
        /// </summary>
        public User User { get; internal set; }

        /// <summary>
        /// Gets or sets the error rates.
        /// </summary>
        public string ErrorRates
        {
            get
            {
                return this.errorRates;
            }

            set
            {
                if (value == this.errorRates)
                {
                    return;
                }

                this.errorRates = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the trials.
        /// </summary>
        public Dictionary<FeatureSetType, ModelRunner.Trial> Trials { get; set; }

        /// <summary>
        /// Gets or sets the cut off.
        /// </summary>
        public double CutOff
        {
            get
            {
                return this.cutOff;
            }

            set
            {
                if (value.Equals(this.cutOff))
                {
                    return;
                }

                this.cutOff = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Updates the error rates.
        /// </summary>
        /// <param name="featureSetType">
        /// The feature Set Type.
        /// </param>
        internal void UpdateErrorRates(FeatureSetType featureSetType)
        {
            if (this.User == null)
            {
                return;
            }

            var trial = this.Trials[featureSetType];
            var metrics = trial.ExperimentCollection.ValidationMetrics[0];
            this.ErrorRates = string.Format(
                "Average Precision {0}, Area Under ROC Curve {1}, Calibration Error {2}",
                metrics.AveragePrecision.ToString("P1"),
                metrics.AreaUnderCurve.ToString("P1"),
                metrics.CalibrationError.ToString("N3"));
        }

        /// <summary>
        /// Updates to reply.
        /// </summary>
        internal void UpdateToReply()
        {
            if (this.User == null)
            {
                return;
            }

            // var messages = this.User.ValidationMessages.Where(m => m.IsLikelyToReply).OrderByDescending(m => m.ProbabilityOfReply);
            IOrderedEnumerable<Message> messages;
            if (this.SortOrder == SortOrder.ByDate)
            {
                messages = this.SortDirection == SortDirection.Ascending
                               ? this.User.ValidationMessages.OrderBy(m => m.DateSent)
                               : this.User.ValidationMessages.OrderByDescending(m => m.DateSent);
            }
            else
            {
                messages = this.SortDirection == SortDirection.Ascending
                               ? this.User.ValidationMessages.OrderBy(m => m.ProbabilityOfReply)
                               : this.User.ValidationMessages.OrderByDescending(m => m.ProbabilityOfReply);
            }

            var conversations = messages.Select(m => m.Conversation).Distinct();
            this.ToReply = conversations.ToList();
        }

        /// <summary>
        /// Called when [property changed].
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}