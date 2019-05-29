// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The message folder.
    /// </summary>
    [Serializable]
    public class MessageFolder
    {
        /// <summary>
        /// The folder is rule target.
        /// </summary>
        private bool? folderIsRuleTarget;

        /// <summary>
        /// The is conversation history.
        /// </summary>
        private bool? isConversationHistory;

        /// <summary>
        /// The is deleted.
        /// </summary>
        private bool? isDeleted;

        /// <summary>
        /// The is drafts.
        /// </summary>
        private bool? isDrafts;

        /// <summary>
        /// The is inbox.
        /// </summary>
        private bool? isInbox;

        /// <summary>
        /// The is junk email.
        /// </summary>
        private bool? isJunkEmail;

        /// <summary>
        /// The is sent items.
        /// </summary>
        private bool? isSentItems;

        /// <summary>
        /// The reply fraction.
        /// </summary>
        private double? replyFraction;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageFolder"/> class. 
        /// </summary>
        public MessageFolder()
        {
            this.Messages = new List<Message>();
        }

        /// <summary>
        /// Gets or sets the folder id.
        /// </summary>
        public string FolderId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the folder is the target of a rule. This is a heuristic
        /// based on what percentage of e-mails are moved 'quickly'. 
        /// </summary>
        public bool FolderIsMoveTarget
        {
            get
            {
                if (this.folderIsRuleTarget == null)
                {
                    if (this.IsSentItems || this.IsDrafts || this.IsDeleted || this.IsInbox || this.IsJunkEmail)
                    {
                        this.folderIsRuleTarget = false;
                    }
                    else
                    {
                        const double SecondsThreshold = 600.0; // Defines a quick move

                        // Defines the percentage that are moved quickly. This is deliberately
                        // low to support userswho are not hooked up to the network frequently.
                        const double ProportionThreshold = 0.3;
                        int count = 0;
                        int autoCount = 0;

                        foreach (Message email in this.Messages)
                        {
                            // If LastModifiedName is available, then this acts as a cue for whether
                            // the move was automatic or manual
                            if (!string.IsNullOrEmpty(email.LastModifiedName))
                            {
                                bool moverEqualsSender = false;
                                if (email.Sender != null && email.Sender.Person != null
                                    && email.Sender.Person.Identities != null)
                                {
                                    if (
                                        email.Sender.Person.Identities.Any(
                                            cd => cd.Name.IsCertain && email.LastModifiedName == cd.Name.Value))
                                    {
                                        moverEqualsSender = true;
                                    }
                                }

                                // If the mover is not the sender then this was not moved automatically
                                if (!moverEqualsSender)
                                {
                                    count++;
                                    continue;
                                }
                            }

                            if (email.LastModifiedTime == null)
                            {
                                continue;
                            }

                            double timeForMove = (email.LastModifiedTime.Value - email.DateReceived).TotalSeconds;
                            if (timeForMove < SecondsThreshold)
                            {
                                autoCount++;
                            }

                            count++;
                        }

                        // Proportion we are counting as automatic moves
                        double prop = count == 0 ? 0.0 : ((double)autoCount) / count;

                        this.folderIsRuleTarget = prop > ProportionThreshold;
                    }
                }

                return this.folderIsRuleTarget.Value;
            }

            set
            {
                this.folderIsRuleTarget = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this is the Inbox
        /// </summary>
        public bool IsConversationHistory
        {
            get
            {
                if (this.isConversationHistory == null)
                {
                    this.isConversationHistory = this.Name != null
                                                 && this.Name.ToLowerInvariant().Contains("conversation history");
                }

                return this.isConversationHistory.Value;
            }

            set
            {
                this.isConversationHistory = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this is the deleted items folder
        /// </summary>
        public bool IsDeleted
        {
            get
            {
                if (this.isDeleted == null)
                {
                    this.isDeleted = this.Name != null && this.Name.ToLowerInvariant().Contains("deleted items");
                }

                return this.isDeleted.Value;
            }

            set
            {
                this.isDeleted = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this is the sent items folder
        /// </summary>
        public bool IsDrafts
        {
            get
            {
                if (this.isDrafts == null)
                {
                    this.isDrafts = this.Name != null && this.Name.ToLowerInvariant().Equals("drafts");
                }

                return this.isDrafts.Value;
            }

            set
            {
                this.isDrafts = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this is the Inbox
        /// </summary>
        public bool IsInbox
        {
            get
            {
                if (this.isInbox == null)
                {
                    this.isInbox = this.Name != null && this.Name.ToLowerInvariant().Equals("inbox");
                }

                return this.isInbox.Value;
            }

            set
            {
                this.isInbox = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this is the junk folder
        /// </summary>
        public bool IsJunkEmail
        {
            get
            {
                if (this.isJunkEmail == null)
                {
                    this.isJunkEmail = (this.Name != null) && this.Name.ToLowerInvariant().Contains("junk")
                                       && this.Name.ToLowerInvariant().Contains("mail");
                }

                return this.isJunkEmail.Value;
            }

            set
            {
                this.isJunkEmail = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this is the sent items folder
        /// </summary>
        public bool IsSentItems
        {
            get
            {
                if (this.isSentItems == null)
                {
                    this.isSentItems = this.Name != null && this.Name.ToLowerInvariant().Contains("sent items");
                }

                return this.isSentItems.Value;
            }

            set
            {
                this.isSentItems = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this folder is a user-created folder, rather than a special
        /// folder like inbox, drafts, deleted items, outbox, junk mail, sent mail etc.
        /// </summary>
        public bool IsUserFolder
        {
            get
            {
                bool isSpecial = this.IsInbox || this.IsJunkEmail || this.IsSentItems || this.IsDeleted || this.IsDrafts || this.IsConversationHistory;
                return !isSpecial;
            }
        }

        /// <summary>
        /// Gets the list of messages in this folder
        /// </summary>
        public IList<Message> Messages { get; internal set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the reply fraction.
        /// </summary>
        public double? ReplyFraction
        {
            get
            {
                return this.replyFraction
                       ?? (this.replyFraction =
                           this.Messages.Count == 0 ? 0.0 : (double)this.Messages.Count(ia => ia.IsRepliedTo) / this.Messages.Count);
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
            return this.Name;
        }
    }
}
