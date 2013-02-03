//------------------------------------------------------------------------------
// <copyright file="KinectChooser.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Avateering
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Kinect;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    /// <summary>
    /// This class will pick a Kinect sensor, if available.
    /// </summary>
    public class KinectChooser : DrawableGameComponent
    {
        /// <summary>
        /// The status to string mapping.
        /// </summary>
        private readonly Dictionary<KinectStatus, string> statusMap = new Dictionary<KinectStatus, string>();

        /// <summary>
        /// The requested color image format.
        /// </summary>
        private readonly ColorImageFormat colorImageFormat;

        /// <summary>
        /// The requested depth image format.
        /// </summary>
        private readonly DepthImageFormat depthImageFormat;

        /// <summary>
        /// The chooser background texture.
        /// </summary>
        private Texture2D chooserBackground;
        
        /// <summary>
        /// The font for rendering the state text.
        /// </summary>
        private SpriteFont font;

        /// <summary>
        /// Gets or sets near mode.
        /// </summary>
        private bool nearMode;

        /// <summary>
        /// Gets or sets seated mode.
        /// </summary>
        private bool seatedMode;

        /// <summary>
        /// Initializes a new instance of the KinectChooser class.
        /// </summary>
        /// <param name="game">The related game object.</param>
        /// <param name="colorFormat">The desired color image format.</param>
        /// <param name="depthFormat">The desired depth image format.</param>
        public KinectChooser(Game game, ColorImageFormat colorFormat, DepthImageFormat depthFormat)
            : base(game)
        {
            this.colorImageFormat = colorFormat;
            this.depthImageFormat = depthFormat;

            this.nearMode = false;
            this.seatedMode = false;

            KinectSensor.KinectSensors.StatusChanged += this.KinectSensors_StatusChanged;
            this.DiscoverSensor();

            this.statusMap.Add(KinectStatus.Undefined, "Not connected, or in use");
            this.statusMap.Add(KinectStatus.Connected, string.Empty);
            this.statusMap.Add(KinectStatus.DeviceNotGenuine, "Device Not Genuine");
            this.statusMap.Add(KinectStatus.DeviceNotSupported, "Device Not Supported");
            this.statusMap.Add(KinectStatus.Disconnected, "Required");
            this.statusMap.Add(KinectStatus.Error, "Error");
            this.statusMap.Add(KinectStatus.Initializing, "Initializing...");
            this.statusMap.Add(KinectStatus.InsufficientBandwidth, "Insufficient Bandwidth");
            this.statusMap.Add(KinectStatus.NotPowered, "Not Powered");
            this.statusMap.Add(KinectStatus.NotReady, "Not Ready");
        }

        /// <summary>
        /// Gets the SpriteBatch from the services.
        /// </summary>
        public SpriteBatch SharedSpriteBatch
        {
            get
            {
                return (SpriteBatch)this.Game.Services.GetService(typeof(SpriteBatch));
            }
        }

        /// <summary>
        /// Gets the selected KinectSensor.
        /// </summary>
        public KinectSensor Sensor { get; private set; }

        /// <summary>
        /// Gets the last known status of the KinectSensor.
        /// </summary>
        public KinectStatus LastStatus { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether near mode is enabled.
        /// Near mode enables depth between 0.4 to 3m, default is between 0.8 to 4m.
        /// </summary>
        public bool NearMode
        {
            get
            {
                return this.nearMode;
            }

            set
            {
                if (null != this.Sensor && null != this.Sensor.DepthStream)
                {
                    try
                    {
                        this.Sensor.DepthStream.Range = value ? DepthRange.Near : DepthRange.Default;   // set near or default mode
                        this.nearMode = value;
                    }
                    catch (InvalidOperationException)
                    {
                        // not valid for this camera
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether seated mode is enabled for skeletal tracking.
        /// Seated mode tracks only the upper body skeleton,
        /// returning the 10 joints of the arms, shoulders and head.
        /// </summary>
        public bool SeatedMode
        {
            get
            {
                return this.seatedMode;
            }

            set
            {
                if (null != this.Sensor && null != this.Sensor.SkeletonStream)
                {
                    try
                    {
                        this.Sensor.SkeletonStream.TrackingMode = value ? SkeletonTrackingMode.Seated : SkeletonTrackingMode.Default; // Set seated or default mode
                        this.seatedMode = value;
                    }
                    catch (InvalidOperationException)
                    {
                        // not valid for this camera
                    }
                }
            }
        }

        /// <summary>
        /// This method renders the current state of the KinectChooser.
        /// </summary>
        /// <param name="gameTime">The elapsed game time.</param>
        public override void Draw(GameTime gameTime)
        {
            // If the background is not loaded, load it now
            if (null == this.chooserBackground)
            {
                this.LoadContent();
            }

            if (null == this.SharedSpriteBatch)
            {
                return;
            }

            // If we don't have a sensor, or the sensor we have is not connected
            // then we will display the information text
            if (null == this.Sensor || this.LastStatus != KinectStatus.Connected)
            {
                this.SharedSpriteBatch.Begin();

                // Render the background
                this.SharedSpriteBatch.Draw(
                    this.chooserBackground,
                    new Vector2(Game.GraphicsDevice.Viewport.Width / 2, Game.GraphicsDevice.Viewport.Height / 2),
                    null,
                    Color.White,
                    0,
                    new Vector2(this.chooserBackground.Width / 2, this.chooserBackground.Height / 2),
                    1,
                    SpriteEffects.None,
                    0);

                // Determine the text
                string txt = this.statusMap[KinectStatus.Undefined];
                if (this.Sensor != null)
                {
                    txt = this.statusMap[this.LastStatus];
                }

                // Render the text
                Vector2 size = this.font.MeasureString(txt);
                this.SharedSpriteBatch.DrawString(
                    this.font,
                    txt,
                    new Vector2((Game.GraphicsDevice.Viewport.Width - size.X) / 2, (Game.GraphicsDevice.Viewport.Height / 2) + size.Y),
                    Color.White);
                this.SharedSpriteBatch.End();
            }

            base.Draw(gameTime);
        }

        /// <summary>
        /// This method loads the textures and fonts.
        /// </summary>
        protected override void LoadContent()
        {
            base.LoadContent();

            this.chooserBackground = Game.Content.Load<Texture2D>("ChooserBackground");
            this.font = Game.Content.Load<SpriteFont>("Segoe16");
        }

        /// <summary>
        /// This method ensures that the KinectSensor is stopped before exiting.
        /// </summary>
        protected override void UnloadContent()
        {
            base.UnloadContent();

            // Always stop the sensor when closing down
            if (null != this.Sensor)
            {
                this.Sensor.Stop();
            }
        }

        /// <summary>
        /// This method will use basic logic to try to grab a sensor.
        /// Once a sensor is found, it will start the sensor with the
        /// requested options.
        /// </summary>
        private void DiscoverSensor()
        {
            // Grab any available sensor
            this.Sensor = KinectSensor.KinectSensors.FirstOrDefault();

            if (null != this.Sensor)
            {
                this.LastStatus = this.Sensor.Status;

                // If this sensor is connected, then enable it
                if (this.LastStatus == KinectStatus.Connected)
                {
                    // For many applications we would enable the
                    // automatic joint smoothing, however, in this
                    // Avateering sample, we perform skeleton joint
                    // position corrections, so we will manually
                    // filter when these are complete.

                    // Typical smoothing parameters for the joints:
                    // var parameters = new TransformSmoothParameters
                    // {
                    // Smoothing = 0.25f,
                    // Correction = 0.25f,
                    // Prediction = 0.75f,
                    // JitterRadius = 0.1f,
                    // MaxDeviationRadius = 0.04f 
                    // };
                    this.Sensor.SkeletonStream.Enable();
                    this.Sensor.ColorStream.Enable(this.colorImageFormat);
                    this.Sensor.DepthStream.Enable(this.depthImageFormat);
                    this.Sensor.SkeletonStream.EnableTrackingInNearRange = true; // Enable skeleton tracking in near mode

                    try
                    {
                        this.Sensor.Start();
                    }
                    catch (IOException)
                    {
                        // sensor is in use by another application
                        // will treat as disconnected for display purposes
                        this.Sensor = null;
                    }
                }
            }
            else
            {
                this.LastStatus = KinectStatus.Disconnected;
            }
        }

        /// <summary>
        /// This wires up the status changed event to monitor for 
        /// Kinect state changes.  It automatically stops the sensor
        /// if the device is no longer available.
        /// </summary>
        /// <param name="sender">The sending object.</param>
        /// <param name="e">The event args.</param>
        private void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            // If the status is not connected, try to stop it
            if (e.Status != KinectStatus.Connected)
            {
                e.Sensor.Stop();
            }

            this.LastStatus = e.Status;
            this.DiscoverSensor();
        }
    }
}
