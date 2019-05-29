// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Features
{
    /// <summary>
    /// The CompoundFeature interface.
    /// </summary>
    /// <typeparam name="TFeature1">The type of the feature 1.</typeparam>
    /// <typeparam name="TFeature2">The type of the feature 2.</typeparam>
    public interface ICompoundFeature<TFeature1, TFeature2> 
        where TFeature1 : Feature 
        where TFeature2 : Feature
    {
        /// <summary>
        /// Gets or sets the base feature 1.
        /// </summary>
        TFeature1 Feature1 { get; set; }

        /// <summary>
        /// Gets or sets the base feature 2.
        /// </summary>
        TFeature2 Feature2 { get; set; }
    }
}