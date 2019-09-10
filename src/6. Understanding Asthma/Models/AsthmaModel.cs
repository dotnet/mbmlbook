// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Linq;
using Microsoft.ML.Probabilistic.Distributions;
using Microsoft.ML.Probabilistic.Math;
using Microsoft.ML.Probabilistic.Models;
using Microsoft.ML.Probabilistic.Utilities;

namespace UnderstandingAsthma
{
    /// <summary>
    /// A model of allergies, including skin prick and IgE tests, allowing sensitization gain/retain probabilities
    /// to depend on belonging of a child to one of multiple sensitization classes.
    /// </summary>
    public class AsthmaModel
    {
        private Variable<int> NumYears;
        private Variable<int> NumChildren;
        private Variable<int> NumAllergens;
        private Variable<int> NumVulnerabilities;
        private Range years;
        private Range children;
        private Range allergens;
        private Range classes;
        private VariableArray<VariableArray2D<bool>, bool[][,]> sensitized;
        private VariableArray<VariableArray2D<bool>, bool[][,]> skinTest;
        private VariableArray<VariableArray2D<bool>, bool[][,]> skinTestMissing;
        private VariableArray<VariableArray2D<bool>, bool[][,]> igeTest;
        private VariableArray<VariableArray2D<bool>, bool[][,]> igeTestMissing;
        private Variable<Vector> probSensClass;
        private VariableArray<int> sensClass;
        private VariableArray2D<double> probSens1;
        private VariableArray<VariableArray2D<double>, double[][,]> probGain;
        private VariableArray<VariableArray2D<double>, double[][,]> probRetain;
        private Variable<double> probSkinIfSens;
        private Variable<double> probSkinIfNotSens;
        private Variable<double> probIgeIfSens;
        private Variable<double> probIgeIfNotSens;

        // Priors
        private VariableArray2D<Beta> probSens1Prior;
        private VariableArray<VariableArray2D<Beta>, Beta[][,]> probGainPrior;
        private VariableArray<VariableArray2D<Beta>, Beta[][,]> probRetainPrior;
        private Variable<Dirichlet> probSensClassPrior;
        private Variable<Beta> probSkinIfSensPrior;
        private Variable<Beta> probSkinIfNotSensPrior;
        private Variable<Beta> probIgeIfSensPrior;
        private Variable<Beta> probIgeIfNotSensPrior;

        // Initializers
        private Variable<IDistribution<int[]>> sensClassInitializer;
        private bool BreakSymmetry = true;  // Only set to false for illustration purposes.

        // Engine
        private InferenceEngine Engine;

        /// <summary>
        /// Outputs of the Asthma model
        /// </summary>
        public class Beliefs : ICloneable
        {
            public Bernoulli[][,] Sensitization;
            public Beta ProbSkinIfSensitized;
            public Beta ProbSkinIfNotSensitized;
            public Beta ProbIgEIfSensitized;
            public Beta ProbIgEIfNotSensitized;
            public Beta[,] ProbSensitizationAgeOne;
            public Beta[][,] ProbGainSensitization;
            public Beta[][,] ProbRetainSensitization;
            public Dirichlet ProbVulnerabilityClass;
            public Discrete[] VulnerabilityClass;

            public object Clone()
            {
                Beliefs result = new Beliefs();
                result.Sensitization = (Bernoulli[][,])this.Sensitization.Clone();
                result.ProbSkinIfSensitized = this.ProbSkinIfSensitized;
                result.ProbSkinIfNotSensitized = this.ProbSkinIfNotSensitized;
                result.ProbIgEIfSensitized = this.ProbIgEIfSensitized;
                result.ProbIgEIfNotSensitized = this.ProbIgEIfNotSensitized;
                result.ProbSensitizationAgeOne = (Beta[,])this.ProbSensitizationAgeOne.Clone();
                result.ProbGainSensitization = (Beta[][,])this.ProbGainSensitization.Clone();
                result.ProbRetainSensitization = (Beta[][,])this.ProbRetainSensitization.Clone();
                result.ProbVulnerabilityClass = (Dirichlet)this.ProbVulnerabilityClass;
                result.VulnerabilityClass = (Discrete[])this.VulnerabilityClass;
                return result;
            }

