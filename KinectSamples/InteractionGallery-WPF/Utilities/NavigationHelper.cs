// -----------------------------------------------------------------------
// <copyright file="NavigationHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.InteractionGallery.Utilities
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// NavigationHelper consists of an attached dependency property which the navigation frame utilizes to show the current navigation context.
    /// This encapsulation ensures that ViewModels have no dependencies on any Views.
    /// </summary>
    public class NavigationHelper : DependencyObject
    {
        public static readonly DependencyProperty CurrentNavigationElementProperty =
            DependencyProperty.RegisterAttached("CurrentNavigationElement", typeof(object), typeof(NavigationHelper), new PropertyMetadata(CurrentNavigationElementChanged));

        public static object GetCurrentNavigationElement(DependencyObject obj)
        {
            if (null == obj)
            {
                throw new ArgumentNullException("obj");
            }

            return obj.GetValue(CurrentNavigationElementProperty);
        }

        public static void SetCurrentNavigationElement(DependencyObject obj, object value)
        {
            if (null == obj)
            {
                throw new ArgumentNullException("obj");
            }

            obj.SetValue(CurrentNavigationElementProperty, value);
        }

        private static void CurrentNavigationElementChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var cp = sender as ContentPresenter;
            if (null != cp)
            {
                cp.Content = e.NewValue;
            }
            else
            {
                var cc = sender as ContentControl;
                if (null != cc)
                {
                    cc.Content = e.NewValue;
                }
                else
                {
                    throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Only objects of types {0} and {1} are supported",
                        typeof(ContentPresenter).FullName,
                        typeof(ContentControl).FullName));
                }
            }
        }
    }
}