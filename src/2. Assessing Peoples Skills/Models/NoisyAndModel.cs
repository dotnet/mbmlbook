// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
namespace AssessingPeoplesSkills.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.ML.Probabilistic.Distributions;
    using Microsoft.ML.Probabilistic.Models;
    using Microsoft.ML.Probabilistic.Models.Attributes;

    /// <summary>
    /// Base class for the LearningSkills models
    /// </summary>
    public class NoisyAndModel
    {
        #region Fields
        /// <summary>
        /// The probability of guess
        /// </summary>
        protected VariableArray<double> probabilityOfGuess;
        
        /// <summary>
        /// The probability of not mistake
        /// </summary>
        protected VariableArray<double> probabilityOfNotMistake;

        /// <summary>
        /// The probability of skill true
        /// </summary>
        protected VariableArray<double> probabilityOfSkillTrue;

        /// <summary>
        /// The number of skills.
        /// </summary>
        private Variable<int> numberOfSkills;

        /// <summary>
        /// The number of questions.
        /// </summary>
        private Variable<int> numberOfQuestions;

        /// <summary>
        /// The number of people.
        /// </summary>
        private Variable<int> numberOfPeople;

        /// <summary>
        /// Gets or sets the skills questions mask.
        /// </summary>
        /// <value>
        /// The skills questions mask.
        /// </value>
        private VariableArray<VariableArray<bool>, bool[][]> skillsQuestionsMask;

        /// <summary>
        /// The number of skills for each question
        /// </summary>
        private VariableArray<int> numberOfSkillsForEachQuestion;

        /// <summary>
        /// The skills for question.
        /// </summary>
        private VariableArray<VariableArray<int>, int[][]> skillsNeeded;
        
        /// <summary>
        /// The has skills.
        /// </summary>
        private VariableArray<VariableArray<bool>, bool[][]> skill;

        /// <summary>
        /// The is correct.
        /// </summary>
        private VariableArray<VariableArray<bool>, bool[][]> isCorrect;

        /// <summary>
        /// The engine
        /// </summary>
        private InferenceEngine engine = new InferenceEngine
                                             {
                                                 ShowProgress = false,
                                                 ShowSchedule = false,
                                                 ShowTimings = false,
                                                 ShowMsl = false,
                                                 ShowFactorGraph = false,
                                                 ShowWarnings = false,
                                                 NumberOfIterations = 10,
                                             };

        #endregion

        #region Public Interface

        /// <summary>
        /// Gets or sets the engine.
        /// </summary>
        public InferenceEngine Engine
        {
            get
            {
                return this.engine;
            }

            set
            {
                this.engine = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the model has been constructed.
        /// </summary>
        public bool HasBeenConstructed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show factor graph.
        /// </summary>
        public bool ShowFactorGraph
        {
            get
            {
                return this.Engine.ShowFactorGraph;
            }

            set
            {
               this.Engine.ShowFactorGraph = value;
            }
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is real (i.e. not sampled/perfect/random).
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is real; otherwise, <c>false</c>.
        /// </value>
        public bool IsReal { get; set; }

        /// <summary>
        /// Gets or sets the probability of guess.
        /// </summary>
        /// <value>
        /// The probability of guess.
        /// </value>
        public double ProbabilityOfGuess { get; set; }

        /// <summary>
        /// Gets or sets the probability of not mistake.
        /// </summary>
        /// <value>
        /// The probability of not mistake.
        /// </value>
        public double ProbabilityOfNotMistake { get; set; }
        
        /// <summary>
        /// Gets or sets the probability of skill true.
        /// </summary>
        /// <value>
        /// The probability of skill true.
        /// </value>
        public double ProbabilityOfSkillTrue { get; set; }

        /// <summary>
        /// Gets or sets the model index (used for listing models in a specific order).
        /// </summary>
        public virtual byte Index { get; set; }

        #endregion

        /// <summary>
        /// Gets or sets the skill names.
        /// </summary>
        protected string[] SkillNames { get; set; }

        /// <summary>
        /// Gets or sets the skills questions mask.
        /// </summary>
        protected bool[][] SkillsQuestionsMask
        {
            get
            {
                return this.skillsQuestionsMask.ObservedValue;
            }

            set
            {
                this.skillsQuestionsMask.ClearObservedValue();
                this.skillsQuestionsMask.ObservedValue = value;
            }
        }

        #region variables will be observed

        /// <summary>
        /// Gets or sets the number of skills.
        /// </summary>
        protected int NumberOfSkills
        {
            get
            {
                return this.numberOfSkills.ObservedValue;
            }

            set
            {
                this.numberOfSkills.ClearObservedValue();
                this.numberOfSkills.ObservedValue = value;
            }
        }

        /// <summary>
        /// Gets or sets the number of questions.
        /// </summary>
        protected int NumberOfQuestions
        {
            get
            {
                return this.numberOfQuestions.ObservedValue;
            }

            set
            {
                this.numberOfQuestions.ClearObservedValue();
                this.numberOfQuestions.ObservedValue = value;
            }
        }

        /// <summary>
        /// Gets or sets the number of people.
        /// </summary>
        protected int NumberOfPeople
        {
            get
            {
                return this.numberOfPeople.ObservedValue;
            }

            set
            {
                this.numberOfPeople.ClearObservedValue();
                this.numberOfPeople.ObservedValue = value;
            }
        }
        
        /// <summary>
        /// Gets or sets the number of skills for each question.
        /// </summary>
        /// <value>
        /// The number of skills for each question.
        /// </value>
        protected int[] NumberOfSkillsForEachQuestion
        {
            get
            {
                return this.numberOfSkillsForEachQuestion.ObservedValue;
            }

            set
            {
                this.numberOfSkillsForEachQuestion.ClearObservedValue();
                this.numberOfSkillsForEachQuestion.ObservedValue = value;
            }
        }

        /// <summary>
        /// Gets or sets the skills for question.
        /// </summary>
        protected int[][] SkillsNeeded
        {
            get
            {
                return this.skillsNeeded.ObservedValue;
            }

            set
            {
                this.skillsNeeded.ClearObservedValue();
                this.skillsNeeded.ObservedValue = value;
            }
        }

        #endregion

        /// <summary>
        /// Gets or sets the skills range.
        /// </summary>
        protected Range Skills { get; set; }

        /// <summary>
        /// Gets or sets the questions range.
        /// </summary>
        protected Range Questions { get; set; }

        /// <summary>
        /// Gets or sets the people range.
        /// </summary>
        protected Range People { get; set; }

        /// <summary>
        /// Gets or sets the questions skills range.
        /// </summary>
        protected Range QuestionsSkills { get; set; }

        /// <summary>
        /// Gets or sets the has skills.
        /// </summary>
        protected bool[][] Skill
        {
            get
            {
                return this.skill.ObservedValue;
            }

            set
            {
                this.skill.ClearObservedValue();
                this.skill.ObservedValue = value;
            }
        }
        
        /// <summary>
        /// Gets or sets the is correct.
        /// </summary>
        protected bool[][] IsCorrect
        {
            get
            {
                return this.isCorrect.ObservedValue;
            }

            set
            {
                this.isCorrect.ClearObservedValue();
                this.isCorrect.ObservedValue = value;
            }
        }

        /// <summary>
        /// Gets the priors.
        /// </summary>
        protected virtual string Priors
        {
            get
            {
                return "Guess: " + this.ProbabilityOfGuess.ToString("#0.0") + ", NotMistake: "
                       + this.ProbabilityOfNotMistake.ToString("#0.0");
            }
        }

        /// <summary>
        /// The to string.
        /// </summary>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public override string ToString()
        {
            return this.Name + ", " + this.Priors;
        }

        /// <summary>
        /// Constructs the model.
        /// </summary>
        public void ConstructModel()
        {
            this.numberOfPeople = Variable.New<int>().Named("numPeople").Attrib(new DoNotInfer());
            this.numberOfSkills = Variable.New<int>().Named("numSkills").Attrib(new DoNotInfer());
            this.numberOfQuestions = Variable.New<int>().Named("numQuestions").Attrib(new DoNotInfer());

            this.People = new Range(this.numberOfPeople).Named("people");
            this.Skills = new Range(this.numberOfSkills).Named("skills");
            this.Questions = new Range(this.numberOfQuestions).Named("questions");

            this.probabilityOfGuess = Variable.Array<double>(this.Questions).Named("probGuess");
            this.probabilityOfNotMistake = Variable.Array<double>(this.Questions).Named("probNoMistake");
            this.probabilityOfSkillTrue = Variable.Array<double>(this.Skills).Named("probSkillTrue");

            this.numberOfSkillsForEachQuestion =
                Variable.Array<int>(this.Questions).Named("numSkillsForQuestions").Attrib(new DoNotInfer());
            this.QuestionsSkills = new Range(this.numberOfSkillsForEachQuestion[this.Questions]).Named("questionsXskills");

            this.skillsNeeded =
                Variable.Array(Variable.Array<int>(this.QuestionsSkills), this.Questions)
                        .Named("skillsNeeded")
                        .Attrib(new DoNotInfer());
            this.skillsQuestionsMask =
                Variable.Array(Variable.Array<bool>(this.Skills), this.Questions)
                        .Named("skillsQuestionsMask")
                        .Attrib(new DoNotInfer());

            this.skill = Variable.Array(Variable.Array<bool>(this.Skills), this.People).Named("skill");
            this.skill[this.People][this.Skills] = Variable.Bernoulli(this.probabilityOfSkillTrue[this.Skills]).ForEach(this.People);

            this.isCorrect = Variable.Array(Variable.Array<bool>(this.Questions), this.People).Named("isCorrect");

            this.ConstructNoisyFactor();
        }

        /// <summary>
        /// Clears the observed variables.
        /// </summary>
        public void ClearObservedVariables()
        {
            this.skillsNeeded.ClearObservedValue();
            this.skillsQuestionsMask.ClearObservedValue();
            this.skill.ClearObservedValue();
            this.isCorrect.ClearObservedValue();
            this.numberOfPeople.ClearObservedValue();
            this.numberOfQuestions.ClearObservedValue();
            this.numberOfSkills.ClearObservedValue();
            this.numberOfSkillsForEachQuestion.ClearObservedValue();
            this.probabilityOfGuess.ClearObservedValue();
            this.probabilityOfNotMistake.ClearObservedValue();
            this.probabilityOfSkillTrue.ClearObservedValue();
        }

        /// <summary>
        /// Sets the observed values.
        /// </summary>
        /// <param name="inputs">The input Data.</param>
        /// <param name="parameters">The parameters.</param>
        /// <exception cref="System.ArgumentException">parameters should be length 2</exception>
        public virtual void SetObservedValues(Inputs inputs, params object[] parameters)
        {
            if (parameters == null || parameters.Length != 2)
            {
                throw new ArgumentException("parameters should be length 2");
            }

            bool observeSkillTrue = (bool)parameters[0];
            bool observeIsCorrect = (bool)parameters[1];

            // Copy some input data
            this.SkillsQuestionsMask = inputs.Quiz.SkillsQuestionsMask;
            this.SkillsNeeded = inputs.Quiz.SkillsForQuestion;

            this.NumberOfPeople = inputs.NumberOfPeople;
            this.NumberOfSkills = inputs.Quiz.NumberOfSkills;
            this.NumberOfQuestions = inputs.Quiz.NumberOfQuestions;
            this.NumberOfSkillsForEachQuestion = inputs.Quiz.NumberSkillsForQuestion;

            // the observed data
            if (observeIsCorrect)
            {
                this.IsCorrect = inputs.IsCorrect;
            }
            else
            {
                this.isCorrect.ClearObservedValue();
            }

            if (observeSkillTrue)
            {
                this.Skill = inputs.StatedSkills;
            }
            else
            {
                this.skill.ClearObservedValue();
            }

            this.SetObservedArrays(inputs);
        }

        /// <summary>
        /// Does the inference.
        /// </summary>
        /// <param name="results">
        /// The results.
        /// </param>
        public virtual void DoInference(ref Results results)
        {
            // Infer skills
            if (this.isCorrect.IsObserved && !this.skill.IsObserved)
            {
                this.InferSkills(ref results);
            }

            // Infer is correct
            if (this.skill.IsObserved && !this.isCorrect.IsObserved)
            {
                this.InferIsCorrect(ref results);
            }

            // This last clause is to perform inference on a fully observed model
            if (this.isCorrect.IsObserved && this.skill.IsObserved)
            {
                this.InferSkills(ref results);
            }
        }

        /// <summary>
        /// Computes the evidence.
        /// </summary>
        /// <param name="results">The results.</param>
        /// <exception cref="System.NotImplementedException">Not implemented for this model.</exception>
        public void ComputeEvidence(ref Results results)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Samples from the model.
        /// </summary>
        /// <param name="inputs">The inputs.</param>
        /// <param name="numberOfSamples">The number of samples.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>
        /// The <see cref="Inputs" />.
        /// </returns>
        /// <exception cref="System.ArgumentException">Expecting two parameters</exception>
        /// <exception cref="System.ArgumentNullException">inputsBase should be of type Inputs</exception>
        public Inputs SampleFromModel(Inputs inputs, int numberOfSamples, params object[] parameters)
        {
            if (parameters == null || parameters.Length != 2)
            {
                throw new ArgumentException("Expecting two parameters");
            }

            double probSkillTrue = (double)parameters[0];
            bool useGroundTruthSkills = (bool)parameters[1];

            bool[][] sampleIsCorrect = new bool[numberOfSamples][];

            Quiz sampleQuizData = new Quiz
            {
                SkillNames = inputs.Quiz.SkillNames,
                SkillsForQuestion = inputs.Quiz.SkillsForQuestion
            };
            Inputs sampleInputs = new Inputs
            {
                Quiz = sampleQuizData,
                CorrectAnswers = inputs.CorrectAnswers,
                StatedSkills = new bool[numberOfSamples][],
                RawResponses = new int[numberOfSamples][]
            };

            Random random = new Random(100);
            for (int i = 0; i < numberOfSamples; i++)
            {
                sampleInputs.RawResponses[i] = new int[inputs.Quiz.NumberOfQuestions];
                sampleIsCorrect[i] = new bool[inputs.Quiz.NumberOfQuestions];

                if (useGroundTruthSkills)
                {
                    sampleInputs.StatedSkills = inputs.StatedSkills;
                }
                else
                {
                    // Generate random set of skills for this person
                    sampleInputs.StatedSkills[i] = new bool[inputs.Quiz.NumberOfSkills];
                    for (int j = 0; j < inputs.Quiz.NumberOfSkills; j++)
                    {
                        if (random.NextDouble() >= probSkillTrue)
                        {
                            sampleInputs.StatedSkills[i][j] = true;
                        }
                    }
                }

                for (int j = 0; j < inputs.Quiz.NumberOfQuestions; j++)
                {
                    // Sample skills
                    bool hasAllSkills = sampleInputs.HasAllSkills(i, j);

                    if ((hasAllSkills && random.NextDouble() < this.ProbabilityOfNotMistake)
                        || (!hasAllSkills && random.NextDouble() < this.ProbabilityOfGuess))
                    {
                        // Correct answer
                        sampleInputs.RawResponses[i][j] = inputs.CorrectAnswers[i];
                        sampleIsCorrect[i][j] = true;
                    }
                    else
                    {
                        // Choose from wrong answers at random
                        List<int> possibleAnswers = Enumerable.Range(1, 5).ToList();
                        possibleAnswers.Remove(inputs.CorrectAnswers[i]);
                        sampleInputs.RawResponses[i][j] = possibleAnswers[random.Next(0, 4)];
                        sampleIsCorrect[i][j] = false;
                    }
                }
            }

            sampleInputs.IsCorrect = sampleIsCorrect;

            return sampleInputs;
        }

        /// <summary>
        /// Sets the observed arrays.
        /// </summary>
        /// <param name="inputs">The inputs.</param>
        protected virtual void SetObservedArrays(Inputs inputs)
        {
            this.probabilityOfGuess.ClearObservedValue();
            this.probabilityOfGuess.ObservedValue = Enumerable.Repeat(this.ProbabilityOfGuess, this.NumberOfQuestions).ToArray();

            this.probabilityOfNotMistake.ClearObservedValue();
            this.probabilityOfNotMistake.ObservedValue = Enumerable.Repeat(this.ProbabilityOfNotMistake, this.NumberOfQuestions).ToArray();

            this.probabilityOfSkillTrue.ClearObservedValue();
            this.probabilityOfSkillTrue.ObservedValue = Enumerable.Repeat(this.ProbabilityOfSkillTrue, this.NumberOfSkills).ToArray();
        }

        /// <summary>
        /// Constructs the noisy factor.
        /// </summary>
        protected virtual void ConstructNoisyFactor()
        {
            using (Variable.ForEach(this.People))
            {
                using (Variable.ForEach(this.Questions))
                {
                    var relevantSkills =
                        Variable.Subarray(this.skill[this.People], this.skillsNeeded[this.Questions])
                                .Named("relevantSkills");

                    this.isCorrect[this.People][this.Questions] = Factors.NoisyAllTrue(
                        relevantSkills,
                        this.probabilityOfNotMistake[this.Questions],
                        this.probabilityOfGuess[this.Questions]);
                }
            }
        }

        /// <summary>
        /// Infers the IsCorrect variable.
        /// </summary>
        /// <param name="results">
        /// The results.
        /// </param>
        private void InferIsCorrect(ref Results results)
        {
            results.IsCorrectPosteriors = this.Engine.Infer<Bernoulli[][]>(this.isCorrect);
        }

        /// <summary>
        /// Infers the skills.
        /// </summary>
        /// <param name="results">
        /// The results.
        /// </param>
        private void InferSkills(ref Results results)
        {
            results.SkillsPosteriors = this.Engine.Infer<Bernoulli[][]>(this.skill);
        }

        /// <summary>
        /// The factors.
        /// </summary>
        protected class Factors
        {
            /// <summary>
            /// Noisy all true factor (gated). Asymmetric version with two noise parameters (using a gate)
            /// </summary>
            /// <param name="variableArray">The variable array.</param>
            /// <param name="probabilityIfAllTrue">The probability if all true.</param>
            /// <param name="probabilityIfNotAllTrue">The probability if not all true.</param>
            /// <returns>
            /// The <see cref="Variable" />.
            /// </returns>
            public static Variable<bool> NoisyAllTrue(
                VariableArray<bool> variableArray,
                Variable<double> probabilityIfAllTrue,
                Variable<double> probabilityIfNotAllTrue)
            {
                var hasSkills = Variable.AllTrue(variableArray).Named("hasSkills");
                return AddNoise(hasSkills, probabilityIfAllTrue, probabilityIfNotAllTrue);
            }

            /// <summary>
            /// Add noise factor.
            /// </summary>
            /// <param name="hasSkills">The has all skills variable.</param>
            /// <param name="probabilityIfTrue">The probability if true.</param>
            /// <param name="probabilityIfFalse">The probability if false.</param>
            /// <returns>
            /// The <see cref="Variable" />.
            /// </returns>
            public static Variable<bool> AddNoise(
                Variable<bool> hasSkills, Variable<double> probabilityIfTrue, Variable<double> probabilityIfFalse)
            {
                var noisyAllTrue = Variable.New<bool>().Named("NoisyAllTrueGated");
                using (Variable.If(hasSkills))
                {
                    noisyAllTrue.SetTo(Variable.Bernoulli(probabilityIfTrue).Named("probIfAllTrue"));
                }

                using (Variable.IfNot(hasSkills))
                {
                    noisyAllTrue.SetTo(Variable.Bernoulli(probabilityIfFalse).Named("probIfNotAllTrue"));
                }

                return noisyAllTrue;
            }
        }
    }
}