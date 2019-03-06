// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using Microsoft.ML.Probabilistic.Models;

namespace MurderMystery
{
    /// <summary>
    /// Model that includes observations of both the weapon and the hair
    /// </summary>
    class ObservedHairModel : ObservedWeaponModel
    {
        /// <summary>
        /// Conditional probabilities for observation of the hair
        /// </summary>
        public Variables.ConditionalVariablesHair ConditionalsHair { get; set; }

        /// <summary>
        /// True, if Grey's hair was observer, false otherwise
        /// </summary>
        public bool HairObserved { get; set; }

        /// <summary>
        /// Infer.Net model's variable. True, if Grey's hair was observer, false otherwise
        /// </summary>
        protected Variable<bool> hair;

        protected override void ConstructModel()
        {
            base.ConstructModel();
            if (ConditionalsHair == null)
                throw new InvalidOperationException($"{nameof(ConditionalsHair)} cannot be null.");

            hair = Variable.New<bool>().Named("hair=true");

            using (Variable.If(murderer))
            {
                hair.SetTo(Variable.Bernoulli(ConditionalsHair.HairGivenAuburn));
            }

            using (Variable.IfNot(murderer))
            {
                hair.SetTo(Variable.Bernoulli(ConditionalsHair.HairGivenGrey));
            }

            hair.ObservedValue = HairObserved;
        }

        protected override Variables Variables => new Variables
        {
            MurdererMarginals = Priors,
            ConditionalsWeapon = ConditionalsWeapon,
            ConditionalsHair = ConditionalsHair,
            Name = Name,
            WeaponObserved = WeaponObserved,
            HairObserved = HairObserved,
            Posteriors = Posteriors
        };
    }
}
