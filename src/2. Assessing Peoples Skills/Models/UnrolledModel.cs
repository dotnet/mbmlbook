// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AssessingPeoplesSkills.Models
{
    using System;
    using System.Collections.Generic;

    using Microsoft.ML.Probabilistic;
    using Microsoft.ML.Probabilistic.Distributions;
    using Microsoft.ML.Probabilistic.Models;
    using Microsoft.ML.Probabilistic.Models.Attributes;

    /// <summary>
    /// The model unrolled.
    /// </summary>
    public class UnrolledModel : NoisyAndModel
    {
        /// <summary>
        /// The message histories.
        /// </summary>
        internal readonly Dictionary<string, List<Bernoulli>> MessageHistories = new Dictionary<string, List<Bernoulli>>();
        
        /// <summary>
        /// The unrolled skills.
        /// </summary>
        private Variable<bool>[][] unrolledSkills;

        /// <summary>
        /// The is correct variable.
        /// </summary>
        private Variable<bool>[][] isCorrect;

        /// <summary>
        /// Gets or sets a value indicating whether to use exact inference.
        /// </summary>
        internal bool ExactInference { get; set; }

        /// <summary>
        /// The set observed values.
        /// </summary>
        /// <param name="inputData">The input data.</param>
        /// <param name="parameters">The parameters.</param>
        /// <exception cref="System.ArgumentException">parameters should be length 2</exception>
        /// <exception cref="System.NotSupportedException">Inference not supported for this model configuration</exception>
        /// <exception cref="NotSupportedException"></exception>
        public override void SetObservedValues(Inputs inputData, params object[] parameters)
        {
            if (parameters == null || parameters.Length != 2)
            {
                throw new ArgumentException("parameters should be length 2");
            }

            bool observeSkillTrue = (bool)parameters[0];
            bool observeIsCorrect = (bool)parameters[1];

            if (observeSkillTrue || !observeIsCorrect)
            {
                throw new NotSupportedException("Inference not supported for this model configuration");
            }

            this.SkillNames = inputData.Quiz.SkillNames;

            this.NumberOfPeople = inputData.NumberOfPeople;
            this.NumberOfSkills = inputData.Quiz.NumberOfSkills;
            this.NumberOfQuestions = inputData.Quiz.NumberOfQuestions;

            this.NumberOfSkillsForEachQuestion = inputData.Quiz.NumberSkillsForQuestion;

            this.SkillsNeeded = inputData.Quiz.SkillsForQuestion;

            this.FinishModelConstruction();

            for (int p = 0; p < this.NumberOfPeople; p++)
            {
                for (int q = 0; q < this.NumberOfQuestions; q++)
                {
                    this.isCorrect[p][q].ObservedValue = inputData.IsCorrect[p][q];
                }
            }
        }

        /// <summary>
        /// Does the inference.
        /// </summary>
        /// <param name="results">
        /// The results.
        /// </param>
        public override void DoInference(ref Results results)
        {
            this.InferSkills(ref results);
        }

        /// <summary>
        /// Finishes the model construction.
        /// </summary>
        private void FinishModelConstruction()
        {
            this.unrolledSkills = new Variable<bool>[this.NumberOfPeople][];
            this.isCorrect = new Variable<bool>[this.NumberOfPeople][];

            for (int p = 0; p < this.NumberOfPeople; p++)
            {
                string personSuffix = (this.NumberOfPeople == 1) ? string.Empty : string.Format("-{0}", p); 

                // Skills for person
                this.unrolledSkills[p] = new Variable<bool>[this.NumberOfSkills];
                for (int s = 0; s < this.NumberOfSkills; s++)
                {
                    this.unrolledSkills[p][s] = Variable.Bernoulli(this.ProbabilityOfSkillTrue).Named(this.SkillNames[s] + personSuffix);
                    this.unrolledSkills[p][s].AddAttribute(new DivideMessages(false));
                    this.unrolledSkills[p][s].AddAttribute(new ListenToMessages { Containing = "_uses" });
                }

                // Answers for person
                this.isCorrect[p] = new Variable<bool>[this.NumberOfQuestions];
                for (int q = 0; q < this.NumberOfQuestions; q++)
                {
                    this.isCorrect[p][q] = Variable.New<bool>().Named("isCorrect" + q + personSuffix);
                }

                if (!this.ExactInference)
                {
                    this.QuestionModelPart(p);
                }
                else
                {
                    // Cut the loop on the first skill variable (cutset conditioning)
                    var oldSkill = this.unrolledSkills[p][0];
                    this.unrolledSkills[p][0] = Variable.Constant(true);
                    using (Variable.If(oldSkill))
                    {
                        this.QuestionModelPart(p, "_T");
                    }

                    this.unrolledSkills[p][0] = Variable.Constant(false);
                    using (Variable.IfNot(oldSkill))
                    {
                        this.QuestionModelPart(p, "_F");
                    }

                    this.unrolledSkills[p][0] = oldSkill;
                }
            }
        }

        /// <summary>
        /// Question part of the model
        /// </summary>
        /// <param name="person">The person.</param>
        /// <param name="suffix">The suffix.</param>
        /// <exception cref="System.Exception">Unrolling not implemented if more than two skills needed for a question</exception>
        private void QuestionModelPart(int person, string suffix = "")
        {
            for (int question = 0; question < this.NumberOfQuestions; question++)
            {
                int[] skillsNeeded = this.SkillsNeeded[question];
                switch (skillsNeeded.Length)
                {
                    case 1:
                        this.isCorrect[person][question].SetTo(
                            Factors.AddNoise(
                                this.unrolledSkills[person][skillsNeeded[0]],
                                this.ProbabilityOfNotMistake,
                                this.ProbabilityOfGuess));
                        break;
                    case 2:
                        var hasSkills =
                            (this.unrolledSkills[person][skillsNeeded[0]] & this.unrolledSkills[person][skillsNeeded[1]])
                                .Named("hasSkills" + (question + 1) + suffix);

                        this.isCorrect[person][question].SetTo(
                            Factors.AddNoise(hasSkills, this.ProbabilityOfNotMistake, this.ProbabilityOfGuess));
                        break;
                    default:
                        throw new Exception("Unrolling not implemented if more than two skills needed for a question");
                }
            }
        }
        
        /// <summary>
        /// Infers the skills.
        /// </summary>
        /// <param name="results">
        /// The results.
        /// </param>
        private void InferSkills(ref Results results)
        {
            // Engine.BrowserMode = BrowserMode.Always;
            // Engine.ShowFactorGraph = true;
            this.Engine.NumberOfIterations = 5;
            this.Engine.MessageUpdated += this.EngineMessageUpdated;

            Engine.Compiler.GivePriorityTo(typeof(Microsoft.ML.Probabilistic.Factors.ReplicateOp_NoDivide));
            results.SkillsPosteriors = new Bernoulli[this.NumberOfPeople][];
            for (int p = 0; p < this.NumberOfPeople; p++)
            {
                results.SkillsPosteriors[p] = new Bernoulli[this.NumberOfSkills];
                for (int j = 0; j < this.NumberOfSkills; j++)
                {
                    results.SkillsPosteriors[p][j] = this.Engine.Infer<Bernoulli>(this.unrolledSkills[p][j]);
                }                
            }
        }
        
        /// <summary>
        /// Handles the engine message updated.
        /// </summary>
        /// <param name="algorithm">The algorithm.</param>
        /// <param name="messageEvent">The <see cref="MessageUpdatedEventArgs"/> instance containing the event data.</param>
        private void EngineMessageUpdated(IGeneratedAlgorithm algorithm, MessageUpdatedEventArgs messageEvent)
        {
            if (!this.MessageHistories.ContainsKey(messageEvent.MessageId))
            {
                this.MessageHistories[messageEvent.MessageId] = new List<Bernoulli>();
            }

            // Console.WriteLine(messageEvent);
            if (messageEvent.Message is Bernoulli)
            {
                this.MessageHistories[messageEvent.MessageId].Add((Bernoulli)messageEvent.Message);
            }
        }
    }
}
