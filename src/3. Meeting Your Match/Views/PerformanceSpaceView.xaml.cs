// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MeetingYourMatch.Views
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    using Microsoft.Research.Glo;

    /// <summary>
    /// Interaction logic for PerformanceSpaceView.xaml
    /// </summary>
    [ViewInformation(TargetType = typeof(PerformanceSpaceViewModel), Priority = 11, MinimumSize = ViewSize.LargePanel)]
    [Feature(Description = "Performance Space View", Date = "15/08/2013")]
    public partial class PerformanceSpaceView : IConstrainableView, INotifyPropertyChanged
    {
        /// <summary>
        /// The data point size.
        /// </summary>
        private double dataPointSize = 2.5;

        /// <summary>
        /// Initializes a new instance of the <see cref="PerformanceSpaceView"/> class.
        /// </summary>
        public PerformanceSpaceView()
        {
            InitializeComponent();
            this.ViewConstraints = new ViewInformation { MinimumSize = ViewSize.SmallPanel };
        }

        /// <summary>
        /// The property changed event.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the data point size.
        /// </summary>
        [DisplayName(@"DataPoint Size")]
        public double DataPointSize
        {
            get
            {
                return this.dataPointSize;
            }

            set
            {
                this.dataPointSize = value;
                this.NotifyPropertyChanged();
            }
        }

        #region IConstrainableView Members
        /// <summary>
        /// Gets or sets the view constraints.
        /// </summary>
        public ViewInformation ViewConstraints { get; set; }

        #endregion

        #region INotifyPropertyChanged Members
        /// <summary>
        /// Notifies the property changed.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        protected virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }
}
