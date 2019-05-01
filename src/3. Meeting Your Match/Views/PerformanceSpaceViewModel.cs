// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MeetingYourMatch.Views
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;

    using Microsoft.ML.Probabilistic.Distributions;
    using Microsoft.ML.Probabilistic.Math;

    using MeetingYourMatch.Annotations;

#if NETFULL
    using Point = System.Windows.Point;
#else
    using Point = MBMLCommon.Point;
#endif

    /// <summary>
    /// The Performance Space View Model.
    /// </summary>
    public sealed class PerformanceSpaceViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// The samples.
        /// </summary>
        private Point[] samples;

        /// <summary>
        /// The x minimum.
        /// </summary>
        private double xMinimum = double.NaN;

        /// <summary>
        /// The x maximum.
        /// </summary>
        private double xMaximum = double.NaN;

        /// <summary>
        /// The y minimum.
        /// </summary>
        private double yMinimum = double.NaN;

        /// <summary>
        /// The y maximum.
        /// </summary>
        private double yMaximum = double.NaN;

        /// <summary>
        /// The number of samples.
        /// </summary>
        private int numberOfSamples;

        /// <summary>
        /// The draw margin.
        /// </summary>
        private double drawMargin = 32.0;

        /// <summary>
        /// Initializes a new instance of the <see cref="PerformanceSpaceViewModel"/> class.
        /// </summary>
        public PerformanceSpaceViewModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PerformanceSpaceViewModel" /> class.
        /// </summary>
        /// <param name="player1Performance">The player1 performance.</param>
        /// <param name="player2Performance">The player2 performance.</param>
        /// <param name="numberOfSamples">The number of samples.</param>
        public PerformanceSpaceViewModel(Gaussian player1Performance, Gaussian player2Performance, int numberOfSamples)
        {
            this.Player1Performance = player1Performance;
            this.Player2Performance = player2Performance;
            this.NumberOfSamples = numberOfSamples;
            this.Samples = GenerateSamples(this.Player1Performance, this.Player2Performance, numberOfSamples);
            this.ResetRanges();
        }

        /// <summary>
        /// The property changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the player 1 name.
        /// </summary>
        public string Player1Name { get; set; }

        /// <summary>
        /// Gets or sets the player 2 name.
        /// </summary>
        public string Player2Name { get; set; }

        /// <summary>
        /// Gets the player 1 label.
        /// </summary>
        public string Player1Label
        {
            get
            {
                return this.Player1Name + "s performance";
            }
        }

        /// <summary>
        /// Gets the player 2 label.
        /// </summary>
        public string Player2Label
        {
            get
            {
                return this.Player2Name + "s performance";
            }
        }

        /// <summary>
        /// Gets or sets the number of samples.
        /// </summary>
        public int NumberOfSamples
        {
            get
            {
                return this.numberOfSamples;
            }

            set
            {
                if (value == this.numberOfSamples)
                {
                    return;
                }

                this.numberOfSamples = value;
                
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the samples.
        /// </summary>
        public Point[] Samples
        {
            get
            {
                return this.samples;
            }

            set
            {
                if (ReferenceEquals(value, this.samples))
                {
                    return;
                }

                this.samples = value;

                this.OnPropertyChanged();
                this.OnPropertyChanged("Player1WinProportion");
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
                this.OnPropertyChanged("XMinimumText");
            }
        }

        /// <summary>
        /// Gets the x minimum text.
        /// </summary>
        public string XMinimumText
        {
            get
            {
                return double.IsNaN(this.XMinimum) ? "Low" : this.XMinimum.ToString("N1");
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
                this.OnPropertyChanged("XMaximumText");
            }
        }

        /// <summary>
        /// Gets the x maximum text.
        /// </summary>
        public string XMaximumText
        {
            get
            {
                return double.IsNaN(this.XMaximum) ? "High" : this.XMaximum.ToString("N1");
            }
        }

        /// <summary>
        /// Gets or sets the y minimum.
        /// </summary>
        public double YMinimum
        {
            get
            {
                return this.yMinimum;
            }

            set
            {
                if (value.Equals(this.yMinimum))
                {
                    return;
                }

                this.yMinimum = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged("YMinimumText");
            }
        }

        /// <summary>
        /// Gets the y minimum text.
        /// </summary>
        public string YMinimumText
        {
            get
            {
                return double.IsNaN(this.YMinimum) ? "Low" : this.YMinimum.ToString("N1");
            }
        }

        /// <summary>
        /// Gets or sets the y maximum.
        /// </summary>
        public double YMaximum
        {
            get
            {
                return this.yMaximum;
            }

            set
            {
                if (value.Equals(this.yMaximum))
                {
                    return;
                }

                this.yMaximum = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged("YMaximumText");
            }
        }

        /// <summary>
        /// Gets the y maximum text.
        /// </summary>
        public string YMaximumText
        {
            get
            {
                return double.IsNaN(this.YMaximum) ? "High" : this.YMaximum.ToString("N1");
            }
        }

        /// <summary>
        /// Gets the player 1 win count.
        /// </summary>
        public double Player1WinCount
        {
            get
            {
                return this.Samples == null || this.Samples.Length == 0 ? double.NaN : this.Samples.Sum(ia => ia.Y > ia.X ? 1.0 : 0.0);
            }
        }

        /// <summary>
        /// Gets the player 1 win proportion.
        /// </summary>
        public double Player1WinProportion
        {
            get
            {
                return this.Samples == null || this.Samples.Length == 0 ? double.NaN : this.Samples.Average(ia => ia.Y > ia.X ? 1.0 : 0.0);
            }
        }

        /// <summary>
        /// Gets the player 1 wins.
        /// </summary>
        public string Player1Wins
        {
            get
            {
                return this.Player1Name + " Wins"
                       + (double.IsNaN(this.Player1WinProportion) ? string.Empty : "\n" + this.Player1WinProportion.ToString("P1"));
            }
        }

        /// <summary>
        /// Gets the player 2 wins.
        /// </summary>
        public string Player2Wins
        {
            get
            {
                return this.Player2Name + " Wins"
                       + (double.IsNaN(this.Player1WinProportion) ? string.Empty : "\n" + (1.0 - this.Player1WinProportion).ToString("P1"));
            }
        }

        /// <summary>
        /// Gets or sets player 1's performance.
        /// </summary>
        public Gaussian Player1Performance { get; set; }

        /// <summary>
        /// Gets or sets player 2's performance.
        /// </summary>
        public Gaussian Player2Performance { get; set; }

        /// <summary>
        /// Gets or sets the draw margin.
        /// </summary>
        public double DrawMargin
        {
            get
            {
                return this.drawMargin;
            }

            set
            {
                if (value.Equals(this.drawMargin))
                {
                    return;
                }

                this.drawMargin = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged("FourHundredMinusDrawMargin");
            }
        }

        /// <summary>
        /// Gets the draw margin visibility.
        /// </summary>
        public bool DrawMarginVisibility
        {
            get { return !double.IsNaN(this.DrawMargin); }
        }

        /// <summary>
        /// Gets four hundred minus the draw margin.
        /// </summary>
        public double FourHundredMinusDrawMargin
        {
            get { return 400 - this.DrawMargin; }
        }

        /// <summary>
        /// Generates the samples.
        /// </summary>
        /// <param name="player1Performance">The player1 performance.</param>
        /// <param name="player2Performance">The player2 performance.</param>
        /// <param name="numberOfSamples">The number of samples.</param>
        /// <param name="seed">The random seed.</param>
        /// <returns>
        /// The <see cref="Point" /> array.
        /// </returns>
        public static Point[] GenerateSamples(Gaussian player1Performance, Gaussian player2Performance, int numberOfSamples, int seed = 0)
        {
            // Set random seed
            Rand.Restart(seed);

            return
                (from ia in Enumerable.Range(0, numberOfSamples)
                 let x = player2Performance.Sample()
                 let y = player1Performance.Sample()
                 select new Point(x, y)).ToArray();
        }

        /// <summary>
        /// Called when [property changed].
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Resets the ranges.
        /// </summary>
        private void ResetRanges()
        {
            var player1Max = this.Player1Performance.GetMean() + (4 * Math.Sqrt(this.Player1Performance.GetVariance()));
            var player2Max = this.Player2Performance.GetMean() + (4 * Math.Sqrt(this.Player2Performance.GetVariance()));
            var player1Min = this.Player1Performance.GetMean() - (4 * Math.Sqrt(this.Player1Performance.GetVariance()));
            var player2Min = this.Player2Performance.GetMean() - (4 * Math.Sqrt(this.Player2Performance.GetVariance()));

            var min = Math.Min(player1Min, player2Min);
            var max = Math.Max(player1Max, player2Max);

            this.XMinimum = min;
            this.XMaximum = max;
            this.YMinimum = min;
            this.YMaximum = max;
        }
    }
}