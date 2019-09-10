// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using Microsoft.ML.Probabilistic.Models;
using Microsoft.ML.Probabilistic.Distributions;

namespace UnderstandingAsthma
{
    /// <summary>
    /// A model for a clinical trial with a treated and a control group.
    /// </summary>
    public class ClinicalTrialModel
    {
        private Variable<int> numberControl;
        private Variable<int> numberTreated;
        private VariableArray<bool> recoveredControl;
        private VariableArray<bool> recoveredTreated;
        private Variable<bool> model;
        private Variable<double> probControl;
        private Variable<double> probTreated;
        private Variable<double> probRecovery;

        public class Posteriors
        {
            public Bernoulli TreatmentHasEffect;
            public Beta ProbIfControl;
            public Beta ProbIfTreated;
        }

        // The inference engine
        public InferenceEngine Engine = new InferenceEngine { };

        public ClinicalTrialModel()
        {
            numberControl = Variable.New<int>().Named("numberControl");
            numberTreated = Variable.New<int>().Named("numberTreated");
            Range controlGroup = new Range(numberControl).Named("control group");
            Range treatedGroup = new Range(numberTreated).Named("treated group");
            recoveredControl = Variable.Array<bool>(controlGroup).Named("recoveredControl");
            recoveredTreated = Variable.Array<bool>(treatedGroup).Named("recoveredTreated");

            // Whether the treatment is effective. Use uniform prior.
            model = Variable.Bernoulli(0.5).Named("model");
            using (Variable.If(model))
            {
                // Model if treatment is effective. Use uniform priors.
                probControl = Variable.Beta(1, 1).Named("probControl");
                recoveredControl[controlGroup] = Variable.Bernoulli(probControl).ForEach(controlGroup);
                probTreated = Variable.Beta(1, 1).Named("probTreated");
                recoveredTreated[treatedGroup] = Variable.Bernoulli(probTreated).ForEach(treatedGroup);
            }

            using (Variable.IfNot(model))
            {
                // Model if treatment is not effective. Use uniform prior.
                probRecovery = Variable.Beta(1, 1).Named("probRecovery");
                recoveredControl[controlGroup] = Variable.Bernoulli(probRecovery).ForEach(controlGroup);
                recoveredTreated[treatedGroup] = Variable.Bernoulli(probRecovery).ForEach(treatedGroup);
            }

            Engine = new InferenceEngine()
            {
                ShowProgress = false
            };
        }

        public Posteriors Run(bool[] recoveredControl, bool[] recoveredTreated, bool showFactorGraph = false)
        {
            Engine.ShowFactorGraph = showFactorGraph;
            // Set the observed values
            numberControl.ObservedValue = recoveredControl.Length;
            numberTreated.ObservedValue = recoveredTreated.Length;
            this.recoveredControl.ObservedValue = recoveredControl;
            this.recoveredTreated.ObservedValue = recoveredTreated;

            var result = new Posteriors();
            result.TreatmentHasEffect = Engine.Infer<Bernoulli>(model);
            result.ProbIfControl = Engine.Infer<Beta>(probControl);
            result.ProbIfTreated = Engine.Infer<Beta>(probTreated);

            return result;
        }
    }
}