            public int NumberOfClasses
            {
                get
                {
                    if (this.VulnerabilityClass == null || this.VulnerabilityClass.Count() == 0)
                    {
                        return 0;
                    }
                    else
                    {
                        return VulnerabilityClass.First().Dimension;
                    }
                }
            }
            public int NumberOfChildren
            {
                get
                {
                    if (this.VulnerabilityClass == null || this.VulnerabilityClass.Count() == 0)
                    {
                        return 0;
                    }
                    else
                    {
                        return VulnerabilityClass.Count();
                    }
                }
            }
        }

        public AsthmaModel(string modelName = "AsthmaModel", bool breakSymmetry = true)
        {
            BreakSymmetry = breakSymmetry;

            NumYears = Variable.New<int>().Named("NumYears");
            NumChildren = Variable.New<int>().Named("NumChildren");
            NumAllergens = Variable.New<int>().Named("NumAllergens");
            NumVulnerabilities = Variable.New<int>().Named("NumVulnerabilities");
            years = new Range(this.NumYears).Named("years");
            children = new Range(this.NumChildren).Named("children");
            allergens = new Range(this.NumAllergens).Named("allergens");
            classes = new Range(this.NumVulnerabilities).Named("classes");

            sensitized = Variable.Array(Variable.Array<bool>(children, allergens), years).Named("sensitized");
            skinTest = Variable.Array(Variable.Array<bool>(children, allergens), years).Named("skinTest");
            igeTest = Variable.Array(Variable.Array<bool>(children, allergens), years).Named("igeTest");
            skinTestMissing = Variable.Array(Variable.Array<bool>(children, allergens), years).Named("skinTestMissing");
            igeTestMissing = Variable.Array(Variable.Array<bool>(children, allergens), years).Named("igeTestMissing");

            probSensClassPrior = Variable.New<Dirichlet>().Named("probSensClassPrior");
            probSensClass = Variable<Vector>.Random(probSensClassPrior).Named("probSensClass");
            probSensClass.SetValueRange(classes);
            sensClass = Variable.Array<int>(children).Named("sensClass");
            sensClass[children] = Variable.Discrete(probSensClass).ForEach(children);
            sensClassInitializer = Variable.New<IDistribution<int[]>>().Named("sensClassInitializer");
            if (BreakSymmetry)
            {
                sensClass.InitialiseTo(sensClassInitializer);
            }

            // Transition probabilities
            probSens1Prior = Variable.Array<Beta>(allergens, classes).Named("probSens1Prior");
            probGainPrior = Variable.Array(Variable.Array<Beta>(allergens, classes), years).Named("probGainPrior");
            probRetainPrior = Variable.Array(Variable.Array<Beta>(allergens, classes), years).Named("probRetainPrior");
            probSens1 = Variable.Array<double>(allergens, classes).Named("probSens1");
            probGain = Variable.Array(Variable.Array<double>(allergens, classes), years).Named("probGain");
            probRetain = Variable.Array(Variable.Array<double>(allergens, classes), years).Named("probRetain");
            probSens1[allergens, classes] = Variable<double>.Random(probSens1Prior[allergens, classes]);
            probGain[years][allergens, classes] = Variable<double>.Random(probGainPrior[years][allergens, classes]);
            probRetain[years][allergens, classes] = Variable<double>.Random(probRetainPrior[years][allergens, classes]);

            // Emission probabilities
            probSkinIfSensPrior = Variable.New<Beta>().Named("probSkinIfSensPrior");
            probSkinIfNotSensPrior = Variable.New<Beta>().Named("probSkinIfNotSensPrior");
            probIgeIfSensPrior = Variable.New<Beta>().Named("probIgeIfSensPrior");
            probIgeIfNotSensPrior = Variable.New<Beta>().Named("probIgeIfNotSensPrior");
            probSkinIfSens = Variable<double>.Random(probSkinIfSensPrior).Named("probSkinIfSens");
            probSkinIfNotSens = Variable<double>.Random(probSkinIfNotSensPrior).Named("probSkinIfNotSens");
            probIgeIfSens = Variable<double>.Random(probIgeIfSensPrior).Named("probIgeIfSens");
            probIgeIfNotSens = Variable<double>.Random(probIgeIfNotSensPrior).Named("probIgeIfNotSens");

            // Transitions
            using (Variable.ForEach(children))
            {
                using (Variable.Switch(sensClass[children]))
                {
                    using (Variable.ForEach(allergens))
                    {
                        using (var block = Variable.ForEach(years))
                        {
                            var year = block.Index;
                            var yearIs0 = (year == 0).Named("year == 0");
                            var yearIsGr0 = (year > 0).Named("year > 0");
                            using (Variable.If(yearIs0))
                            {
                                sensitized[year][children, allergens] = Variable.Bernoulli(probSens1[allergens, sensClass[children]]);
                            }

                            using (Variable.If(yearIsGr0))
                            {
                                var prevYear = (year - 1).Named("year - 1");
                                using (Variable.If(sensitized[prevYear][children, allergens]))
                                {
                                    sensitized[year][children, allergens] = Variable.Bernoulli(probRetain[year][allergens, sensClass[children]]);
                                }

                                using (Variable.IfNot(sensitized[prevYear][children, allergens]))
                                {
                                    sensitized[year][children, allergens] = Variable.Bernoulli(probGain[year][allergens, sensClass[children]]);
                                }
                            }
                        }
                    }
                }
            }

            // Emissions
            using (Variable.ForEach(children))
            {
                using (Variable.ForEach(allergens))
                {
                    using (Variable.ForEach(years))
                    {
                        using (Variable.If(sensitized[years][children, allergens]))
                        {
                            using (Variable.IfNot(skinTestMissing[years][children, allergens]))
                            {
                                skinTest[years][children, allergens] = Variable.Bernoulli(probSkinIfSens);
                            }

                            using (Variable.IfNot(igeTestMissing[years][children, allergens]))
                            {
                                igeTest[years][children, allergens] = Variable.Bernoulli(probIgeIfSens);
                            }
                        }

                        using (Variable.IfNot(sensitized[years][children, allergens]))
                        {
                            using (Variable.IfNot(skinTestMissing[years][children, allergens]))
                            {
                                skinTest[years][children, allergens] = Variable.Bernoulli(probSkinIfNotSens);
                            }

                            using (Variable.IfNot(igeTestMissing[years][children, allergens]))
                            {
                                igeTest[years][children, allergens] = Variable.Bernoulli(probIgeIfNotSens);
                            }
                        }
                    }
                }
            }

            Engine = new InferenceEngine()
            {
                ShowProgress = false,
                ModelName = modelName
            };
            Engine.ProgressChanged += Engine_ProgressChanged;
        }

