// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Views
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows;

    using UnclutteringYourInbox.DataCleaning;

    using Microsoft.Research.Glo;

    using MBMLViews.Annotations;

    /// <summary>
    /// Interaction logic for ConversationListView.xaml
    /// </summary>
    [ViewInformation(TargetType = typeof(List<Conversation>), MinimumSize = ViewSize.MediumPanel, Priority = 10)]
    public partial class ConversationListView : INotifyPropertyChanged
    {
        /// <summary>
        /// The cut off property.
        /// </summary>
        private static readonly DependencyProperty CutOffProperty = ConversationView.CutOffProperty.AddOwner(
            typeof(ConversationListView));

        /// <summary>
        /// The Anonymize property
        /// </summary>
        private static readonly DependencyProperty AnonymizeProperty = MessageView.AnonymizeProperty.AddOwner(
            typeof(ConversationListView));

        /// <summary>
        /// Initializes a new instance of the <see cref="ConversationListView"/> class.
        /// </summary>
        public ConversationListView()
        {
            this.InitializeComponent();
            this.SelectedIndex = 1;
        }

        /// <summary>
        /// The property changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the cut off.
        /// </summary>
        [DisplayName(@"Cut Off")]
        public double CutOff
        {
            get
            {
                return (double)this.GetValue(CutOffProperty);
            }

            set
            {
                this.SetValue(CutOffProperty, value);
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the Anonymize.
        /// </summary>
        [DisplayName(@"Anonymize")]
        public Anonymize Anonymize
        {
            get
            {
                return (Anonymize)this.GetValue(AnonymizeProperty);
            }

            set
            {
                this.SetValue(AnonymizeProperty, value);
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Called when [property changed].
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        [NotifyPropertyChangedInvocator]
        internal virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
