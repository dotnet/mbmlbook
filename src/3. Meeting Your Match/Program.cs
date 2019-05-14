// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MeetingYourMatch
{
    using System;

    using Microsoft.ML.Probabilistic.Distributions;
    using Microsoft.ML.Probabilistic.Models;
    using MBMLCommon;

    /// <summary>
    /// The program.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point.
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
            // This flag runs experiments which are not fully described in the book
            // but may be helpful to understand the text. 
            var runAdditionalExperiments = false;
            var showFactorGraphs = false;
            var modelRunner = new ModelRunner(outputter, showFactorGraphs);
            var demoFigures = new DemoFigures();

            // Reset the random number generator
            Microsoft.ML.Probabilistic.Math.Rand.Restart(0);

            //Section 0
            Console.WriteLine($"\n{Contents.S0MeetingYourMatch.NumberedName}.\n");

            //Section 1
            Console.WriteLine($"\n{Contents.S1ModellingTheOutcomeOfGames.NumberedName}.\n");

            outputter.Out(demoFigures.PerformanceSpace,
                Contents.S1ModellingTheOutcomeOfGames.NumberedName,
                "Jill's and Fred's performances");

            outputter.Out(demoFigures.PerformanceCurve,
                Contents.S1ModellingTheOutcomeOfGames.NumberedName,
                "Bell curve");

            outputter.Out(demoFigures.JillFred,
                Contents.S1ModellingTheOutcomeOfGames.NumberedName,
                "Gaussian distributions of performance for Jill and Fred");

            outputter.Out(demoFigures.PerformanceSpaceSamples,
                Contents.S1ModellingTheOutcomeOfGames.NumberedName,
                "Samples of Jill and Fred's performances");

            // Plot of performance space with additional samples
            modelRunner.JillFredSamples();

            outputter.Out(demoFigures.Gaussians,
                Contents.S1ModellingTheOutcomeOfGames.NumberedName,
                "Gaussian distributions");

            outputter.Out(demoFigures.CumGauss,
                Contents.S1ModellingTheOutcomeOfGames.NumberedName,
                "Gaussian cumulative distribution function");

            outputter.Out(demoFigures.CumGaussWithShaded,
                Contents.S1ModellingTheOutcomeOfGames.NumberedName,
                "Cumulative Gaussian distribution function with shaded Gaussian");

            //Section 2-3
            Console.WriteLine($"\n{Contents.S2InferringThePlayersSkills.NumberedName}.\n");

            outputter.Out(demoFigures.SampledPerformanceDistributions,
                Contents.S2InferringThePlayersSkills.NumberedName,
                "Sampling approximation to the computation of message");

            modelRunner.EpMessageExample();

            //Section 3

            modelRunner.HeadToHead();

            //Section 4

            Console.WriteLine($"\n{Contents.S4ExtensionsToTheCoreModel.NumberedName}.\n");

            outputter.Out(demoFigures.PerformanceSpaceWithDraws,
                Contents.S4ExtensionsToTheCoreModel.NumberedName,
                "Space where the game ends in a draw");

            modelRunner.ThreePlayerToyExperiment();
            modelRunner.SmallTeams();

            if (runAdditionalExperiments)
            {
                modelRunner.FreeForAll();
                modelRunner.LargeTeams();
            }

            outputter.Out(modelRunner.LogProbs,
                Contents.S4ExtensionsToTheCoreModel.NumberedName,
                "LogProbs");

            // Section 5
            Console.WriteLine($"\n{Contents.S5AllowingTheSkillsToVary.NumberedName}.\n");

            // Demo of TTT
            modelRunner.DynamicsDemo(5, 5, 1);

            Console.WriteLine("\nCompleted all experiments.");
        }

        private static void InitializeUI()
        {
#if NETFULL
            // Initialise MBMLViews/Glo
            var types = new[] { typeof(Gaussian), typeof(MBMLViews.Views.GaussianView) };
            InferenceEngine.Visualizer = new Microsoft.ML.Probabilistic.Compiler.Visualizers.WindowsVisualizer();
#endif
        }
    }
}
