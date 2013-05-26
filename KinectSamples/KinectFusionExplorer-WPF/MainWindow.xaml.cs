using System.Drawing;
//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.KinectFusionExplorer
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Globalization;
    using System.IO;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Threading;
    using Microsoft.Kinect;
    using Microsoft.Kinect.Toolkit;
    using Microsoft.Kinect.Toolkit.Fusion;

    /// <summary>
    /// A struct containing depth image pixels and frame timestamp
    /// </summary>
    internal struct DepthData
    {
        public DepthImagePixel[] DepthImagePixels;
        public long FrameTimestamp;
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged, IDisposable
    {
        #region Constants

        /// <summary>
        /// Max tracking error count, will reset the reconstruction if tracking errors
        /// reach the number
        /// </summary>
        private const int MaxTrackingErrors = 100;

        /// <summary>
        /// Time threshold to reset the reconstruction if tracking can't be restored within it.
        /// This value is valid if GPU is used
        /// </summary>
        private const int ResetOnTimeStampSkippedMillisecondsGPU = 1000;

        /// <summary>
        /// Time threshold to reset the reconstruction if tracking can't be restored within it.
        /// This value is valid if CPU is used
        /// </summary>
        private const int ResetOnTimeStampSkippedMillisecondsCPU = 6000;

        /// <summary>
        /// If set true, will automatically reset the reconstruction when MaxTrackingErrors have occurred
        /// </summary>
        private const bool AutoResetReconstructionWhenLost = false;

        /// <summary>
        /// Event interval for FPS timer
        /// </summary>
        private const int FpsInterval = 5;

        /// <summary>
        /// The reconstruction volume processor type. This parameter sets whether AMP or CPU processing
        /// is used. Note that CPU processing will likely be too slow for real-time processing.
        /// </summary>
        private const ReconstructionProcessor ProcessorType = ReconstructionProcessor.Amp;

        /// <summary>
        /// Format of depth image to use
        /// </summary>
        private const DepthImageFormat ImageFormat = DepthImageFormat.Resolution640x480Fps30;

        #endregion

        #region Fields

        /// <summary>
        /// Track whether Dispose has been called
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Saving mesh flag
        /// </summary>
        private bool savingMesh;

        /// <summary>
        /// Image width of depth frame
        /// </summary>
        private int width = 0;

        /// <summary>
        /// Image height of depth frame
        /// </summary>
        private int height = 0;

        /// <summary>
        /// The counter for image process failures
        /// </summary>
        private int trackingErrorCount = 0;

        /// <summary>
        /// The counter for frames that have been processed
        /// </summary>
        private int processedFrameCount = 0;

        /// <summary>
        /// Timestamp of last depth frame in milliseconds
        /// </summary>
        private long lastFrameTimestamp = 0;

        /// <summary>
        /// Timer to count FPS
        /// </summary>
        private DispatcherTimer fpsTimer;

        /// <summary>
        /// Timer stamp of last computation of FPS
        /// </summary>
        private DateTime lastFPSTimestamp;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Kinect sensor chooser object
        /// </summary>
        private KinectSensorChooser sensorChooser;

        /// <summary>
        /// The Kinect Fusion volume
        /// </summary>
        private Reconstruction volume;

        /// <summary>
        /// Intermediate storage for the depth float data converted from depth image frame
        /// </summary>
        private FusionFloatImageFrame depthFloatFrame;

        /// <summary>
        /// Per-pixel alignment values
        /// </summary>
        private FusionFloatImageFrame deltaFromReferenceFrame;

        /// <summary>
        /// minT alignment energy for frame
        /// </summary>
        private float alignmentEnergy;

        /// <summary>
        /// Shaded surface frame from shading point cloud frame
        /// </summary>
        private FusionColorImageFrame shadedSurfaceFrame;

        /// <summary>
        /// Shaded surface normals frame from shading point cloud frame
        /// </summary>
        private FusionColorImageFrame shadedSurfaceNormalsFrame;

        /// <summary>
        /// Calculated point cloud frame from image integration
        /// </summary>
        private FusionPointCloudImageFrame pointCloudFrame;

        /// <summary>
        /// Bitmap contains depth float frame data for rendering
        /// </summary>
        private WriteableBitmap depthFloatFrameBitmap;

        //// <summary>
        //// Bitmap contains delta from reference frame data for rendering
        //// </summary>
        private WriteableBitmap deltaFromReferenceFrameBitmap;

        /// <summary>
        /// Bitmap contains shaded surface frame data for rendering
        /// </summary>
        private WriteableBitmap shadedSurfaceFrameBitmap;

        /// <summary>
        /// Pixel buffer of depth float frame with pixel data in float format
        /// </summary>
        private float[] depthFloatFrameDepthPixels;

        /// <summary>
        /// Pixel buffer of delta from reference frame with pixel data in float format
        /// </summary>
        private float[] deltaFromReferenceFrameFloatPixels;

        /// <summary>
        /// Pixel buffer of depth float frame with pixel data in 32bit color
        /// </summary>
        private int[] depthFloatFramePixels;

        //// <summary>
        //// Pixel buffer of delta from reference frame in 32bit color
        //// </summary>
        private int[] deltaFromReferenceFramePixels;

        /// <summary>
        /// Pixels buffer of shaded surface frame in 32bit color
        /// </summary>
        private int[] shadedSurfaceFramePixels;

        /// <summary>
        /// Frame data is being processed
        /// </summary>
        private bool processing = false;

        /// <summary>
        /// The transformation between the world and camera view coordinate system
        /// </summary>
        private Matrix4 worldToCameraTransform;

        /// <summary>
        /// The default transformation between the world and volume coordinate system
        /// </summary>
        private Matrix4 defaultWorldToVolumeTransform;

        /// <summary>
        /// To display shaded surface normals frame instead of shaded surface frame
        /// </summary>
        private bool displayNormals;

        /// <summary>
        /// Pause or resume image integration
        /// </summary>
        private bool pauseIntegration;

        /// <summary>
        /// Depth image is mirrored
        /// </summary>
        private bool mirrorDepth;

        /// <summary>
        /// If near mode is enabled
        /// </summary>
        private bool nearMode;

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
        /// Image integration weight
        /// </summary>
        private short integrationWeight = FusionDepthProcessor.DefaultIntegrationWeight;

        /// <summary>
        /// The reconstruction volume voxel density in voxels per meter (vpm)
        /// 1000mm / 256vpm = ~3.9mm/voxel
        /// </summary>
        private float voxelsPerMeter = 256.0f;

        /// <summary>
        /// The reconstruction volume voxel resolution in the X axis
        /// At a setting of 256vpm the volume is 512 / 256 = 2m wide
        /// </summary>
        private int voxelsX = 512;

        /// <summary>
        /// The reconstruction volume voxel resolution in the Y axis
        /// At a setting of 256vpm the volume is 384 / 256 = 1.5m high
        /// </summary>
        private int voxelsY = 384;

        /// <summary>
        /// The reconstruction volume voxel resolution in the Z axis
        /// At a setting of 256vpm the volume is 512 / 256 = 2m deep
        /// </summary>
        private int voxelsZ = 512;

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

        #endregion

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
        /// Property change event
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #region Binding properties

        /// <summary>
        /// Binding property to check box "Display Surface Normals"
        /// </summary>
        public bool DisplayNormals
        {
            get
            {
                return this.displayNormals;
            }

            set
            {
                this.displayNormals = value;
                if (null != this.PropertyChanged)
                {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("DisplayNormals"));
                }
            }
        }

        /// <summary>
        /// Binding property to check box "Near Mode"
        /// </summary>
        public bool NearMode
        {
            get
            {
                return this.nearMode;
            }

            set
            {
                this.nearMode = value;

                if (null != this.PropertyChanged)
                {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("NearMode"));
                }
            }
        }

        /// <summary>
        /// Binding property to check box "Pause Integration"
        /// </summary>
        public bool PauseIntegration
        {
            get
            {
                return this.pauseIntegration;
            }

            set
            {
                this.pauseIntegration = value;
                if (null != this.PropertyChanged)
                {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("PauseIntegration"));
                }
            }
        }

        /// <summary>
        /// Binding property to check box "Mirror Depth"
        /// </summary>
        public bool MirrorDepth
        {
            get
            {
                return this.mirrorDepth;
            }

            set
            {
                this.mirrorDepth = value;
                if (null != this.PropertyChanged)
                {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("MirrorDepth"));
                }

                this.ResetReconstruction();
            }
        }

        /// <summary>
        /// Binding property to min clip depth slider
        /// </summary>
        public double MinDepthClip
        {
            get
            {
                return (double)this.minDepthClip;
            }

            set
            {
                this.minDepthClip = (float)value;
                if (null != this.PropertyChanged)
                {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("MinDepthClip"));
                }
            }
        }

        /// <summary>
        /// Binding property to max clip depth slider
        /// </summary>
        public double MaxDepthClip
        {
            get
            {
                return (double)this.maxDepthClip;
            }

            set
            {
                this.maxDepthClip = (float)value;
                if (null != this.PropertyChanged)
                {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("MaxDepthClip"));
                }
            }
        }

        /// <summary>
        /// Binding property to integration weight slider
        /// </summary>
        public double IntegrationWeight
        {
            get
            {
                return (double)this.integrationWeight;
            }

            set
            {
                this.integrationWeight = (short)(value + 0.5);
                if (null != this.PropertyChanged)
                {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("IntegrationWeight"));
                }
            }
        }

        /// <summary>
        /// Binding property to voxels per meter slider
        /// </summary>
        public double VoxelsPerMeter
        {
            get
            {
                return (double)this.voxelsPerMeter;
            }

            set
            {
                this.voxelsPerMeter = (float)value;
                if (null != this.PropertyChanged)
                {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("VoxelsPerMeter"));
                }
            }
        }

        /// <summary>
        /// Binding property to X-axis volume resolution slider
        /// </summary>
        public double VoxelsX
        {
            get
            {
                return (double)this.voxelsX;
            }

            set
            {
                this.voxelsX = (int)(value + 0.5);
                if (null != this.PropertyChanged)
                {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("VoxelsX"));
                }
            }
        }

        /// <summary>
        /// Binding property to Y-axis volume resolution slider
        /// </summary>
        public double VoxelsY
        {
            get
            {
                return (double)this.voxelsY;
            }

            set
            {
                this.voxelsY = (int)(value + 0.5);
                if (null != this.PropertyChanged)
                {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("VoxelsY"));
                }
            }
        }

        /// <summary>
        /// Binding property to Z-axis volume resolution slider
        /// </summary>
        public double VoxelsZ
        {
            get
            {
                return (double)this.voxelsZ;
            }

            set
            {
                this.voxelsZ = (int)(value + 0.5);
                if (null != this.PropertyChanged)
                {
                    this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs("VoxelsZ"));
                }
            }
        }
        #endregion

        // added: rgb frame list
        private byte[] curColorData = null;
        private List<byte[]> colorFrames = new List<byte[]>();
        private List<Matrix4> cameraPose = new List<Matrix4>();
        private List<FusionPointCloudImageFrame> pointCloudFrames = new List<FusionPointCloudImageFrame>();

        /// <summary>
        /// Dispose resources
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
                if (disposing)
                {
                    if (null != this.depthFloatFrame)
                    {
                        this.depthFloatFrame.Dispose();
                    }

                    if (null != this.deltaFromReferenceFrame)
                    {
                        this.deltaFromReferenceFrame.Dispose();
                    }

                    if (null != this.shadedSurfaceFrame)
                    {
                        this.shadedSurfaceFrame.Dispose();
                    }

                    if (null != this.shadedSurfaceNormalsFrame)
                    {
                        this.shadedSurfaceNormalsFrame.Dispose();
                    }

                    if (null != this.pointCloudFrame)
                    {
                        this.pointCloudFrame.Dispose();
                    }

                    if (null != this.volume)
                    {
                        this.volume.Dispose();
                    }
                }
            }

            this.disposed = true;
        }

        /// <summary>
        /// Render Fusion color frame to UI
        /// </summary>
        /// <param name="colorFrame">Fusion color frame</param>
        /// <param name="colorPixels">Pixel buffer for fusion color frame</param>
        /// <param name="bitmap">Bitmap contains color frame data for rendering</param>
        /// <param name="image">UI image component to render the color frame</param>
        private static void RenderColorImage(FusionColorImageFrame colorFrame, ref int[] colorPixels, ref WriteableBitmap bitmap, System.Windows.Controls.Image image)
        {
            if (null == colorFrame)
            {
                return;
            }

            if (null == colorPixels || colorFrame.PixelDataLength != colorPixels.Length)
            {
                // Create pixel array of correct format
                colorPixels = new int[colorFrame.PixelDataLength];
            }

            if (null == bitmap || colorFrame.Width != bitmap.Width || colorFrame.Height != bitmap.Height)
            {
                // Create bitmap of correct format
                bitmap = new WriteableBitmap(colorFrame.Width, colorFrame.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

                // Set bitmap as source to UI image object
                image.Source = bitmap;
            }

            // Copy pixel data to pixel buffer
            colorFrame.CopyPixelDataTo(colorPixels);

            // Write pixels to bitmap
            bitmap.WritePixels(
                        new Int32Rect(0, 0, colorFrame.Width, colorFrame.Height),
                        colorPixels,
                        bitmap.PixelWidth * sizeof(int),
                        0);
        }

        /// <summary>
        /// Render Fusion depth float frame to UI
        /// </summary>
        /// <param name="depthFloatFrame">Fusion depth float frame</param>
        /// <param name="depthPixels">Pixel buffer for depth float frame with pixel in depth</param>
        /// <param name="colorPixels">Pixel buffer for depth float frame with pixel in colors</param>
        /// <param name="bitmap">Bitmap contains depth float frame data for rendering</param>
        /// <param name="image">UI image component to render depth float frame to</param>
        private static void RenderDepthFloatImage(FusionFloatImageFrame depthFloatFrame, ref float[] depthPixels, ref int[] colorPixels, ref WriteableBitmap bitmap, System.Windows.Controls.Image image)
        {
            if (null == depthFloatFrame)
            {
                return;
            }

            if (null == depthPixels || depthFloatFrame.PixelDataLength != depthPixels.Length)
            {
                // Create depth pixel array of correct format
                depthPixels = new float[depthFloatFrame.PixelDataLength];
            }

            if (null == colorPixels || depthFloatFrame.PixelDataLength != colorPixels.Length)
            {
                // Create colored pixel array of correct format
                colorPixels = new int[depthFloatFrame.PixelDataLength];
            }

            if (null == bitmap || depthFloatFrame.Width != bitmap.Width || depthFloatFrame.Height != bitmap.Height)
            {
                // Create bitmap of correct format
                bitmap = new WriteableBitmap(depthFloatFrame.Width, depthFloatFrame.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

                // Set bitmap as source to UI image object
                image.Source = bitmap;
            }

            depthFloatFrame.CopyPixelDataTo(depthPixels);

            // Calculate color pixels based on depth of each pixel
            float range = 4.0f;
            float minRange = 0.0f;

            for (int i = 0; i < depthPixels.Length; i++)
            {
                float depth = depthPixels[i];
                int intensity = (depth >= minRange) ? ((int)(((depth - minRange) / range) * 256.0f) % 256) : 0;

                colorPixels[i] = 0;
                colorPixels[i] += intensity; // blue

                intensity *= 256;
                colorPixels[i] += intensity; // green

                intensity *= 256;
                colorPixels[i] += intensity; // red
            }

            // Copy colored pixels to bitmap
            bitmap.WritePixels(
                        new Int32Rect(0, 0, depthFloatFrame.Width, depthFloatFrame.Height),
                        colorPixels,
                        bitmap.PixelWidth * sizeof(int),
                        0);
        }

        /// <summary>
        /// Save mesh in binary .STL file
        /// </summary>
        /// <param name="mesh">Calculated mesh object</param>
        /// <param name="writer">Binary file writer</param>
        /// <param name="flipYZ">Flag to determine whether the Y and Z values are flipped on save</param>
        private static void SaveBinarySTLMesh(Mesh mesh, BinaryWriter writer, bool flipYZ = true)
        {
            var vertices = mesh.GetVertices();
            var normals = mesh.GetNormals();
            var indices = mesh.GetTriangleIndexes();

            // Check mesh arguments
            if (0 == vertices.Count || 0 != vertices.Count % 3 || vertices.Count != indices.Count)
            {
                throw new ArgumentException(Properties.Resources.InvalidMeshArgument);
            }

            char[] header = new char[80];
            writer.Write(header);

            // Write number of triangles
            int triangles = vertices.Count / 3;
            writer.Write(triangles);

            // Sequentially write the normal, 3 vertices of the triangle and attribute, for each triangle
            for (int i = 0; i < triangles; i++)
            {
                // Write normal
                var normal = normals[i * 3];
                writer.Write(normal.X);
                writer.Write(flipYZ ? -normal.Y : normal.Y);
                writer.Write(flipYZ ? -normal.Z : normal.Z);

                // Write vertices
                for (int j = 0; j < 3; j++)
                {
                    var vertex = vertices[(i * 3) + j];
                    writer.Write(vertex.X);
                    writer.Write(flipYZ ? -vertex.Y : vertex.Y);
                    writer.Write(flipYZ ? -vertex.Z : vertex.Z);
                }

                ushort attribute = 0;
                writer.Write(attribute);
            }
        }

        /// <summary>
        /// Save mesh in ASCII Wavefront .OBJ file
        /// </summary>
        /// <param name="mesh">Calculated mesh object</param>
        /// <param name="writer">Stream writer</param>
        /// <param name="flipYZ">Flag to determine whether the Y and Z values are flipped on save</param>
        private static void SaveAsciiObjMesh(Mesh mesh, StreamWriter writer, bool flipYZ = true)
        {
            var vertices = mesh.GetVertices();
            var normals = mesh.GetNormals();
            var indices = mesh.GetTriangleIndexes();

            // Check mesh arguments
            if (0 == vertices.Count || 0 != vertices.Count % 3 || vertices.Count != indices.Count)
            {
                throw new ArgumentException(Properties.Resources.InvalidMeshArgument);
            }

            // Write the header lines
            writer.WriteLine("#");
            writer.WriteLine("# OBJ file created by Microsoft Kinect Fusion");
            writer.WriteLine("#");

            // Sequentially write the 3 vertices of the triangle, for each triangle
            for (int i = 0; i < vertices.Count; i++)
            {
                var vertex = vertices[i];

                string vertexString = "v " + vertex.X.ToString(CultureInfo.CurrentCulture) + " ";
                
                if (flipYZ)
                {
                    vertexString += (-vertex.Y).ToString(CultureInfo.CurrentCulture) + " " + (-vertex.Z).ToString(CultureInfo.CurrentCulture);
                }
                else
                {
                    vertexString += vertex.Y.ToString(CultureInfo.CurrentCulture) + " " + vertex.Z.ToString(CultureInfo.CurrentCulture);
                }

                writer.WriteLine(vertexString);
            }

            // Sequentially write the 3 normals of the triangle, for each triangle
            for (int i = 0; i < normals.Count; i++)
            {
                var normal = normals[i];

                string normalString = "vn " + normal.X.ToString(CultureInfo.CurrentCulture) + " ";
                
                if (flipYZ)
                {
                    normalString += (-normal.Y).ToString(CultureInfo.CurrentCulture) + " " + (-normal.Z).ToString(CultureInfo.CurrentCulture);
                }
                else
                {
                    normalString += normal.Y.ToString(CultureInfo.CurrentCulture) + " " + normal.Z.ToString(CultureInfo.CurrentCulture);
                }

                writer.WriteLine(normalString);
            }

            // Sequentially write the 3 vertex indices of the triangle face, for each triangle
            // Note this is typically 1-indexed in an OBJ file when using absolute referencing!
            for (int i = 0; i < vertices.Count / 3; i++)
            {
                string baseIndex0 = ((i * 3) + 1).ToString(CultureInfo.CurrentCulture);
                string baseIndex1 = ((i * 3) + 2).ToString(CultureInfo.CurrentCulture);
                string baseIndex2 = ((i * 3) + 3).ToString(CultureInfo.CurrentCulture);

                string faceString = "f " + baseIndex0 + "//" + baseIndex0 + " " + baseIndex1 + "//" + baseIndex1 + " " + baseIndex2 + "//" + baseIndex2;
                writer.WriteLine(faceString);
            }
        }

        /// <summary>
        /// Clamp a float value if outside two given thresholds
        /// </summary>summary>
        /// <param name="x">The value to clamp.</param>
        /// <param name="a">The minimum inclusive threshold.</param>
        /// <param name="b">The maximum inclusive threshold.</param>
        /// <returns>Returns the clamped value.</returns>
        private static float ClampFloat(float x, float a, float b)
        {
            if (x < a)
            {
                return a;
            }
            else if (x > b)
            {
                return b;
            }
            else
            {
                return x;
            }
        }

        /// <summary>
        /// Execute startup tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            // Start Kinect sensor chooser
            this.sensorChooser = new KinectSensorChooser();
            this.sensorChooserUI.KinectSensorChooser = this.sensorChooser;
            this.sensorChooser.KinectChanged += this.OnKinectSensorChanged;
            this.sensorChooser.Start();

            // Start fps timer
            this.fpsTimer = new DispatcherTimer(DispatcherPriority.Send);
            this.fpsTimer.Interval = new TimeSpan(0, 0, FpsInterval);
            this.fpsTimer.Tick += this.FpsTimerTick;
            this.fpsTimer.Start();

            // Set last fps timestamp as now
            this.lastFPSTimestamp = DateTime.Now;
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">Object sending the event</param>
        /// <param name="e">Event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Stop timer
            if (null != this.fpsTimer)
            {
                this.fpsTimer.Stop();
                this.fpsTimer.Tick -= this.FpsTimerTick;
            }

            // Unregister Kinect sensor chooser event
            if (null != this.sensorChooser)
            {
                this.sensorChooser.KinectChanged -= this.OnKinectSensorChanged;
            }

            // Stop sensor
            if (null != this.sensor)
            {
                this.sensor.Stop();
                this.sensor.DepthFrameReady -= this.OnDepthFrameReady;
            }
        }

        /// <summary>
        /// Handler function for Kinect changed event
        /// </summary>
        /// <param name="sender">Event generator</param>
        /// <param name="e">Event parameter</param>
        private void OnKinectSensorChanged(object sender, KinectChangedEventArgs e)
        {
            // Check new sensor's status
            if (this.sensor != e.NewSensor)
            {
                // Stop old sensor
                if (null != this.sensor)
                {
                    this.sensor.Stop();
                    this.sensor.DepthFrameReady -= this.OnDepthFrameReady;
                    this.sensor.ColorFrameReady -= this.kinect_colorframe_ready;
                }

                this.sensor = null;

                if (null != e.NewSensor && KinectStatus.Connected == e.NewSensor.Status)
                {
                    // Start new sensor
                    this.sensor = e.NewSensor;
                    this.StartDepthStream(ImageFormat);
                }
            }
        }

        /// <summary>
        /// Handler for FPS timer tick
        /// </summary>
        /// <param name="sender">Object sending the event</param>
        /// <param name="e">Event arguments</param>
        private void FpsTimerTick(object sender, EventArgs e)
        {
            if (!this.savingMesh)
            {
                if (null == this.sensor)
                {
                    // Show "No ready Kinect found!" on status bar
                    this.statusBarText.Text = Properties.Resources.NoReadyKinect;
                }
                else
                {
                    // Calculate time span from last calculation of FPS
                    double intervalSeconds = (DateTime.Now - this.lastFPSTimestamp).TotalSeconds;

                    // Calculate and show fps on status bar
                    this.statusBarText.Text = string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        Properties.Resources.Fps,
                        (double)this.processedFrameCount / intervalSeconds);
                }
            }

            // Reset frame counter
            this.processedFrameCount = 0;
            this.lastFPSTimestamp = DateTime.Now;
        }

        /// <summary>
        /// Reset FPS timer and counter
        /// </summary>
        private void ResetFps()
        {
            // Restart fps timer
            if (null != this.fpsTimer)
            {
                this.fpsTimer.Stop();
                this.fpsTimer.Start();
            }

            // Reset frame counter
            this.processedFrameCount = 0;
            this.lastFPSTimestamp = DateTime.Now;
        }

        /// <summary>
        /// Start depth stream at specific resolution
        /// </summary>
        /// <param name="format">The resolution of image in depth stream</param>
        private void StartDepthStream(DepthImageFormat format)
        {
            try
            {
                // Enable depth stream, register event handler and start
                this.sensor.DepthStream.Enable(format);
                this.sensor.DepthFrameReady += this.OnDepthFrameReady;

                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                this.sensor.ColorFrameReady += this.kinect_colorframe_ready;

                this.sensor.Start();
            }
            catch (IOException ex)
            {
                // Device is in use
                this.sensor = null;
                this.ShowStatusMessage(ex.Message);

                return;
            }
            catch (InvalidOperationException ex)
            {
                // Device is not valid, not supported or hardware feature unavailable
                this.sensor = null;
                this.ShowStatusMessage(ex.Message);

                return;
            }

            // Set Near Mode by default
            try
            {
                this.sensor.DepthStream.Range = DepthRange.Near;
                this.NearMode = true;
            }
            catch (InvalidOperationException)
            {
                this.ShowStatusMessage(Properties.Resources.NearModeNotSupported);
            }

            // Create volume
            if (this.RecreateReconstruction())
            {
                // Show introductory message
                this.ShowStatusMessage(Properties.Resources.IntroductoryMessage);
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's DepthFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void OnDepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            // Open depth frame
            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (null != depthFrame && !this.processing)
                {
                    DepthData depthData = new DepthData();

                    // Save frame timestamp
                    depthData.FrameTimestamp = depthFrame.Timestamp;

                    // Create local depth pixels buffer
                    depthData.DepthImagePixels = new DepthImagePixel[depthFrame.PixelDataLength];

                    // Copy depth pixels to local buffer
                    depthFrame.CopyDepthImagePixelDataTo(depthData.DepthImagePixels);

                    this.width = depthFrame.Width;
                    this.height = depthFrame.Height;

                    // Use dispatcher object to invoke ProcessDepthData function to process
                    this.Dispatcher.BeginInvoke(
                                        DispatcherPriority.Background,
                                        (Action<DepthData>)((d) => { this.ProcessDepthData(d); }),
                                        depthData);

                    // Mark one frame will be processed
                    this.processing = true;
                }
            }
        }

        // ADDED
        private void kinect_colorframe_ready(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame frame = e.OpenColorImageFrame())
            {
                if (frame == null)
                    return;

                curColorData = new byte[frame.PixelDataLength];
                frame.CopyPixelDataTo(curColorData);
            }
        }

        /// <summary>
        /// Process the depth input
        /// </summary>
        /// <param name="depthData">The depth data containing depth pixels and frame timestamp</param>
        private void ProcessDepthData(DepthData depthData)
        {
            try
            {
                if (null != this.volume && !this.savingMesh)
                {
                    // Ensure frame resources are ready
                    this.AllocateFrames();

                    // Check near mode
                    this.CheckNearMode();

                    // To enable playback of a .xed file through Kinect Studio and reset of the reconstruction
                    // if the .xed loops, we test for when the frame timestamp has skipped a large number. 
                    // Note: this will potentially continually reset live reconstructions on slow machines which
                    // cannot process a live frame in less time than the reset threshold. Increase the number of
                    // milliseconds if this is a problem.
                    this.CheckResetTimeStamp(depthData.FrameTimestamp);

                    // Convert depth frame to depth float frame
                    FusionDepthProcessor.DepthToDepthFloatFrame(
                                            depthData.DepthImagePixels,
                                            this.width,
                                            this.height,
                                            this.depthFloatFrame,
                                            this.minDepthClip,
                                            this.maxDepthClip,
                                            this.MirrorDepth);

                    // Render depth float frame
                    RenderDepthFloatImage(this.depthFloatFrame, ref this.depthFloatFrameDepthPixels, ref this.depthFloatFramePixels, ref this.depthFloatFrameBitmap, this.depthFloatImage);

                    // Align new depth float image with reconstruction
                    bool trackingSucceeded = this.volume.AlignDepthFloatToReconstruction(
                        this.depthFloatFrame,
                        FusionDepthProcessor.DefaultAlignIterationCount,
                        this.deltaFromReferenceFrame,
                        out this.alignmentEnergy,
                        this.worldToCameraTransform);

                    bool ifAddedCameraPose = false;

                    if (!trackingSucceeded)
                    {
                        this.trackingErrorCount++;

                        // Show tracking error on status bar
                        this.ShowStatusMessage(Properties.Resources.CameraTrackingFailed);
                    }
                    else
                    {
                        // Get updated camera transform from image alignment
                        Matrix4 calculatedCameraPos = this.volume.GetCurrentWorldToCameraTransform();

                        cameraPose.Add(calculatedCameraPos);
                        ifAddedCameraPose = true;

                        if (curColorData != null)
                            colorFrames.Add(curColorData);

                        // Render delta from reference frame
                        this.RenderAlignDeltasFloatImage(this.deltaFromReferenceFrame, ref this.deltaFromReferenceFrameBitmap, this.deltaFromReferenceImage);

                        // Clear track error count
                        this.trackingErrorCount = 0;

                        this.worldToCameraTransform = calculatedCameraPos;

                        // Integrate the frame to volume
                        if (!this.PauseIntegration)
                        {
                            this.volume.IntegrateFrame(this.depthFloatFrame, this.integrationWeight, this.worldToCameraTransform);
                        }

                    }


                    if (AutoResetReconstructionWhenLost && !trackingSucceeded && this.trackingErrorCount >= MaxTrackingErrors)
                    {
                        // Bad tracking
                        this.ShowStatusMessage(Properties.Resources.ResetVolumeAuto);

                        // Automatically Clear Volume and reset tracking if tracking fails
                        this.ResetReconstruction();
                    }

                    // Calculate the point cloud of integration
                    this.volume.CalculatePointCloud(this.pointCloudFrame, this.worldToCameraTransform);

                    // add to list
                    if (ifAddedCameraPose)
                        pointCloudFrames.Add(pointCloudFrame);

                    // Map X axis to blue channel, Y axis to green channel and Z axiz to red channel,
                    // normalizing each to the range [0, 1].
                    Matrix4 worldToBGRTransform = new Matrix4();
                    worldToBGRTransform.M11 = this.voxelsPerMeter / this.voxelsX;
                    worldToBGRTransform.M22 = this.voxelsPerMeter / this.voxelsY;
                    worldToBGRTransform.M33 = this.voxelsPerMeter / this.voxelsZ;
                    worldToBGRTransform.M41 = 0.5f;
                    worldToBGRTransform.M42 = 0.5f;
                    worldToBGRTransform.M44 = 1.0f;

                    // Shade point cloud frame for rendering
                    FusionDepthProcessor.ShadePointCloud(this.pointCloudFrame, this.worldToCameraTransform, worldToBGRTransform, this.shadedSurfaceFrame, this.shadedSurfaceNormalsFrame);

                    // Render shaded surface frame or shaded surface normals frame
                    RenderColorImage(this.displayNormals ? this.shadedSurfaceNormalsFrame : this.shadedSurfaceFrame, ref this.shadedSurfaceFramePixels, ref this.shadedSurfaceFrameBitmap, this.shadedSurfaceImage);

                    if (trackingSucceeded)
                    {
                        // Increase processed frame counter
                        this.processedFrameCount++;
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                this.ShowStatusMessage(ex.Message);
            }
            finally
            {
                this.processing = false;
            }
        }

        /// <summary>
        /// Render Fusion AlignDepthFloatToReconstruction float deltas frame to UI
        /// </summary>
        /// <param name="alignDeltasFloatFrame">Fusion depth float frame</param>
        /// <param name="bitmap">Bitmap contains float frame data for rendering</param>
        /// <param name="image">UI image component to render float frame to</param>
        private void RenderAlignDeltasFloatImage(FusionFloatImageFrame alignDeltasFloatFrame, ref WriteableBitmap bitmap, System.Windows.Controls.Image image)
        {
            if (null == alignDeltasFloatFrame)
            {
                return;
            }

            if (null == this.deltaFromReferenceFrameFloatPixels || alignDeltasFloatFrame.PixelDataLength != this.deltaFromReferenceFrameFloatPixels.Length)
            {
                // Create depth pixel array of correct format
                this.deltaFromReferenceFrameFloatPixels = new float[alignDeltasFloatFrame.PixelDataLength];
            }

            if (null == this.deltaFromReferenceFramePixels || alignDeltasFloatFrame.PixelDataLength != this.deltaFromReferenceFramePixels.Length)
            {
                // Create colored pixel array of correct format
                this.deltaFromReferenceFramePixels = new int[alignDeltasFloatFrame.PixelDataLength];
            }

            if (null == bitmap || alignDeltasFloatFrame.Width != bitmap.Width || alignDeltasFloatFrame.Height != bitmap.Height)
            {
                // Create bitmap of correct format
                bitmap = new WriteableBitmap(alignDeltasFloatFrame.Width, alignDeltasFloatFrame.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

                // Set bitmap as source to UI image object
                image.Source = bitmap;
            }

            alignDeltasFloatFrame.CopyPixelDataTo(this.deltaFromReferenceFrameFloatPixels);

            Parallel.For(
            0, 
            alignDeltasFloatFrame.Height, 
            y => 
            {
                int index = y * alignDeltasFloatFrame.Width;
                for (int x = 0; x < alignDeltasFloatFrame.Width; ++x, ++index)
                {
                    float residue = this.deltaFromReferenceFrameFloatPixels[index];

                    if (residue < 1.0f)
                    {
                        this.deltaFromReferenceFramePixels[index] = (byte)(255.0f * ClampFloat(1.0f - residue, 0.0f, 1.0f)); // blue
                        this.deltaFromReferenceFramePixels[index] |= ((byte)(255.0f * ClampFloat(1.0f - Math.Abs(residue), 0.0f, 1.0f))) << 8; // green
                        this.deltaFromReferenceFramePixels[index] |= ((byte)(255.0f * ClampFloat(1.0f + residue, 0.0f, 1.0f))) << 16; // red
                    }
                    else
                    {
                        this.deltaFromReferenceFramePixels[index] = 0;
                    }
                }
            });

            // Copy colored pixels to bitmap
            bitmap.WritePixels(
                        new Int32Rect(0, 0, alignDeltasFloatFrame.Width, alignDeltasFloatFrame.Height),
                        this.deltaFromReferenceFramePixels,
                        bitmap.PixelWidth * sizeof(int),
                        0);
        }

        /// <summary>
        /// Allocate the frame buffers used in the process
        /// </summary>
        private void AllocateFrames()
        {
            // Allocate depth float frame
            if (null == this.depthFloatFrame || this.width != this.depthFloatFrame.Width || this.height != this.depthFloatFrame.Height)
            {
                this.depthFloatFrame = new FusionFloatImageFrame(this.width, this.height);
            }

            // Allocate delta from reference frame
            if (null == this.deltaFromReferenceFrame || this.width != this.deltaFromReferenceFrame.Width || this.height != this.deltaFromReferenceFrame.Height)
            {
                this.deltaFromReferenceFrame = new FusionFloatImageFrame(this.width, this.height);
            }

            // Allocate point cloud frame
            if (null == this.pointCloudFrame || this.width != this.pointCloudFrame.Width || this.height != this.pointCloudFrame.Height)
            {
                this.pointCloudFrame = new FusionPointCloudImageFrame(this.width, this.height);
            }

            // Allocate shaded surface frame
            if (null == this.shadedSurfaceFrame || this.width != this.shadedSurfaceFrame.Width || this.height != this.shadedSurfaceFrame.Height)
            {
                this.shadedSurfaceFrame = new FusionColorImageFrame(this.width, this.height);
            }

            // Allocate shaded surface normals frame
            if (null == this.shadedSurfaceNormalsFrame || this.width != this.shadedSurfaceNormalsFrame.Width || this.height != this.shadedSurfaceNormalsFrame.Height)
            {
                this.shadedSurfaceNormalsFrame = new FusionColorImageFrame(this.width, this.height);
            }
        }

        /// <summary>
        /// Check and enable or disable near mode
        /// </summary>
        private void CheckNearMode()
        {
            if (null != this.sensor && this.nearMode != (this.sensor.DepthStream.Range != DepthRange.Default))
            {
                this.sensor.DepthStream.Range = this.nearMode ? DepthRange.Near : DepthRange.Default;
            }
        }

        /// <summary>
        /// Check if the gap between 2 frames has reached reset time threshold. If yes, reset the reconstruction
        /// </summary>
        private void CheckResetTimeStamp(long frameTimestamp)
        {
            if (0 != this.lastFrameTimestamp)
            {
                long timeThreshold = (ReconstructionProcessor.Amp == ProcessorType) ? ResetOnTimeStampSkippedMillisecondsGPU : ResetOnTimeStampSkippedMillisecondsCPU;

                // Calculate skipped milliseconds between 2 frames
                long skippedMilliseconds = Math.Abs(frameTimestamp - this.lastFrameTimestamp);

                if (skippedMilliseconds >= timeThreshold)
                {
                    this.ShowStatusMessage(Properties.Resources.ResetVolume);
                    this.ResetReconstruction();
                }
            }

            // Set timestamp of last frame
            this.lastFrameTimestamp = frameTimestamp;
        }

        /// <summary>
        /// Reset reconstruction object to initial state
        /// </summary>
        private void ResetReconstruction()
        {
            this.cameraPose.Clear();
            this.colorFrames.Clear();

            if (null == this.sensor)
            {
                return;
            }

            // Reset tracking error counter
            this.trackingErrorCount = 0;

            // Set the world-view transform to identity, so the world origin is the initial camera location.
            this.worldToCameraTransform = Matrix4.Identity;

            // Reset volume
            if (null != this.volume)
            {
                try
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
                        worldToVolumeTransform.M43 -= minDist * this.voxelsPerMeter;

                        this.volume.ResetReconstruction(this.worldToCameraTransform, worldToVolumeTransform); 
                    }
                    else
                    {
                        this.volume.ResetReconstruction(this.worldToCameraTransform);
                    }

                    if (this.PauseIntegration)
                    {
                        this.PauseIntegration = false;
                    }
                }
                catch (InvalidOperationException)
                {
                    this.ShowStatusMessage(Properties.Resources.ResetFailed);
                }
            }

            // Reset fps counter
            this.ResetFps();
        }

        /// <summary>
        /// Re-create the reconstruction object
        /// </summary>
        /// <returns>Indicate success or failure</returns>
        private bool RecreateReconstruction()
        {
            // Check if sensor has been initialized
            if (null == this.sensor)
            {
                return false;
            }

            if (null != this.volume)
            {
                this.volume.Dispose();
            }

            try
            {
                // The zero-based GPU index to choose for reconstruction processing if the 
                // ReconstructionProcessor AMP options are selected.
                // Here we automatically choose a device to use for processing by passing -1, 
                int deviceIndex = -1;

                ReconstructionParameters volParam = new ReconstructionParameters(this.voxelsPerMeter, this.voxelsX, this.voxelsY, this.voxelsZ);

                // Set the world-view transform to identity, so the world origin is the initial camera location.
                this.worldToCameraTransform = Matrix4.Identity;

                this.volume = Reconstruction.FusionCreateReconstruction(volParam, ProcessorType, deviceIndex, this.worldToCameraTransform);

                this.defaultWorldToVolumeTransform = this.volume.GetCurrentWorldToVolumeTransform();

                if (this.translateResetPoseByMinDepthThreshold)
                {
                    this.ResetReconstruction();
                }

                // Reset "Pause Integration"
                if (this.PauseIntegration)
                {
                    this.PauseIntegration = false;
                }

                return true;
            }
            catch (ArgumentException)
            {
                this.volume = null;
                this.ShowStatusMessage(Properties.Resources.VolumeResolution);
            }
            catch (InvalidOperationException ex)
            {
                this.volume = null;
                this.ShowStatusMessage(ex.Message);
            }
            catch (DllNotFoundException)
            {
                this.volume = null;
                this.ShowStatusMessage(Properties.Resources.MissingPrerequisite);
            }
            catch (OutOfMemoryException)
            {
                this.volume = null;
                this.ShowStatusMessage(Properties.Resources.OutOfMemory);
            }

            return false;
        }

        /// <summary>
        /// Handler for click event from "Reset Reconstruction" button
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void ResetReconstructionButtonClick(object sender, RoutedEventArgs e)
        {
            if (null == this.sensor)
            {
                return;
            }

            // Reset volume
            this.ResetReconstruction();

            // Update manual reset information to status bar
            this.ShowStatusMessage(Properties.Resources.ResetVolume);
        }

        /// <summary>
        /// Handler for click event from "Create Mesh" button
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void CreateMeshButtonClick(object sender, RoutedEventArgs e)
        {
            if (null == this.volume)
            {
                this.ShowStatusMessage(Properties.Resources.MeshNullVolume);
                return;
            }

            this.savingMesh = true;

            // Mark the start time of saving mesh
            DateTime begining = DateTime.Now;

            try
            {
                this.ShowStatusMessage(Properties.Resources.SavingMesh);

                Mesh mesh = this.volume.CalculateMesh(1);

                Win32.SaveFileDialog dialog = new Win32.SaveFileDialog();

                if (true == this.stlFormat.IsChecked)
                {
                    dialog.FileName = "MeshedReconstruction.stl";
                    dialog.Filter = "STL Mesh Files|*.stl|All Files|*.*";
                }
                else
                {
                    dialog.FileName = "MeshedReconstruction.obj";
                    dialog.Filter = "OBJ Mesh Files|*.obj|All Files|*.*";
                }

                if (true == dialog.ShowDialog())
                {
                    if (true == this.stlFormat.IsChecked)
                    {
                        using (BinaryWriter writer = new BinaryWriter(dialog.OpenFile()))
                        {
                            SaveBinarySTLMesh(mesh, writer);
                        }
                    }
                    else
                    {
                        using (StreamWriter writer = new StreamWriter(dialog.FileName))
                        {
                            SaveAsciiObjMesh(mesh, writer);
                        }
                    }

                    // save color frame and camera pose
                    WriteableBitmap bitmap = new WriteableBitmap(640, 480, 96, 96, PixelFormats.Bgr32, null);
                    int stride = bitmap.PixelWidth * sizeof(int);
                    Int32Rect drawRect = new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight);
                    for (int i = 0; i < colorFrames.Count; i++)
                    {
                        // save color frame
                        string colorfile = dialog.FileName + i.ToString() + ".jpg";
                        bitmap.WritePixels(drawRect, colorFrames[i], stride, 0);

                        JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(bitmap));
                        using (var stream = File.Open(colorfile, FileMode.Create))
                            encoder.Save(stream);

                        // save camera pose file
                        string posefile = dialog.FileName + i.ToString() + ".pose";
                        using (StreamWriter writer = new StreamWriter(posefile))
                        {
                            writer.Write(cameraPose[i].M11 + " " + cameraPose[i].M12 + " " + cameraPose[i].M13 + " " + cameraPose[i].M14 + " " +
                                        cameraPose[i].M21 + " " + cameraPose[i].M22 + " " + cameraPose[i].M23 + " " + cameraPose[i].M24 + " " +
                                        cameraPose[i].M31 + " " + cameraPose[i].M32 + " " + cameraPose[i].M33 + " " + cameraPose[i].M34 + " " +
                                        cameraPose[i].M41 + " " + cameraPose[i].M42 + " " + cameraPose[i].M43 + " " + cameraPose[i].M44 + " ");
                        }

                        // save point cloud file for each time stamp
                        string pcFile = dialog.FileName + i.ToString() + ".pc";
                        using (StreamWriter writer = new StreamWriter(pcFile))
                        {
                            float[] pixelValues = new float[pointCloudFrames[i].PixelDataLength * pointCloudFrames[i].BytesPerPixel / sizeof(float)];
                            pointCloudFrames[i].CopyPixelDataTo(pixelValues);
                            writer.WriteLine(pointCloudFrames[i].Width + " " + pointCloudFrames[i].Height);
                            for (int r = 0; r < pointCloudFrames[i].Height; r++)
                            {
                                for (int c = 0; c < pointCloudFrames[i].Width; c++)
                                {
                                    writer.Write(pixelValues[r * 6 * pointCloudFrames[i].Width + 6 * c] + " ");
                                    writer.Write(pixelValues[r * 6 * pointCloudFrames[i].Width + 6 * c + 1] + " ");
                                    writer.Write(pixelValues[r * 6 * pointCloudFrames[i].Width + 6 * c + 2] + " ");
                                    writer.Write(pixelValues[r * 6 * pointCloudFrames[i].Width + 6 * c + 3] + " ");
                                    writer.Write(pixelValues[r * 6 * pointCloudFrames[i].Width + 6 * c + 4] + " ");
                                    writer.Write(pixelValues[r * 6 * pointCloudFrames[i].Width + 6 * c + 5]);
                                }
                                writer.WriteLine();
                            }
                        }

                    }

                    this.ShowStatusMessage(Properties.Resources.MeshSaved);
                }
                else
                {
                    this.ShowStatusMessage(Properties.Resources.MeshSaveCanceled);
                }
            }
            catch (ArgumentException)
            {
                this.ShowStatusMessage(Properties.Resources.ErrorSaveMesh);
            }
            catch (InvalidOperationException)
            {
                this.ShowStatusMessage(Properties.Resources.ErrorSaveMesh);
            }
            catch (IOException)
            {
                this.ShowStatusMessage(Properties.Resources.ErrorSaveMesh);
            }

            // Update timestamp of last frame to avoid auto reset reconstruction
            this.lastFrameTimestamp += (long)(DateTime.Now - begining).TotalMilliseconds;

            this.savingMesh = false;
        }

        /// <summary>
        /// Handler for volume setting changing event
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event argument</param>
        private void VolumeSettingsChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.RecreateReconstruction();
        }

        /// <summary>
        /// Show exception info on status bar
        /// </summary>
        /// <param name="message">Message to show on status bar</param>
        private void ShowStatusMessage(string message)
        {
            this.Dispatcher.BeginInvoke((Action)(() =>
            {
                this.ResetFps();
                this.statusBarText.Text = message;
            }));
        }
    }

    /// <summary>
    /// Convert depth to UI text
    /// </summary>
    public class DepthToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ((double)value).ToString("0.00", CultureInfo.CurrentCulture) + "m";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
