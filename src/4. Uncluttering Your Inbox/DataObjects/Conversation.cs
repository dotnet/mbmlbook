// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;

    using Microsoft.Research.Glo;

    using MBMLViews.Annotations;

    /// <summary>
    /// A series of messages.
    /// </summary>
    [Serializable]
    public class Conversation : IHasDescription, IHasGroup, INotifyPropertyChanged
    {
        /// <summary>
        /// The messages
        /// </summary>
        private IList<Message> messages = new ObservableCollection<Message>();

        /// <summary>
        /// The number of messages for last calculation
        /// </summary>
        private int numMessagesForLastCalculation = int.MinValue;

        /// <summary>
        /// The participating contacts.
        /// </summary>
        private IList<ContactDetails> participatingContacts = new ObservableCollection<ContactDetails>();

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        
        /// <summary>
        /// Gets or sets the set of messages that belong to this conversation;
        /// </summary>
        public IList<Message> Messages
        {
            get
            {
                return this.messages;
            }

            set
            {
                this.messages = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged("Subject");
                this.OnPropertyChanged("Folder");
                this.OnPropertyChanged("IsPassive");
                this.OnPropertyChanged("IsRepliedTo");
                this.OnPropertyChanged("Date");
                this.OnPropertyChanged("HasAttachments");
                this.OnPropertyChanged("IsLikelyToReply");
                this.OnPropertyChanged("ProbabilityOfReply");
                this.OnPropertyChanged("Flag");
                this.OnPropertyChanged("From");
                this.OnPropertyChanged("Contributors");
                this.OnPropertyChanged("IsRead");
                this.OnPropertyChanged("Description");
                
                foreach (Message msg in this.messages)
                {
                    this.SetVariables(msg);
                }
            }
        }

        /// <summary>
        /// Gets or sets all ContactDetail objects that participate in this conversation (sender, sent-to, copy-to).
        /// </summary>
        public IList<ContactDetails> ParticipatingContacts
        {
            get
            {
                return this.participatingContacts;
            }

            set
            {
                if (ReferenceEquals(value, this.participatingContacts))
                {
                    return;
                }

                this.participatingContacts = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged("Participants");
            }
        }

        /// <summary>
        /// Gets the people in this conversation
        /// </summary>
        public IList<Person> Participants
        {
            get
            {
                return this.ParticipatingContacts == null
                           ? new List<Person>()
                           : this.ParticipatingContacts.Select(x => x.Person).Distinct().ToList();
            }
        }

        /// <summary>
        /// Gets or sets the group of people associated with this conversation, if any.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Uncertain<Group> Group { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>
        /// The id.
        /// </value>
        public string Id { get; set; }

        /// <summary>
        /// Gets the subject.
        /// </summary>
        /// <value>
        /// The subject.
        /// </value>
        public string Subject
        {
            get
            {
                string s = this.Messages.Count > 0 ? this.Messages[0].Subject : string.Empty;
                
                // remove two or three letter strings, followed by a colon
                while (true)
                {
                    s = s.TrimStart();
                    int k = s.IndexOf(":", StringComparison.Ordinal);
                    if ((k >= 1) && (k <= 3))
                    {
                        s = s.Substring(k + 1);
                    }
                    else
                    {
                        break;
                    }
                }
                
                return s;
            }
        }

        /// <summary>
        /// Gets the folder.
        /// </summary>
        /// <value>
        /// The folder.
        /// </value>
        public MessageFolder Folder
        {
            get
            {
                MessageFolder folder = null;
                
                // return the folder of the last message, unless this is 'sent items'
                // in which case look for an earlier message which is in a different folder and
                // return that if possible.
                for (int i = this.Messages.Count - 1; i >= 0; i--)
                {
                    folder = this.Messages[i].Folder;
                    if (!folder.IsSentItems)
                    {
                        return folder;
                    }
                }

                return folder;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is passive.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is passive; otherwise, <c>false</c>.
        /// </value>
        public bool IsPassive
        {
            get { return !this.Messages.Any(x => x.Sender.IsMe); }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is replied to.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is replied to; otherwise, <c>false</c>.
        /// </value>
        public bool IsRepliedTo
        {
            get { return this.Messages.Any(x => x.IsRepliedTo); }
        }

        /// <summary>
        /// Gets the date.
        /// </summary>
        /// <value>
        /// The date.
        /// </value>
        public DateTime Date
        {
            get
            {
                return this.Messages.Count > 0 ? this.Messages.Last().DateSent : default(DateTime);
            }
        }

        /// <summary>
        /// Gets a value indicating whether has attachments.
        /// </summary>
        public bool HasAttachments
        {
            get { return this.Messages.Any(x => x.HasAttachments); }
        }

        /// <summary>
        /// Gets a value indicating whether is likely to reply.
        /// </summary>
        public bool IsLikelyToReply
        {
            get
            {
                return this.Messages.Any(m => m.IsRepliedTo);
            }
        }

        /// <summary>
        /// Gets the probability of reply.
        /// </summary>
        public double ProbabilityOfReply
        {
            get
            {
                return this.Messages.Select(ia => double.IsNaN(ia.ProbabilityOfReply) ? 0.0 : ia.ProbabilityOfReply).Max();
            }
        }

        /// <summary>
        /// Gets the flag.
        /// </summary>
        public FlagState Flag
        {
            get
            {
                FlagState f = FlagState.NotFlagged;
                if (this.Messages.Any(x => x.Flag == FlagState.FlaggedComplete))
                {
                    f = FlagState.FlaggedComplete;
                }

                if (this.Messages.Any(x => x.Flag == FlagState.Flagged))
                {
                    f = FlagState.Flagged;
                }

                return f;
            }
        }

        /// <summary>
        /// Gets who the message is from.
        /// </summary>
        [Browsable(false)]
        public ContactDetails From
        {
            get
            {
                return this.Messages.Count > 0 ? this.Messages[0].Sender : null;
            }
        }

        /// <summary>
        /// Gets or sets the mailbox owner.
        /// </summary>
        /// <value>
        /// The mailbox owner.
        /// </value>
        public ContactDetails MailboxOwner { get; set; }

        /// <summary>
        /// Gets the contributors.
        /// </summary>
        /// <value>
        /// The contributors.
        /// </value>
        public IList<ContactDetails> Contributors
        {
            get
            {
                return this.Messages.Select(m => m.Sender).Distinct().ToList();
            }
        }
        
        #region IHasDescription Members

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>
        /// The description.
        /// </value>
        public string Description
        {
            get
            {
                return ToCommaSeparatedString(this.Contributors.Select(x => x.Person == null ? string.Empty : x.Person.Name).Distinct());
            }
        }

        /// <summary>
        /// Gets a value indicating whether is read.
        /// </summary>
        public bool IsRead
        {
            get
            {
                return this.Messages.All(x => x.IsRead);
            }
        }

        #endregion

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.Subject;
        }

        /// <summary>
        /// Adds the message.
        /// </summary>
        /// <param name="email">The email.</param>
        public void AddMessage(Message email)
        {
            int i;
            for (i = 0; i < this.Messages.Count; i++)
            {
                if (email.DateSent < this.Messages[i].DateSent)
                {
                    break; // email < Messages[i] == email is older (has smaller date value)
                }
            }

            this.Messages.Insert(i, email); // inserted in order where earliest emails have lower indices
        }

        /// <summary> Conversations the view list.</summary>
        /// <returns>The list of messages.</returns>
        public IList<Message> ConversationViewList()
        {
            if (this.messages.Count <= 0)
            {
                return new ObservableCollection<Message>(); // this should never happen
            }
            
            this.CalculateMessageDepths();
            
            // This is needed for data from desktop search provider to populate Replies property
            IList<Message> list = new ObservableCollection<Message>();
            list = this.messages.Where(msg => msg.ConversationDepth == 0)
                       .Aggregate(list, (current, m) => current.Concat(this.RecursiveConversationViewList(m)).ToList());

            list = list.Concat(this.messages.Where(msg => !list.Contains(msg))).ToList();

            return list;
        }

        /// <summary>
        /// Recursive conversation view list.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>A list of messages</returns>
        public IList<Message> RecursiveConversationViewList(Message message)
        {
            IList<Message> list = new ObservableCollection<Message>();

            list.Add(message);

            // Depth First Search traversal of message tree
            return message.Replies.Aggregate(list, (current, child) => current.Concat(this.RecursiveConversationViewList(child)).ToList());
        }

        /// <summary>
        /// To the comma separated string.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <returns>The string</returns>
        internal static string ToCommaSeparatedString(IEnumerable items)
        {
            StringBuilder sb = new StringBuilder();
            int ct = 0;
            foreach (var p in items)
            {
                if (ct++ > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(p);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Calculates the message depths.
        /// TODO: these calculations could replace the hacky reply-to calculation in Message class
        /// </summary>
        internal void CalculateMessageDepths()
        {
            if (this.numMessagesForLastCalculation == this.Messages.Count)
            {
                return;
            }

            IList<Message> messageEnumerable = this.Messages;
            for (int i = 0; i < messageEnumerable.Count; i++)
            {
                Message m = messageEnumerable[i];

                m.ConversationDepth = 0;
                if (i == 0) 
                { 
                    m.ConversationDepth = 0;
                }
                else
                {
                    bool foundPreviousEmail = false;

                    if (m.SentTo != null)
                    {
                        // first look for previous message sent by somebody on the SentTo list of M
                        IList<Person> sendToListOfM = m.SentTo.Select(cd => cd.Person).ToList();
                        for (int j = i - 1; j >= 0; j--) 
                        {
                            // search through all messages before this one in reverse order
                            if (!sendToListOfM.Contains(messageEnumerable[j].Sender.Person))
                            {
                                continue;
                            } 
                            
                            // find the latest message before m who's sender is on the sent-to list of m
                            m.ConversationDepth = messageEnumerable[j].ConversationDepth + 1;
                            messageEnumerable[j].AddReply(m); // add m to the Reply list of j
                            foundPreviousEmail = true;
                            break;
                        }
                    }

                    if (!foundPreviousEmail)
                    {
                        // next look for previous message sent by somebody on the CopiedTo list of M
                        IList<Person> copiedToListOfM = m.CopiedTo.Select(cd => cd.Person).ToList(); 
                        for (int j = i - 1; j >= 0; j--) 
                        {
                            // search through all messages before this one in reverse order
                            if (!copiedToListOfM.Contains(messageEnumerable[j].Sender.Person))
                            {
                                continue;
                            } 
                            
                            // find the latest message before m who's sender is on the copied-to list of m
                            m.ConversationDepth = messageEnumerable[j].ConversationDepth + 1;
                            messageEnumerable[j].AddReply(m); // add m to the Reply list of j
                            break;
                        }
                    }

                    if (m.ConversationDepth == -1)
                    {
                        // do nothing
                        //Console.WriteLine("couldn't find parent message of" + m.Summary);
                    }
                }
            }

            this.numMessagesForLastCalculation = this.Messages.Count;
        }

        /// <summary>
        /// Sets the variables.
        /// </summary>
        /// <param name="email">The email.</param>
        internal void SetVariables(Message email)
        {
            email.Conversation = this;

            this.AddParticipant(email.Sender);
            
            if (email.SentTo != null)
            {
                foreach (ContactDetails p in email.SentTo)
                {
                    this.AddParticipant(p);
                }
            }

            if (email.CopiedTo != null)
            {
                foreach (ContactDetails p in email.CopiedTo)
                {
                    this.AddParticipant(p);
                }
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

        /// <summary>
        /// Adds the participant.
        /// </summary>
        /// <param name="person">The person.</param>
        private void AddParticipant(ContactDetails person)
        {
            if (person == null)
            {
                return;
            }

            person.MessageCount++;

            if (!this.ParticipatingContacts.Contains(person))
            {
                this.ParticipatingContacts.Add(person);
            }

            if (!person.Conversations.Contains(this))
            {
                person.Conversations.Add(this);
            }
        }
    }
}
