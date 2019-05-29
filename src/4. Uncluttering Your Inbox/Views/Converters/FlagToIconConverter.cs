// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace UnclutteringYourInbox.Views.Converters
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;


    using MBMLViews.Converters;

    /// <summary>
    /// The flag to icon converter.
    /// </summary>
    public class FlagToIconConverter : BaseConverter<FlagToIconConverter>
    {
        /// <summary>
        /// The icon dictionary.
        /// </summary>
        private readonly Dictionary<string, ImageSource> iconDict = new Dictionary<string, ImageSource>();

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            FlagState flag = (FlagState)value;
            string key = flag.ToString();
            if ((parameter != null) && (flag == FlagState.NotFlagged))
            {
                return null;
            }

            if (!this.iconDict.ContainsKey(key))
            {
                this.iconDict[key] = new BitmapImage(new Uri("pack://application:,,,/UnclutteringYourInbox;component/Icons/" + key + ".png"));
            }

            return this.iconDict[key];
        }
    }
}