        private void Engine_ProgressChanged(InferenceEngine engine, InferenceProgressEventArgs progress)
        {
            ProgressChanged?.Invoke(engine, progress);
        }

        private void SetPriors(AllergenData data, int numVulnerabilities, Beliefs beliefs)
        {
            int nY = AllergenData.NumYears;
            int nN = data.DataCountChild.Length;
            int nA = data.NumAllergens;
            bool useUniformClassPrior = true;

            if (beliefs == null)
            {
                this.probSensClassPrior.ObservedValue = useUniformClassPrior ? Dirichlet.PointMass(Vector.Constant(numVulnerabilities, 1.0 / numVulnerabilities)) : Dirichlet.Symmetric(numVulnerabilities, 0.1);
                this.probSens1Prior.ObservedValue = Util.ArrayInit(nA, numVulnerabilities, (a, v) => new Beta(1, 1));
                this.probGainPrior.ObservedValue = Util.ArrayInit(nY, y => Util.ArrayInit(nA, numVulnerabilities, (a, v) => new Beta(1, 1)));
                this.probRetainPrior.ObservedValue = Util.ArrayInit(nY, y => Util.ArrayInit(nA, numVulnerabilities, (a, v) => new Beta(1, 1)));
                this.probSkinIfSensPrior.ObservedValue = new Beta(2.0, 1);
                this.probSkinIfNotSensPrior.ObservedValue = new Beta(1, 2.0);
                this.probIgeIfSensPrior.ObservedValue = new Beta(2.0, 1);
                this.probIgeIfNotSensPrior.ObservedValue = new Beta(1, 2.0);
            }
            else
            {
                this.probSensClassPrior.ObservedValue = beliefs.ProbVulnerabilityClass;
                probSens1Prior.ObservedValue = Util.ArrayInit(nA, numVulnerabilities, (a, v) => beliefs.ProbSensitizationAgeOne[a, v]);
                probGainPrior.ObservedValue = Util.ArrayInit(nY, y => Util.ArrayInit(nA, numVulnerabilities, (a, v) => beliefs.ProbGainSensitization[y][a, v]));
                probRetainPrior.ObservedValue = Util.ArrayInit(nY, y => Util.ArrayInit(nA, numVulnerabilities, (a, v) => beliefs.ProbRetainSensitization[y][a, v]));
                probSkinIfSensPrior.ObservedValue = beliefs.ProbSkinIfSensitized;
                probSkinIfNotSensPrior.ObservedValue = beliefs.ProbSkinIfNotSensitized;
                probIgeIfSensPrior.ObservedValue = beliefs.ProbIgEIfSensitized;
                probIgeIfNotSensPrior.ObservedValue = beliefs.ProbIgEIfNotSensitized;
            }
        }

