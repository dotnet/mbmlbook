// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Views
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;

    using UnclutteringYourInbox.DataCleaning;
    using UnclutteringYourInbox.Features;

    using Microsoft.Research.Glo;

    using MBMLViews.Annotations;
    using MBMLViews.Views;

    /// <summary>
    /// Interaction logic for UserView.xaml
    /// </summary>
    [ViewInformation(TargetType = typeof(InboxViewModel), Priority = 10, MinimumSize = ViewSize.MediumPanel)]
    public partial class InboxView : INotifyPropertyChanged
    {
        /// <summary>
        /// The view model.
        /// </summary>
        private InboxViewModel viewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="InboxView"/> class.
        /// </summary>
        public InboxView()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// The property changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the view model.
        /// </summary>
        public InboxViewModel ViewModel
        {
            get
            {
                return this.viewModel;
            }

            set
            {
                if (ReferenceEquals(value, this.viewModel))
                {
                    return;
                }

                this.viewModel = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged("FeatureSetTypes");
                this.OnPropertyChanged("FeatureSetType");
            }
        }

        /// <summary>
        /// Gets or sets the anonymize.
        /// </summary>
        [DisplayName(@"Anonymize")]
        public Anonymize Anonymize
        {
            get
            {
                return this.ViewModel == null || this.ViewModel.User == null ? Anonymize.DoNotAnonymize : this.ViewModel.User.Anonymize;
            }

            set
            {
                if (this.ViewModel == null || this.ViewModel.User == null)
                {
                    return;
                }
                
                this.ViewModel.User.Anonymize = value;
                this.OnPropertyChanged();
                this.ResetChildViews();
            }
        }

        /// <summary>
        /// Gets or sets the feature set types.
        /// </summary>
        public IList<FeatureSetType> FeatureSetTypes
        {
            get
            {
                return this.ViewModel == null || this.ViewModel.User == null ? null : this.ViewModel.User.FeatureSetTypes;
            }

            set
            {
                if (this.ViewModel == null || this.ViewModel.User == null)
                {
                    return;
                }

                this.ViewModel.User.FeatureSetTypes = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the feature set type.
        /// </summary>
        [DisplayName(@"Feature Set Type")]
        public FeatureSetType FeatureSetType
        {
            get
            {
                return this.ViewModel == null || this.ViewModel.User == null ? FeatureSetType.Initial : this.ViewModel.User.CurrentFeatureSetType;
            }

            set
            {
                if (this.ViewModel == null || this.ViewModel.User == null || value == this.ViewModel.User.CurrentFeatureSetType)
                {
                    return;
                }

                this.ViewModel.User.CurrentFeatureSetType = value;
                this.OnPropertyChanged();
                this.ViewModel.UpdateErrorRates(value);
                this.ViewModel.UpdateToReply();
                this.ResetChildViews();
            }
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

        /// <summary>
        /// Resets the child views.
        /// </summary>
        private void ResetChildViews()
        {
            // todo: find a better way to make the child views update
            var dataContext = this.ConversationListView.DataContext;
            this.ConversationListView.DataContext = null;
            this.ConversationListView.DataContext = dataContext;
            dataContext = this.ConversationView.DataContext;
            this.ConversationView.DataContext = null;
            this.ConversationView.DataContext = dataContext;
            var itemsSource = this.ConversationView.MessageListBox.ItemsSource;
            this.ConversationView.MessageListBox.ItemsSource = null;
            this.ConversationView.MessageListBox.ItemsSource = itemsSource;
        }

        /// <summary>
        /// Handles the SelectionChanged event of the ConversationListView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void ConversationListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ConversationView.DataContext = ((Selector)e.Source).SelectedItem;
        }

        /// <summary>
        /// Handles the IsKeyboardFocusWithinChanged event of the ConversationListView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private void ConversationListViewIsKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ConversationView.DataContext = ((Selector)sender).SelectedItem;
        }

        /// <summary>
        /// Orders the by date.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void OrderByDate(object sender, RoutedEventArgs e)
        {
            if (this.viewModel == null)
            {
                return;
            }

            this.viewModel.SortOrder = SortOrder.ByDate;
            this.viewModel.UpdateToReply();
        }

        /// <summary>
        /// Orders the by probability of reply.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void OrderByProbabilityOfReply(object sender, RoutedEventArgs e)
        {
            if (this.viewModel == null)
            {
                return;
            }

            this.viewModel.SortOrder = SortOrder.ByProbabilityOfReply;
            this.viewModel.UpdateToReply();
        }

        /// <summary>
        /// Toggles the order direction.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void ToggleOrderDirection(object sender, RoutedEventArgs e)
        {
            if (this.viewModel == null)
            {
                return;
            }

            this.viewModel.SortDirection = this.viewModel.SortDirection == SortDirection.Ascending
                                          ? SortDirection.Descending
                                          : SortDirection.Ascending;
            this.viewModel.UpdateToReply();
        }

        /// <summary>
        /// Inboxes the view on data context changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private void InboxViewOnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.ViewModel = DataContext as InboxViewModel;
        }
    }
}