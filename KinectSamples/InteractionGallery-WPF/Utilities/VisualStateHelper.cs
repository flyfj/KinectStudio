// -----------------------------------------------------------------------
// <copyright file="VisualStateHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.InteractionGallery.Utilities
{
    using System;
    using System.Globalization;
    using System.Windows;

    /// <summary>
    /// VisualStateHelper consists of an attached dependency property which Views utilize to trigger transitions through the VisualStateManager.
    /// This encapsulation ensures that ViewModels have no dependencies on any Views.
    /// </summary>
    public class VisualStateHelper : DependencyObject
    {
        public static readonly DependencyProperty VisualStatePropertyProperty =
            DependencyProperty.RegisterAttached("VisualStateProperty", typeof(string), typeof(VisualStateHelper), new PropertyMetadata(VisualStateChanged));

        public static string GetVisualStateProperty(DependencyObject obj)
        {
            if (null == obj)
            {
                throw new ArgumentNullException("obj");
            }

            return (string)obj.GetValue(VisualStatePropertyProperty);
        }

        public static void SetVisualStateProperty(DependencyObject obj, string value)
        {
            if (null == obj)
            {
                throw new ArgumentNullException("obj");
            }

            obj.SetValue(VisualStatePropertyProperty, value);
        }

        private static void VisualStateChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var elem = sender as FrameworkElement;
            if (null == elem)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Only objects of type {0} are supported", typeof(FrameworkElement).FullName));
            }

            if (null != e.NewValue)
            {
                VisualStateManager.GoToState(elem, (string)e.NewValue, true);
            }
        }
    }
}