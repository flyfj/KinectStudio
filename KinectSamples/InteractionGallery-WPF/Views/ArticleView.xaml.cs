//------------------------------------------------------------------------------
// <copyright file="ArticleView.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.InteractionGallery.Views
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    /// <summary>
    /// Interaction logic for ArticleView
    /// </summary>
    public partial class ArticleView
    {
        public static readonly DependencyProperty SelectedImageProperty = DependencyProperty.Register(
            "SelectedImage", typeof(ImageSource), typeof(ArticleView), new PropertyMetadata(default(ImageSource)));

        /// <summary>
        /// Name of the non-transitioning visual state.
        /// </summary>
        internal const string NormalState = "Normal";

        /// <summary>
        /// Name of the fade in transition.
        /// </summary>
        internal const string FadeInTransitionState = "FadeIn";

        /// <summary>
        /// Name of the fade out transition.
        /// </summary>
        internal const string FadeOutTransitionState = "FadeOut";

        public ArticleView()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// CLR Property Wrappers for SelectedImageProperty
        /// </summary>
        public ImageSource SelectedImage
        {
            get
            {
                return (ImageSource)GetValue(SelectedImageProperty);
            }

            set
            {
                this.SetValue(SelectedImageProperty, value);
            }
        }


        /// <summary>
        /// Close the full screen view of the image
        /// </summary>
        private void OnCloseFullImage(object sender, RoutedEventArgs e)
        {
            // Always go to normal state before a transition
            VisualStateManager.GoToElementState(OverlayGrid, NormalState, false);
            VisualStateManager.GoToElementState(OverlayGrid, FadeOutTransitionState, true);
        }

        /// <summary>
        /// Overlay the full screen view of the image
        /// </summary>
        private void OnDisplayFullImage(object sender, RoutedEventArgs e)
        {
            // Always go to normal state before a transition
            this.SelectedImage = ((ContentControl)e.OriginalSource).Content as ImageSource;
            VisualStateManager.GoToElementState(OverlayGrid, NormalState, false);
            VisualStateManager.GoToElementState(OverlayGrid, FadeInTransitionState, false);
        }
    }
}
