// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace MeetingYourMatch.Models.TrueSkill
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.ML.Probabilistic;
    using Microsoft.ML.Probabilistic.Distributions;
    using Microsoft.ML.Probabilistic.Models;
    using Microsoft.ML.Probabilistic.Models.Attributes;

    using global::MeetingYourMatch.Items;
    using MBMLViews;
    using MBMLViews.Views;

    /// <summary>
    /// Two player model with listening for messages.
    /// </summary>
    public class TwoPlayerMessages
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TwoPlayerMessages"/> class.
        /// </summary>
        public TwoPlayerMessages()
        {
            // Parameterless constructor for serialization only
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TwoPlayerMessages" /> class.
        /// </summary>
        /// <param name="performanceVariance">The performance variance.</param>
        /// <param name="skill1Prior">The skill1 prior.</param>
        /// <param name="skill2Prior">The skill2 prior.</param>
        /// <param name="outcome">if set to <c>true</c> [outcome].</param>
        /// <param name="showFactorGraph">if set to <c>true</c> [show factor graph].</param>
        public TwoPlayerMessages(double performanceVariance, Gaussian skill1Prior, Gaussian skill2Prior, MatchOutcome outcome, bool showFactorGraph = false)
        {
            var skill1 = Variable.New<double>().Named("JSkill").Attrib(new ListenToMessages());
            var skill2 = Variable.New<double>().Named("FSkill").Attrib(new ListenToMessages());

            skill1.SetTo(Variable.Random(skill1Prior).Named("JSkillPrior").Attrib(new ListenToMessages()));
            skill2.SetTo(Variable.Random(skill2Prior).Named("FSkillPrior").Attrib(new ListenToMessages()));

            var player1Performance = Variable.GaussianFromMeanAndVariance(skill1, performanceVariance).Named("JPerf").Attrib(new ListenToMessages());
            var player2Performance = Variable.GaussianFromMeanAndVariance(skill2, performanceVariance).Named("FPerf").Attrib(new ListenToMessages());
            
            var player1Wins = (player1Performance > player2Performance).Named("greaterThan").Attrib(new ListenToMessages());

            // Set observed value
            player1Wins.ObservedValue = outcome == MatchOutcome.Player1Win;

            this.MessageHistories = new Dictionary<string, List<Gaussian>>();

            var engine = Utils.GetDefaultEngine(showFactorGraph);
            engine.OptimiseForVariables = new IVariable[] { skill1 };
            engine.NumberOfIterations = 1;
            engine.MessageUpdated += this.MessageUpdated;

            var skill1Posterior = engine.Infer<Gaussian>(skill1);
            Console.WriteLine(@"Skill posterior {0}", skill1Posterior.ToString("N2"));
        }

        /// <summary>
        /// Gets or sets the message histories.
        /// </summary>
        public Dictionary<string, List<Gaussian>> MessageHistories { get; set; }

        /// <summary>
        /// Gets the message histories concise.
        /// </summary>
        public Dictionary<string, Gaussian> MessageHistoriesConcise
        {
            get
            {
                return this.MessageHistories?.Where(ia => !ia.Key.Contains("use"))
                    .ToDictionary(ia => ia.Key.Replace("_", string.Empty), ia => ia.Value.First());
            }
        }

        /// <summary>
        /// Gets the message histories strings.
        /// </summary>
        public Dictionary<string, string> MessageHistoriesStrings
        {
            get
            {
                return this.MessageHistoriesConcise?.ToDictionary(ia => ia.Key, ia => ia.Value.ToString());
            }
        }

        /// <summary>
        /// Gets the message histories table.
        /// </summary>
        public List<MessageDetails> MessageHistoriesTable
        {
            get
            {
                if (MessageHistoriesConcise == null)
                    return null;

                var range = new RealRange { Min = 0, Max = 400, Steps = 1500 };

                var list = new List<MessageDetails>();
                foreach (var ia in MessageHistoriesConcise)
                {
                    string direction;
                    string factor;
                    string variable = ia.Key.Substring(0, ia.Key.Length - 1); 
                    var gaussian = ia.Value;

                    if (ia.Key.EndsWith("F"))
                    {
                        direction = "Forwards";
                        factor = variable.EndsWith("marginal")
                                   ? list.Last(el => el.Factor.StartsWith(variable.Substring(0, variable.Length - "marginal".Length))).Variable
                                   : list.LastOrDefault(el => el.Variable.EndsWith(new string(variable.Last(), 1))).Variable ?? variable + "prior";
                    }
                    else
                    {
                        direction = "Backwards";
                        factor = list.Last(el => el.Variable == variable).Factor;
                    }

                    double EvalFunc(double x) => Math.Exp(gaussian.GetLogProb(x));

                    list.Add(
                        new MessageDetails
                            {
                                Factor = factor,
                                Variable = variable,
                                Direction = direction,
                                Message = new FunctionViewModel { Name = gaussian.ToString("N2"), Function = EvalFunc, Range = range }
                            });
                }

                return list;
            }
        }

        /// <summary>
        /// Message updated.
        /// </summary>
        /// <param name="algorithm">The algorithm.</param>
        /// <param name="messageEvent">The <see cref="MessageUpdatedEventArgs"/> instance containing the event data.</param>
        private void MessageUpdated(IGeneratedAlgorithm algorithm, MessageUpdatedEventArgs messageEvent)
        {
            if (!this.MessageHistories.ContainsKey(messageEvent.MessageId))
            {
                this.MessageHistories[messageEvent.MessageId] = new List<Gaussian>();
            }

            if (messageEvent.Message is Gaussian item)
            {
                this.MessageHistories[messageEvent.MessageId].Add(item);
            }

            // Console.WriteLine(messageEvent);
        }

        /// <summary>
        /// The message details.
        /// </summary>
        public struct MessageDetails
        {
            /// <summary>
            /// Gets or sets the factor.
            /// </summary>
            public string Factor { get; set; }

            /// <summary>
            /// Gets or sets the variable.
            /// </summary>
            public string Variable { get; set; }

            /// <summary>
            /// Gets or sets the direction.
            /// </summary>
            public string Direction { get; set; }

            /// <summary>
            /// Gets or sets the message.
            /// </summary>
            public FunctionViewModel Message { get; set; }
        } 
    }
}
