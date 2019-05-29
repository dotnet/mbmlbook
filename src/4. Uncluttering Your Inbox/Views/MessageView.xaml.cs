// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Views
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Windows;
    using System.Windows.Media;

    using UnclutteringYourInbox.DataCleaning;

    using Microsoft.Research.Glo;

    using MBMLViews.Annotations;

    /// <summary>
    /// Interaction logic for MessageView.xaml
    /// </summary>
    [ViewInformation(TargetType = typeof(Message), Priority = 10, MinimumSize = ViewSize.SmallPanel)]
    public partial class MessageView : INotifyPropertyChanged
    {
        /// <summary>
        /// The Anonymize property.
        /// </summary>
        internal static readonly DependencyProperty AnonymizeProperty = DependencyProperty.Register(
            "Anonymize",
            typeof(Anonymize),
            typeof(MessageView),
            new FrameworkPropertyMetadata(
                Anonymize.DoNotAnonymize,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.Inherits));
        
        /// <summary>
        /// The actions border background property.
        /// </summary>
        private static readonly DependencyProperty ActionsBorderBackgroundProperty = DependencyProperty.Register(
            "ActionsBorderBackground",
            typeof(Brush),
            typeof(MessageView),
            new PropertyMetadata(Brushes.Transparent));

        /// <summary>
        /// The feature list box items source.
        /// </summary>
        private IList<FeatureViewModel> featureListBoxItemsSource;

        /// <summary>
        /// The feature list visibility.
        /// </summary>
        private Visibility featureListVisibility;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageView"/> class.
        /// </summary>
        public MessageView()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// The property changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        
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
        /// Gets or sets the feature list box items source.
        /// </summary>
        public IList<FeatureViewModel> FeatureListBoxItemsSource
        {
            get
            {
                return this.featureListBoxItemsSource;
            }

            set
            {
                if (ReferenceEquals(value, this.featureListBoxItemsSource))
                {
                    return;
                }

                this.featureListBoxItemsSource = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged("FeatureListVisibility");
            }
        }

        /// <summary>
        /// Gets or sets the actions border background.
        /// </summary>
        public Brush ActionsBorderBackground
        {
            get
            {
                return (Brush)this.GetValue(ActionsBorderBackgroundProperty);
            }

            set
            {
                SetValue(ActionsBorderBackgroundProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the feature list visibility.
        /// </summary>
        public Visibility FeatureListVisibility
        {
            get
            {
                return this.featureListVisibility;
            }

            set
            {
                if (value == this.featureListVisibility)
                {
                    return;
                }

                this.featureListVisibility = value;
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
        /// Users the control data context changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private void UserControlDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.FeatureListVisibility = Visibility.Collapsed;

            Message m = e.NewValue as Message;
            if (m == null)
            {
                return;
            }

            this.CopiedToBlock.Visibility = (m.CopiedTo.Count == 0) ? Visibility.Collapsed : Visibility.Visible;

            if (m.FeatureValuesAndWeights == null || m.FeatureValuesAndWeights.Count == 0)
            {
                return;
            }

            this.FeatureListVisibility = Visibility.Visible;

            this.FeatureListBoxItemsSource =
                m.FeatureValuesAndWeights.Select(
                    ia =>
                    new FeatureViewModel
                        {
                            Name = ia.Key.Feature.Name,
                            Type = ia.Key.Feature.BaseTypeName,
                            Bucket = ia.Key.Name,
                            Value = ia.Value.First.ToString("N1"),
                            WeightMean = ia.Value.Second.GetMean().ToString("N4"),
                            WeightVariance = ia.Value.Second.GetVariance().ToString("N4")
                            //// WeightMean = ia.Key.Weight.GetMean().ToString("N4"),
                            //// WeightVariance = ia.Key.Weight.GetVariance().ToString("N4")
                        }).ToList();
        }

        /// <summary>
        /// The feature view model.
        /// </summary>
        public class FeatureViewModel
        {
            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the type.
            /// </summary>
            public string Type { get; set; }

            /// <summary>
            /// Gets or sets the bucket.
            /// </summary>
            public string Bucket { get; set; }

            /// <summary>
            /// Gets or sets the value.
            /// </summary>
            public string Value { get; set; }

            /// <summary>
            /// Gets or sets the weight.
            /// </summary>
            public string WeightMean { get; set; }

            /// <summary>
            /// Gets or sets the weight variance.
            /// </summary>
            public string WeightVariance { get; set; }
        }
    }
}