        public int Iterations
        {
            get;
            set;
        }

        public event InferenceProgressEventHandler ProgressChanged;

        public void InitializeMessages(AllergenData data, int numVulnerabilities)
        {
            int nN = data.DataCountChild.Length;
            var discreteUniform = Discrete.Uniform(numVulnerabilities);
            sensClassInitializer.ObservedValue = Distribution<int>.Array(Util.ArrayInit(nN, n => Discrete.PointMass(discreteUniform.Sample(), numVulnerabilities)));
        }

        private void SetObservations(AllergenData data, int numVulnerabilities, bool initialize = true)
        {
            int nY = AllergenData.NumYears;
            int nN = data.DataCountChild.Length;
            int nA = data.NumAllergens;

            // Observations
            NumYears.ObservedValue = nY;
            NumChildren.ObservedValue = nN;
            NumAllergens.ObservedValue = nA;
            NumVulnerabilities.ObservedValue = numVulnerabilities;
            skinTest.ObservedValue = Util.ArrayInit(nY, y => Util.ArrayInit(nN, nA, (n, a) => data.SkinTestData[y][n][a] == 1));
            igeTest.ObservedValue = Util.ArrayInit(nY, y => Util.ArrayInit(nN, nA, (n, a) => data.IgeTestData[y][n][a] == 1));
            skinTestMissing.ObservedValue = Util.ArrayInit(nY, y => Util.ArrayInit(nN, nA, (n, a) => data.SkinTestData[y][n][a] == null));
            igeTestMissing.ObservedValue = Util.ArrayInit(nY, y => Util.ArrayInit(nN, nA, (n, a) => data.IgeTestData[y][n][a] == null));
        }

        public Beliefs Run(AllergenData data, int numVulnerabilities, Beliefs beliefs = null, bool initializeMessages = true, bool showFactorGraph = false)
        {
            this.Engine.ShowFactorGraph = showFactorGraph;
            
            if (initializeMessages && BreakSymmetry)
            {
                this.InitializeMessages(data, numVulnerabilities);
            }

            this.SetObservations(data, numVulnerabilities);
            this.SetPriors(data, numVulnerabilities, beliefs);

            var result = new Beliefs();

            Engine.NumberOfIterations = this.Iterations;
            result.Sensitization = Engine.Infer<Bernoulli[][,]>(this.sensitized);
            result.ProbSkinIfSensitized = Engine.Infer<Beta>(this.probSkinIfSens);
            result.ProbSkinIfNotSensitized = Engine.Infer<Beta>(this.probSkinIfNotSens);
            result.ProbIgEIfSensitized = Engine.Infer<Beta>(this.probIgeIfSens);
            result.ProbIgEIfNotSensitized = Engine.Infer<Beta>(this.probIgeIfNotSens);
            result.ProbSensitizationAgeOne = Engine.Infer<Beta[,]>(this.probSens1);
            result.ProbGainSensitization = Engine.Infer<Beta[][,]>(this.probGain);
            result.ProbRetainSensitization = Engine.Infer<Beta[][,]>(this.probRetain);
            result.VulnerabilityClass = Engine.Infer<Discrete[]>(this.sensClass);

            return result;
        }
    }
}
