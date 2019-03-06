// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MurderMystery
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows;

    using MBMLViews.Annotations;

    /// <summary>
    /// The murder mystery variables view model.
    /// </summary>
    public class VariablesViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// The grey visibility.
        /// </summary>
        private Visibility greyVisibility;

        /// <summary>
        /// The auburn visibility.
        /// </summary>
        private Visibility auburnVisibility;

        /// <summary>
        /// The column gap.
        /// </summary>
        private double columnGap;

        /// <summary>
        /// The view type.
        /// </summary>
        private ViewType viewType;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the view type.
        /// </summary>
        public ViewType ViewType
        {
            get
            {
                return this.viewType;
            }

            set
            {
                if (value == this.viewType)
                {
                    return;
                }

                this.viewType = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged("WeaponVisibility");
                this.OnPropertyChanged("GreyRevolverWidth");
                this.OnPropertyChanged("GreyRevolverHeight");
                this.OnPropertyChanged("GreyDaggerWidth");
                this.OnPropertyChanged("GreyDaggerHeight");
                this.OnPropertyChanged("AuburnRevolverWidth");
                this.OnPropertyChanged("AuburnRevolverHeight");
                this.OnPropertyChanged("AuburnDaggerWidth");
                this.OnPropertyChanged("AuburnDaggerHeight");
                this.OnPropertyChanged("GreyWidth");
                this.OnPropertyChanged("AuburnWidth");
                this.OnPropertyChanged("GreyTotalText");
                this.OnPropertyChanged("AuburnTotalText");
                this.OnPropertyChanged("GreyRevolverText");
                this.OnPropertyChanged("GreyRevolverMarginalText");
                this.OnPropertyChanged("GreyDaggerText");
                this.OnPropertyChanged("GreyDaggerMarginalText");
                this.OnPropertyChanged("AuburnRevolverText");
                this.OnPropertyChanged("AuburnRevolverMarginalText");
                this.OnPropertyChanged("AuburnDaggerText");
                this.OnPropertyChanged("AuburnDaggerMarginalText");
                this.OnPropertyChanged("MurdererOnlyVisibility");
                this.OnPropertyChanged("ConditionalsVisibility");
                this.OnPropertyChanged("JointVisibility");
                this.OnPropertyChanged("NotConditionalsVisibility");
                this.OnPropertyChanged("AuburnColumn");
                this.OnPropertyChanged("AuburnTextColumn");
                this.OnPropertyChanged("GreyTooltip");
                this.OnPropertyChanged("AuburnTooltip");
                this.OnPropertyChanged("GreyRevolverTooltip");
                this.OnPropertyChanged("GreyDaggerTooltip");
                this.OnPropertyChanged("AuburnDaggerTooltip");
                this.OnPropertyChanged("AuburnRevolverTooltip");
            }
        }

        /// <summary>
        /// Gets or sets the opacity.
        /// </summary>
        public double Opacity { get; set; }

        /// <summary>
        /// Gets or sets the square width.
        /// </summary>
        public double SquareWidth { get; set; }

        /// <summary>
        /// Gets or sets the square height.
        /// </summary>
        public double SquareHeight { get; set; }

        /// <summary>
        /// Gets or sets the column gap.
        /// </summary>
        public double ColumnGap
        {
            get
            {
                return this.columnGap;
            }

            set
            {
                if (value.Equals(this.columnGap))
                {
                    return;
                }

                this.columnGap = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the priors.
        /// </summary>
        public Variables Variables { get; set; }

        /// <summary>
        /// Gets or sets the string format.
        /// </summary>
        public string StringFormat { get; set; }

        /// <summary>
        /// Gets the weapon visibility.
        /// </summary>
        public Visibility WeaponVisibility
        {
            get
            {
                return this.ViewType == ViewType.Priors ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        /// <summary>
        /// Gets the grey revolver width.
        /// </summary>
        public double GreyRevolverWidth
        {
            get
            {
                if (this.ViewType == ViewType.Conditionals)
                {
                    return this.SquareWidth;
                }

                if (this.ViewType == ViewType.Joint)
                {
                    return this.SquareWidth * this.Variables.MurdererMarginals.Grey;
                }

                return double.NaN;
            }
        }

        /// <summary>
        /// Gets the grey revolver height.
        /// </summary>
        public double GreyRevolverHeight
        {
            get
            {
                switch (this.ViewType)
                {
                    case ViewType.Priors:
                    case ViewType.Posteriors:
                        return this.SquareHeight;
                    case ViewType.Conditionals:
                        return this.SquareHeight * this.Variables.ConditionalsWeapon.RevolverGivenGrey;
                    case ViewType.Joint:
                        return this.SquareHeight * this.Variables.JointWeapon.RevolverGrey / this.Variables.MurdererMarginals.Grey;
                    default:
                        return this.SquareHeight;
                }
            }
        }

        /// <summary>
        /// Gets the grey dagger width.
        /// </summary>
        public double GreyDaggerWidth
        {
            get
            {
                if (this.ViewType == ViewType.Conditionals)
                {
                    return this.SquareWidth;
                }

                if (this.ViewType == ViewType.Joint)
                {
                    return this.SquareWidth * this.Variables.MurdererMarginals.Grey;
                }

                return double.NaN;
            }
        }

        /// <summary>
        /// Gets the grey dagger height.
        /// </summary>
        public double GreyDaggerHeight
        {
            get
            {
                switch (this.ViewType)
                {
                    case ViewType.Priors:
                    case ViewType.Posteriors:
                        return this.SquareHeight;
                    case ViewType.Conditionals:
                        return this.SquareHeight * this.Variables.ConditionalsWeapon.DaggerGivenGrey;
                    case ViewType.Joint:
                        return this.SquareHeight * this.Variables.JointWeapon.DaggerGrey / this.Variables.MurdererMarginals.Grey;
                    default:
                        return this.SquareHeight;
                }
            }
        }

        /// <summary>
        /// Gets the auburn revolver width.
        /// </summary>
        public double AuburnRevolverWidth
        {
            get
            {
                if (this.ViewType == ViewType.Conditionals)
                {
                    return this.SquareWidth;
                }

                if (this.ViewType == ViewType.Joint)
                {
                    return this.SquareWidth * this.Variables.MurdererMarginals.Auburn;
                }

                return double.NaN;
            }
        }

        /// <summary>
        /// Gets the auburn revolver height.
        /// </summary>
        public double AuburnRevolverHeight
        {
            get
            {
                switch (this.ViewType)
                {
                    case ViewType.Priors:
                    case ViewType.Posteriors:
                        return this.SquareHeight;
                    case ViewType.Conditionals:
                        return this.SquareHeight * this.Variables.ConditionalsWeapon.RevolverGivenAuburn;
                    case ViewType.Joint:
                        return this.SquareHeight * this.Variables.JointWeapon.RevolverAuburn / this.Variables.MurdererMarginals.Auburn;
                    default:
                        return this.SquareHeight;
                }
            }
        }

        /// <summary>
        /// Gets the auburn dagger width.
        /// </summary>
        public double AuburnDaggerWidth
        {
            get
            {
                if (this.ViewType == ViewType.Conditionals)
                {
                    return this.SquareWidth;
                }

                if (this.ViewType == ViewType.Joint)
                {
                    return this.SquareWidth * this.Variables.MurdererMarginals.Auburn;
                }

                return double.NaN;
            }
        }

        /// <summary>
        /// Gets the auburn dagger height.
        /// </summary>
        public double AuburnDaggerHeight
        {
            get
            {
                switch (this.ViewType)
                {
                    case ViewType.Priors:
                    case ViewType.Posteriors:
                        return this.SquareHeight;
                    case ViewType.Conditionals:
                        return this.SquareHeight * this.Variables.ConditionalsWeapon.DaggerGivenAuburn;
                    case ViewType.Joint:
                        return this.SquareHeight * this.Variables.JointWeapon.DaggerAuburn / this.Variables.MurdererMarginals.Auburn;
                    default:
                        return this.SquareHeight;
                }
            }
        }

        /// <summary>
        /// Gets the dagger opacity.
        /// </summary>
        public double DaggerOpacity
        {
            get
            {
                return this.Variables.WeaponObserved == Weapon.Revolver ? this.Opacity : 1.0;
            }
        }

        /// <summary>
        /// Gets the grey width.
        /// </summary>
        public double GreyWidth
        {
            get
            {
                switch (this.ViewType)
                {
                    case ViewType.Conditionals:
                        return this.SquareWidth;
                    case ViewType.Posteriors:
                        return this.SquareWidth * this.Variables.Posteriors.Grey;
                    default:
                        return this.SquareWidth * this.Variables.MurdererMarginals.Grey;
                }
            }
        }

        /// <summary>
        /// Gets the auburn width.
        /// </summary>
        public double AuburnWidth
        {
            get
            {
                switch (this.ViewType)
                {
                    case ViewType.Conditionals:
                        return this.SquareWidth;
                    case ViewType.Posteriors:
                        return this.SquareWidth * this.Variables.Posteriors.Auburn;
                    default:
                        return this.SquareWidth * this.Variables.MurdererMarginals.Auburn;
                }
            }
        }

        /// <summary>
        /// Gets the grey posterior text.
        /// </summary>
        public string GreyPosteriorText
        {
            get
            {
                return this.Variables.Posteriors.Grey.ToString(this.StringFormat);
            }
        }

        /// <summary>
        /// Gets the grey total text.
        /// </summary>
        public string GreyTotalText
        {
            get
            {
                switch (this.ViewType)
                {
                    case ViewType.Conditionals:
                        return 1.0.ToString(this.StringFormat);
                    case ViewType.Posteriors:
                        return this.GreyPosteriorText;
                    default:
                        return this.Variables.MurdererMarginals.Grey.ToString(this.StringFormat);
                }
            }
        }

        /// <summary>
        /// Gets the auburn posterior text.
        /// </summary>
        public string AuburnPosteriorText
        {
            get
            {
                return this.Variables.Posteriors.Auburn.ToString(this.StringFormat);
            }
        }

        /// <summary>
        /// Gets the grey total text.
        /// </summary>
        public string AuburnTotalText
        {
            get
            {
                switch (this.ViewType)
                {
                    case ViewType.Conditionals:
                        return 1.0.ToString(this.StringFormat);
                    case ViewType.Posteriors:
                        return this.AuburnPosteriorText;
                    default:
                        return this.Variables.MurdererMarginals.Auburn.ToString(this.StringFormat);
                }
            }
        }

        /// <summary>
        /// Gets the grey revolver text.
        /// </summary>
        public string GreyRevolverText
        {
            get
            {
                switch (this.ViewType)
                {
                    case ViewType.Conditionals:
                        return this.Variables.ConditionalsWeapon.RevolverGivenGrey.ToString(this.StringFormat);
                    case ViewType.Joint:
                        return this.Variables.JointWeapon.RevolverGrey.ToString(this.StringFormat);
                    default:
                        return string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets the grey revolver marginal text.
        /// </summary>
        public string GreyRevolverMarginalText
        {
            get
            {
                switch (this.ViewType)
                {
                    case ViewType.Conditionals:
                        return this.Variables.ConditionalsWeapon.RevolverGivenGrey.ToString(this.StringFormat);
                    case ViewType.Joint:
                        return (this.Variables.JointWeapon.RevolverGrey / this.Variables.MurdererMarginals.Grey).ToString(this.StringFormat);
                    default:
                        return string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets the grey dagger text.
        /// </summary>
        public string GreyDaggerText
        {
            get
            {
                switch (this.ViewType)
                {
                    case ViewType.Conditionals:
                        return this.Variables.ConditionalsWeapon.DaggerGivenGrey.ToString(this.StringFormat);
                    case ViewType.Joint:
                        return this.Variables.JointWeapon.DaggerGrey.ToString(this.StringFormat);
                    default:
                        return string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets the grey dagger marginal text.
        /// </summary>
        public string GreyDaggerMarginalText
        {
            get
            {
                switch (this.ViewType)
                {
                    case ViewType.Conditionals:
                        return this.Variables.ConditionalsWeapon.DaggerGivenGrey.ToString(this.StringFormat);
                    case ViewType.Joint:
                        return (this.Variables.JointWeapon.DaggerGrey / this.Variables.MurdererMarginals.Grey).ToString(this.StringFormat);
                    default:
                        return string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets the auburn revolver text.
        /// </summary>
        public string AuburnRevolverText
        {
            get
            {
                switch (this.ViewType)
                {
                    case ViewType.Conditionals:
                        return this.Variables.ConditionalsWeapon.RevolverGivenAuburn.ToString(this.StringFormat);
                    case ViewType.Joint:
                        return this.Variables.JointWeapon.RevolverAuburn.ToString(this.StringFormat);
                    default:
                        return string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets the auburn revolver marginal text.
        /// </summary>
        public string AuburnRevolverMarginalText
        {
            get
            {
                switch (this.ViewType)
                {
                    case ViewType.Conditionals:
                        return this.Variables.ConditionalsWeapon.RevolverGivenAuburn.ToString(this.StringFormat);
                    case ViewType.Joint:
                        return (this.Variables.JointWeapon.RevolverAuburn / this.Variables.MurdererMarginals.Auburn).ToString(this.StringFormat);
                    default:
                        return string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets the auburn dagger text.
        /// </summary>
        public string AuburnDaggerText
        {
            get
            {
                switch (this.ViewType)
                {
                    case ViewType.Conditionals:
                        return this.Variables.ConditionalsWeapon.DaggerGivenAuburn.ToString(this.StringFormat);
                    case ViewType.Joint:
                        return this.Variables.JointWeapon.DaggerAuburn.ToString(this.StringFormat);
                    default:
                        return string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets the auburn dagger marginal text.
        /// </summary>
        public string AuburnDaggerMarginalText
        {
            get
            {
                switch (this.ViewType)
                {
                    case ViewType.Conditionals:
                        return this.Variables.ConditionalsWeapon.DaggerGivenAuburn.ToString(this.StringFormat);
                    case ViewType.Joint:
                        return (this.Variables.JointWeapon.DaggerAuburn / this.Variables.MurdererMarginals.Auburn).ToString(this.StringFormat);
                    default:
                        return string.Empty;
                }
            }
        }

        /// <summary>
        /// Gets the total text.
        /// </summary>
        public string TotalText
        {
            get
            {
                return 1.0.ToString(this.StringFormat);
            }
        }

        /// <summary>
        /// Gets or sets the grey visibility.
        /// </summary>
        public Visibility GreyVisibility
        {
            get
            {
                return this.greyVisibility;
            }

            set
            {
                if (value == this.greyVisibility)
                {
                    return;
                }

                this.greyVisibility = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged("WhiteStripeVisibility");
            }
        }

        /// <summary>
        /// Gets or sets the auburn visibility.
        /// </summary>
        public Visibility AuburnVisibility
        {
            get
            {
                return this.auburnVisibility;
            }

            set
            {
                if (value == this.auburnVisibility)
                {
                    return;
                }

                this.auburnVisibility = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged("WhiteStripeVisibility");
            }
        }

        /// <summary>
        /// Gets the white stripe visibility.
        /// </summary>
        public Visibility WhiteStripeVisibility
        {
            get
            {
                return this.GreyVisibility == Visibility.Visible && this.AuburnVisibility == Visibility.Visible
                       && this.ConditionalsVisibility == Visibility.Visible
                           ? Visibility.Visible
                           : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Gets the murderer only visibility.
        /// </summary>
        public Visibility MurdererOnlyVisibility
        {
            get
            {
                return this.ViewType == ViewType.Priors || this.ViewType == ViewType.Posteriors ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Gets the conditionals visibility.
        /// </summary>
        public Visibility ConditionalsVisibility
        {
            get
            {
                return this.ViewType == ViewType.Conditionals ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Gets the joint visibility.
        /// </summary>
        public Visibility JointVisibility
        {
            get
            {
                return this.ViewType == ViewType.Joint ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Gets the not conditionals visibility.
        /// </summary>
        public Visibility NotConditionalsVisibility
        {
            get
            {
                return this.ViewType == ViewType.Conditionals 
                           ? Visibility.Collapsed 
                           : Visibility.Visible;
            }
        }

        /// <summary>
        /// Gets the auburn column.
        /// </summary>
        public int AuburnColumn
        {
            get
            {
                return this.ViewType == ViewType.Conditionals ? 2 : 1;
            }
        }

        /// <summary>
        /// Gets the auburn axis label column.
        /// </summary>
        public int AuburnAxisLabelColumn
        {
            get
            {
                return this.ViewType == ViewType.Conditionals ? 0 : 1;
            }
        }


        /// <summary>
        /// Gets the auburn text column.
        /// </summary>
        public int AuburnTextColumn
        {
            get
            {
                return this.ViewType == ViewType.Conditionals ? 1 : 2;
            }
        }

        /// <summary>
        /// Gets the grey tooltip.
        /// </summary>
        public string GreyTooltip
        {
            get
            {
                return this.ViewType == ViewType.Priors
                           ? string.Format("P(murderer=Grey) = {0}", this.GreyTotalText)
                           : string.Format("P(murderer=Grey | weapon=Revolver) = {0}", this.GreyTotalText);
            }
        }

        /// <summary>
        /// Gets the auburn tooltip.
        /// </summary>
        public string AuburnTooltip
        {
            get
            {
                return this.ViewType == ViewType.Priors
                           ? string.Format("P(murderer=Auburn) = {0}", this.AuburnTotalText)
                           : string.Format("P(murderer=Auburn | Weapon=Revolver) = {0}", this.AuburnTotalText);
            }
        }

        /// <summary>
        /// Gets the grey revolver tooltip.
        /// </summary>
        public string GreyRevolverTooltip
        {
            get
            {
                return this.ViewType == ViewType.Conditionals
                           ? string.Format("P(murderer=Grey | weapon=Revolver) = {0}", this.GreyRevolverText)
                           : this.Variables.WeaponObserved == Weapon.Revolver
                                 ? string.Format("P(murderer=Grey, weapon=Revolver) = {0}", this.GreyPosteriorText)
                                 : string.Format("P(murderer=Grey, weapon=Revolver) = {0}", this.GreyRevolverText);
            }
        }

        /// <summary>
        /// Gets a value indicating whether grey revolver tooltip enabled.
        /// </summary>
        public bool GreyRevolverTooltipEnabled
        {
            get
            {
                return this.Variables.WeaponObserved == Weapon.Unknown || this.Variables.WeaponObserved == Weapon.Revolver;
            }
        }

        /// <summary>
        /// Gets the grey dagger tooltip.
        /// </summary>
        public string GreyDaggerTooltip
        {
            get
            {
                return this.ViewType == ViewType.Conditionals
                           ? string.Format("P(murderer=Grey | weapon=Dagger) = {0}", this.GreyDaggerText)
                           : this.Variables.WeaponObserved == Weapon.Dagger
                                 ? string.Format("P(murderer=Grey, weapon=Dagger) = {0}", this.GreyPosteriorText)
                                 : string.Format("P(murderer=Grey, weapon=Dagger) = {0}", this.GreyDaggerText);
            }
        }

        /// <summary>
        /// Gets a value indicating whether grey dagger tooltip enabled.
        /// </summary>
        public bool GreyDaggerTooltipEnabled
        {
            get
            {
                return this.Variables.WeaponObserved == Weapon.Unknown || this.Variables.WeaponObserved == Weapon.Dagger;
            }
        }

        /// <summary>
        /// Gets the auburn dagger tooltip.
        /// </summary>
        public string AuburnDaggerTooltip
        {
            get
            {
                return this.ViewType == ViewType.Conditionals
                           ? string.Format("P(murderer=Auburn | weapon=Dagger) = {0}", this.AuburnDaggerText)
                           : this.Variables.WeaponObserved == Weapon.Dagger
                                 ? string.Format("P(murderer=Auburn, weapon=Dagger) = {0}", this.AuburnPosteriorText)
                                 : string.Format("P(murderer=Auburn, weapon=Dagger) = {0}", this.AuburnDaggerText);
            }
        }

        /// <summary>
        /// Gets a value indicating whether auburn dagger tooltip enabled.
        /// </summary>
        public bool AuburnDaggerTooltipEnabled
        {
            get
            {
                return this.Variables.WeaponObserved == Weapon.Unknown || this.Variables.WeaponObserved == Weapon.Dagger;
            }
        }

        /// <summary>
        /// Gets the auburn revolver tooltip.
        /// </summary>
        public string AuburnRevolverTooltip
        {
            get
            {
                return this.ViewType == ViewType.Conditionals
                           ? string.Format("P(murderer=Auburn | weapon=Revolver) = {0}", this.AuburnRevolverText)
                           : this.Variables.WeaponObserved == Weapon.Revolver
                                 ? string.Format("P(murderer=Auburn, weapon=Revolver) = {0}", this.AuburnPosteriorText)
                                 : string.Format("P(murderer=Auburn, weapon=Revolver) = {0}", this.AuburnRevolverText);
            }
        }

        /// <summary>
        /// Gets a value indicating whether auburn revolver tooltip enabled.
        /// </summary>
        public bool AuburnRevolverTooltipEnabled
        {
            get
            {
                return this.Variables.WeaponObserved == Weapon.Unknown || this.Variables.WeaponObserved == Weapon.Revolver;
            }
        }

        /// <summary>
        /// Sets up the view model.
        /// </summary>
        /// <param name="showGrey">if set to <c>true</c> [show grey].</param>
        /// <param name="showAuburn">if set to <c>true</c> [show auburn].</param>
        public void SetUpViewModel(bool showGrey, bool showAuburn)
        {
            var greyVis = Visibility.Collapsed;
            var auburnVis = Visibility.Collapsed;

            // Set the view type according to variables present
            var vt = ViewType.Priors;

            if (this.Variables.Posteriors != null)
            {
                vt = ViewType.Posteriors;
            }
            else if (this.Variables.JointWeapon != null)
            {
                vt = ViewType.Joint;
                greyVis = Visibility.Visible;
                auburnVis = Visibility.Visible;
            }
            else if (this.Variables.ConditionalsWeapon != null)
            {
                vt = ViewType.Conditionals;
                greyVis = showGrey ? Visibility.Visible : Visibility.Collapsed;
                auburnVis = showAuburn ? Visibility.Visible : Visibility.Collapsed;
            }

            this.GreyVisibility = greyVis;
            this.AuburnVisibility = auburnVis;
            this.ColumnGap = vt == ViewType.Conditionals ? 40.0 : 0.0;
            this.ViewType = vt;
        }

        /// <summary>
        /// Create VariablesViewModel object from Variables.
        /// </summary>
        /// <param name="variables">The variables.</param>
        /// <returns>The <see cref="VariablesViewModel"/></returns>
        internal static VariablesViewModel FromVariables(Variables variables)
        {
            var viewModel = new VariablesViewModel
                       {
                           SquareWidth = 400,
                           SquareHeight = 400,
                           Variables = variables,
                           Opacity = 0.1,
                           StringFormat = "F"
                       };

            viewModel.SetUpViewModel(true, true);

            return viewModel;
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