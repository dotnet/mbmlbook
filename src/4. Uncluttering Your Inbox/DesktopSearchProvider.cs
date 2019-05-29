// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox
{
    #region usings

    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Data.OleDb;
    using System.Linq;


    using MBMLViews;

    #endregion

    /// <summary>
    ///     Provides data objects using Windows Desktop Search
    /// </summary>
    public class DesktopSearchProvider : IDisposable
    {
        #region Constants

        /// <summary>
        ///     The connection string
        /// </summary>
        private const string ConnectionString =
            "Provider=Search.CollatorDSO.1;Extended Properties=\"Application=Windows\"";

        #endregion

        #region Fields

        /// <summary>
        ///     The database connection
        /// </summary>
        private readonly OleDbConnection databaseConnection = new OleDbConnection(ConnectionString);
        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="DesktopSearchProvider" /> class.
        /// </summary>
        public DesktopSearchProvider()
        {
            this.databaseConnection.Open();
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets the user.
        /// </summary>
        /// <value>
        ///     The user.
        /// </value>
        public User User { private get; set; }

        /// <summary>
        /// Gets or sets the start time
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets the end time
        /// </summary>
        public DateTime EndTime { get; set; }
        
        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (this.databaseConnection.State == ConnectionState.Open)
            {
                this.databaseConnection.Close();
            }
        }

        /// <summary>
        ///     Populates the contacts.
        /// </summary>
        public void PopulateContacts()
        {
            string query =
                string.Format(
                    "SELECT System.Contact.FullName, System.Contact.EmailAddress, System.Contact.JobTitle,"
                    + "System.Contact.OfficeLocation" + " FROM SystemIndex WHERE System.Kind = 'contact'");
            this.AddContacts(this.ExecuteQuery(query));
        }

        /// <summary>
        ///     Populates the messages.
        /// </summary>
        public void PopulateMessages()
        {
            this.PopulateMessages(int.MaxValue);
        }

        #endregion

        #region Methods
        /// <summary>
        /// The load user data from desktop search.
        /// </summary>
        /// <param name="endTime">The end time.</param>
        /// <param name="months">The months.</param>
        /// <param name="errors">The errors.</param>
        /// <returns>The <see cref="User"/></returns>
        internal static User LoadUserData(DateTime endTime, int months, out IList<string> errors)
        {
            errors = new List<string>();

            // The object that holds all user data
            var user = new User();
            
            DateTime startTime = endTime.AddMonths(-months);

            Console.WriteLine(@"Start {0}, End {1}", startTime, endTime);

            // Populate user mailbox data from desktop search
            // Note: this does not retrieve email content, just summaries
            using (var dsp = new DesktopSearchProvider { User = user, StartTime = startTime, EndTime = endTime })
            {
                using (new CodeTimer("Populating contacts"))
                {
                    dsp.PopulateContacts();
                }

                using (new CodeTimer("Populating messages"))
                {
                    dsp.PopulateMessages();
                }

                Console.WriteLine(@"{0} contacts found", user.ContactDetailsAddressLookup.Count + user.ContactDetailsNameLookup.Count);
                Console.WriteLine(@"{0} conversations found", user.Conversations.Count());
                Console.WriteLine(@"{0} messages found", user.Messages.Count);

                // Heuristic to merge people with multiple email addresses
                using (new CodeTimer("Merging contacts"))
                {
                    user.MergeContacts();
                }

                Console.WriteLine(@"{0} contacts after merging", user.Contacts.Count);
                Console.WriteLine(@"{0} identities", user.Identities.Count);

                if (user.Conversations.Count == 0)
                {
                    errors.Add("Conversation count is 0");
                }

                return user;
            }
        }

        /// <summary>
        /// Reads the value.
        /// </summary>
        /// <typeparam name="T">The type of the value</typeparam>
        /// <param name="reader">The reader.</param>
        /// <param name="column">The column.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>
        /// The value
        /// </returns>
        private static T ReadValue<T>(IDataRecord reader, int column, T defaultValue)
        {
            if (reader.IsDBNull(column))
            {
                return defaultValue;
            }

            return (T)reader.GetValue(column);
        }

        /// <summary>
        /// Populates the messages.
        /// </summary>
        /// <param name="max">
        /// The max.
        /// </param>
        private void PopulateMessages(int max)
        {
            string query = string.Format(
                "SELECT" + " System.Message.ToName, System.Message.ToAddress," + // (0,1)
                " System.Message.ConversationID, System.Message.DateSent, System.Message.SenderName," + // (2, 3, 4)
                " System.Message.SenderAddress, System.Title, System.ItemUrl, System.Message.CcName, System.Message.CcAddress," + // (5, 6, 7, 8, 9)
                " System.Search.Autosummary, System.IsFlagged, System.IsFlaggedComplete," + // (10, 11, 12)
                " System.IsDeleted, System.IsRead, System.ItemFolderNameDisplay, System.Keywords, " + // (13, 14, 15, 16)
                " System.Message.HasAttachments, System.Message.AttachmentNames, System.Importance, System.Message.ToDoFlags," + // (17, 18, 19, 20)
                " System.DateModified, System.Message.DateReceived, System.ItemFolderPathDisplay" + // (21, 22, 23)
                " FROM SystemIndex WHERE System.Kind = 'email'" + 
                " AND NOT CONTAINS(System.ItemUrl, '\"Sync Issues\"')" + 
                " AND NOT CONTAINS(System.ItemUrl, '\"Junk E-mail\"')"  + 
                " AND System.Message.DateSent >= '{0:yyyy/MM/dd}'" + 
                " AND System.Message.DateSent <= '{1:yyyy/MM/dd}'",
                this.StartTime,
                this.EndTime);
            this.AddMessages(this.ExecuteQuery(query), max);
        }

        /// <summary>
        /// Adds the contacts.
        /// </summary>
        /// <param name="reader">
        /// The reader.
        /// </param>
        private void AddContacts(DbDataReader reader)
        {
            if ((reader == null) || (!reader.HasRows))
            {
                return;
            }

            while (reader.Read())
            {
                string fullname = reader.GetValue(0) as string;
                if (string.IsNullOrEmpty(fullname))
                {
                    continue;
                }

                ContactDetails p = this.User.GetContactDetails(fullname, reader.GetValue(1) as string);
                p.Name = fullname;
                string jobtitle = reader.GetValue(2) as string;
                if (!string.IsNullOrEmpty(jobtitle))
                {
                    p.JobTitle = jobtitle;
                }

                string officeLocation = reader.GetValue(3) as string;
                if (!string.IsNullOrEmpty(officeLocation))
                {
                    p.OfficeLocation = officeLocation;
                }
            }
        }

        /// <summary>
        /// Adds the messages.
        /// </summary>
        /// <param name="reader">
        /// The reader.
        /// </param>
        /// <param name="max">
        /// The max.
        /// </param>
        private void AddMessages(DbDataReader reader, int max)
        {
            if ((reader == null) || (!reader.HasRows))
            {
                return;
            }

            int count = 0;
            var folderDict = new Dictionary<string, MessageFolder>();
            try
            {
                while (reader.Read() && (count < max))
                {
                    try
                    {
                        /*
                        " System.Message.ToName, System.Message.ToAddress," + // (0,1)
                        " System.Message.ConversationID, System.Message.DateSent, System.Message.SenderName," + // (2, 3, 4)
                        " System.Message.SenderAddress, System.Title, System.ItemUrl, System.Message.CcName, System.Message.CcAddress," + // (5, 6, 7, 8, 9)
                        " System.Search.Autosummary, System.IsFlagged, System.IsFlaggedComplete," + // (10, 11, 12)
                        " System.IsDeleted, System.IsRead, System.ItemFolderNameDisplay, System.Keywords, " + // (13, 14, 15, 16)
                        " System.Message.HasAttachments, System.Message.AttachmentNames, System.Importance, System.Message.ToDoFlags," + // (17, 18, 19, 20)
                        " System.DateModified, System.Message.DateReceived, System.ItemFolderPathDisplay" + // (21, 22, 23)
                        */

                        // check for any keywords matching "private" and skip these messages
                        string[] keywords = reader.GetValue(16) as string[];
                        if ((keywords != null) && keywords.Any(s => s.ToLowerInvariant().Contains("private")))
                        {
                            continue;
                        }

                        List<Attachment> attachments = new List<Attachment>();
                        if (!reader.IsDBNull(18))
                        {
                            string[] attNameArr = (string[])reader.GetValue(18);
                            attachments.AddRange(attNameArr.Select(a => new Attachment(a)));
                        }

                        string folder = reader.GetValue(15) as string;
                        if (folder == null)
                        {
                            var fi = reader.GetValue(15) as int?;
                            folder = fi.HasValue ? fi.ToString() : string.Empty;
                        }

                        // For some users the Junk mail folder seems to be incorrectly labelled as the number 1. Skip these
                        if (folder == "1" || folder == "1d00006d")
                        {
                            continue;
                        }

                        string folderPath = reader.GetValue(23) as string;
                        if (!string.IsNullOrWhiteSpace(folderPath) && !folderDict.ContainsKey(folderPath))
                        {
                            folderDict[folderPath] = new MessageFolder { Name = folder };
                            this.User.MessageFolders.Add(folderDict[folderPath]);
                        }

                        string conversationId = reader.GetValue(2) as string ?? Guid.NewGuid().ToString("N");
                        
                        MessageFolder messageFolder = folderDict[folderPath];
                        Message email = new Message
                                            {
                                                Subject = reader.GetValue(6) as string, 
                                                DateSent = ReadValue(reader, 3, new DateTime()),
                                                DateReceived = ReadValue(reader, 22, new DateTime()),
                                                Summary = reader.GetValue(10) as string,
                                                IsDeleted = ReadValue(reader, 13, false), 
                                                IsRead = ReadValue(reader, 14, false), 
                                                Attachments = attachments, 
                                                Folder = messageFolder,
                                                LastModifiedTime = ReadValue(reader, 21, new DateTime()),
                                                IsFlaggedBySender = ReadValue(reader, 20, 0) > 1, 
                                                ConversationId = conversationId
                                            };

                        messageFolder.Messages.Add(email);
                        if (email.Subject == "[No subject]")
                        {
                            email.Subject = string.Empty;
                        }

                        int importance = ReadValue(reader, 19, 0);
                        if (importance > 3)
                        {
                            email.IsMarkedImportantBySender = true;
                        }

                        if (importance < 3)
                        {
                            email.IsMarkedUnimportantBySender = true;
                        }

                        bool isFlagged = ReadValue(reader, 11, false);
                        bool isFlaggedComplete = ReadValue(reader, 12, false);
                        email.Flag = isFlagged ? FlagState.Flagged : FlagState.NotFlagged;
                        if (isFlaggedComplete)
                        {
                            email.Flag = FlagState.FlaggedComplete;
                        }

                        var conversation = this.User.GetOrCreateConversation(conversationId);
                        
                        email.Sender = this.User.GetContactDetails(conversation, reader.GetValue(4) as string, reader.GetValue(5) as string);

                        if (email.Folder.IsSentItems)
                        {
                            email.Sender.IsMe = true;
                        }

                        email.SentTo = this.User.GetContactList(conversation, reader.GetValue(0) as string[], reader.GetValue(1) as string[]);
                        email.CopiedTo = this.User.GetContactList(conversation, reader.GetValue(8) as string[], reader.GetValue(9) as string[]);

                        conversation.AddMessage(email);
                        conversation.SetVariables(email);
                        this.User.Messages.Add(email);
                        
                        count++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(@"error reading email: " + ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(@"error reading all email: " + ex);
            }
        }

        /// <summary>
        /// Executes the query.
        /// </summary>
        /// <param name="query">
        /// The query.
        /// </param>
        /// <returns>
        /// The reader
        /// </returns>
        private OleDbDataReader ExecuteQuery(string query)
        {
            OleDbDataReader myDataReader = null;
            try
            {
                if (this.databaseConnection != null)
                {
                    OleDbCommand myOleDbCommand = new OleDbCommand(query, this.databaseConnection);
                    myDataReader = myOleDbCommand.ExecuteReader();
                }
            }
            catch (OleDbException ex)
            {
                Console.WriteLine(@"OleDb exception" + ex);
            }

            return myDataReader;
        }

        #endregion
    }
}