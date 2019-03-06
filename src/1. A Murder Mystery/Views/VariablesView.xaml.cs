// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MurderMystery
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows;

    using Microsoft.Research.Glo;

    /// <summary>
    /// The view type.
    /// </summary>
    public enum ViewType
    {
        /// <summary>
        /// The priors.
        /// </summary>
        Priors,

        /// <summary>
        /// The conditionals.
        /// </summary>
        Conditionals,

        /// <summary>
        /// The joint.
        /// </summary>
        Joint,

        /// <summary>
        /// The posteriors.
        /// </summary>
        Posteriors
    }

    /// <summary>
    /// View to show priors and posteriors of variables
    /// </summary>
    [ViewInformation(TargetType = typeof(VariablesViewModel), Priority = 13, MinimumSize = ViewSize.SmallPanel)]
    public partial class VariablesView : INotifyPropertyChanged
    {
        /// <summary>
        /// The view model.
        /// </summary>
        private VariablesViewModel viewModel;

        /// <summary>
        /// Whether to show grey (conditionals view).
        /// </summary>
        private bool showShowGrey = true;

        /// <summary>
        /// Whether to show auburn (conditionals view).
        /// </summary>
        private bool showAuburn = true;

        /// <summary>
        /// The text right visibility. Default to hidden rather than collapsed because of WPF issue
        /// </summary>
        private Visibility textRightVisibility = Visibility.Hidden;

        /// <summary>
        /// The arrow visibility.
        /// </summary>
        private bool showArrows = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="VariablesView"/> class. 
        /// </summary>
        public VariablesView()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// The property changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets a value indicating whether show grey.
        /// </summary>
        [DisplayName(@"Show Grey")]
        public bool ShowGrey
        {
            get
            {
                return this.showShowGrey;
            }

            set
            {
                this.showShowGrey = value;
                this.OnPropertyChanged();
                this.BuildView();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether show auburn.
        /// </summary>
        [DisplayName(@"Show Auburn")]
        public bool ShowAuburn
        {
            get
            {
                return this.showAuburn;
            }

            set
            {
                this.showAuburn = value;
                this.OnPropertyChanged();
                this.BuildView();
            }
        }

        /// <summary>
        /// Gets or sets the view model.
        /// </summary>
        [DisplayName(@"ViewModel")]
        public VariablesViewModel ViewModel
        {
            get
            {
                return this.viewModel;
            }

            set
            {
                this.viewModel = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the text right visibility.
        /// </summary>
        public Visibility TextRightVisibility
        {
            get
            {
                return this.textRightVisibility;
            }

            set
            {
                this.textRightVisibility = value;
                this.OnPropertyChanged();
                this.BuildView();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether show arrows.
        /// </summary>
        [DisplayName(@"Show Arrows")]
        public bool ShowArrows
        {
            get
            {
                return this.showArrows;
            }

            set
            {
                this.showArrows = value;
                this.OnPropertyChanged();
                this.BuildView();
            }
        }

        /// <summary>
        /// Called when [property changed].
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Builds the view.
        /// </summary>
        private void BuildView()
        {
            this.ViewModel.SetUpViewModel(this.ShowGrey, this.ShowAuburn);
        }

        /// <summary>
        /// Murders the mystery priors view on data context changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private void VariablesViewOnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            this.ViewModel = DataContext as VariablesViewModel;

            if (this.ViewModel == null)
            {
                return;
            }

            this.BuildView();
        }
    }
}
