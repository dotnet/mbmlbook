// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace AssessingPeoplesSkills
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    using MBMLViews;

    /// <summary>
    /// All of the input data (quiz data and responses)
    /// </summary>
    [Serializable]
    public class Inputs
    {
        /// <summary>
        /// Gets or sets the quiz data.
        /// </summary>
        /// <value>
        /// The quiz data.
        /// </value>
        public Quiz Quiz { get; set; }

        /// <summary>
        /// Gets the number of people.
        /// </summary>
        /// <value>
        /// The number of people.
        /// </value>
        public int NumberOfPeople
        {
            get
            {
                return this.IsCorrect == null ? 0 : this.IsCorrect.Length;
            }
        }

        /// <summary>
        /// Gets or sets the raw responses.
        /// </summary>
        /// <value>
        /// The raw responses.
        /// </value>
        public int[][] RawResponses { get; set; }

        /// <summary>
        /// Gets the raw responses as dictionary.
        /// </summary>
        /// <value>
        /// The raw responses as dictionary.
        /// </value>
        public Dictionary<string, object> RawResponsesAsDictionary
        {
            get { return this.GetResponsesAsDictionary(transpose: true); }
        }

        /// <summary>
        /// Gets the is correct masked by skills questions.
        /// </summary>
        /// <value>
        /// The is correct masked by skills questions.
        /// </value>
        public MaskedMatrix IsCorrectMaskedBySkillsQuestions
        {
            get
            {
                return this.IsCorrect == null || Quiz == null
                           ? null
                           : new MaskedMatrix
                               {
                                   Data = this.IsCorrect,
                                   Mask = Quiz.SkillsForQuestion,
                                   MaskLabels = Quiz.SkillShortNames
                               };
            }
        }

        /// <summary>
        /// Gets the has skills masked by skills for questions.
        /// </summary>
        /// <value>
        /// The has skills masked by skills for questions.
        /// </value>
        public MaskedMatrix HasSkillsMaskedBySkillsQuestions
        {
            get
            {
                return this.HasSkills == null || Quiz == null
                           ? null
                           : new MaskedMatrix
                                 {
                                     Data = this.HasSkills,
                                     Mask = Quiz.SkillsForQuestion,
                                     MaskLabels = Quiz.SkillShortNames
                                 };
            }
        }

        /// <summary>
        /// Gets or sets the is correct.
        /// </summary>
        /// <value>
        /// The is correct.
        /// </value>
        public bool[][] IsCorrect { get; set; }
        
        /// <summary>
        /// Gets or sets the correct answers for each question.
        /// </summary>
        /// <value>
        /// The correct answers.
        /// </value>
        public int[] CorrectAnswers { get; set; }

        /// <summary>
        /// Gets or sets the people skills matrix.
        /// </summary>
        /// <value>
        /// The people skills variable.
        /// </value>
        public bool[][] StatedSkills { get; set; }

        /// <summary>
        /// Gets the hasSkills variable.
        /// </summary>
        /// <value>
        /// The hasSkills variable.
        /// </value>
        public bool[][] HasSkills
        {
            get
            {
                bool[][] hasSkills = new bool[this.NumberOfPeople][]; 
                for (int i = 0; i < this.NumberOfPeople; i++)
                {
                    hasSkills[i] = new bool[Quiz.NumberOfQuestions];
                    for (int j = 0; j < Quiz.NumberOfQuestions; j++)
                    {
                        bool b = this.HasAllSkills(i, j);
                        hasSkills[i][j] = b;
                    }
                }

                return hasSkills;
            }
        }

        /// <summary>
        /// Determines whether the specified person has all skills for question.
        /// </summary>
        /// <param name="person">The person.</param>
        /// <param name="question">The question.</param>
        /// <returns>
        ///   <c>true</c> if [the specified person] [has all skills]; otherwise, <c>false</c>.
        /// </returns>
        public bool HasAllSkills(int person, int question)
        {
            bool[] statedSkillsForThisPerson = this.StatedSkills[person];
            bool[] skillsRequiredForThisQuestion = Quiz.SkillsQuestionsMaskTransposed[question];
            return !statedSkillsForThisPerson.Where((t, i) => skillsRequiredForThisQuestion[i] && !t).Any();
        }

        /// <summary>
        /// Gets the responses as dictionary.
        /// </summary>
        /// <param name="transpose">if set to <c>true</c> [transpose].</param>
        /// <param name="raw">if set to <c>true</c> [raw].</param>
        /// <param name="includeColumnOfRowTitles">if set to <c>true</c> [include column of row titles].</param>
        /// <param name="includeCorrectAnswers">if set to <c>true</c> [include correct answers].</param>
        /// <param name="includeSkills">if set to <c>true</c> [include skills].</param>
        /// <returns>The responses</returns>
        public Dictionary<string, object> GetResponsesAsDictionary(
            bool transpose,
            bool raw = true,
            bool includeColumnOfRowTitles = true,
            bool includeCorrectAnswers = true,
            bool includeSkills = true)
        {
            if (Quiz == null)
            {
                return null;
            }

            Dictionary<string, object> annotatedResponses = new Dictionary<string, object>();

            Array responses;
            if (raw)
            {
                responses = this.RawResponses;
            }
            else
            {
                responses = this.IsCorrect;
            }

            if (responses == null)
            {
                return null;
            }

            if (transpose)
            {
                if (includeColumnOfRowTitles)
                {
                    List<string> rowTitles = Enumerable.Range(1, this.NumberOfPeople).Select(ia => "P" + ia.ToString(CultureInfo.InvariantCulture)).ToList();

                    if (includeCorrectAnswers)
                    {
                        rowTitles.Insert(0, "ANS");
                    }

                    annotatedResponses.Add("#", rowTitles.ToArray());
                }

                if (includeSkills)
                {
                    for (int i = 0; i < Quiz.NumberOfSkills; i++)
                    {
                        bool?[] skills = new bool?[this.NumberOfPeople + 1];
                        skills[0] = null;

                        for (int j = 0; j < this.NumberOfPeople; j++)
                        {
                            skills[j + 1] = this.StatedSkills[j][i];
                        }

                        annotatedResponses.Add(string.Format("S{0}", i + 1), skills);
                    }
                }

                for (int i = 0; i < Quiz.NumberOfQuestions; i++)
                {
                    var query = from Array inner in responses select inner.GetValue(i) as int?;
                    List<int?> row = query.ToList();

                    if (includeCorrectAnswers)
                    {
                        row.Insert(0, this.CorrectAnswers[i]);
                    }

                    annotatedResponses.Add(string.Format("Q{0}", i + 1), row.ToArray());
                }
            }
            else
            {
                if (includeColumnOfRowTitles)
                {
                    List<string> rowTitles = Enumerable.Range(1, Quiz.NumberOfQuestions).Select(ia => "Q" + string.Format("{0}", ia)).ToList();

                    if (includeSkills)
                    {
                        rowTitles.InsertRange(
                            0,
                            Enumerable.Range(1, Quiz.NumberOfSkills)
                                      .Select(ia => "S" + ia.ToString(CultureInfo.InvariantCulture)));
                    }

                    annotatedResponses.Add("#", rowTitles);
                }

                if (includeCorrectAnswers)
                {
                    List<int> correct = this.CorrectAnswers.ToList();
                    if (includeSkills)
                    {
                        correct.InsertRange(0, Enumerable.Repeat(1, Quiz.NumberOfSkills));
                    }

                    annotatedResponses.Add("ANS", correct.ToArray());
                }

                for (int i = 0; i < this.NumberOfPeople; i++)
                {
                    int[] ra = responses.GetValue(i) as int[];
                    if (ra == null)
                    {
                        continue;
                    }

                    List<int> row = ra.ToList();

                    if (includeSkills)
                    {
                        for (int j = 0; j < Quiz.NumberOfSkills; j++)
                        {
                            row.Insert(0, Convert.ToInt32(this.StatedSkills[i][j]));
                        }
                    }

                    annotatedResponses.Add(string.Format("P{0}", i + 1), row.ToArray());
                }
            }

            return annotatedResponses;
        }
    }
}
