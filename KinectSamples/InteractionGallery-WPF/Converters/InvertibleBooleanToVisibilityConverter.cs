//------------------------------------------------------------------------------
// <copyright file="InvertibleBooleanToVisibilityConverter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.InteractionGallery.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;

    /// <summary>
    /// Converts between Booleans and Visibility states. Accepts an optional Boolean, String or Integer parameter defining whether True or False
    /// convert to Visibility.Visible.
    /// </summary>
    public class InvertibleBooleanToVisibilityConverter : IValueConverter
    {
        private BooleanToVisibilityConverter converter = new BooleanToVisibilityConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (null != parameter && !System.Convert.ToBoolean(parameter, CultureInfo.InvariantCulture))
            {
                return converter.Convert(!((bool)value), targetType, parameter, culture);
            }

            return converter.Convert(value, targetType, parameter, culture); 
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (null != parameter && !System.Convert.ToBoolean(parameter, CultureInfo.InvariantCulture))
            {
                Visibility invertedVisibility = (Visibility.Visible == (Visibility)value) ? Visibility.Collapsed : Visibility.Visible;
                return converter.ConvertBack(invertedVisibility, targetType, parameter, culture);
            }

            return converter.ConvertBack(value, targetType, parameter, culture);
        }
    }
}
