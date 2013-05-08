//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.InteractionGallery
{
    using System;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;

    using Microsoft.Samples.Kinect.InteractionGallery.Utilities;
    using Microsoft.Samples.Kinect.InteractionGallery.ViewModels;

    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int MinimumScreenWidth = 1920;
        private const int MinimumScreenHeight = 1080;

        /// <summary>
        /// Mouse movement detector.
        /// </summary>
        private readonly MouseMovementDetector movementDetector;

        /// <summary>
        /// Controller used as ViewModel for this window.
        /// </summary>
        private readonly KinectController controller;

        public MainWindow(KinectController controller)
        {
            this.InitializeComponent();

            if (controller == null)
            {
                throw new ArgumentNullException("controller", Properties.Resources.KinectControllerInvalid);
            }

            this.controller = controller;
            
            controller.EngagedUserColor = (Color)this.Resources["EngagedUserColor"];
            controller.TrackedUserColor = (Color)this.Resources["TrackedUserColor"];
            controller.EngagedUserMessageBrush = (Brush)this.Resources["EngagedUserMessageBrush"];
            controller.TrackedUserMessageBrush = (Brush)this.Resources["TrackedUserMessageBrush"];

            this.kinectRegion.HandPointersUpdated += (sender, args) => controller.OnHandPointersUpdated(this.kinectRegion.HandPointers);

            this.DataContext = controller;

            this.movementDetector = new MouseMovementDetector(this);
            this.movementDetector.IsMovingChanged += this.OnIsMouseMovingChanged;
            this.movementDetector.Start();
        }

        /// <summary>
        /// Handles all key up events and closes the window if the Escape key is pressed
        /// </summary>
        /// <param name="e">The data for the key up event</param>
        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (null == e)
            {
                throw new ArgumentNullException("e");
            }

            if (Key.Escape == e.Key)
            {
                this.Close();
            }

            base.OnKeyUp(e);
        }

        /// <summary>
        /// Handles Window.Loaded event, and prompts user if screen resolution does not meet
        /// minimal requirements.
        /// </summary>
        /// <param name="sender">
        /// Object that sent the event.
        /// </param>
        /// <param name="e">
        /// Event arguments.
        /// </param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // get the main screen size
            double height = SystemParameters.PrimaryScreenHeight;
            double width = SystemParameters.PrimaryScreenWidth;

            // if the main screen is less than 1920 x 1080 then warn the user it is not the optimal experience 
            if ((width < MinimumScreenWidth) || (height < MinimumScreenHeight))
            {
                MessageBoxResult continueResult = MessageBox.Show(Properties.Resources.SuboptimalScreenResolutionMessage, Properties.Resources.SuboptimalScreenResolutionTitle, MessageBoxButton.YesNo);
                if (continueResult == MessageBoxResult.No)
                {
                    this.Close();
                }
            }
        }

        /// <summary>
        /// Handles MouseMovementDetector.IsMovingChanged event and shows/hides the window bezel,
        /// as appropriate.
        /// </summary>
        /// <param name="sender">
        /// Object that sent the event.
        /// </param>
        /// <param name="e">
        /// Event arguments.
        /// </param>
        private void OnIsMouseMovingChanged(object sender, EventArgs e)
        {
            WindowBezelHelper.UpdateBezel(this, this.movementDetector.IsMoving);
            this.controller.IsInEngagementOverrideMode = this.movementDetector.IsMoving;
        }
    }
}
