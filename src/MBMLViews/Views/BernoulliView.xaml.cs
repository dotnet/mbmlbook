// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using Microsoft.Research.Glo;
using Microsoft.ML.Probabilistic.Distributions;
using Microsoft.ML.Probabilistic.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MBMLViews.Views
{
    /// <summary>
    /// Interaction logic for BernoulliView.xaml
    /// </summary>
    [ViewInformation(TargetType=typeof(Bernoulli), Priority=12, MinimumSize=ViewSize.Cell/*, ViewType=ViewType.DisplayOnly*/)]
    public partial class BernoulliView : UserControl, IConstrainableView
    {
        public BernoulliView()
        {
            InitializeComponent();
        }

        private void UserControl_DataContextChanged_1(object sender, DependencyPropertyChangedEventArgs e)
        {
            var b = DataContext as Bernoulli?;
            if (!b.HasValue) return;
            double p = b.Value.GetProbTrue();
            //p = Rand.Int(11) * 0.1; //- for testing
            Bar.Width = Width * p;
            NumberText.Text = p.ToString("0.000"); 
        }

        ViewInformation viewConstraints;
        public ViewInformation ViewConstraints
        {
            get
            {
                return viewConstraints;
            }
            set
            {
                viewConstraints = value;
                if (viewConstraints.MaximumSize == ViewSize.Cell)
                {
                    this.BorderThickness = new Thickness(0);
                }
            }
        }
    }
}
