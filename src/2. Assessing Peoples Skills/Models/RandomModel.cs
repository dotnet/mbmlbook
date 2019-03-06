// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AssessingPeoplesSkills.Models
{
    using Microsoft.ML.Probabilistic.Models;
    using Microsoft.ML.Probabilistic.Models.Attributes;

    /// <summary>
    /// The random model.
    /// </summary>
    public class RandomModel : NoisyAndModel
    {
        /// <summary>
        /// The number of people.
        /// </summary>
        private Variable<int> numberOfPeople;

        /// <summary>
        /// The number of skills.
        /// </summary>
        private Variable<int> numberOfSkills;

        /// <summary>
        /// The number of questions.
        /// </summary>
        private Variable<int> numberOfQuestions;

        /// <summary>
        /// The number of skills for each question.
        /// </summary>
        private VariableArray<int> numberOfSkillsForEachQuestion;

        /// <summary>
        /// The skills for question.
        /// </summary>
        private VariableArray<VariableArray<int>, int[][]> skillsForQuestion;

        /// <summary>
        /// The is correct.
        /// </summary>
        private VariableArray<VariableArray<bool>, bool[][]> isCorrect;

        /// <summary>
        /// The has skills.
        /// </summary>
        private VariableArray<VariableArray<bool>, bool[][]> hasSkills;

        /// <summary>
        /// Constructs the model.
        /// </summary>
        public new void ConstructModel()
        {
            this.numberOfPeople = Variable.New<int>().Named("numPeople").Attrib(new DoNotInfer());
            this.numberOfSkills = Variable.New<int>().Named("numSkills").Attrib(new DoNotInfer());
            this.numberOfQuestions = Variable.New<int>().Named("numQuestions").Attrib(new DoNotInfer());

            this.People = new Range(this.numberOfPeople).Named("people");
            this.Skills = new Range(this.numberOfSkills).Named("skills");
            this.Questions = new Range(this.numberOfQuestions).Named("questions");

            this.numberOfSkillsForEachQuestion = Variable.Array<int>(this.Questions).Named("numSkillsForQuestions").Attrib(new DoNotInfer());
            var questionsSkills = new Range(this.numberOfSkillsForEachQuestion[this.Questions]).Named("questionsXskills");

            this.skillsForQuestion = Variable.Array(Variable.Array<int>(questionsSkills), this.Questions).Named("skillsForQuestion").Attrib(new DoNotInfer());
            
            this.hasSkills = Variable.Array(Variable.Array<bool>(this.Skills), this.People).Named("hasSkills");
            this.hasSkills[this.People][this.Skills] = Variable.Bernoulli(0.5).ForEach(this.People, this.Skills);
            
            this.isCorrect = Variable.Array(Variable.Array<bool>(this.Questions), this.People).Named("isCorrect");

            using (Variable.ForEach(this.People))
            {
                using (Variable.ForEach(this.Questions))
                {
                    var skillsForThisQuestion = Variable.Subarray(this.hasSkills[this.People], this.skillsForQuestion[this.Questions]).Named("skillsForThisQuestion");
                    this.isCorrect[this.People][this.Questions] = Factors.NoisyAllTrue(skillsForThisQuestion, 0.5, 0.5);
                }
            }
        }
    }
}
