// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AssessingPeoplesSkills
{
    using System;
    using System.Linq;

    using MBMLViews;

    /// <summary>
    /// Quiz Data
    /// </summary>
    [Serializable]
    public class Quiz
    {
        /// <summary>
        /// Gets the number of skills.
        /// </summary>
        /// <value>
        /// The number of skills.
        /// </value>
        public int NumberOfSkills => SkillNames?.Length ?? 0;

        /// <summary>
        /// Gets the number of questions.
        /// </summary>
        /// <value>
        /// The number of questions.
        /// </value>
        public int NumberOfQuestions => SkillsForQuestion?.Length ?? 0;

        /// <summary>
        /// Gets or sets the skill names.
        /// </summary>
        /// <value>
        /// The skill names.
        /// </value>
        public string[] SkillNames { get; set; }

        /// <summary>
        /// Gets the skill short names.
        /// </summary>
        /// <value>
        /// The skill short names.
        /// </value>
        public string[] SkillShortNames
        {
            get
            {
                Func<string, string> shortener =
                    x =>
                    x.Split('(')[0].Replace("Core programming skills", "Core")
                                   .Replace("Object Oriented Programming", "OOP")
                                   .Replace("Application Life Cycle Management", "Life Cycle")
                                   .Replace("Application", "App")
                                   .Replace("application", "app")
                                   .Replace("Microsoft Windows desktop", "Desktop")
                                   .Replace("Databases & SQL", "SQL");

                Func<string, string> removeNumbers = x => 
                {
                    var parts = x.Split(':');
                    if (parts.Length > 1)
                        return parts[1].Trim();
                    else
                        return parts[0].Trim();
                };
                
                return SkillNames?.Select(removeNumbers).Select(shortener).ToArray();
            }
        }

        /// <summary>
        /// Gets or sets the skills for question.
        /// </summary>
        /// <value>
        /// The skills for question.
        /// </value>
        public int[][] SkillsForQuestion { get; set; }

        /// <summary>
        /// Gets the number skills for question.
        /// </summary>
        /// <value>
        /// The number skills for question.
        /// </value>
        public int[] NumberSkillsForQuestion => SkillsForQuestion?.Select(row => row.Length).ToArray();

        /// <summary>
        /// Gets the skills questions mask.
        /// </summary>
        /// <value>
        /// The skills questions mask.
        /// </value>
        public bool[][] SkillsQuestionsMask
        {
            get
            {
                if (this.SkillsForQuestion == null)
                {
                    return null;
                }

                int numberOfQuestions = this.SkillsForQuestion.Length;
                int numberOfSkills = this.SkillNames.Length;

                bool[][] skillsQuestionsMask = new bool[numberOfSkills][];

                for (int i = 0; i < numberOfSkills; i++)
                {
                    skillsQuestionsMask[i] = new bool[numberOfQuestions];
                }

                for (int i = 0; i < numberOfQuestions; i++)
                {
                    int[] sfqi = this.SkillsForQuestion[i];
                    foreach (int t in sfqi)
                    {
                        skillsQuestionsMask[t][i] = true;
                    }
                }

                return skillsQuestionsMask;
            }
        }

        /// <summary>
        /// Gets the skills questions mask transposed.
        /// </summary>
        /// <value>
        /// The skills questions mask transposed.
        /// </value>
        public bool[][] SkillsQuestionsMaskTransposed
        {
            get
            {
                return this.SkillsQuestionsMask.Transpose();
            }
        }
    }
}
