// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Features
{

    /// <summary>
    /// The ConfigurableFeature interface.
    /// </summary>
    public interface IConfigurableFeature
    {
        /// <summary>
        /// Configures the specified user.
        /// </summary>
        /// <param name="user">The user.</param>
        void Configure(User user);
    }
}