// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MurderMystery
{
    using System;
    using Microsoft.ML.Probabilistic.Models;
#if NETFULL
    using VariablesCollection = System.Collections.Generic.Dictionary<string, VariablesViewModel>;
#else
    using VariablesCollection = System.Collections.Generic.Dictionary<string, Variables>;
#endif
    using System.Collections.Generic;
    using MBMLCommon;
    
    public static class Program
    {
        /// <summary>
        /// Defines the entry point of the application.
        /// The first argument, if present, sets the folder to save the output artifacts in.
        /// </summary>
        public static void Main(string[] args)
        {
            InitializeUI();
            Outputter outputter = Outputter.GetOutputter(Contents.ChapterName);

            try
            {
                RunExperiments(outputter);
            }
            catch (Exception e)
            {
                Console.WriteLine($"\nAn unhandled exception was thrown:\n{e}");
            }
            finally
            {
                if (args.Length == 1)
                {
                    Console.WriteLine("\n\nSaving outputs...");
                    outputter.SaveOutputAsProducedFlattening(args[0]);
                    Console.WriteLine("Done saving.");
                }
            }
        }

        public static void RunExperiments(Outputter outputter)
        {
            var engine = new InferenceEngine();
            engine.ShowFactorGraph = false;

            // Input probabilities
            var priors = new MurdererProbs { Grey = 0.3, Auburn = 0.7 };

            var conditionalsWeapon = new Variables.ConditionalVariablesWeapon
            {
                RevolverGivenGrey = 0.9,
                DaggerGivenGrey = 0.1,
                RevolverGivenAuburn = 0.2,
                DaggerGivenAuburn = 0.8,
            };

            var conditionalsHair = new Variables.ConditionalVariablesHair
            {
                HairGivenGrey = 0.5,
                HairGivenAuburn = 0.05
            };

            Console.WriteLine($"\n{Contents.S0AMurderMystery.NumberedName}.\n");
            var priorKnowledgeModel = new PriorKnowledgeModel
            {
                Engine = engine,
                Name = "Priors",
                Priors = priors
            };
            outputter.Out(priorKnowledgeModel, Contents.S0AMurderMystery.NumberedName, priorKnowledgeModel.Name + " Model");
            var priorKnowledge = priorKnowledgeModel.DoInference();
            var priorKnowledgeOutput = GenerateOutput(priorKnowledge);
            outputter.Out(priorKnowledgeOutput, Contents.S0AMurderMystery.NumberedName, priorKnowledge.Name);

            Console.WriteLine($"\n{Contents.S1IncorporatingEvidence.NumberedName}.\n");
            var conditionalsWeaponUnknown = new Variables
            {
                Name = "Conditionals",
                MurdererMarginals = priors,
                ConditionalsWeapon = conditionalsWeapon,
                WeaponObserved = Weapon.Unknown
            };
            var conditionalsWeaponUnknownOutput = GenerateOutput(conditionalsWeaponUnknown);
            outputter.Out(conditionalsWeaponUnknownOutput, Contents.S1IncorporatingEvidence.NumberedName, conditionalsWeaponUnknown.Name);

            Console.WriteLine($"\n{Contents.S2AModelOfAMurder.NumberedName}.\n");
            var jointWeaponUnknown = new Variables
            {
                Name = "Joint",
                MurdererMarginals = priors,
                JointWeapon = conditionalsWeaponUnknown.GetJointForWeapon(),
                WeaponObserved = Weapon.Unknown
            };
            var jointWeaponUnknownOutput = GenerateOutput(jointWeaponUnknown);
            outputter.Out(jointWeaponUnknownOutput, Contents.S2AModelOfAMurder.NumberedName, jointWeaponUnknown.Name);

            var jointWeaponObserved = new Variables
            {
                Name = "Joint-WeaponObserved",
                MurdererMarginals = priors,
                JointWeapon = conditionalsWeaponUnknown.GetJointForWeapon(),
                WeaponObserved = Weapon.Revolver
            };
            var jointWeaponObservedOutput = GenerateOutput(jointWeaponObserved);
            outputter.Out(jointWeaponObservedOutput, Contents.S2AModelOfAMurder.NumberedName, jointWeaponObserved.Name);

            var observedWeaponModel = new ObservedWeaponModel
            {
                Engine = engine,
                Name = "Observed Weapon",
                Priors = priors,
                ConditionalsWeapon = conditionalsWeapon,
                WeaponObserved = Weapon.Revolver
            };
            Console.WriteLine($"Running {observedWeaponModel.Name} Model");
            outputter.Out(observedWeaponModel, Contents.S2AModelOfAMurder.NumberedName, observedWeaponModel.Name + " Model");
            var posteriors = observedWeaponModel.DoInference();
            var posteriorsOutput = GenerateOutput(posteriors);
            outputter.Out(posteriorsOutput, Contents.S2AModelOfAMurder.NumberedName, posteriors.Name);

            var variables = new VariablesCollection
                                    {
                                        { "Priors", priorKnowledgeOutput },
                                        { "Conditionals", conditionalsWeaponUnknownOutput },
                                        { "Joint", jointWeaponUnknownOutput },
                                        { "JointWeaponObserved", jointWeaponObservedOutput },
                                        { "Posteriors", posteriorsOutput }
                                    };

            outputter.Out(variables, Contents.S2AModelOfAMurder.NumberedName, "AllProbabilityPlots");

            Console.WriteLine($"\n{Contents.S4ExtendingTheModel.NumberedName}\n");
            var observedHairModel = new ObservedHairModel
            {
                Engine = engine,
                Name = "Observed Hair",
                Priors = priors,
                ConditionalsWeapon = conditionalsWeapon,
                WeaponObserved = Weapon.Revolver,
                ConditionalsHair = conditionalsHair,
                HairObserved = true
            };
            Console.WriteLine($"Running {observedHairModel.Name} Model");
            outputter.Out(observedHairModel, Contents.S4ExtendingTheModel.NumberedName, observedHairModel.Name + " Model");
            observedHairModel.DoInference();

            var progression = new Dictionary<string, Dictionary<string, double>>();
            var times = new[] { "Prior", "After observing weapon", "After observing hair" };
            progression.Add("Grey", new Dictionary<string, double>
                {
                    { times[0] , priorKnowledgeModel.Priors.Grey },
                    { times[1] , observedWeaponModel.Posteriors.Grey },
                    { times[2] , observedHairModel.Posteriors.Grey }
                });
            progression.Add("Auburn", new Dictionary<string, double>
                {
                    { times[0] , priorKnowledgeModel.Priors.Auburn },
                    { times[1] , observedWeaponModel.Posteriors.Auburn },
                    { times[2] , observedHairModel.Posteriors.Auburn }
                });

            outputter.Out(progression, Contents.S4ExtendingTheModel.NumberedName, "Progression");

            Console.WriteLine("\nCompleted all experiments.");
        }

#if NETFULL
        private static VariablesViewModel GenerateOutput(Variables priorKnowledge)
        {
            return VariablesViewModel.FromVariables(priorKnowledge);
        }
#else
        private static Variables GenerateOutput(Variables priorKnowledge)
        {
            return priorKnowledge;
        }
#endif

        private static void InitializeUI()
        {
#if NETFULL
            // Initialise MBMLViews/Glo
            var types = new[]
                            {
                                typeof(MBMLViews.Views.MatrixCanvasView),
                                typeof(VariablesView)
                            };
            InferenceEngine.Visualizer = new Microsoft.ML.Probabilistic.Compiler.Visualizers.WindowsVisualizer();
#endif
        }
    }
}
