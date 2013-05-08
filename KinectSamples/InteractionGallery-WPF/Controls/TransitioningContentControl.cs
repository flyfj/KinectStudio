// -----------------------------------------------------------------------
// <copyright file="TransitioningContentControl.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.InteractionGallery.Controls
{
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Represents a control that displays a single piece of content.  When the content changes, a custom transition
    /// defined in the VisualStateManager is initiated.
    /// </summary>
    [TemplatePart(Name = PreviousContentPresentationSitePartName, Type = typeof(ContentControl))]
    [TemplatePart(Name = CurrentContentPresentationSitePartName, Type = typeof(ContentControl))]
    public class TransitioningContentControl : ContentControl
    {
        public static readonly DependencyProperty TransitionProperty =
            DependencyProperty.Register("Transition", typeof(string), typeof(TransitioningContentControl), new PropertyMetadata(DefaultTransitionState));

        /// <summary>
        /// Template part name for the ContentPresenter responsible for displaying the previous content.
        /// </summary>
        internal const string PreviousContentPresentationSitePartName = "PreviousContentPresentationSite";

        /// <summary>
        /// Template part name for the ContentPresenter responsible for displaying the current content.
        /// </summary>
        internal const string CurrentContentPresentationSitePartName = "CurrentContentPresentationSite";

        /// <summary>
        /// Name of the non-transitioning visual state.
        /// </summary>
        internal const string NormalState = "Normal";

        /// <summary>
        /// Name of the default transition.
        /// </summary>
        internal const string DefaultTransitionState = "Fade";

        public TransitioningContentControl()
        {
            this.DefaultStyleKey = typeof(TransitioningContentControl);
        }

        /// <summary>
        /// Gets or sets the name of the current transition to be applied when the content changes
        /// </summary>
        public string Transition
        {
            get { return this.GetValue(TransitionProperty) as string; }
            set { this.SetValue(TransitionProperty, value); }
        }

        /// <summary>
        /// ContentPresenter responsible for presenting the current content
        /// </summary>
        private ContentPresenter CurrentContentPresentationSite { get; set; }

        /// <summary>
        /// ContentPresenter responsible for presenting the previous content
        /// </summary>
        private ContentPresenter PreviousContentPresentationSite { get; set; }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.PreviousContentPresentationSite = this.GetTemplateChild(PreviousContentPresentationSitePartName) as ContentPresenter;
            this.CurrentContentPresentationSite = this.GetTemplateChild(CurrentContentPresentationSitePartName) as ContentPresenter;

            if (null != this.CurrentContentPresentationSite)
            {
                this.CurrentContentPresentationSite.Content = this.Content;
            }

            VisualStateManager.GoToState(this, NormalState, false);
        }

        protected override void OnContentChanged(object oldContent, object newContent)
        {
            base.OnContentChanged(oldContent, newContent);

            if (null != this.CurrentContentPresentationSite && null != this.PreviousContentPresentationSite)
            {
                this.CurrentContentPresentationSite.Content = newContent;
                this.PreviousContentPresentationSite.Content = oldContent;

                // Go to the normal state first to ensure a state change occurs
                VisualStateManager.GoToState(this, NormalState, false);
                VisualStateManager.GoToState(this, this.Transition, true);
            }
        }
    }
}
