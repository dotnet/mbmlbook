// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox
{
    using System;

    /// <summary>
    /// Attachment class.
    /// </summary>
    [Serializable]
    public class Attachment
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Attachment"/> class.
        /// </summary>
        public Attachment()
        {
            this.Name = string.Empty;
            this.IsFile = null;
            this.IsInline = null;
            this.Size = -1; // unknown size
            this.DateModified = DateTime.MinValue;
            this.ContentType = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Attachment"/> class.
        /// </summary>
        /// <param name="name">
        /// The name.
        /// </param>
        public Attachment(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the is file.
        /// </summary>
        public bool? IsFile { get; set; }

        /// <summary>
        /// Gets or sets the is inline.
        /// </summary>
        public bool? IsInline { get; set; }

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Gets or sets the date modified.
        /// </summary>
        public DateTime DateModified { get; set; }

        /// <summary>
        /// Gets or sets the content type.
        /// </summary>
        public string ContentType { get; set; }
    }
}
