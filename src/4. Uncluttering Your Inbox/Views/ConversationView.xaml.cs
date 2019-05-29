// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Views
{
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Windows;

    using UnclutteringYourInbox.DataCleaning;

    using Microsoft.Research.Glo;

    using MBMLViews.Annotations;

    /// <summary>
    /// Interaction logic for ConversationView.xaml
    /// </summary>
    [ViewInformation(TargetType = typeof(Conversation), Priority = 10)]
    ////[ViewInformation(TargetType = typeof(IEnumerable<Message>), Priority = 8)]
    public partial class ConversationView : INotifyPropertyChanged
    {
        /// <summary>
        /// The cut off property.
        /// </summary>
        internal static readonly DependencyProperty CutOffProperty = DependencyProperty.Register(
            "CutOff",
            typeof(double),
            typeof(ConversationView),
            new FrameworkPropertyMetadata(
                0.5,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Inherits));

        /// <summary>
        /// The Anonymize property
        /// </summary>
        private static readonly DependencyProperty AnonymizeProperty = MessageView.AnonymizeProperty.AddOwner(
            typeof(ConversationView));

        /// <summary>
        /// Initializes a new instance of the <see cref="ConversationView"/> class.
        /// </summary>
        public ConversationView()
        {
            this.InitializeComponent();
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
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Handles the DataContextChanged event of the UserControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private void UserControlDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Conversation c = this.DataContext as Conversation;
            if (c != null)
            {
                this.MessageListBox.ItemsSource = c.Messages.Reverse();
                this.Subject.Text = c.Subject;
            }

            ////var messages = this.DataContext as IEnumerable<Message>;
            ////if (messages != null)
            ////{
            ////    this.MessageListBox.ItemsSource = messages;
            ////}
        }
    }
}
