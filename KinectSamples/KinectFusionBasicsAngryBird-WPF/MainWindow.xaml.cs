//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.KinectFusionBasics
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Windows;
    using System.Collections;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Threading;
    using Microsoft.Kinect;
    using Microsoft.Kinect.Toolkit.Fusion;
    using System.Collections.Generic;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable
    {
        /// <summary>
        /// Max tracking error count, we will reset the reconstruction if tracking errors
        /// reach this number
        /// </summary>
        private const int MaxTrackingErrors = 100;

        /// <summary>
        /// If set true, will automatically reset the reconstruction when MaxTrackingErrors have occurred
        /// </summary>
        private const bool AutoResetReconstructionWhenLost = false;

        /// <summary>
        /// The resolution of the depth image to be processed.
        /// </summary>
        private const DepthImageFormat DepthImageResolution = DepthImageFormat.Resolution640x480Fps30;

        /// <summary>
        /// The seconds interval to calculate FPS
        /// </summary>
        private const int FpsInterval = 10;

        /// <summary>
        /// The reconstruction volume voxel density in voxels per meter (vpm)
        /// 1000mm / 256vpm = ~3.9mm/voxel
        /// </summary>
        private const int VoxelsPerMeter = 256;

        /// <summary>
        /// The reconstruction volume voxel resolution in the X axis
        /// At a setting of 256vpm the volume is 512 / 256 = 2m wide
        /// </summary>
        private const int VoxelResolutionX = 512;

        /// <summary>
        /// The reconstruction volume voxel resolution in the Y axis
        /// At a setting of 256vpm the volume is 384 / 256 = 1.5m high
        /// </summary>
        private const int VoxelResolutionY = 384;

        /// <summary>
        /// The reconstruction volume voxel resolution in the Z axis
        /// At a setting of 256vpm the volume is 512 / 256 = 2m deep
        /// </summary>
        private const int VoxelResolutionZ = 512;

        /// <summary>
        /// The reconstruction volume processor type. This parameter sets whether AMP or CPU processing
        /// is used. Note that CPU processing will likely be too slow for real-time processing.
        /// </summary>
        private const ReconstructionProcessor ProcessorType = ReconstructionProcessor.Amp;

        /// <summary>
        /// The zero-based device index to choose for reconstruction processing if the 
        /// ReconstructionProcessor AMP options are selected.
        /// Here we automatically choose a device to use for processing by passing -1, 
        /// </summary>
        private const int DeviceToUse = -1;

        /// <summary>
        /// Parameter to translate the reconstruction based on the minimum depth setting. When set to
        /// false, the reconstruction volume +Z axis starts at the camera lens and extends into the scene.
        /// Setting this true in the constructor will move the volume forward along +Z away from the
        /// camera by the minimum depth threshold to enable capture of very small reconstruction volumes
        /// by setting a non-identity world-volume transformation in the ResetReconstruction call.
        /// Small volumes should be shifted, as the Kinect hardware has a minimum sensing limit of ~0.35m,
        /// inside which no valid depth is returned, hence it is difficult to initialize and track robustly  
        /// when the majority of a small volume is inside this distance.
        /// </summary>
        private bool translateResetPoseByMinDepthThreshold = true;

        /// <summary>
        /// Minimum depth distance threshold in meters. Depth pixels below this value will be
        /// returned as invalid (0). Min depth must be positive or 0.
        /// </summary>
        private float minDepthClip = FusionDepthProcessor.DefaultMinimumDepth;

        /// <summary>
        /// Maximum depth distance threshold in meters. Depth pixels above this value will be
        /// returned as invalid (0). Max depth must be greater than 0.
        /// </summary>
        private float maxDepthClip = FusionDepthProcessor.DefaultMaximumDepth;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Bitmap that will hold color information
        /// </summary>
        private WriteableBitmap colorBitmap;

        /// <summary>
        /// Intermediate storage for the depth data converted to color
        /// </summary>
        private int[] colorPixels;

        /// <summary>
        /// Intermediate storage for the depth float data converted from depth image frame
        /// </summary>
        private FusionFloatImageFrame depthFloatBuffer;

        /// <summary>
        /// Intermediate storage for the point cloud data converted from depth float image frame
        /// </summary>
        private FusionPointCloudImageFrame pointCloudBuffer;

        /// <summary>
        /// Raycast shaded surface image
        /// </summary>
        private FusionColorImageFrame shadedSurfaceColorFrame;

        /// <summary>
        /// The transformation between the world and camera view coordinate system
        /// </summary>
        private Matrix4 worldToCameraTransform;

        /// <summary>
        /// The default transformation between the world and volume coordinate system
        /// </summary>
        private Matrix4 defaultWorldToVolumeTransform;

        /// <summary>
        /// The Kinect Fusion volume
        /// </summary>
        private Reconstruction volume;

        /// <summary>
        /// The timer to calculate FPS
        /// </summary>
        private DispatcherTimer fpsTimer;

        /// <summary>
        /// The count of the frames processed in the FPS interval
        /// </summary>
        private int processedFrameCount;

        /// <summary>
        /// The tracking error count
        /// </summary>
        private int trackingErrorCount;

        /// <summary>
        /// The sensor depth frame data length
        /// </summary>
        private int frameDataLength;

        /// <summary>
        /// The count of the depth frames to be processed
        /// </summary>
        private bool processingFrame;

        /// <summary>
        /// Track whether Dispose has been called
        /// </summary>
        private bool disposed;

        // added: rgb frame list
        private List<byte[]> colorFrames = new List<byte[]>();

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Finalizes an instance of the MainWindow class.
        /// This destructor will run only if the Dispose method does not get called.
        /// </summary>
        ~MainWindow()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Get the image size of fusion images and bitmap.
        /// </summary>
        public static Size ImageSize
        {
            get
            {
                return GetImageSize(DepthImageResolution);
            }
        }

        /// <summary>
        /// Dispose the allocated frame buffers and reconstruction.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);

            // This object will be cleaned up by the Dispose method.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Frees all memory associated with the FusionImageFrame.
        /// </summary>
        /// <param name="disposing">Whether the function was called from Dispose.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (null != this.depthFloatBuffer)
                {
                    this.depthFloatBuffer.Dispose();
                }

                if (null != this.pointCloudBuffer)
                {
                    this.pointCloudBuffer.Dispose();
                }

                if (null != this.shadedSurfaceColorFrame)
                {
                    this.shadedSurfaceColorFrame.Dispose();
                }

                if (null != this.volume)
                {
                    this.volume.Dispose();
                }

                this.disposed = true;
            }
        }

        /// <summary>
        /// Get the depth image size from the input depth image format.
        /// </summary>
        /// <param name="imageFormat">The depth image format.</param>
        /// <returns>The widht and height of the input depth image format.</returns>
        private static Size GetImageSize(DepthImageFormat imageFormat)
        {
            switch (imageFormat)
            {
                case DepthImageFormat.Resolution320x240Fps30:
                    return new Size(320, 240);

                case DepthImageFormat.Resolution640x480Fps30:
                    return new Size(640, 480);

                case DepthImageFormat.Resolution80x60Fps30:
                    return new Size(80, 60);
            }

            throw new ArgumentOutOfRangeException("imageFormat");
        }

        /// <summary>
        /// Execute startup tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (null == this.sensor)
            {
                this.statusBarText.Text = Properties.Resources.NoKinectReady;
                return;
            }

            // Turn on the depth stream to receive depth frames
            this.sensor.DepthStream.Enable(DepthImageResolution);

            this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

            this.frameDataLength = this.sensor.DepthStream.FramePixelDataLength;

            // Allocate space to put the color pixels we'll create
            this.colorPixels = new int[this.frameDataLength];

            // This is the bitmap we'll display on-screen
            this.colorBitmap = new WriteableBitmap(
                (int)ImageSize.Width,
                (int)ImageSize.Height,
                96.0,
                96.0,
                PixelFormats.Bgr32,
                null);

            // Set the image we display to point to the bitmap where we'll put the image data
            this.Image.Source = this.colorBitmap;

            // Add an event handler to be called whenever there is new depth frame data
            this.sensor.DepthFrameReady += this.SensorDepthFrameReady;

            this.sensor.ColorFrameReady += this.kinect_colorframe_ready;

            var volParam = new ReconstructionParameters(VoxelsPerMeter, VoxelResolutionX, VoxelResolutionY, VoxelResolutionZ);

            // Set the world-view transform to identity, so the world origin is the initial camera location.
            this.worldToCameraTransform = Matrix4.Identity;

            try
            {
                // This creates a volume cube with the Kinect at center of near plane, and volume directly
                // in front of Kinect.
                this.volume = Reconstruction.FusionCreateReconstruction(volParam, ProcessorType, DeviceToUse, this.worldToCameraTransform);

                this.defaultWorldToVolumeTransform = this.volume.GetCurrentWorldToVolumeTransform();

                if (this.translateResetPoseByMinDepthThreshold)
                {
                    this.ResetReconstruction();
                }
            }
            catch (InvalidOperationException ex)
            {
                this.statusBarText.Text = ex.Message;
                return;
            }
            catch (DllNotFoundException)
            {
                this.statusBarText.Text = this.statusBarText.Text = Properties.Resources.MissingPrerequisite;
                return;
            }

            // Depth frames generated from the depth input
            this.depthFloatBuffer = new FusionFloatImageFrame((int)ImageSize.Width, (int)ImageSize.Height);

            // Point cloud frames generated from the depth float input
            this.pointCloudBuffer = new FusionPointCloudImageFrame((int)ImageSize.Width, (int)ImageSize.Height);

            // Create images to raycast the Reconstruction Volume
            this.shadedSurfaceColorFrame = new FusionColorImageFrame((int)ImageSize.Width, (int)ImageSize.Height);

            // Start the sensor!
            try
            {
                this.sensor.Start();
            }
            catch (IOException ex)
            {
                // Device is in use
                this.sensor = null;
                this.statusBarText.Text = ex.Message;

                return;
            }
            catch (InvalidOperationException ex)
            {
                // Device is not valid, not supported or hardware feature unavailable
                this.sensor = null;
                this.statusBarText.Text = ex.Message;

                return;
            }

            // Set Near Mode by default
            try
            {
                this.sensor.DepthStream.Range = DepthRange.Near;
                checkBoxNearMode.IsChecked = true;
            }
            catch
            {
                // device not near mode capable
            }

            // Initialize and start the FPS timer
            this.fpsTimer = new DispatcherTimer();
            this.fpsTimer.Tick += new EventHandler(this.FpsTimerTick);
            this.fpsTimer.Interval = new TimeSpan(0, 0, FpsInterval);

            this.fpsTimer.Start();

            // Reset the reconstruction
            this.ResetReconstruction();
        }

        // ADDED
        private void kinect_colorframe_ready(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame frame = e.OpenColorImageFrame())
            {
                if (frame == null)
                    return;

                byte[] colorData = new byte[frame.PixelDataLength];
                frame.CopyPixelDataTo(colorData);

                colorFrames.Add(colorData);
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.fpsTimer)
            {
                this.fpsTimer.Stop();
            }

            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }

        /// <summary>
        /// Update the FPS reading in the status text bar
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void FpsTimerTick(object sender, EventArgs e)
        {
            // Update the FPS reading
            this.statusBarText.Text = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                Properties.Resources.Fps,
                (double)this.processedFrameCount / FpsInterval);

            // Reset the frame count
            this.processedFrameCount = 0;
        }

        /// <summary>
        /// Event handler for Kinect sensor's DepthFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorDepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame != null && !this.processingFrame)
                {
                    var depthPixels = new DepthImagePixel[this.frameDataLength];

                    // Copy the pixel data from the image to a temporary array
                    depthFrame.CopyDepthImagePixelDataTo(depthPixels);

                    this.Dispatcher.BeginInvoke(
                        DispatcherPriority.Background,
                        (Action<DepthImagePixel[]>)((d) => { this.ProcessDepthData(d); }),
                        depthPixels);

                    // Mark that one frame will be processed
                    this.processingFrame = true;
                }
            }
        }

        /// <summary>
        /// Process the depth input
        /// </summary>
        /// <param name="depthPixels">The depth data array to be processed</param>
        private void ProcessDepthData(DepthImagePixel[] depthPixels)
        {
            Debug.Assert(null != this.volume, "volume should be initialized");
            Debug.Assert(null != this.shadedSurfaceColorFrame, "shaded surface should be initialized");
            Debug.Assert(null != this.colorBitmap, "color bitmap should be initialized");

            try
            {
                // Convert the depth image frame to depth float image frame
                FusionDepthProcessor.DepthToDepthFloatFrame(
                    depthPixels,
                    (int)ImageSize.Width,
                    (int)ImageSize.Height,
                    this.depthFloatBuffer,
                    FusionDepthProcessor.DefaultMinimumDepth,
                    FusionDepthProcessor.DefaultMaximumDepth,
                    false);

                // ProcessFrame will first calculate the camera pose and then integrate
                // if tracking is successful
                bool trackingSucceeded = this.volume.ProcessFrame(
                    this.depthFloatBuffer,
                    FusionDepthProcessor.DefaultAlignIterationCount,
                    FusionDepthProcessor.DefaultIntegrationWeight,
                    this.volume.GetCurrentWorldToCameraTransform());

                // If camera tracking failed, no data integration or raycast for reference
                // point cloud will have taken place, and the internal camera pose
                // will be unchanged.
                if (!trackingSucceeded)
                {
                    this.trackingErrorCount++;

                    // Show tracking error on status bar
                    this.statusBarText.Text = Properties.Resources.CameraTrackingFailed;
                }
                else
                {
                    Matrix4 calculatedCameraPose = this.volume.GetCurrentWorldToCameraTransform();
                     
                    // Set the camera pose and reset tracking errors
                    this.worldToCameraTransform = calculatedCameraPose;
                    this.trackingErrorCount = 0;
                }

                if (AutoResetReconstructionWhenLost && !trackingSucceeded && this.trackingErrorCount == MaxTrackingErrors)
                {
                    // Auto Reset due to bad tracking
                    this.statusBarText.Text = Properties.Resources.ResetVolume;

                    // Automatically Clear Volume and reset tracking if tracking fails
                    this.ResetReconstruction();
                }

                // Calculate the point cloud
                this.volume.CalculatePointCloud(this.pointCloudBuffer, this.worldToCameraTransform);

                // Shade point cloud and render
                FusionDepthProcessor.ShadePointCloud(
                    this.pointCloudBuffer,
                    this.worldToCameraTransform,
                    this.shadedSurfaceColorFrame,
                    null);

                this.shadedSurfaceColorFrame.CopyPixelDataTo(this.colorPixels);

                // Write the pixel data into our bitmap
                this.colorBitmap.WritePixels(
                    new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
                    this.colorPixels,
                    this.colorBitmap.PixelWidth * sizeof(int),
                    0);

                // The input frame was processed successfully, increase the processed frame count
                ++this.processedFrameCount;
            }
            catch (InvalidOperationException ex)
            {
                this.statusBarText.Text = ex.Message;
            }
            finally
            {
                this.processingFrame = false;
            }
        }

        /// <summary>
        /// Reset the reconstruction to initial value
        /// </summary>
        private void ResetReconstruction()
        {
            // Reset tracking error counter
            this.trackingErrorCount = 0;

            // Set the world-view transform to identity, so the world origin is the initial camera location.
            this.worldToCameraTransform = Matrix4.Identity;

            if (null != this.volume)
            {
                // Translate the reconstruction volume location away from the world origin by an amount equal
                // to the minimum depth threshold. This ensures that some depth signal falls inside the volume.
                // If set false, the default world origin is set to the center of the front face of the 
                // volume, which has the effect of locating the volume directly in front of the initial camera
                // position with the +Z axis into the volume along the initial camera direction of view.
                if (this.translateResetPoseByMinDepthThreshold)
                {
                    Matrix4 worldToVolumeTransform = this.defaultWorldToVolumeTransform;

                    // Translate the volume in the Z axis by the minDepthThreshold distance
                    float minDist = (this.minDepthClip < this.maxDepthClip) ? this.minDepthClip : this.maxDepthClip;
                    worldToVolumeTransform.M43 -= minDist * VoxelsPerMeter;

                    this.volume.ResetReconstruction(this.worldToCameraTransform, worldToVolumeTransform); 
                }
                else
                {
                    this.volume.ResetReconstruction(this.worldToCameraTransform);
                }
            }

            if (null != this.fpsTimer)
            {
                // Reset the processed frame count and reset the FPS timer
                this.fpsTimer.Stop();

                this.processedFrameCount = 0;
                this.fpsTimer.Start();
            }
        }

        /// <summary>
        /// Handles the user clicking on the reset reconstruction button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void ButtonResetReconstructionClick(object sender, RoutedEventArgs e)
        {
            if (null == this.sensor)
            {
                this.statusBarText.Text = Properties.Resources.ConnectDeviceFirst;
                return;
            }

            // reset the reconstruction and update the status text
            this.ResetReconstruction();
            this.statusBarText.Text = Properties.Resources.ResetReconstruction;
        }

        /// <summary>
        /// Handles the checking or un-checking of the near mode combo box
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void CheckBoxNearModeChanged(object sender, RoutedEventArgs e)
        {
            if (this.sensor != null)
            {
                // will not function on non-Kinect for Windows devices
                try
                {
                    if (this.checkBoxNearMode.IsChecked.GetValueOrDefault())
                    {
                        this.sensor.DepthStream.Range = DepthRange.Near;
                    }
                    else
                    {
                        this.sensor.DepthStream.Range = DepthRange.Default;
                    }
                }
                catch (InvalidOperationException)
                {
                }
            }
        }
    }
}
