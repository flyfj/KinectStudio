// -----------------------------------------------------------------------
// <copyright file="PannableContentView.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.InteractionGallery.Views
{
    using System.Windows;

    /// <summary>
    /// Interaction logic for PannableContentView
    /// </summary>
    public partial class PannableContentView
    {
        public PannableContentView()
        {
            this.InitializeComponent();
        }

        private void WindowOnLoaded(object sender, RoutedEventArgs e)
        {
            KinectScrollViewer.ScrollToHorizontalOffset(KinectScrollViewer.ScrollableWidth * 0.5);
            KinectScrollViewer.ScrollToVerticalOffset(KinectScrollViewer.ScrollableHeight * 0.5);
        }
    }
}
