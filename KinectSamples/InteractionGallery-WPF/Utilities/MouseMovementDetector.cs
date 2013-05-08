// -----------------------------------------------------------------------
// <copyright file="MouseMovementDetector.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.InteractionGallery.Utilities
{
    using System;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Threading;

    /// <summary>
    /// Keeps track of mouse movement over a specified window and sends events whenever
    /// movement state (moving versus not moving for a long enough period of time) changes.
    /// </summary>
    public class MouseMovementDetector
    {
        /// <summary>
        /// Interval for which the mouse must be stationary before we decide it's not moving anymore.
        /// </summary>
        private const double StationaryMouseIntervalInMilliseconds = 3000;

        /// <summary>
        /// Timer used to determine whether mouse has not moved for long enough to call it stationary.
        /// </summary>
        private readonly DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(StationaryMouseIntervalInMilliseconds) };

        /// <summary>
        /// Window for which mouse movement is being monitored.
        /// </summary>
        private readonly Window window;

        /// <summary>
        /// Last mouse position (expressed in screen coordinates) observed.
        /// </summary>
        private Point? lastMousePosition;

        /// <summary>
        /// true if mouse has moved recently, false if mouse is stationary.
        /// </summary>
        private bool isMoving;

        /// <summary>
        /// Initializes a new instance of the <see cref="MouseMovementDetector"/> class.
        /// </summary>
        /// <param name="window">
        /// Window for which mouse movement will be monitored.
        /// </param>
        public MouseMovementDetector(Window window)
        {
            if (window == null)
            {
                throw new ArgumentNullException("window");
            }

            this.window = window;

            this.timer.Tick += (s, args) =>
            {
                // Mouse is now stationary, so we should update movement state and stop timer
                this.IsMoving = false;

                this.timer.Stop();
            };
        }

        /// <summary>
        /// Event triggered whenever IsMoving property value changes.
        /// </summary>
        public event EventHandler<EventArgs> IsMovingChanged;

        /// <summary>
        /// true if mouse has moved recently, false if mouse is stationary.
        /// </summary>
        public bool IsMoving
        {
            get
            {
                return this.isMoving;
            }

            set
            {
                bool oldValue = this.isMoving;

                this.isMoving = value;

                if ((oldValue != value) && (this.IsMovingChanged != null))
                {
                    this.IsMovingChanged(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Starts tracking mouse movement.
        /// </summary>
        public void Start()
        {
            this.window.MouseMove += this.OnMouseMove;
        }

        /// <summary>
        /// Stops tracking mouse movement.
        /// </summary>
        public void Stop()
        {
            this.window.MouseMove -= this.OnMouseMove;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (null == e)
            {
                throw new ArgumentNullException("e");
            }

            // Use mouse position in the screen relative coordinate system as hiding/showing the bezel changes the client-area position
            Point mousePosition = window.PointToScreen(e.GetPosition(window));

            if (lastMousePosition.HasValue && lastMousePosition.Value != mousePosition)
            {
                this.IsMoving = true;
                this.timer.Stop();
                this.timer.Start();
            }

            this.lastMousePosition = mousePosition;
        }
    }
}
