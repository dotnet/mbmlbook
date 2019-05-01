// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AssessingPeoplesSkills
{
    using System;
    using MBMLCommon;
    using Microsoft.ML.Probabilistic.Models;

    /// <summary>
    /// The program.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The entry point for the application.
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
            ModelRunner runner = new ModelRunner(outputter);

            Console.WriteLine($"\n{Contents.S2TestingOutTheModel.NumberedName}.\n");
            runner.ToyWith3QuestionsAnd2Skills();
            Console.WriteLine($"\n{Contents.S3Loopiness.NumberedName}.\n");
            runner.LoopyExample();

            // Inference on the real data. Sections 4-6.
            runner.RealDataInference();

            // PDF demo
            Console.WriteLine("PDF demonstration");
            runner.ProbabilityDensityFunctionDemo();

            // Demo of Beta distribution
            Console.WriteLine("Beta function demonstration");
            runner.BetaDemo();

            // For Beta self assessment
            runner.BetaSelfAssessment();

            Console.WriteLine("\nCompleted all experiments.");
        }

        private static void InitializeUI()
        {
#if NETFULL
            // Initialise MBMLViews/Glo
            var types = new[]
                            {
                                typeof(MBMLViews.Views.MatrixCanvasView),
                                typeof(Views.ExperimentView),
                                typeof(Experiment),
                                typeof(ExperimentComparison)
                            };
            InferenceEngine.Visualizer = new Microsoft.ML.Probabilistic.Compiler.Visualizers.WindowsVisualizer();
#endif
        }
    }
}
