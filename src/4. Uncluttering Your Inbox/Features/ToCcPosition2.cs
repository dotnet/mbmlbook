// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Features
{
    using System;
    using System.Collections.Generic;
    using System.Linq;


    /// <summary>
    /// The position.
    /// </summary>
    public enum Position2
    {
        /// <summary>
        /// Part of a list.
        /// </summary>
        PartOfList,

        /// <summary>
        /// First and only on the to line.
        /// </summary>
        FirstAndOnlyInToLine,

        /// <summary>
        /// The first and others on the to line.
        /// </summary>
        FirstAndOthers,

        /// <summary>
        /// Second on the to line.
        /// </summary>
        SecondInToLine,

        /// <summary>
        /// Third or more on the to line.
        /// </summary>
        ThirdOrMoreInToLine,

        /// <summary>
        /// First on the cc line.
        /// </summary>
        FirstInCcLine,

        /// <summary>
        /// Not first on cc line.
        /// </summary>
        NotFirstInCcLine
    }

    /// <summary>
    /// The modified position on to line or Cc line feature.
    /// </summary>
    [Serializable]
    public class ToCcPosition2 : OneOfNFeature
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ToCcPosition2"/> class.
        /// </summary>
        public ToCcPosition2()
            : this(FeatureSet.PositionsShort1)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ToCcPosition2"/> class.
        /// </summary>
        /// <param name="positions">The positions.</param>
        public ToCcPosition2(IEnumerable<string> positions)
        {
            this.Description = "Your position on the To or Cc lines";
            this.StringFormat = "{0}";
            this.BucketNames = positions.Select(ia => new[] { ia }).ToArray();
            this.FeatureBucketFunc = (ia, i) => new FeatureBucket { Index = i, Name = ia[0], Feature = this, Item = (Position2)i };
        }

        /// <summary>
        /// Computes the specified vector.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="message">The message.</param>
        /// <returns>
        /// The index of the feature that is on.
        /// </returns>
        /// <exception cref="FeatureSet.FeatureException">Feature was not configured</exception>
        public override FeatureBucket ComputeFeature(User user, Message message)
        {
            if (this.Count == 0)
            {
                throw new FeatureSet.FeatureException("Feature was not configured");
            }

            return this.Buckets.First(ia => ia.Item == GetPosition(message));
        }

        /// <summary>
        /// Gets the position on the to line.
        /// </summary>
        /// <param name="m">The m.</param>
        /// <returns>
        /// The Position
        /// </returns>
        internal static Position2 GetPosition(Message m)
        {
            Position2 pos = Position2.PartOfList;
            int ct = 0;
            for (int i = 0; i < m.SentTo.Count; i++)
            {
               ContactDetails cd = m.SentTo[i];
                if (cd.IsMe)
                {
                    switch (ct)
                    {
                        case 0:
                            pos = m.SentTo.Count == 1 ? Position2.FirstAndOnlyInToLine : Position2.FirstAndOthers;
                            break;
                        case 1:
                            pos = Position2.SecondInToLine;
                            break;
                        default:
                            pos = Position2.ThirdOrMoreInToLine;
                            break;
                    }

                    break;
                }

                ct++;
            }

            ct = 0;
            foreach (ContactDetails cd in m.CopiedTo)
            {
                if (cd.IsMe)
                {
                    pos = ct == 0 ? Position2.FirstInCcLine : Position2.NotFirstInCcLine;
                    break;
                }

                ct++;
            }

            return pos;
        }
    }
}
