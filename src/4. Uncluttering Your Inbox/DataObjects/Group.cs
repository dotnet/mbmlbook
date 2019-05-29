// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Interface to an item which has an associated date.
    /// </summary>
    public interface IHasDate
    {
        /// <summary>
        /// Gets the date.
        /// </summary>
        DateTime Date { get; }
    }

    /// <summary>
    /// Interface to an item which is associated with a group of people.
    /// </summary>
    public interface IHasGroup : IHasDate
    {
        /// <summary>
        /// Gets or sets the group.
        /// </summary>
        Uncertain<Group> Group { get; set; }
    }

    /// <summary>
    /// Represents a group of people
    /// </summary>
    [Serializable]
    public class Group
    {
        /// <summary>
        /// The name.
        /// </summary>
        private string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="Group"/> class.
        /// </summary>
        public Group()
        {
            this.Members = new List<Uncertain<Person>>();
            this.Items = new List<IHasGroup>();
        }

        /// <summary>
        /// Gets the people in the group
        /// </summary>
        public IList<Uncertain<Person>> Members { get; internal set; }

        /// <summary>
        /// Gets or sets the items associated with this group, ordered with most recent first.
        /// </summary>
        public IList<IHasGroup> Items { get; protected set; }

        /// <summary>
        /// Gets the date the group was formed
        /// </summary>
        public DateTime StartDate
        {
            get
            {
                return (this.Items.Count > 0 ? this.Items.Last().Date : DateTime.Now).Date;
            }
        }

        /// <summary>
        /// Gets the date the group last interacted, or the date of the most recent interaction
        /// </summary>
        public DateTime EndDate
        {
            get
            {
                return (this.Items.Count > 0 ? this.Items.First().Date : DateTime.Now).Date;
            }
        }

        /// <summary>
        /// Gets the owner fraction.
        /// </summary>
        public double OwnerFraction
        {
            get
            {
                double ct = this.Items.Count(IsOwner);
                return ct / this.Items.Count;
            }
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name
        {
            get
            {
                return this.name ?? this.GetGroupString();
            }

            set
            {
                this.name = value;
            }
        }

        /// <summary>
        /// Gets or sets the weight associated with this group 
        /// </summary>
        public double Weight { get; set; }

        /// <summary>
        /// Gets the contribution.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The contribution.</returns>
        public double GetContribution(IHasGroup item)
        {
            Conversation conversation = item as Conversation;
            if (conversation != null)
            {
                return conversation.Contributors.Any(p => p.IsMe) ? 1 : 0;
            }

            return 0;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.GetGroupString();
        }

        /// <summary>
        /// Gets the group string.
        /// </summary>
        /// <returns>The group string.</returns>
        protected string GetGroupString()
        {
            StringBuilder sb = new StringBuilder();
            int count = 0;
            foreach (var cd in this.Members.Where(cd => !(cd.Value is User)))
            {
                if (cd.Probability < 0.15)
                {
                    count++;
                    continue;
                }

                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(cd.Value);
                if (cd.Probability < 0.3)
                {
                    sb.Append("?");
                }
            }

            if (count > 0)
            {
                if (sb.Length == 0)
                {
                    sb.Append(count + ((count > 1) ? " people" : " person"));
                }
                else
                {
                    sb.Append(" and " + count + ((count > 1) ? " others" : " other"));
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Determines whether the specified item is owner.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>
        ///   <c>true</c> if the specified item is owner; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsOwner(IHasGroup item)
        {
            Conversation conversation = item as Conversation;
            return conversation != null && conversation.From.IsMe;
        }
    }
}
