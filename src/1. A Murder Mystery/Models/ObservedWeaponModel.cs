// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using Microsoft.ML.Probabilistic.Distributions;
using Microsoft.ML.Probabilistic.Models;

namespace MurderMystery
{
    /// <summary>
    /// Model that includes the observation of the murder weapon
    /// </summary>
    class ObservedWeaponModel : PriorKnowledgeModel
    {
        /// <summary>
        /// Conditional probabilities for observing different weapons
        /// </summary>
        public Variables.ConditionalVariablesWeapon ConditionalsWeapon { get; set; }

        /// <summary>
        /// Observed weapon
        /// </summary>
        public Weapon WeaponObserved { get; set; }

        /// <summary>
        /// Infer.Net model's variable. 0, if observed weapon is the revolver, 1, if it's the dagger.
        /// </summary>
        protected Variable<int> weapon;

        protected override void ConstructModel()
        {
            base.ConstructModel();
            if (ConditionalsWeapon == null)
                throw new InvalidOperationException($"{nameof(ConditionalsWeapon)} cannot be null.");

            weapon = Variable.New<int>().Named("weapon=revolver");
            weapon.SetValueRange(new Range(2));

            using (Variable.If(murderer))
            {
                weapon.SetTo(Variable.Discrete(new[] { ConditionalsWeapon.RevolverGivenAuburn, ConditionalsWeapon.DaggerGivenAuburn }));
            }

            using (Variable.IfNot(murderer))
            {
                weapon.SetTo(Variable.Discrete(new[] { ConditionalsWeapon.RevolverGivenGrey, ConditionalsWeapon.DaggerGivenGrey }));
            }

            weapon.ObservedValue = WeaponObserved == Weapon.Revolver ? 0 : 1;
        }

        protected override void ComputePosteriors()
        {
            if (Engine == null)
                throw new InvalidOperationException($"{nameof(Engine)} cannot be null.");

            var posterior = Engine.Infer<Bernoulli>(murderer);
            Posteriors = new MurdererProbs { Grey = Math.Exp(posterior.GetLogProbFalse()), Auburn = Math.Exp(posterior.GetLogProbTrue()) };
        }

        protected override Variables Variables => new Variables
        {
            MurdererMarginals = Priors,
            ConditionalsWeapon = ConditionalsWeapon,
            Name = Name,
            WeaponObserved = WeaponObserved,
            Posteriors = Posteriors
        };
    }
}
