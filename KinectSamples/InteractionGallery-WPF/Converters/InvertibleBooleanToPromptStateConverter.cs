// -----------------------------------------------------------------------
// <copyright file="InvertibleBooleanToPromptStateConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.InteractionGallery.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    using Microsoft.Samples.Kinect.InteractionGallery.Utilities;

    /// <summary>
    /// Converts between Booleans and PromptState states. Accepts an optional Boolean, String
    /// or Integer parameter defining whether True or False converts to PromptState.Prompting
    /// (other value will convert to PromptState.Hidden).
    /// </summary>
    public class InvertibleBooleanToPromptStateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = System.Convert.ToBoolean(value, culture);
            bool isPrompting = (null != parameter) ? System.Convert.ToBoolean(parameter, culture) == boolValue : boolValue;

            return isPrompting ? PromptState.Prompting : PromptState.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
