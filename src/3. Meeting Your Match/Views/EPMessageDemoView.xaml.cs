// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MeetingYourMatch.Views
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Windows;

    using Microsoft.Research.Glo;
    using Microsoft.Research.Glo.Views;

    using MeetingYourMatch.Annotations;

    /// <summary>
    /// Interaction logic for EPMessageDemoView.xaml
    /// </summary>
    [ViewInformation(TargetType = typeof(EPMessageDemoViewModel), Priority = 7, MinimumSize = ViewSize.LargePanel)]
    public partial class EPMessageDemoView : INotifyPropertyChanged
    {
        /// <summary>
        /// The x axis label.
        /// </summary>
        private string xAxisLabel = "Jperf";

        /// <summary>
        /// The y axis label.
        /// </summary>
        private string yAxisLabel = "P(Jperf)";

        /// <summary>
        /// The x minimum.
        /// </summary>
        private double xMinimum = -50;

        /// <summary>
        /// The x maximum.
        /// </summary>
        private double xMaximum = 310;

        /// <summary>
        /// The numeric axis string format.
        /// </summary>
        private string numericAxisStringFormat = "{0:N3}";

        /// <summary>
        /// show the legend.
        /// </summary>
        private BooleanWithAuto showLegend = BooleanWithAuto.No;

        /// <summary>
        /// show the y axis.
        /// </summary>
        private bool showYAxis = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="EPMessageDemoView"/> class.
        /// </summary>
        public EPMessageDemoView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the x axis label.
        /// </summary>
        public string XAxisLabel
        {
            get
            {
                return this.xAxisLabel;
            }

            set
            {
                if (value == this.xAxisLabel)
                {
                    return;
                }

                this.xAxisLabel = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the y axis label.
        /// </summary>
        public string YAxisLabel
        {
            get
            {
                return this.yAxisLabel;
            }

            set
            {
                if (value == this.yAxisLabel)
                {
                    return;
                }

                this.yAxisLabel = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the x minimum.
        /// </summary>
        public double XMinimum
        {
            get
            {
                return this.xMinimum;
            }

            set
            {
                if (value.Equals(this.xMinimum))
                {
                    return;
                }

                this.xMinimum = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the x maximum.
        /// </summary>
        public double XMaximum
        {
            get
            {
                return this.xMaximum;
            }

            set
            {
                if (value.Equals(this.xMaximum))
                {
                    return;
                }

                this.xMaximum = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the numeric axis string format.
        /// </summary>
        public string NumericAxisStringFormat
        {
            get
            {
                return this.numericAxisStringFormat;
            }

            set
            {
                if (value == this.numericAxisStringFormat)
                {
                    return;
                }

                this.numericAxisStringFormat = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets whether to show the legend.
        /// </summary>
        [DisplayName("Show Legend")]
        public BooleanWithAuto ShowLegend
        {
            get
            {
                return this.showLegend;
            }

            set
            {
                this.showLegend = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets whether to show the legend.
        /// </summary>
        [DisplayName("Show Y Axis")]
        public bool ShowYAxis
        {
            get
            {
                return this.showYAxis;
            }

            set
            {
                this.showYAxis = value;
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
        /// EPMessageDemoView on data context changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private void EPMessageDemoViewOnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var items = DataContext as EPMessageDemoViewModel;
            if (items == null)
            {
                return;
            }

            MyChart.DataContext = items;

            // Now remove items that are emtpy, but preserve the colour order
            MyChart.Series.Clear();
            var palette = MyChart.Palette;
            MyChart.Palette = null;
            var newPalette = new Collection<ResourceDictionary>();

            foreach (var pair in items.Zip(palette, (item, p) => new { item, p }))
            {
                var points = pair.item.Value as Point[];
                if (points == null || points.Length == 0)
                {
                    continue;
                }

                var fs = new FastLineSeries<double, double>
                             {
                                 LegendItemStyle = (Style)this.MyChart.Resources["CustomLegendItem"],
                                 ItemsSource = points.Select(ia => new ChartPoint<double, double>(ia.X, ia.Y)),
                                 Title = pair.item.Key
                             };
                this.MyChart.Series.Add(fs);
                newPalette.Add(pair.p);
            }

            MyChart.Palette = newPalette;
        }
    }
}
