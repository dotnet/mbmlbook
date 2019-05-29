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
    using System.Xml.Serialization;

    using UnclutteringYourInbox.Features;

    using Microsoft.ML.Probabilistic.Collections;
    using UnclutteringYourInbox.DataCleaning;

    /// <summary>
    ///     A user.
    /// </summary>
    [Serializable]
    public class User : Person
    {
        /// <summary>
        /// The bias.
        /// </summary>
        private const string Unknown = "<unknown>";
        
        /// <summary>
        /// The conversations.
        /// </summary>
        private readonly Dictionary<string, Conversation> conversations = new Dictionary<string, Conversation>();

        /// <summary>
        /// The active batches. By default just a single batch
        /// </summary>
        private int[] activeBatches = { 0 };

        /// <summary>
        /// The current feature set type.
        /// </summary>
        private FeatureSetType currentFeatureSetType;

        /// <summary>
        /// The feature set types.
        /// </summary>
        private IList<FeatureSetType> featureSetTypes;

        /// <summary>
        /// The anonymize.
        /// </summary>
        private Anonymize anonymize = Anonymize.DoNotAnonymize;

        /// <summary>
        /// The contacts.
        /// </summary>
        private ObservableCollection<Person> contacts;

        /// <summary>
        /// The name hash.
        /// </summary>
        private string nameHash;

        /// <summary>
        /// Initializes a new instance of the <see cref="User"/> class.
        /// </summary>
        public User()
        {
            this.ContactDetailsNameLookup = new Dictionary<string, ContactDetails>();
            this.ContactDetailsAddressLookup = new Dictionary<string, ContactDetails>();
            this.PersonAddressLookup = new Dictionary<string, Person>();
            this.PersonNameLookup = new Dictionary<string, Person>();
            this.Contacts = new ObservableCollection<Person>();
            this.NameMapping = new Dictionary<string, string>();
            this.FeatureCache = new Dictionary<Type, IFeature>();
            this.FeatureBucketCache = new Dictionary<FeatureSet.MessageFeaturePair, IList<FeatureBucketValuePair>>();
        }

        /// <summary>
        /// Gets or sets the feature set types. Used only for demonstration.
        /// </summary>
        [XmlIgnore]
        public IList<FeatureSetType> FeatureSetTypes
        {
            get
            {
                return this.featureSetTypes;
            }

            set
            {
                this.featureSetTypes = value;
                this.Messages.ForEach(ia => ia.FeatureSetTypes = value);
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
                if (!this.FeatureSetTypes.Contains(value))
                {
                    throw new ArgumentOutOfRangeException("value", @"not in FeatureSetTypes");
                }

                this.currentFeatureSetType = value;
                this.Messages.ForEach(ia => ia.CurrentFeatureSetType = value);
            }
        }

        /// <summary>
        /// Gets or sets the anonymize.
        /// </summary>
        [XmlIgnore]
        public Anonymize Anonymize
        {
            get
            {
                return this.anonymize;
            }

            set
            {
                this.anonymize = value;

                if (this.OriginalName == null)
                {
                    this.OriginalName = this.Name.ToString();
                }

                this.AnonymizeNames(value);
                this.OnPropertyChanged();
                this.OnPropertyChanged("Name");
                this.OnPropertyChanged("Conversations");
                this.OnPropertyChanged("Contacts");
                this.OnPropertyChanged("Messages");
                this.OnPropertyChanged("TrainMessages");
                this.OnPropertyChanged("TrainAndValidationMessages");
                this.OnPropertyChanged("ValidationMessages");
                this.OnPropertyChanged("TestMessages");
                this.OnPropertyChanged("TrainContacts");
                this.OnPropertyChanged("BestIdentity");
                this.OnPropertyChanged("Identities");
                this.OnPropertyChanged("TrainIdentities");
            }
        }

        /// <summary>
        /// Gets or sets the top-level list of contacts for this user.
        /// </summary>
        public ObservableCollection<Person> Contacts
        {
            get
            {
                return this.contacts;
            }

            set
            {
                if (object.ReferenceEquals(value, this.contacts))
                {
                    return;
                }

                this.contacts = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged("TrainContacts");
                this.OnPropertyChanged("TopSenderCounts");
                this.OnPropertyChanged("TopSenderFractions");
                this.OnPropertyChanged("TopSenders");
                this.OnPropertyChanged("TopSendersAndCc");
            }
        }

        /// <summary>
        /// Gets or sets the person name lookup.
        /// </summary>
        public Dictionary<string, Person> PersonNameLookup { get; set; }

        /// <summary>
        /// Gets or sets the person address lookup.
        /// </summary>
        public Dictionary<string, Person> PersonAddressLookup { get; set; }

        /// <summary>
        /// Gets or sets the contact details address lookup.
        /// </summary>
        public Dictionary<string, ContactDetails> ContactDetailsAddressLookup { get; set; }

        /// <summary>
        /// Gets or sets the contact details name lookup.
        /// </summary>
        public Dictionary<string, ContactDetails> ContactDetailsNameLookup { get; set; }

        /// <summary>
        ///     Gets the short name.
        /// </summary>
        public override Uncertain<string> ShortName
        {
            get
            {
                return "Me";
            }
        }

        /// <summary>
        /// Gets the name hash.
        /// </summary>
        public string NameHash
        {
            get
            {
                return this.nameHash ?? (this.nameHash = Anonymizer.CalculateMd5Hash(this.Name.ToString()));
            }
        }

        /// <summary>
        /// Gets or sets the name mapping. Only used for debugging - not serialized.
        /// </summary>
        [XmlIgnore]
        public Dictionary<string, string> NameMapping { get; set; }

        /// <summary>
        /// Gets or sets the train messages.
        /// </summary>
        [XmlIgnore]
        public IList<Message> TrainMessages { get; set; }

        /// <summary>
        /// Gets or sets the test messages.
        /// </summary>
        [XmlIgnore]
        public IList<Message> TestMessages { get; set; }

        /// <summary>
        /// Gets or sets the validation messages.
        /// </summary>
        [XmlIgnore]
        public IList<Message> ValidationMessages { get; set; }

        /// <summary>
        /// Gets the train and validation messages.
        /// </summary>
        public IList<Message> TrainAndValidationMessages
        {
            get
            {
                return this.TrainMessages == null ? null : this.TrainMessages.Concat(this.ValidationMessages).ToList();
            }
        }

        /// <summary>
        /// Gets all messages.
        /// </summary>
        public IList<Message> AllMessages
        {
            get
            {
                return this.TrainMessages == null
                           ? null
                           : this.TrainMessages.Concat(this.ValidationMessages)
                                 .Concat(this.TestMessages)
                                 .OrderByDescending(m => m.DateSent)
                                 .ToList();
            }
        }

        /// <summary>
        /// Gets the train identities.
        /// </summary>
        public IList<ContactDetails> TrainIdentities
        {
            get
            {
                return this.TrainMessages == null
                           ? null
                           : this.TrainMessages.Select(m => m.ReceivedAs).Where(ia => ia.IsMe).Distinct().ToList();
            }
        }

        /// <summary>
        /// Gets the train contacts.
        /// </summary>
        public IList<Person> TrainContacts
        {
            get
            {
                return this.TrainMessages == null ? null : GetContacts(this.TrainMessages).ToList();
            }
        }

        /// <summary>
        /// Gets or sets the active batches.
        /// </summary>
        [XmlIgnore]
        public int[] ActiveBatches
        {
            get { return this.activeBatches; }
            set { this.activeBatches = value; }
        }

        /// <summary>
        /// Gets the top senders.
        /// </summary>
        public IEnumerable<IGrouping<Person, Message>> TopSenders
        {
            get
            {
                return this.TrainAndValidationMessages == null
                           ? null
                           : this.TrainAndValidationMessages.GroupBy(ia => ia.Sender.Person)
                                 .OrderByDescending(ia => ia.Count())
                                 .Where(ia => !ia.Key.IsMe);
            }
        }

        /// <summary>
        /// Gets the top senders and cc.
        /// </summary>
        /// <value>
        /// The top senders and cc.
        /// </value>
        public IEnumerable<IGrouping<Person, Message>> TopSendersAndCc
        {
            get
            {
                return this.TrainAndValidationMessages == null
                           ? null
                           : this.TrainAndValidationMessages.GroupBy(ia => ia.Sender.Person)
                                 .OrderByDescending(ia => 
                                     ia.Count(m => m.SentTo.Contains(this.BestIdentity)) 
                                   * ia.Count(m => m.CopiedTo.Contains(this.BestIdentity)))
                                 .Where(ia => !ia.Key.IsMe && ia.Count(m => m.IsRepliedTo) > 0);
            }
        }

        /// <summary>
        /// Gets to senders and cc counts.
        /// </summary>
        /// <value>
        /// To senders and cc counts.
        /// </value>
        public IEnumerable<Tuple<Person, int, int, double, int, int>> ToSendersAndCcCounts
        {
            get
            {
                return this.TrainAndValidationMessages == null
                           ? null
                           : from grp in this.TopSendersAndCc.Take(10)
                             let pos = grp.Count(m => m.IsRepliedTo)
                             let neg = grp.Count(m => !m.IsRepliedTo)
                             let frac = (double)pos / (pos + neg)
                             let to = grp.Count(m => m.SentTo.Contains(this.BestIdentity))
                             let cc = grp.Count(m => m.CopiedTo.Contains(this.BestIdentity))
                             select new Tuple<Person, int, int, double, int, int>(grp.Key, pos, neg, frac, to, cc);
            }
        }

        /// <summary>
        /// Gets the top sender counts.
        /// </summary>
        /// <value>
        /// The top sender counts.
        /// </value>
        public IEnumerable<Tuple<Person, int, int, double>> TopSenderCounts
        {
            get
            {
                return this.TrainAndValidationMessages == null
                           ? null
                           : from grp in this.TopSenders.Take(20)
                             let pos = grp.Count(m => m.IsRepliedTo)
                             let neg = grp.Count(m => !m.IsRepliedTo)
                             let frac = (double)pos / (pos + neg)
                             select new Tuple<Person, int, int, double>(grp.Key, pos, neg, frac);
            }
        }

        /// <summary>
        /// Gets the top sender fractions.
        /// </summary>
        /// <value>
        /// The top sender fractions.
        /// </value>
        public Dictionary<string, double> TopSenderFractions
        {
            get
            {
                return this.TrainAndValidationMessages == null
                           ? null
                           : this.TopSenderCounts.ToDictionary(
                               ia => string.Format("{0} ({1})", ia.Item1.Name.ToString(), ia.Item2 + ia.Item3),
                               ia => ia.Item4);
            }
        }

        /// <summary>
        /// Gets or sets the original name. For debugging only
        /// </summary>
        [XmlIgnore]
        [Browsable(false)]
        public string OriginalName { get; set; }

        /// <summary>
        /// Gets or sets the computed feature buckets.
        /// </summary>
        [XmlIgnore]
        public Dictionary<FeatureSet.MessageFeaturePair, IList<FeatureBucketValuePair>> FeatureBucketCache { get; set; }

        /// <summary>
        /// Gets or sets the computed features.
        /// </summary>
        [XmlIgnore]
        public Dictionary<Type, IFeature> FeatureCache { get; set; }

        /// <summary>
        /// Gets unknown contact details.
        /// </summary>
        protected internal static ContactDetails UnknownContactDetails
        {
            get
            {
                return new ContactDetails { Name = Unknown, Email = Unknown };
            }
        }

        /// <summary>
        /// Gets a dummy person.
        /// </summary>
        protected internal static Person UnknownPerson
        {
            get
            {
                return new Person { Identities = new ObservableCollection<ContactDetails>(new[] { UnknownContactDetails }) };
            }
        }

        /// <summary>
        ///     Merges the contacts.
        /// </summary>
        public void MergeContacts()
        {
            var allContacts = new HashSet<ContactDetails>();
            foreach (ContactDetails cd in this.ContactDetailsAddressLookup.Values)
            {
                allContacts.Add(cd);
            }

            foreach (ContactDetails cd in this.ContactDetailsNameLookup.Values)
            {
                allContacts.Add(cd);
            }

            foreach (ContactDetails cd in allContacts.Where(cd => cd.IsMe))
            {
                this.RegisterIdentity(cd, this);
                this.AddIdentity(cd);
            }

            foreach (ContactDetails cd in allContacts)
            {
                if (cd.IsMe)
                {
                    continue;
                }

                Person p = this.GetOrCreatePerson(cd);
                if (p == this)
                {
                    cd.IsMe = true;
                }

                this.RegisterIdentity(cd, p);
                p.AddIdentity(cd);

                if (!this.Contacts.Contains(p) && (p != this))
                {
                    this.Contacts.Add(p);
                }
            }

            ContactDetails bestIdentity = this.BestIdentity;
            foreach (Conversation c in this.Conversations)
            {
                c.MailboxOwner = bestIdentity;
            }
        }

        /// <summary>
        /// Sets up the data.
        /// </summary>
        /// <param name="months">The months.</param>
        /// <param name="trainingSetSize">Size of the training set.</param>
        /// <param name="validationSetSize">Size of the validation set.</param>
        /// <param name="testSetSize">Size of the test set.</param>
        /// <param name="errors">The errors.</param>
        /// <param name="verbose">if set to <c>true</c> [verbose].</param>
        internal void SetUpData(int months, int trainingSetSize, int validationSetSize, int testSetSize, out IList<string> errors, bool verbose = false)
        {
            if (this.MostRecentMessageTime == DateTime.MinValue)
            {
                // Need to fix the conversations
                foreach (var contact in this.Contacts)
                {
                    this.CopyIdentityLists(contact.BestIdentity);
                }
            }

            // Assume that emails before this date that have not been replied to are not going to be
            // replied to i.e. can be treated as ground truth.
            DateTime cutoffDate = this.MostRecentMessageTime.AddDays(-20);

            // don't take recent messages, so that we have 'ground truth' as to whether they were replied to or not.
            Func<Message, bool> predicate;
            if (months > 0)
            {
                months = Math.Min(12000, months);
                DateTime oldestDate = DateTime.Now.AddMonths(-months);
                predicate =
                    x =>
                    (!(x.Folder.IsSentItems || x.Folder.IsConversationHistory || x.Folder.ReplyFraction < 0.01)
                        && x.DateSent < cutoffDate && x.DateSent > oldestDate);
            }
            else
            {
                predicate =
                    x =>
                    (!(x.Folder.IsSentItems || x.Folder.IsConversationHistory || x.Folder.ReplyFraction < 0.01)
                        && x.DateSent < cutoffDate);
            }

            int total = this.Messages.Count(predicate); 
            int desiredTotal = trainingSetSize + validationSetSize + testSetSize;

            if (total < desiredTotal)
            {
                if (total < trainingSetSize + validationSetSize)
                {
                    // Really small data set. Split according to proportions
                    var trainProportion = (double)trainingSetSize / desiredTotal;
                    var valProportion = (double)validationSetSize / desiredTotal;
                    trainingSetSize = (int)(total * trainProportion);
                    validationSetSize = (int)(total * valProportion);
                    testSetSize = total - trainingSetSize - validationSetSize;
                }
                else
                {
                    // Somewhat small data set. Reduce validation set to ensure there are enough test examples. Leave training set as is.
                    var valProportion = (double)validationSetSize / (validationSetSize + testSetSize);
                    validationSetSize = (int)((total - trainingSetSize) * valProportion);
                    testSetSize = total - trainingSetSize - validationSetSize;
                }
            }
            else
            {
                // We have more data than desired - use it all and split according to proportions
                trainingSetSize = (int)(total * (double)trainingSetSize / desiredTotal);
                validationSetSize = (int)(total * (double)validationSetSize / desiredTotal);
                testSetSize = total - trainingSetSize - validationSetSize;
            }

            this.TrainMessages = new List<Message>();
            this.ValidationMessages = new List<Message>();
            this.TestMessages = new List<Message>();

            Random random = new Random(100);

            // Order conversations by date
            // foreach (var messages in this.Conversations.OrderBy(ia => ia.Date).Select(c => c.Messages.Where(predicate)))
            // Random ordering
            foreach (var messages in this.Conversations.OrderBy(ia => random.Next()).Select(c => c.Messages.Where(predicate)))
            {
                if (this.TrainMessages.Count < trainingSetSize)
                {
                    this.TrainMessages.AddRange(messages);
                }
                else
                {
                    if (this.ValidationMessages.Count < validationSetSize)
                    {
                        this.ValidationMessages.AddRange(messages);
                    }
                    else
                    {
                        if (this.TestMessages.Count < testSetSize)
                        {
                            this.TestMessages.AddRange(messages);
                        }
                    }
                }
            }
            
            if (verbose)
            {
                Console.WriteLine(@"Total messages {0}", this.AllMessages.Count());
                Console.WriteLine(@"Train messages {0}", this.TrainMessages.Count());
                Console.WriteLine(@"Validation messages {0}", this.ValidationMessages.Count());
                Console.WriteLine(@"Test messages {0}", this.TestMessages.Count());
            }

            errors = new List<string>();
            if (!this.TrainMessages.Any())
            {
                errors.Add("No training messages found!");
            }

            if (!this.ValidationMessages.Any())
            {
                errors.Add("No validation messages found!");
            }

            if (!this.TestMessages.Any())
            {
                errors.Add("No test messages found!");
            }
        }

        /// <summary>
        /// Repairs the conversations.
        /// </summary>
        internal void RepairConversations()
        {
            foreach (var message in this.Messages)
            {
                var conversation = this.GetOrCreateConversation(message.ConversationId);
                conversation.AddMessage(message);

                foreach (var contact in message.ParticipatingContacts.Where(contact => !contact.Conversations.Contains(conversation)))
                {
                    if (!contact.Conversations.Contains(conversation))
                    {
                        contact.Conversations.Add(conversation);
                    }
                }

                conversation.SetVariables(message);
            }

            ContactDetails bestIdentity = this.BestIdentity;
            foreach (var conversation in this.Conversations)
            {
                conversation.MailboxOwner = bestIdentity;
            }
        }

        /// <summary>
        /// Repairs the message folders.
        /// </summary>
        internal void RepairMessageFolders()
        {
            // Slow & inefficient but no alternative?
            foreach (var message in this.Messages)
            {
                int index = this.MessageFolders.IndexOf(message.Folder);
                var folder = this.MessageFolders[index];
                if (!folder.Messages.Contains(message))
                {
                    folder.Messages.Add(message);
                }
            }
        }

        /// <summary>
        /// Get or create the conversation.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>The <see cref="Conversation"/></returns>
        internal Conversation GetOrCreateConversation(string id)
        {
            if (id == null)
            {
                return new Conversation { Id = Guid.NewGuid().ToString("N") };
            }

            if (!this.conversations.ContainsKey(id))
            {
                this.conversations[id] = new Conversation { Id = id };
            }

            return this.conversations[id];
        }

        /// <summary>
        /// Get contact details from name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>
        /// The <see cref="ContactDetails" />.
        /// </returns>
        internal ContactDetails GetContactDetails(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            string name2 = NormalizeName(name);
            if (!this.ContactDetailsNameLookup.ContainsKey(name2))
            {
                this.ContactDetailsNameLookup[name2] = new ContactDetails
                        {
                            Name = Uncertain<string>.FromProb(name, 0.2)
                        };
            }

            return this.ContactDetailsNameLookup[name2];
        }

        /// <summary>
        /// Get contact details from item address and name.
        /// </summary>
        /// <param name="conversation">The conversation.</param>
        /// <param name="name">The name.</param>
        /// <param name="address">The address.</param>
        /// <returns>
        /// The <see cref="ContactDetails" />.
        /// </returns>
        internal ContactDetails GetContactDetails(Conversation conversation, string name, string address)
        {
            if (address != null)
            {
                address = address.Trim('<', '>', '\'', '\"');
            }

            var contactDetails = this.GetContactDetails(name, address);

            if (string.IsNullOrEmpty(name))
            {
                if (!string.IsNullOrWhiteSpace(address))
                {
                    contactDetails.Name = Uncertain<string>.FromProb(address, 0.1);
                }
            }
            else
            {
                string s = name.Trim('<', '>', '\'', '\"');
                if (s.EndsWith("(" + address + ")"))
                {
                    if (address != null)
                    {
                        s = s.Substring(0, s.Length - (address.Length + 2));
                    }

                    s = s.TrimEnd();
                }

                int k = s.IndexOf('@');
                if (k != -1)
                {
                    if (contactDetails.Name.Probability <= 0.5)
                    {
                        // don't want to overwrite a name that is more certain with a name truncated from an email address
                        contactDetails.Name = Uncertain<string>.FromProb(s.Substring(0, k).Replace('.', ' '), 0.5);
                    }
                }
                else
                {
                    if (contactDetails.Name.Probability <= 1.0)
                    {
                        contactDetails.Name = Uncertain<string>.FromProb(s, 1.0); // a name that didn't have an @
                    }
                }
            }

            return contactDetails;
        }

        /// <summary>
        /// Get contact details from address and name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="address">The address.</param>
        /// <returns>
        /// The <see cref="ContactDetails" />.
        /// </returns>
        internal ContactDetails GetContactDetails(string name, string address)
        {
            ContactDetails res = null;
            if (address == null)
            {
                res = this.GetContactDetails(name);
            }

            if ((res != null) || string.IsNullOrWhiteSpace(address))
            {
                return res ?? this.GetContactDetails("unknown");
            }

            string key = address.ToLowerInvariant();
            if (!this.ContactDetailsAddressLookup.ContainsKey(key))
            {
                this.ContactDetailsAddressLookup[key] = new ContactDetails { Email = address, Name = Uncertain<string>.FromProb(name, 0.2) };

                if (!string.IsNullOrEmpty(name))
                {
                    this.ContactDetailsNameLookup[NormalizeName(name)] = this.ContactDetailsAddressLookup[key];
                }
            }

            res = this.ContactDetailsAddressLookup[key];

            return res ?? this.GetContactDetails("unknown");
        }

        /// <summary>
        /// Get the contact list.
        /// </summary>
        /// <param name="conversation">The conversation.</param>
        /// <param name="names">The names.</param>
        /// <param name="addresses">The addresses.</param>
        /// <returns>
        /// The contact list.
        /// </returns>
        internal List<ContactDetails> GetContactList(Conversation conversation, string[] names, IEnumerable<string> addresses)
        {
            var people = new List<ContactDetails>();
            if (addresses != null)
            {
                people.AddRange(
                    addresses.Select((t, i) => this.GetContactDetails(conversation, i < names.Length ? names[i] : null, t)));
            }

            return people;
        }

        /// <summary>
        /// Anonymize the names.
        /// </summary>
        /// <param name="anonymizeMethod">The anonymize.</param>
        /// <exception cref="System.NotImplementedException">Unknown anonymize method</exception>
        internal void AnonymizeNames(Anonymize anonymizeMethod)
        {
            this.NameMapping = new Dictionary<string, string>();
            Func<bool> initFunc = null;
            Func<ContactDetails, string> anonymizeFunc = null;

            switch (anonymizeMethod)
            {
                case Anonymize.AnonymizeByRandomNames:
                    var randomNames = new MBMLCommon.RandomNameGenerator().GetEnumerator();
                    initFunc = randomNames.MoveNext;
                    anonymizeFunc = _ => randomNames.Current;
                    break;
                case Anonymize.AnonymizeByCodes:
                    var generator = new HashCodeNameGenerator("User{0}");
                    anonymizeFunc = generator.GetValue;
                    break;
                case Anonymize.DoNotAnonymize:
                    break;
                default:
                    throw new NotSupportedException("Unknown anonymize method");
            }

            foreach (Person person in this.Contacts.Where(p => p != UnknownPerson))
            {
                AnonymizeIdentities(person.Identities, initFunc, anonymizeFunc, this.NameMapping, anonymizeMethod);
            }

            AnonymizeIdentities(this.Identities, initFunc, anonymizeFunc, this.NameMapping, anonymizeMethod);
        }
        
        /// <summary>
        /// Anonymize the identities.
        /// </summary>
        /// <param name="identities">The identities.</param>
        /// <param name="initAction">The initialize action.</param>
        /// <param name="anonymizeFunc">The anonymize function.</param>
        /// <param name="nameMapping">The name mapping.</param>
        /// <param name="anonymizeMethod">The anonymize method.</param>
        private static void AnonymizeIdentities(
            IList<ContactDetails> identities, 
            Func<bool> initAction,
            Func<ContactDetails, string> anonymizeFunc,
            IDictionary<string, string> nameMapping, 
            Anonymize anonymizeMethod)
        {
            if (anonymizeFunc == null)
            {
                identities.ForEach(ia => ia.Anonymize = Anonymize.DoNotAnonymize);
                return;
            }
            
            for (int i = 0; i < identities.Count; i++)
            {
                if (initAction != null)
                {
                    if (!initAction())
                    {
                        throw new InvalidOperationException("Initialization action failed");
                    }
                }
                
                ContactDetails identity = identities[i];
                if (identity == UnknownContactDetails)
                {
                    continue;
                }

                identity.Anonymize = Anonymize.DoNotAnonymize;

                string name = identity.Name.ToString();
                if (!nameMapping.ContainsKey(name))
                {
                    nameMapping[name] = anonymizeFunc(identity);
                }

                identity.AnonymizedName = new Uncertain<string> { Value = nameMapping[name], Probability = 1.0 };
                identity.AnonymizedEmail = new Uncertain<string>
                {
                    Value = nameMapping[name].ToLower().Replace(".", string.Empty).Replace(' ', '.') + i + "@example.com",
                    Probability = 1.0
                };

                identity.Anonymize = anonymizeMethod;
            }
        }

        /// <summary>
        /// Normalizes the name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The normalized name.</returns>
        private static string NormalizeName(string name)
        {
            string s = name.ToLowerInvariant();
            int k = s.IndexOf('(');
            if (k > 0)
            {
                s = s.Substring(0, k).TrimEnd();
            }

            return s.Trim();
        }

        /// <summary>
        /// Gets the contacts.
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <returns>The <see cref="IEnumerable{Person}"/></returns>
        private static IEnumerable<Person> GetContacts(IEnumerable<Message> messages)
        {
            return messages.SelectMany(m => new[] { m.Sender.Person, m.Conversation.From.Person }).Where(ia => !ia.IsMe).Distinct();
        }

        /// <summary>
        /// Registers the identity.
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <param name="person">
        /// The person.
        /// </param>
        private void RegisterIdentity(ContactDetails id, Person person)
        {
            if (id.Name.Value != null)
            {
                string nameKey = NormalizeName(id.Name.Value);
                this.PersonNameLookup[nameKey] = person;
            }

            if (id.Email.Value != null)
            {
                string addressKey = id.Email.Value.ToLowerInvariant();
                this.PersonAddressLookup[addressKey] = person;
            }
        }

        /// <summary>
        /// Gets the or create person.
        /// </summary>
        /// <param name="contactDetails">The contact details.</param>
        /// <returns>The <see cref="Person"/></returns>
        private Person GetOrCreatePerson(ContactDetails contactDetails)
        {
            Person p = this.GetPersonFromAddress(contactDetails.Email.Value)
                ?? this.GetPersonFromName(contactDetails.Name.Value)
                ?? new Person();

            this.RegisterIdentity(contactDetails, p);
            p.AddIdentity(contactDetails);
            return p;
        }

        /// <summary>
        /// Gets the name of the person from.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The <see cref="Person"/></returns>
        private Person GetPersonFromName(string name)
        {
            if (name == null)
            {
                return null;
            }

            string key = NormalizeName(name);
            if (!this.PersonNameLookup.ContainsKey(key))
            {
                this.PersonNameLookup[key] = new Person();
            }

            return this.PersonNameLookup[key];
        }

        /// <summary>
        /// Gets the or create person.
        /// </summary>
        /// <param name="address">The address.</param>
        /// <returns>The <see cref="Person"/></returns>
        private Person GetPersonFromAddress(string address)
        {
            if (address == null)
            {
                return null;
            }

            string key = address.ToLowerInvariant();
            return !this.PersonAddressLookup.ContainsKey(key) ? null : this.PersonAddressLookup[key];
        }
    }
}