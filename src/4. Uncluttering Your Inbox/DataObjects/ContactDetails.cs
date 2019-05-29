// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
namespace UnclutteringYourInbox
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Xml.Serialization;

    using UnclutteringYourInbox.DataCleaning;

    using MBMLViews.Annotations;

    using Microsoft.ML.Probabilistic.Distributions;

    /// <summary>
    /// A person's contact details
    /// </summary>
    [Serializable]
    public class ContactDetails : IEquatable<ContactDetails>, INotifyPropertyChanged
    {
        /// <summary>
        /// The conversations.
        /// </summary>
        private readonly IList<Conversation> conversations = new ObservableCollection<Conversation>();

        /// <summary>
        /// The email.
        /// </summary>
        private Uncertain<string> email;

        /// <summary>
        /// The name.
        /// </summary>
        private Uncertain<string> name;

        /// <summary>
        /// The anonymize.
        /// </summary>
        private Anonymize anonymize;

        /// <summary>
        /// The person.
        /// </summary>
        private Person person;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContactDetails" /> class.
        /// </summary>
        public ContactDetails()
        {
            this.RoutingType = "EX";
        }

        /// <summary>
        /// The property changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the anonymize.
        /// </summary>
        public Anonymize Anonymize
        {
            get
            {
                return this.anonymize;
            }

            set
            {
                if (value == this.anonymize)
                {
                    return;
                }

                this.anonymize = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged("Email");
                this.OnPropertyChanged("Name");
                this.OnPropertyChanged("Person");
                this.OnPropertyChanged("Conversations");
            }
        }

        /// <summary>
        ///  Gets known conversations involving this contact;
        /// </summary>
        public IList<Conversation> Conversations
        {
            get
            {
                return this.conversations;
            }
        }

        /// <summary>
        /// Gets or sets the person's primary email address
        /// </summary>
        public Uncertain<string> Email
        {
            get
            {
                switch (Anonymize)
                {
                    case Anonymize.AnonymizeByCodes:
                    case Anonymize.AnonymizeByRandomNames:
                        return this.AnonymizedEmail;
                    case Anonymize.DoNotAnonymize:
                        return this.email;
                }

                return this.email;
            }

            set
            {
                this.email = value;
            }
        }

        /// <summary>
        /// Gets or sets the anonymized email.
        /// </summary>
        [XmlIgnore]
        public Uncertain<string> AnonymizedEmail { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this contact is one of my identities
        /// </summary>
        public bool IsMe { get; set; }

        /// <summary>
        /// Gets or sets the person's job title
        /// </summary>
        public Uncertain<string> JobTitle { get; set; }

        /// <summary>
        /// Gets or sets the person's name; .
        /// </summary>
        public Uncertain<string> Name
        {
            get
            {
                switch (Anonymize)
                {
                    case Anonymize.AnonymizeByCodes:
                    case Anonymize.AnonymizeByRandomNames:
                        return this.AnonymizedName;
                    case Anonymize.DoNotAnonymize:
                        return this.name;
                }

                return this.name;
            }

            set
            {
                this.name = value;
            }
        }

        /// <summary>
        /// Gets or sets the anonymized name.
        /// </summary>
        [XmlIgnore]
        public Uncertain<string> AnonymizedName { get; set; } 

        /// <summary>
        /// Gets or sets the person's office location; .
        /// </summary>
        public string OfficeLocation { get; set; }

        /// <summary>
        /// Gets or sets the person these details belong to.
        /// </summary>
        public Person Person
        {
            get
            {
                return this.person;
            }

            set
            {
                if (ReferenceEquals(value, this.person))
                {
                    return;
                }

                this.person = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the received as weight.
        /// </summary>
        public Gaussian ReceivedAsWeight { get; set; }

        /// <summary>
        /// Gets or sets the routing type of the recipient
        /// </summary>
        public string RoutingType { get; set; }

        /// <summary>
        /// Gets or sets the person's user name
        /// </summary>
        public Uncertain<string> UserName { get; set; }

        /// <summary>
        /// Gets or sets a count of the number of messages that this contact detail is a participant of.
        /// </summary>
        internal int MessageCount { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.Name.ToString();
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        bool IEquatable<ContactDetails>.Equals(ContactDetails other)
        {
            if ((other == null) || ReferenceEquals(other.Email.Value, null))
            {
                return false;
            }

            return other.Email.Value.Equals(this.Email.Value);
        }

        /// <summary>
        /// Adds the conversation.
        /// </summary>
        /// <param name="conversation">The conversation.</param>
        internal void AddConversation(Conversation conversation)
        {
            if (this.Conversations.Contains(conversation))
            {
                return;
            }

            this.Conversations.Add(conversation);
        }

        /// <summary>
        /// Remove the conversation.
        /// </summary>
        /// <param name="conversation">The conversation.</param>
        internal void RemoveConversation(Conversation conversation)
        {
            this.Conversations.Remove(conversation);
            this.MessageCount -= conversation.Messages.Count(msg => msg.ParticipatingContacts.Any(cd => cd == this));
        }

        /// <summary>
        /// Called when [property changed].
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}