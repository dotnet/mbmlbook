// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;

    using MBMLViews.Annotations;

    using Microsoft.ML.Probabilistic.Distributions;

    /// <summary>
    ///     Person class
    /// </summary>
    [Serializable]
    public class Person : INotifyPropertyChanged
    {
        /// <summary>
        ///     The identities
        /// </summary>
        private ObservableCollection<ContactDetails> identities = new ObservableCollection<ContactDetails>();

        /// <summary>
        /// The messages.
        /// </summary>
        private IList<Message> messages;

        /// <summary>
        /// Initializes a new instance of the <see cref="Person"/> class.
        /// </summary>
        public Person()
        {
            this.Identities = new ObservableCollection<ContactDetails>();
            this.Messages = new ObservableCollection<Message>();
            this.Reports = new ObservableCollection<Person>();
            this.MessageFolders = new ObservableCollection<MessageFolder>();
            this.ManagerHierarchy = new ObservableCollection<Person>();
            this.ManagerHasBeenSet = false;
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the conversations where this Person is a participant in at least one message.
        /// </summary>
        public IList<Conversation> Conversations
        {
            get
            {
                return this.Messages.Select(m => m.Conversation).Distinct().ToList();
            }
        }

        /// <summary>
        /// Gets the first name.
        /// </summary>
        [Browsable(false)]
        public Uncertain<string> FirstName
        {
            get
            {
                string s = this.Name.Value;
                if (s == null)
                {
                    return null;
                }

                int k = s.IndexOf(" ", StringComparison.Ordinal);
                if (k != -1)
                {
                    s = s.Substring(0, k);
                }

                return Uncertain<string>.FromProb(s, this.Name.Probability);
            }
        }

        /// <summary>
        /// Gets or sets the identities. Each Person consists of several ContactDetails (e.g., separate email accounts for the same person).
        /// </summary>
        public ObservableCollection<ContactDetails> Identities
        {
            get
            {
                return this.identities;
            }

            set
            {
                this.identities = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged("IsMe");
                this.OnPropertyChanged("BestIdentity");
                
                foreach (ContactDetails cd in this.identities)
                {
                    cd.Person = this; // make sure to set this backwards link appropriately
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether is me.
        /// </summary>
        public bool IsMe
        {
            get
            {
                return this.Identities.Any(cd => cd.IsMe);
            }
        }

        /// <summary>
        /// Gets or sets this person's manager - could be null
        /// </summary>
        public Person Manager { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the manager has been set. If true, <see cref="Manager" /> can still be null
        /// </summary>
        public bool ManagerHasBeenSet { get; set; }

        /// <summary>
        /// Gets or sets a list of managers in the org hierarchy of this person, starting
        ///     with the immediate manager and working upwards
        /// </summary>
        public IList<Person> ManagerHierarchy { get; set; }

        /// <summary>
        ///     Gets or sets the message folders for this Person
        /// </summary>
        public IList<MessageFolder> MessageFolders { get; set; }

        /// <summary>
        ///     Gets or sets the messages that this person is a participant of
        /// </summary>
        public IList<Message> Messages
        {
            get
            {
                return this.messages;
            }

            set
            {
                if (ReferenceEquals(value, this.messages))
                {
                    return;
                }

                this.messages = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged("Conversations");
            }
        }

        /// <summary>
        ///     Gets or sets the messages from user.
        /// </summary>
        public List<Message> MessagesFromUser { get; set; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public Uncertain<string> Name
        {
            get
            {
                ContactDetails best = this.BestIdentity;
                return best == null ? null : best.Name;
            }
        }

        /// <summary>
        /// Gets the owner cc weight.
        /// </summary>
        public Gaussian OwnerCcWeight { get; internal set; }

        /// <summary>
        /// Gets the owner weight.
        /// </summary>
        public Gaussian OwnerWeight { get; internal set; }

        /// <summary>
        /// Gets or sets the persons who report to this person
        /// </summary>
        public IList<Person> Reports { get; set; }

        /// <summary>
        /// Gets the sender cc weight.
        /// </summary>
        public Gaussian SenderCcWeight { get; internal set; }

        /// <summary>
        /// Gets the sender weight.
        /// </summary>
        public Gaussian SenderWeight { get; internal set; }

        /// <summary>
        /// Gets the short name.
        /// </summary>
        [Browsable(false)]
        public virtual Uncertain<string> ShortName
        {
            get
            {
                return this.FirstName;
            }
        }

        /// <summary>
        /// Gets the best name.
        /// </summary>
        public Uncertain<string> BestName
        {
            get
            {
                return this.IsMe ? this.ShortName : this.Name;
            }
        }

        /// <summary>
        /// Gets the most recent message time.
        /// </summary>
        public DateTime MostRecentMessageTime
        {
            get
            {
                IEnumerable<Message> allMessages = this.Conversations.SelectMany(c => c.Messages);
                IEnumerable<Message> messagesArray = allMessages as Message[] ?? allMessages.ToArray();
                return messagesArray.Count() != 0 ? messagesArray.Max(m => m.DateReceived) : DateTime.MinValue;
            }
        }

        /// <summary>
        /// Gets the best identity.
        /// </summary>
        public ContactDetails BestIdentity
        {
            get
            {
                return this.Identities == null || this.Identities.Count == 0 ? null : this.Identities.OrderByDescending(ia => ia.MessageCount).First();
            }
        }

        /// <summary>
        /// Add an identity.
        /// </summary>
        /// <param name="contactDetails">The contact details.</param>
        public void AddIdentity(ContactDetails contactDetails)
        {
            if (this.Identities.Contains(contactDetails))
            {
                return;
            }

            this.Identities.Add(contactDetails);
            contactDetails.Person = this;
            this.CopyIdentityLists(contactDetails);
        }

        /// <summary>
        /// Removes the conversation.
        /// </summary>
        /// <param name="c">
        /// The c.
        /// </param>
        public void RemoveConversation(Conversation c)
        {
            // first remove the conversation/messages from this person object
            this.Conversations.Remove(c);
            foreach (Message msg in c.Messages)
            {
                this.Messages.Remove(msg);
            }

            // and then let the ContactDetails do what they need to do
            foreach (ContactDetails cd in this.Identities)
            {
                cd.RemoveConversation(c);
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
            ContactDetails id = this.BestIdentity;
            return (id == null) ? string.Empty : id.ToString();
        }

        /// <summary>
        /// Copies the identity lists.
        /// </summary>
        /// <param name="contactDetails">The details.</param>
        internal void CopyIdentityLists(ContactDetails contactDetails)
        {
            foreach (Conversation c in contactDetails.Conversations.Where(c => !this.Conversations.Contains(c)))
            {
                this.Conversations.Add(c);
            }
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