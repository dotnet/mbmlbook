// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MurderMystery
{
    /// <summary>
    /// The murder mystery variables.
    /// </summary>
    public class Variables
    {
        /// <summary>
        /// Gets or sets the murderer marginal.
        /// </summary>
        public MurdererProbs MurdererMarginals { get; set; }

        /// <summary>
        /// Gets or sets the posteriors.
        /// </summary>
        public MurdererProbs Posteriors { get; set; }
   
        /// <summary>
        /// Gets or sets the weapon observed.
        /// </summary>
        public Weapon WeaponObserved { get; set; }

        /// <summary>
        /// Gets or sets whether major Grey's hair was observed.
        /// </summary>
        public bool HairObserved { get; set; }

        /// <summary>
        /// Gets or sets the conditionals for the observed weapon.
        /// </summary>
        public ConditionalVariablesWeapon ConditionalsWeapon { get; set; }
        
        /// <summary>
        /// Gets or sets the conditionals for the observed hair.
        /// </summary>
        public ConditionalVariablesHair ConditionalsHair { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the joint distribution for murderer and weapon.
        /// </summary>
        public JointVariablesWeapon JointWeapon { get; set; }

        /// <summary>
        /// Gets the joint distribution for murderer and weapon.
        /// </summary>
        public JointVariablesWeapon GetJointForWeapon()
        {
            if (this.ConditionalsWeapon == null)
            {
                return null;
            }

            return new JointVariablesWeapon
            {
                RevolverGrey = this.MurdererMarginals.Grey * this.ConditionalsWeapon.RevolverGivenGrey,
                DaggerGrey = this.MurdererMarginals.Grey * this.ConditionalsWeapon.DaggerGivenGrey,
                RevolverAuburn = this.MurdererMarginals.Auburn * this.ConditionalsWeapon.RevolverGivenAuburn,
                DaggerAuburn = this.MurdererMarginals.Auburn * this.ConditionalsWeapon.DaggerGivenAuburn
            };
        }



        /// <summary>
        /// The joint probabilities for weapon and murderer.
        /// </summary>
        public class JointVariablesWeapon
        {
            /// <summary>
            /// P(weapon=revolver, murderer=grey).
            /// </summary>
            public double RevolverGrey { get; set; }

            /// <summary>
            /// P(weapon=revolver, murderer=auburn).
            /// </summary>
            public double RevolverAuburn { get; set; }

            /// <summary>
            /// P(weapon=dagger, murderer=grey).
            /// </summary>
            public double DaggerGrey { get; set; }

            /// <summary>
            /// P(weapon=dagger, murderer=auburn).
            /// </summary>
            public double DaggerAuburn { get; set; }
        }

        /// <summary>
        /// The conditionals for the observed weapon.
        /// </summary>
        public class ConditionalVariablesWeapon
        {
            /// <summary>
            /// P(weapon=revolver | murderer=grey).
            /// </summary>
            public double RevolverGivenGrey { get; set; }

            /// <summary>
            /// P(weapon=revolver | murderer=auburn).
            /// </summary>
            public double RevolverGivenAuburn { get; set; }

            /// <summary>
            /// P(weapon=dagger | murderer=grey).
            /// </summary>
            public double DaggerGivenGrey { get; set; }

            /// <summary>
            /// P(weapon=dagger | murderer=auburn).
            /// </summary>
            public double DaggerGivenAuburn { get; set; }
        }

        /// <summary>
        /// Conditionals for the observed hair.
        /// </summary>
        public class ConditionalVariablesHair
        {
            /// <summary>
            /// Probality for observation of Grey's hair, if Grey is the murderer
            /// </summary>
            public double HairGivenGrey { get; set; }
            /// <summary>
            /// Probality for observation of Grey's hair, if Auburn is the murderer
            /// </summary>
            public double HairGivenAuburn { get; set; }
        }
    }

    /// <summary>
    /// The murderer.
    /// </summary>
    public class MurdererProbs
    {
        /// <summary>
        /// Probability of Grey being the murderer.
        /// </summary>
        public double Grey { get; set; }

        /// <summary>
        /// Probability of Auburn being the murderer.
        /// </summary>
        public double Auburn { get; set; }
    }
}