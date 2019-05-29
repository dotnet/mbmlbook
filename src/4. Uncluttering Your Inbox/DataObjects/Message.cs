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

    using UnclutteringYourInbox.Features;
    using UnclutteringYourInbox.Text;

    using MBMLViews.Annotations;

    using Microsoft.ML.Probabilistic.Collections;
    using Microsoft.ML.Probabilistic.Distributions;

    #region enumerated types
    /// <summary>
    ///     The flag state.
    /// </summary>
    public enum FlagState
    {
        /// <summary>
        ///     not flagged state.
        /// </summary>
        NotFlagged, 

        /// <summary>
        ///     flagged state.
        /// </summary>
        Flagged, 

        /// <summary>
        ///     flagged complete.
        /// </summary>
        FlaggedComplete
    }

    /// <summary>
    ///     The sender type.
    /// </summary>
    public enum SenderType
    {
        /// <summary>
        ///     The yes.
        /// </summary>
        Yes, 

        /// <summary>
        ///     The no.
        /// </summary>
        No, 

        /// <summary>
        ///     The all new recipients.
        /// </summary>
        AllNewRecipients, 

        /// <summary>
        ///     The no cc recipients.
        /// </summary>
        NoCcRecipients
    }

    /// <summary>
    ///     The subject word type.
    /// </summary>
    public enum SubjectWordType
    {
        /// <summary>
        ///     The contains top 10 word.
        /// </summary>
        ContainsTop10Word, 

        /// <summary>
        ///     The no top 10 words.
        /// </summary>
        NoTop10Words
    }

    /// <summary>
    ///     The primary action.
    /// </summary>
    public enum PrimaryAction
    {
        /// <summary>
        ///     The not read.
        /// </summary>
        NotRead,

        /// <summary>
        ///     The read.
        /// </summary>
        Read,

        /// <summary>
        ///     The move.
        /// </summary>
        Move,

        /// <summary>
        ///     The delete.
        /// </summary>
        Delete,

        /// <summary>
        ///     The reply.
        /// </summary>
        Reply,

        /// <summary>
        ///     The flag.
        /// </summary>
        Flag
    }

    /// <summary>
    ///     The from me.
    /// </summary>
    public enum FromMe
    {
        /// <summary>
        ///     The from me to only me.
        /// </summary>
        FromMeToOnlyMe,

        /// <summary>
        ///     The forward from me to only me.
        /// </summary>
        ForwardFromMeToOnlyMe,

        /// <summary>
        ///     The from me to me and others.
        /// </summary>
        FromMeToMeAndOthers,

        /// <summary>
        ///     The forward from me to me and others.
        /// </summary>
        ForwardFromMeToMeAndOthers,

        /// <summary>
        ///     The from me cc me.
        /// </summary>
        FromMeCcMe,

        /// <summary>
        ///     The not from me.
        /// </summary>
        NotFromMe
    }

    /// <summary>
    ///     The to me.
    /// </summary>
    public enum ToMe
    {
        /// <summary>
        ///     The to only me.
        /// </summary>
        ToOnlyMe,

        /// <summary>
        ///     The to only me with cc.
        /// </summary>
        ToOnlyMeWithCc,

        /// <summary>
        ///     The other.
        /// </summary>
        Other
    }

    /// <summary>
    ///     The position.
    /// </summary>
    public enum Position
    {
        /// <summary>
        ///     The unknown.
        /// </summary>
        Unknown,

        /// <summary>
        ///     The sender.
        /// </summary>
        Sender,

        /// <summary>
        ///     The on to line.
        /// </summary>
        OnToLine,

        /// <summary>
        ///     The on cc line.
        /// </summary>
        OnCcLine,

        /// <summary>
        ///     The not on mail.
        /// </summary>
        NotOnMail
    }

    /// <summary>
    ///     The conversation started by me.
    /// </summary>
    public enum ConversationStartedByMe
    {
        /// <summary>
        ///     The yes.
        /// </summary>
        Yes,

        /// <summary>
        ///     The no.
        /// </summary>
        No,

        /// <summary>
        ///     The this is first message.
        /// </summary>
        ThisIsFirstMessage
    }

    /// <summary>
    ///     The reply to message from me.
    /// </summary>
    public enum ReplyToMessageFromMe
    {
        /// <summary>
        ///     The yes.
        /// </summary>
        Yes,

        /// <summary>
        ///     The no.
        /// </summary>
        No,

        /// <summary>
        ///     The this is first message.
        /// </summary>
        ThisIsFirstMessage,

        /// <summary>
        ///     The don't know.
        /// </summary>
        DontKnow
    }

    /// <summary>
    ///     The forward type.
    /// </summary>
    public enum ForwardType
    {
        /// <summary>
        ///     The yes.
        /// </summary>
        Yes,

        /// <summary>
        ///     The no.
        /// </summary>
        No,

        /// <summary>
        ///     The no but conversation was started by forward.
        /// </summary>
        NoButConversationWasStartedByForward
    }
    #endregion enumerated types

    /// <summary>
    ///     Message class
    /// </summary>
    [Serializable]
    public class Message : INotifyPropertyChanged
    {
        /// <summary>
        ///     The conversation depth.
        /// </summary>
        private int conversationDepth = int.MinValue;

        /// <summary>
        ///     The message body.
        /// </summary>
        private string messageBody;

        /// <summary>
        ///     The new text.
        /// </summary>
        private string newText;

        /// <summary>
        ///     The participants.
        /// </summary>
        private IList<Person> participants;

        /// <summary>
        ///     Replies are set by the exchange provider, or in CalculateConversationDepth (for the desktop search provider)
        ///     Would like to figure out the best time for CalculateConversationDepth to be called, don't really want
        ///     it to be called after each AddMessage, but not sure where else;
        /// </summary>
        private IList<Message> replies;

        /// <summary>
        ///     The subject words.
        /// </summary>
        private List<string> subjectWords;

        /// <summary>
        ///     The time of reply.
        /// </summary>
        private DateTime? timeOfReply;

        /// <summary>
        /// The new text individual words.
        /// </summary>
        private List<string> newTextWords;

        /// <summary>
        /// The copied to.
        /// </summary>
        private List<ContactDetails> copiedTo;

        /// <summary>
        /// The sent to.
        /// </summary>
        private List<ContactDetails> sentTo;

        /// <summary>
        /// The sender.
        /// </summary>
        private ContactDetails sender;

        /// <summary>
        /// The conversation.
        /// </summary>
        private Conversation conversation;

        /// <summary>
        /// The from.
        /// </summary>
        private ContactDetails from;

        /// <summary>
        /// The received by.
        /// </summary>
        private ContactDetails receivedBy;

        /// <summary>
        /// The received representing.
        /// </summary>
        private ContactDetails receivedRepresenting;

        /// <summary>
        /// The replied to message.
        /// </summary>
        private Message repliedToMessage;

        /// <summary>
        /// The subject.
        /// </summary>
        private string subject;

        /// <summary>
        /// The summary.
        /// </summary>
        private string summary;

        /// <summary>
        /// The current feature set type.
        /// </summary>
        private FeatureSetType currentFeatureSetType;

        /// <summary>
        /// The probability of reply.
        /// </summary>
        private Dictionary<FeatureSetType, double> probabilityOfReplyDictionary;

        /// <summary>
        /// The feature value and weight dictionary.
        /// </summary>
        private Dictionary<FeatureSetType, Dictionary<FeatureBucket, Pair<double, Gaussian>>> featureValueAndWeightDictionary;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Message" /> class.
        /// </summary>
        public Message()
        {
            this.Attachments = new List<Attachment>();
            this.Replies = new ObservableCollection<Message>();
            this.LastModifiedTime = null;
            this.LastVerbExecutedIndicatesReply = null;
            this.LastVerbExecutedTime = null;
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        ///     Gets or sets the attachments.
        /// </summary>
        public List<Attachment> Attachments { get; set; }

        /// <summary>
        ///     Gets or sets the categories
        /// </summary>
        public IList<string> Categories { get; set; }

        /// <summary>
        ///     Gets the cc position.
        /// </summary>
        public int CcPosition
        {
            get
            {
                if (this.CopiedTo == null)
                {
                    return -1;
                }

                return this.CopiedTo.IndexOf(this.MailboxOwner);
            }
        }

        /// <summary>
        ///     Gets or sets the conversation this message belongs to.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [XmlIgnore]
        public Conversation Conversation
        {
            get
            {
                return this.conversation;
            }

            set
            {
                if (ReferenceEquals(value, this.conversation))
                {
                    return;
                }

                this.conversation = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged("ConversationDepth");
                this.OnPropertyChanged("ConversationStartedByMe");
                this.OnPropertyChanged("ForwardType");
                this.OnPropertyChanged("IsInConversationWithMeeting");
                this.OnPropertyChanged("LengthExceptStarter");
                this.OnPropertyChanged("MailboxOwner");
                this.OnPropertyChanged("PreviousReplies");
                this.OnPropertyChanged("PreviousUnread");
                this.OnPropertyChanged("ProportionsAndSize");
                this.OnPropertyChanged("ProportionsAndSizeNew");
                this.OnPropertyChanged("ReplyToMessageFromMe");
                this.OnPropertyChanged("SwitchedToToLine");
            }
        }

        /// <summary>
        ///     Gets the conversation depth.
        /// </summary>
        public int ConversationDepth
        {
            get
            {
                if (this.conversationDepth != int.MinValue)
                {
                    return this.conversationDepth;
                }

                if (this.Conversation != null)
                {
                    this.Conversation.CalculateMessageDepths();
                }

                return this.conversationDepth;
            }

            internal set
            {
                this.conversationDepth = value;
            }
        }

        /// <summary>
        ///     Gets or sets the conversation id.
        /// </summary>
        [Browsable(false)]
        public string ConversationId { get; set; }

        /// <summary>
        ///     Gets or sets the conversation index.
        /// </summary>
        [Browsable(false)]
        public byte[] ConversationIndex { get; set; }

        /// <summary>
        ///     Gets the conversation started by me.
        /// </summary>
        public ConversationStartedByMe ConversationStartedByMe
        {
            get
            {
                if (this.Conversation == null || this.MailboxOwner == null)
                {
                    return ConversationStartedByMe.ThisIsFirstMessage;
                }

                IList<Message> messages = this.Conversation.Messages;
                int k = messages.IndexOf(this);
                if (k == 0)
                {
                    return ConversationStartedByMe.ThisIsFirstMessage;
                }

                ContactDetails conversationStarter = messages[0].Sender;
                return this.MailboxOwner.Equals(conversationStarter)
                           ? ConversationStartedByMe.Yes
                           : ConversationStartedByMe.No;
            }
        }

        /// <summary>
        ///     Gets or sets the conversation topic
        /// </summary>
        public string ConversationTopic { get; set; }

        /// <summary>
        ///     Gets or sets the copied to.
        /// </summary>
        public List<ContactDetails> CopiedTo
        {
            get
            {
                return this.copiedTo;
            }

            set
            {
                if (ReferenceEquals(value, this.copiedTo))
                {
                    return;
                }

                this.copiedTo = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged("CcPosition");
                this.OnPropertyChanged("FromMe");
                this.OnPropertyChanged("ParticipatingContacts");
                this.OnPropertyChanged("ReceivedAs");
                this.OnPropertyChanged("Recipients");
                this.OnPropertyChanged("SentToOnlyMe");
            }
        }

        /// <summary>
        ///     Gets or sets the date received.
        /// </summary>
        public DateTime DateReceived { get; set; }

        /// <summary>
        ///     Gets or sets the date sent.
        /// </summary>
        public DateTime DateSent { get; set; }

        /// <summary>
        ///     Gets or sets the flag.
        /// </summary>
        public FlagState Flag { get; set; }

        /// <summary>
        ///     Gets or sets the folder.
        /// </summary>
        public MessageFolder Folder { get; set; }

        /// <summary>
        ///     Gets the forward type.
        /// </summary>
        public ForwardType ForwardType
        {
            get
            {
                if (this.IsForward)
                {
                    return ForwardType.Yes;
                }

                if (this.Conversation == null)
                {
                    return ForwardType.No;
                }

                return this.Conversation.Messages[0].IsForward
                           ? ForwardType.NoButConversationWasStartedByForward
                           : ForwardType.No;
            }
        }

        /// <summary>
        ///     Gets or sets the from.
        /// </summary>
        public ContactDetails From
        {
            get
            {
                return this.from;
            }

            set
            {
                if (ReferenceEquals(value, this.from))
                {
                    return;
                }

                this.from = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Gets the from me.
        /// </summary>
        public FromMe FromMe
        {
            get
            {
                if ((this.Sender == null) || (!this.Sender.Equals(this.MailboxOwner)))
                {
                    return FromMe.NotFromMe;
                }

                if ((this.SentTo.Count == 1) && this.SentTo[0].Equals(this.MailboxOwner))
                {
                    if (this.IsForward)
                    {
                        return FromMe.ForwardFromMeToOnlyMe;
                    }

                    return FromMe.FromMeToOnlyMe;
                }

                if (this.CopiedTo.Contains(this.MailboxOwner))
                {
                    return FromMe.FromMeCcMe;
                }

                if (this.IsForward)
                {
                    return FromMe.ForwardFromMeToMeAndOthers;
                }

                return FromMe.FromMeToMeAndOthers;
            }
        }

        /// <summary>
        /// Gets the integer value from report
        ///     Returns -2 if reports not known
        ///     Returns -1 if reports are known but sender is not a report
        ///     Returns 0 for report
        /// </summary>
        public int FromReport
        {
            get
            {
                if (this.MailboxOwner == null)
                {
                    return -2;
                }

                Person m = this.MailboxOwner.Person.Manager;
                if (m == null)
                {
                    return -2;
                }

                IList<Person> mh = this.MailboxOwner.Person.Reports;
                List<ContactDetails> mhc = mh.Select(p => p.BestIdentity).ToList();
                return mhc.IndexOf(this.Sender) == -1 ? -1 : 0;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether has attachments.
        /// </summary>
        public bool HasAttachments
        {
            get
            {
                return this.Attachments != null && this.Attachments.Count > 0;
            }
        }

        /// <summary>
        ///     Gets or sets the in reply to id.
        /// </summary>
        [Browsable(false)]
        public string InReplyToId { get; set; }

        /// <summary>
        ///     Gets the inline attachments.
        /// </summary>
        public List<Attachment> InlineAttachments
        {
            get
            {
                return this.Attachments.Where(a => (a.IsInline == true)).ToList();
            }
        }

        /// <summary>
        ///     Gets or sets the internet message id.
        /// </summary>
        [Browsable(false)]
        public string InternetMessageId { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether is categorized.
        /// </summary>
        public bool IsCategorised { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether is deleted.
        /// </summary>
        public bool IsDeleted { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether is flagged by sender.
        /// </summary>
        public bool IsFlaggedBySender { get; set; }

        /// <summary>
        ///     Gets a value indicating whether is forward.
        /// </summary>
        public bool IsForward
        {
            get
            {
                // note: to be localized
                if (string.IsNullOrEmpty(this.Subject))
                {
                    return false;
                }

                string s = this.Subject.ToLowerInvariant().Trim();
                return s.StartsWith("fw:") || s.StartsWith("fwd:");
            }
        }

        /// <summary>
        ///     Gets a value indicating whether is in conversation with meeting.
        /// </summary>
        public bool IsInConversationWithMeeting
        {
            get
            {
                if (this.Conversation == null)
                {
                    return false;
                }

                IList<Message> msgs = this.Conversation.Messages;
                int k = msgs.IndexOf(this);
                for (int i = 0; i < k; i++)
                {
                    if ("IPM.Schedule.Meeting.Request".Equals(msgs[i].ItemClass))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is marked important by sender.
        /// </summary>
        public bool IsMarkedImportantBySender { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether is marked unimportant by sender.
        /// </summary>
        public bool IsMarkedUnimportantBySender { get; set; }

        /// <summary>
        ///     Gets a value indicating whether is potential auto sender.
        /// </summary>
        public bool IsPotentialAutoSender
        {
            get
            {
                if (this.Sender == null || this.Sender.Email.Value == null)
                {
                    return false;
                }

                string email = this.Sender.Email.Value;
                int k = email.IndexOf("@", StringComparison.Ordinal);
                if (k != -1)
                {
                    if (ContainsAutoSenderIndicator(email.Substring(0, k)))
                    {
                        return true;
                    }
                }

                string name = this.Sender.Name.Value;
                return ContainsAutoSenderIndicator(name);
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is read.
        /// </summary>
        public bool IsRead { get; set; }

        /// <summary>
        ///     Gets a value indicating whether this message was replied to by the user
        /// </summary>
        public bool IsRepliedTo
        {
            get
            {
                if (this.UseRepliesProperty)
                {
                    if (this.Replies.Any(msg => msg.Sender.IsMe))
                    {
                        return true;
                    }

                    return this.LastVerbExecutedIndicatesReply != null && this.LastVerbExecutedIndicatesReply.Value;
                }

                return this.FindReply() != null;
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether is a resend
        /// </summary>
        public bool? IsResend { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether is a response requested
        /// </summary>
        public bool? IsResponseRequested { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether has this been submitted for sending
        /// </summary>
        public bool? IsSubmitted { get; set; }

        /// <summary>
        ///     Gets or sets the item class - there is a string-based hierarchy of different item classes
        ///     <see href="http://msdn.microsoft.com/en-us/library/aa580993(v=EXCHG.140).aspx"/>
        ///     <seealso href="http://msdn.microsoft.com/en-us/library/ee217908.aspx"/>
        /// </summary>
        public string ItemClass { get; set; }

        /// <summary>
        ///     Gets or sets the name of person who last modified this message
        /// </summary>
        public string LastModifiedName { get; set; }

        /// <summary>
        ///     Gets or sets the time of last modification (includes delete and move)
        /// </summary>
        public DateTime? LastModifiedTime { get; set; }

        /// <summary>
        ///     Gets or sets the last verb executed.
        /// </summary>
        public int? LastVerbExecuted { get; set; }

        /// <summary>
        ///     Gets or sets the last verb executed indicates reply.
        /// </summary>
        public bool? LastVerbExecutedIndicatesReply { get; set; }

        /// <summary>
        ///     Gets or sets the last verb executed time.
        /// </summary>
        public DateTime? LastVerbExecutedTime { get; set; }

        /// <summary>
        ///     Gets the length except starter.
        /// </summary>
        public int LengthExceptStarter
        {
            get
            {
                if (this.Conversation == null || this.MailboxOwner == null)
                {
                    return 0;
                }

                IList<Message> messages = this.Conversation.Messages;
                int k = messages.IndexOf(this);
                int count = k;
                if ((count > 0) && this.MailboxOwner.Equals(messages[0].Sender))
                {
                    count--;
                }

                return count;
            }
        }

        /// <summary>
        ///     Gets the mailbox owner.
        /// </summary>
        public ContactDetails MailboxOwner
        {
            get
            {
                return this.Conversation == null ? null : this.Conversation.MailboxOwner;
            }
        }

        /// <summary>
        ///     Gets the message attachments.
        /// </summary>
        public List<Attachment> MessageAttachments
        {
            get
            {
                return this.Attachments.Where(a => (a.IsFile == false) && (a.IsInline == false)).ToList();
            }
        }

        /// <summary>
        ///     Gets or sets the message body.
        /// </summary>
        [Browsable(false)]
        public string MessageBody
        {
            get
            {
                return this.messageBody ?? this.Summary;
            }

            set
            {
                this.messageBody = value;
            }
        }

        /// <summary>
        ///     Gets the messages to sender.
        /// </summary>
        public List<Message> MessagesToSender
        {
            get
            {
                // Exclude the case where mailbox owner is the sender since this has its own feature
                if ((this.Sender == null) || (this.MailboxOwner == null) || this.Sender.Equals(this.MailboxOwner))
                {
                    return new List<Message>();
                }

                // Cache messages involving the sender
                if (this.Sender.Person == null)
                {
                    this.Sender.Person = new Person();
                }

                if (this.Sender.Person.MessagesFromUser == null)
                {
                    this.Sender.Person.MessagesFromUser =
                        this.MailboxOwner.Person.Messages.Where(
                            m => m.SentTo.Contains(this.Sender) && m.Sender.Equals(this.MailboxOwner)).ToList();
                }

                return this.Sender.Person.MessagesFromUser.Where(m => m.DateSent < this.DateReceived).ToList();
            }
        }

        /// <summary>
        ///     Gets or sets the text that is new to this message in a conversation.
        ///     (currently implemented in a hacky way)
        /// </summary>
        [Browsable(false)]
        public string NewText
        {
            get
            {
                if (this.newText != null)
                {
                    return this.newText;
                }

                if (string.IsNullOrEmpty(this.Summary))
                {
                    return string.Empty;
                }

                int k = this.Summary.IndexOf("From:", 1, StringComparison.Ordinal);
                
                this.newText = k == -1 ? this.Summary.Trim() : this.Summary.Substring(0, k).Trim();
                
                return this.newText;
            }

            set
            {
                this.newText = value;
            }
        }

        /// <summary>
        /// Gets the new text length.
        /// </summary>
        public int NewTextLength
        {
            get
            {
                return this.NewTextWords.Count;
            }
        }

        /// <summary>
        ///     Gets the participants.
        /// </summary>
        public IList<Person> Participants
        {
            get
            {
                return this.participants
                       ?? (this.participants = this.ParticipatingContacts.Select(x => x.Person).Distinct().ToList());
            }
        }

        /// <summary>
        /// Gets the participating contacts.
        /// </summary>
        public IList<ContactDetails> ParticipatingContacts
        {
            get
            {
                if (this.SentTo == null)
                {
                    return new List<ContactDetails>();
                }

                var contacts = new List<ContactDetails>();

                if (this.SentTo != null)
                {
                    contacts.AddRange(this.SentTo);
                }

                if (this.CopiedTo != null)
                {
                    contacts.AddRange(this.CopiedTo);
                }

                if (this.Sender != null)
                {
                    contacts.Add(this.Sender);
                }

                return contacts.Distinct().ToList();
            }
        }

        /// <summary>
        ///     Gets the previous replies.
        /// </summary>
        public int PreviousReplies
        {
            get
            {
                if (this.Conversation == null || this.MailboxOwner == null)
                {
                    return 0;
                }

                IList<Message> messages = this.Conversation.Messages;
                int k = messages.IndexOf(this);
                int numReplies = 0;

                // ignoring first message, even if from me
                for (int i = 1; i < k; i++)
                {
                    if (this.MailboxOwner.Equals(messages[i].Sender))
                    {
                        numReplies++;
                    }
                }

                return numReplies;
            }
        }

        /// <summary>
        ///     Gets the previous unread.
        /// </summary>
        public int PreviousUnread
        {
            get
            {
                if (this.Conversation == null)
                {
                    return 0;
                }

                IList<Message> messages = this.Conversation.Messages;
                int k = messages.IndexOf(this);
                return messages.Take(k).Count(m => !m.IsRead);
            }
        }

        /// <summary>
        ///     Gets the 'primary' action taken on the message.  This is whichever of the following occurs with the first being reported if more
        ///     than one occurs: Flag, Reply, Delete, Move, Read or None.
        /// </summary>
        public PrimaryAction PrimaryAction
        {
            get
            {
                if ((this.Flag != FlagState.NotFlagged) && (!this.IsFlaggedBySender))
                {
                    return PrimaryAction.Flag;
                }

                if (this.IsRepliedTo)
                {
                    return PrimaryAction.Reply;
                }

                if (this.IsDeleted)
                {
                    return PrimaryAction.Delete;
                }

                if (this.Folder != null && this.Folder.IsUserFolder)
                {
                    return PrimaryAction.Move;
                }

                if (this.IsRead)
                {
                    return PrimaryAction.Read;
                }

                return PrimaryAction.NotRead;
            }
        }

        /// <summary>
        ///     Gets the proportions and size.
        /// </summary>
        public string ProportionsAndSize
        {
            get
            {
                if (this.Conversation == null || this.MailboxOwner == null)
                {
                    return string.Empty;
                }

                IList<Message> messages = this.Conversation.Messages;
                int k = messages.IndexOf(this);
                int numContrib = 0;
                for (int i = 0; i < k; i++)
                {
                    if (this.MailboxOwner.Equals(messages[i].Sender))
                    {
                        numContrib++;
                    }
                }

                int frac = (int)Math.Round(10.0 * numContrib / (k + 1E-10));

                return string.Format("{0:00}0% of {1:00}", frac, k);
            }
        }

        /// <summary>
        ///     Gets the proportions and size new.
        /// </summary>
        public string ProportionsAndSizeNew
        {
            get
            {
                if (this.Conversation == null)
                {
                    return string.Empty;
                }

                int k = this.LengthExceptStarter;
                int numContrib = this.PreviousReplies;
                int frac = (int)Math.Round(10.0 * numContrib / (k + 1E-10));

                return string.Format("{0:00}0% of {1:00}", frac, k);
            }
        }

        /// <summary>
        ///     Gets the contact details that the user received this mail as e.g. the email address or
        ///     distribution list.  If this cannot be determined, the sender contact details are returned.
        /// </summary>
        public ContactDetails ReceivedAs
        {
            get
            {
                if (this.SentTo != null)
                {
                    foreach (ContactDetails cd in this.SentTo.Where(cd => cd.IsMe))
                    {
                        return cd;
                    }
                }

                if (this.CopiedTo != null)
                {
                    foreach (ContactDetails cd in this.CopiedTo.Where(cd => cd.IsMe))
                    {
                        return cd;
                    }
                }

                if (this.SentTo != null && this.SentTo.Count == 1)
                {
                    return this.SentTo[0];
                }

                if ((this.SentTo == null || this.SentTo.Count == 0)
                    && (this.CopiedTo != null && this.CopiedTo.Count == 1))
                {
                    return this.CopiedTo[0];
                }

                return this.Sender;
            }
        }

        /// <summary>
        ///     Gets or sets the received by
        /// </summary>
        public ContactDetails ReceivedBy
        {
            get
            {
                return this.receivedBy;
            }

            set
            {
                if (ReferenceEquals(value, this.receivedBy))
                {
                    return;
                }

                this.receivedBy = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Gets or sets the received representing
        /// </summary>
        public ContactDetails ReceivedRepresenting
        {
            get
            {
                return this.receivedRepresenting;
            }

            set
            {
                if (ReferenceEquals(value, this.receivedRepresenting))
                {
                    return;
                }
                
                this.receivedRepresenting = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        ///     Gets the recipients.
        /// </summary>
        public IList<Person> Recipients
        {
            get
            {
                if (this.SentTo == null)
                {
                    return null;
                }

                IEnumerable<ContactDetails> lst = this.SentTo;
                if (this.CopiedTo != null)
                {
                    lst = lst.Concat(this.CopiedTo);
                }

                return lst.Select(cd => cd.Person).Distinct().ToList();
            }
        }

        /// <summary>
        ///     Gets or sets the replied to message.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Message RepliedToMessage
        {
            get
            {
                return this.repliedToMessage;
            }

            set
            {
                if (ReferenceEquals(value, this.repliedToMessage))
                {
                    return;
                }

                this.repliedToMessage = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged("ReplyToMessageFromMe");
            }
        }

        /// <summary>
        ///     Gets or sets the replies.
        /// </summary>
        public IList<Message> Replies
        {
            get
            {
                // return replies;
                if (this.UseRepliesProperty)
                {
                    return this.replies;
                }

                ObservableCollection<Message> ret = new ObservableCollection<Message>();
                Message reply = this.FindReply();
                if (reply != null)
                {
                    ret.Add(reply);
                }

                return ret;
            }

            set
            {
                this.replies = value;
                foreach (Message msg in this.replies)
                {
                    msg.RepliedToMessage = this;
                }
            }
        }

        /// <summary>
        ///     Gets the reply to message from me.
        /// </summary>
        public ReplyToMessageFromMe ReplyToMessageFromMe
        {
            get
            {
                if (this.Conversation == null || this.MailboxOwner == null)
                {
                    return ReplyToMessageFromMe.DontKnow;
                }

                IList<Message> messages = this.Conversation.Messages;
                int k = messages.IndexOf(this);
                if (k == 0)
                {
                    return ReplyToMessageFromMe.ThisIsFirstMessage;
                }

                Message msg = this.RepliedToMessage;
                if (msg == null)
                {
                    return ReplyToMessageFromMe.DontKnow;
                }

                ContactDetails messageSender = msg.Sender;
                return this.MailboxOwner.Equals(messageSender) ? ReplyToMessageFromMe.Yes : ReplyToMessageFromMe.No;
            }
        }

        /// <summary>
        ///     Gets or sets the sender.
        /// </summary>
        public ContactDetails Sender
        {
            get
            {
                return this.sender;
            }

            set
            {
                if (ReferenceEquals(value, this.sender))
                {
                    return;
                }

                this.sender = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged("ConversationStartedByMe");
                this.OnPropertyChanged("FromMe");
                this.OnPropertyChanged("FromReport");
                this.OnPropertyChanged("IsPotentialAutoSender");
                this.OnPropertyChanged("IsRepliedTo");
                this.OnPropertyChanged("LengthExceptStarter");
                this.OnPropertyChanged("MessagesToSender");
                this.OnPropertyChanged("ParticipatingContacts");
                this.OnPropertyChanged("PreviousReplies");
                this.OnPropertyChanged("ProportionsAndSize");
                this.OnPropertyChanged("ReceivedAs");
                this.OnPropertyChanged("ReplyToMessageFromMe");
                this.OnPropertyChanged("SenderDomain");
                this.OnPropertyChanged("SwitchedToToLine");
            }
        }

        /// <summary>
        ///     Gets the sender domain.
        /// </summary>
        public string SenderDomain
        {
            get
            {
                if (this.Sender == null || this.Sender.Email.Value == null)
                {
                    return string.Empty;
                }

                string s = this.Sender.Email.Value;
                int k = s.IndexOf("@", StringComparison.Ordinal);
                return k == -1 ? null : s.Substring(k + 1);
            }
        }

        /// <summary>
        ///     Gets or sets the sent to.
        /// </summary>
        public List<ContactDetails> SentTo
        {
            get
            {
                return this.sentTo;
            }

            set
            {
                if (ReferenceEquals(value, this.sentTo))
                {
                    return;
                }

                this.sentTo = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged("FromMe");
                this.OnPropertyChanged("MessagesToSender");
                this.OnPropertyChanged("ParticipatingContacts");
                this.OnPropertyChanged("ReceivedAs");
                this.OnPropertyChanged("Recipients");
                this.OnPropertyChanged("SentToOnlyMe");
                this.OnPropertyChanged("SwitchedToToLine");
            }
        }

        /// <summary>
        ///     Gets the sent to only me.
        /// </summary>
        public ToMe SentToOnlyMe
        {
            get
            {
                if (this.SentTo == null)
                {
                    return ToMe.Other;
                }

                if ((this.SentTo.Count != 1) || (!this.SentTo[0].Equals(this.MailboxOwner)))
                {
                    return ToMe.Other;
                }

                return this.CopiedTo.Count != 0 ? ToMe.ToOnlyMeWithCc : ToMe.ToOnlyMe;
            }
        }

        /// <summary>
        ///     Gets or sets the subject.
        /// </summary>
        public string Subject
        {
            get
            {
                return this.subject;
            }

            set
            {
                if (value == this.subject)
                {
                    return;
                }

                this.subject = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged("IsForward");
                this.OnPropertyChanged("SubjectPrefix");
                this.OnPropertyChanged("SubjectWithoutPrefix");
            }
        }

        /// <summary>
        ///     Gets the subject prefix.
        /// </summary>
        public string SubjectPrefix
        {
            get
            {
                if (string.IsNullOrEmpty(this.Subject))
                {
                    return null;
                }

                string s = this.Subject.Trim().ToLowerInvariant();
                int k = s.IndexOf(":", StringComparison.Ordinal);

                if ((k < 0) || (k > 5))
                {
                    return null;
                }

                return s.Substring(0, k).Trim();
            }
        }

        /// <summary>
        ///     Gets the subject without prefix.
        /// </summary>
        public string SubjectWithoutPrefix
        {
            get
            {
                if (string.IsNullOrEmpty(this.Subject))
                {
                    return string.Empty;
                }

                string s = this.Subject.Trim();
                while (true)
                {
                    int k = s.IndexOf(":", StringComparison.Ordinal);
                    if ((k < 0) || (k > 5))
                    {
                        return s;
                    }

                    s = s.Substring(k + 1).Trim();
                }
            }
        }

        /// <summary>
        ///     Gets the subject words.
        /// </summary>
        public List<string> SubjectWords
        {
            get
            {
                if (this.subjectWords != null)
                {
                    return this.subjectWords;
                }

                string s = this.SubjectWithoutPrefix;
                if (string.IsNullOrEmpty(s))
                {
                    this.subjectWords = new List<string>();
                }
                else
                {
                    s = s.Trim().ToLowerInvariant();
                    string[] words = WordSequence.ParseIntoWordStrings(s); // make subject words
                    this.subjectWords = words.ToList();
                }

                return this.subjectWords;
            }
        }

        /// <summary>
        ///     Gets or sets the summary.
        /// </summary>
        [Browsable(false)]
        public string Summary
        {
            get
            {
                return this.summary;
            }

            set
            {
                if (value == this.summary)
                {
                    return;
                }

                this.summary = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged("MessageBody");
                this.OnPropertyChanged("NewText");
            }
        }

        /// <summary>
        ///     Gets a value indicating whether switched to to line.
        /// </summary>
        public bool SwitchedToToLine
        {
            get
            {
                if (this.SentTo == null || this.Conversation == null || this.MailboxOwner == null)
                {
                    return false;
                }

                if (!this.SentTo.Contains(this.MailboxOwner))
                {
                    return false;
                }

                int k = this.Conversation.Messages.IndexOf(this);
                if (k == 0)
                {
                    return false;
                }

                // look backwards through the conversation
                for (int i = k - 1; i >= 0; i--)
                {
                    Message msg = this.Conversation.Messages[i];

                    // ignore messages sent by me
                    if (msg.Sender.Equals(this.MailboxOwner))
                    {
                        continue;
                    }

                    return !msg.SentTo.Contains(this.MailboxOwner);
                }

                return false;
            }
        }

        /// <summary>
        ///     Gets or sets the time of first reply. If time of first reply is not found, may return
        ///     time of last reply. If this also fails, returns DateTime.Now.
        /// </summary>
        public DateTime TimeOfReply
        {
            get
            {
                Message reply = this.FindReply(); // This will give the first reply
                if (reply != null)
                {
                    return reply.DateSent;
                }

                return this.timeOfReply != null ? this.timeOfReply.Value : DateTime.Now;
            }

            set
            {
                this.timeOfReply = value;
            }
        }

        /// <summary>
        /// Gets the new text individual words.
        /// </summary>
        [XmlIgnore]
        public List<string> NewTextWords
        {
            get
            {
                return this.newTextWords
                       ?? (this.newTextWords =
                           string.IsNullOrEmpty(this.NewText)
                               ? new List<string>()
                               : WordSequence.ParseIntoWordStrings(this.NewText.Trim().ToLowerInvariant()).ToList());
            }
        }
        
        /// <summary>
        ///     Gets or sets a value indicating whether use replies property.
        /// </summary>
        public bool UseRepliesProperty { get; set; }

        /// <summary>
        ///     Gets a value indicating whether this message was moved to a folder by a rule.
        /// </summary>
        public bool WasMovedByRule
        {
            get { return this.Folder != null && this.Folder.FolderIsMoveTarget; }
        }

        /// <summary>
        /// Gets the posterior probability of reply dictionary.
        /// </summary>
        public Dictionary<FeatureSetType, double> ProbabilityOfReplyDictionary
        {
            get
            {
                return this.FeatureSetTypes == null ? null : this.probabilityOfReplyDictionary
                       ?? (this.probabilityOfReplyDictionary = this.FeatureSetTypes.ToDictionary(ia => ia, ia => double.NaN));
            }
        }

        /// <summary>
        /// Gets the posterior probability of reply.
        /// </summary>
        public double ProbabilityOfReply
        {
            get
            {
                return this.ProbabilityOfReplyDictionary == null ? double.NaN : this.ProbabilityOfReplyDictionary[this.CurrentFeatureSetType];
            }
        }

        /// <summary>
        /// Gets a value indicating whether sent to and cc me.
        /// </summary>
        public bool SentToAndCcMe
        {
            get
            {
                return this.SentTo.Any(ia => ia.IsMe) && this.CopiedTo.Any(ia => ia.IsMe);
            }
        }

        /// <summary>
        /// Gets or sets the current feature set type.
        /// </summary>
        [XmlIgnore]
        public FeatureSetType CurrentFeatureSetType
        {
            get
            {
                return this.currentFeatureSetType;
            }

            set
            {
                if (value == this.currentFeatureSetType)
                {
                    return;
                }

                this.currentFeatureSetType = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged("ProbabilityOfReply");
                this.OnPropertyChanged("FeatureValuesAndWeights");
            }
        }

        /// <summary>
        /// Gets the feature value and weight dictionary.
        /// </summary>
        public Dictionary<FeatureSetType, Dictionary<FeatureBucket, Pair<double, Gaussian>>> FeatureValueAndWeightDictionary
        {
            get
            {
                return this.FeatureSetTypes == null ? null : this.featureValueAndWeightDictionary
                       ?? (this.featureValueAndWeightDictionary =
                           this.FeatureSetTypes.ToDictionary(ia => ia, ia => new Dictionary<FeatureBucket, Pair<double, Gaussian>>()));
            }
        }

        /// <summary>
        /// Gets the feature values and weights.
        /// </summary>
        [XmlIgnore]
        public Dictionary<FeatureBucket, Pair<double, Gaussian>> FeatureValuesAndWeights
        {
            get
            {
                return this.FeatureValueAndWeightDictionary == null ? null : this.FeatureValueAndWeightDictionary[this.CurrentFeatureSetType];
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether is training.
        /// </summary>
        [XmlIgnore]
        public bool IsTraining { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether is validation.
        /// </summary>
        [XmlIgnore]
        public bool IsValidation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether is test.
        /// </summary>
        [XmlIgnore]
        public bool IsTest { get; set; }

        /// <summary>
        /// Gets the train val test.
        /// </summary>
        [XmlIgnore]
        public string TrainValTest
        {
            get
            {
                if (this.IsTraining)
                {
                    return "Dataset: Train";
                }

                if (this.IsValidation)
                {
                    return "Dataset: Validation";
                }

                if (this.IsTest)
                {
                    return "Dataset: Test";
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// Gets or sets the feature set types.
        /// </summary>
        [XmlIgnore]
        public IList<FeatureSetType> FeatureSetTypes { get; set; }

        /// <summary>
        /// The add reply.
        /// </summary>
        /// <param name="reply">
        /// The reply.
        /// </param>
        public void AddReply(Message reply)
        {
            reply.RepliedToMessage = this;

            if (!this.Replies.Contains(reply))
            {
                this.Replies.Add(reply);
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.Subject + "\tfrom: " + this.Sender + "\tto: " + string.Join("; ", this.Recipients);
        }

        /// <summary>
        ///     Looks for a reply to this message that is from the user.  Returns the first reply found if there are several.
        /// </summary>
        /// <returns>
        ///     The <see cref="Message" />.
        /// </returns>
        public Message FindReply()
        {
            if (this.UseRepliesProperty)
            {
                IEnumerable<Message> repliesByMe = this.Replies.Where(msg => msg.Sender.IsMe).ToArray();
                return repliesByMe.Any() ? repliesByMe.First() : null;
            }

            if (this.Conversation == null)
            {
                return null;
            }

            int k = this.Conversation.Messages.IndexOf(this);
            for (int i = k + 1; i < this.Conversation.Messages.Count; i++)
            {
                // for all the messages after this one
                Message m = this.Conversation.Messages[i];

                // Check no intermediate message is being replied to
                // Check that m isn't a reply to another message between k and m, if it is go to next message
                for (int j = i - 1; j > k; j--)
                {
                    if (!m.IsReplyTo(this.Conversation.Messages[j], true))
                    {
                        continue;
                    }

                    m = null;
                    break;
                }

                if ((m != null) && m.IsReplyTo(this, true))
                {
                    return m;
                }
            }

            return null;
        }

        /// <summary>
        /// Determines whether [is reply automatic] [the specified message].
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="messageIsbyMe">if set to <c>true</c> [message is by me].</param>
        /// <returns>The <see cref="bool"/>.</returns>
        public bool IsReplyTo(Message message, bool messageIsbyMe)
        {
            if (messageIsbyMe)
            {
                if (!this.Sender.IsMe)
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(this.InReplyToId))
            {
                return this.InReplyToId == message.InternetMessageId;
            }

            // This is a hack for providers which don't set InReplyTo (e.g. WDS)
            return this.SentTo.Any(cd => cd.Person.Name.Equals(message.Sender.Person.Name));
        }

        /// <summary>
        /// Gets the number of more recent messages in conversation.
        /// </summary>
        /// <param name="thatAreReplies">
        /// if set to <c>true</c> [that are replies].
        /// </param>
        /// <returns>
        /// The <see cref="int"/>
        /// </returns>
        internal int GetNumberOfMoreRecentMessagesInConversation(bool thatAreReplies)
        {
            int num = 0;
            int k = this.Conversation.Messages.IndexOf(this);
            Message reply = this.FindReply();

            // Has contributed so far
            for (int i = k + 1; i < this.Conversation.Messages.Count; i++)
            {
                Message msg = this.Conversation.Messages[i];
                
                if (ReferenceEquals(msg, reply))
                {
                    break;
                }

                if (msg.IsReplyTo(this, false) || (!thatAreReplies))
                {
                    num++;
                }
            }

            return num;
        }

        /// <summary>
        /// Updates the conversations.
        /// </summary>
        /// <param name="c">The conversation.</param>
        internal void UpdateConversations(Conversation c)
        {
            if (c == null)
            {
                return;
            }

            foreach (var contact in this.ParticipatingContacts)
            {
                contact.AddConversation(c);
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
        /// The contains auto sender indicator.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <returns>
        /// The <see cref="bool" />.
        /// </returns>
        private static bool ContainsAutoSenderIndicator(string s)
        {
            s = s.ToLowerInvariant();
            if (s.Contains("auto"))
            {
                return true;
            }

            s = s.Replace(" ", string.Empty);
            s = s.Replace("-", string.Empty);
            return s.Contains("noreply") || s.Contains("notreply");
        }
    }
}