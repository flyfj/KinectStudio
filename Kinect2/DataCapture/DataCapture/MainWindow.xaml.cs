using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Kinect;
using System.IO;

namespace DataCapture
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private KinectSensor kinectSensor = null;

        private MultiSourceFrameReader multiSourceFrameReader = null;

        private WriteableBitmap rawColorBitmap = null;
        private WriteableBitmap colorBitmap = null;
        private WriteableBitmap depthBitmap = null;
        private FrameDescription colorFrameDescription = null;
        private FrameDescription depthFrameDescription = null;

        private const int MapDepthToByte = 8000 / 256;
        private byte[] depthPixels = null;

        private int frameId = 0;
        private string saveRoot = "N:\\Kinect2Videos\\";
        private List<WriteableBitmap> allColorImgs = new List<WriteableBitmap>();
        private List<WriteableBitmap> allDepthImgs = new List<WriteableBitmap>();

        private enum KinectFrameType
        {
            KinectColorFrame,
            KinectDepthFrame
        }

        public ImageSource RawColorImageSource
        {
            get
            {
                return rawColorBitmap;
            }
        }

        public ImageSource ColorImageSource
        {
            get
            {
                return this.colorBitmap;
            }
        }

        public ImageSource DepthImageSource
        {
            get
            {
                return this.depthBitmap;
            }
        }


        public MainWindow()
        {
            this.kinectSensor = KinectSensor.GetDefault();

            this.multiSourceFrameReader = kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth);

            this.multiSourceFrameReader.MultiSourceFrameArrived += this.MultiSourceFrameArrived;

            // create image data
            colorFrameDescription = kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
            this.rawColorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96, 96, PixelFormats.Bgr32, null);
            depthFrameDescription = kinectSensor.DepthFrameSource.FrameDescription;
            depthPixels = new byte[depthFrameDescription.Width * depthFrameDescription.Height];
            this.depthBitmap = new WriteableBitmap(depthFrameDescription.Width, depthFrameDescription.Height, 96, 96, PixelFormats.Gray8, null);
            this.colorBitmap = new WriteableBitmap(depthFrameDescription.Width, depthFrameDescription.Height, 96, 96, PixelFormats.Bgr32, null);

            kinectSensor.Open();

            this.DataContext = this;

            InitializeComponent();
        }


        private void MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var reference = e.FrameReference.AcquireFrame();

            // process color frame
            using (ColorFrame colorFrame = reference.ColorFrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        this.rawColorBitmap.Lock();

                        // verify data and write the new color frame data to the display bitmap
                        if ((colorFrameDescription.Width == this.rawColorBitmap.PixelWidth) && (colorFrameDescription.Height == this.rawColorBitmap.PixelHeight))
                        {
                            colorFrame.CopyConvertedFrameDataToIntPtr(
                                this.rawColorBitmap.BackBuffer,
                                (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                ColorImageFormat.Bgra);

                            this.rawColorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.rawColorBitmap.PixelWidth, this.rawColorBitmap.PixelHeight));
                        }

                        this.rawColorBitmap.Unlock();
                    }
                }
            }

            // process depth frame
            bool depthFrameProcessed = false;

            // query depth frame
            using (DepthFrame depthFrame = reference.DepthFrameReference.AcquireFrame())
            {
                if (depthFrame != null)
                {
                    // the fastest way to process the body index data is to directly access 
                    // the underlying buffer
                    using (Microsoft.Kinect.KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                    {
                        // verify data and write the color data to the display bitmap
                        if (((this.depthFrameDescription.Width * this.depthFrameDescription.Height) == (depthBuffer.Size / this.depthFrameDescription.BytesPerPixel)) &&
                            (this.depthFrameDescription.Width == this.depthBitmap.PixelWidth) && (this.depthFrameDescription.Height == this.depthBitmap.PixelHeight))
                        {
                            // Note: In order to see the full range of depth (including the less reliable far field depth)
                            // we are setting maxDepth to the extreme potential depth threshold
                            ushort maxDepth = ushort.MaxValue;

                            // If you wish to filter by reliable depth distance, uncomment the following line:
                            //// maxDepth = depthFrame.DepthMaxReliableDistance

                            this.ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size, depthFrame.DepthMinReliableDistance, maxDepth);
                            depthFrameProcessed = true;

                            // align color image
                            ushort[] depthFrameData = new ushort[depthFrameDescription.Width * depthFrameDescription.Height];
                            // align color image
                            depthFrame.CopyFrameDataToArray(depthFrameData);
                            MapColorToDepth(depthFrameData);
                        }
                    }
                }
            }

            if (depthFrameProcessed)
            {
                this.RenderDepthPixels();
            }

            // save frames
            //new Task(() => SaveFrame(string.Format("{0}{1}_color.png", saveRoot, frameId), colorBitmap)).Start();
            //new Task(() => SaveFrame(string.Format("{0}{1}_depth.png", saveRoot, frameId), depthBitmap)).Start();
            allColorImgs.Add(colorBitmap.Clone());
            allDepthImgs.Add(depthBitmap.Clone());

            frameId++;

        }

        /// <summary>
        /// Directly accesses the underlying image buffer of the DepthFrame to 
        /// create a displayable bitmap.
        /// This function requires the /unsafe compiler option as we make use of direct
        /// access to the native memory pointed to by the depthFrameData pointer.
        /// </summary>
        /// <param name="depthFrameData">Pointer to the DepthFrame image data</param>
        /// <param name="depthFrameDataSize">Size of the DepthFrame image data</param>
        /// <param name="minDepth">The minimum reliable depth value for the frame</param>
        /// <param name="maxDepth">The maximum reliable depth value for the frame</param>
        private unsafe void ProcessDepthFrameData(IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth)
        {
            // depth frame data is a 16 bit value
            ushort* frameData = (ushort*)depthFrameData;

            // convert depth to a visual representation
            for (int i = 0; i < (int)(depthFrameDataSize / this.depthFrameDescription.BytesPerPixel); ++i)
            {
                // Get the depth for this pixel
                ushort depth = frameData[i];

                // To convert to a byte, we're mapping the depth value to the byte range.
                // Values outside the reliable depth range are mapped to 0 (black).
                this.depthPixels[i] = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / MapDepthToByte) : 0);
            }
        }

        /// <summary>
        /// Renders color pixels into the writeableBitmap.
        /// </summary>
        private void RenderDepthPixels()
        {
            this.depthBitmap.WritePixels(
                new Int32Rect(0, 0, this.depthBitmap.PixelWidth, this.depthBitmap.PixelHeight),
                this.depthPixels,
                this.depthBitmap.PixelWidth,
                0);
        }

        private void MapColorToDepth(ushort[] depthFrameData)
        {
            if (rawColorBitmap != null)
            {
                var pixelBytes = colorFrameDescription.BytesPerPixel;
                byte[] rawColorPixels = new byte[rawColorBitmap.PixelHeight * rawColorBitmap.PixelWidth * pixelBytes];
                rawColorBitmap.CopyPixels(rawColorPixels, rawColorBitmap.BackBufferStride, 0);
                byte[] colorPixels = new byte[colorBitmap.PixelHeight * colorBitmap.PixelWidth * pixelBytes];
                ColorSpacePoint[] colorPoints = new ColorSpacePoint[depthFrameDescription.Height * depthFrameDescription.Width];
                kinectSensor.CoordinateMapper.MapDepthFrameToColorSpace(depthFrameData, colorPoints);
                for (var id = 0; id < colorPoints.Length; id++)
                {
                    if (colorPoints[id].X < 0 || colorPoints[id].X >= colorFrameDescription.Width ||
                        colorPoints[id].Y < 0 || colorPoints[id].Y >= colorFrameDescription.Height)
                        continue;

                    for (var i = 0; i < pixelBytes; i++)
                        colorPixels[id * pixelBytes + i] =
                            rawColorPixels[(int)colorPoints[id].Y * rawColorBitmap.BackBufferStride + (int)colorPoints[id].X * pixelBytes + i];
                }
                colorBitmap.WritePixels(new Int32Rect(0, 0, colorBitmap.PixelWidth, colorBitmap.PixelHeight),
                    colorPixels, colorBitmap.BackBufferStride, 0);
                colorBitmap.Lock();
                colorBitmap.AddDirtyRect(new Int32Rect(0, 0, colorBitmap.PixelWidth, colorBitmap.PixelHeight));
                colorBitmap.Unlock();
            }
        }

        /// <summary>
        /// release resources when closing the window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.multiSourceFrameReader != null)
            {
                // ColorFrameReder is IDisposable
                this.multiSourceFrameReader.Dispose();
                this.multiSourceFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        private void SaveFrame(string save_fn, WriteableBitmap toSaveImg)
        {
            BitmapEncoder encoder = new PngBitmapEncoder();
            if (toSaveImg != null)
            {
                encoder.Frames.Add(BitmapFrame.Create(toSaveImg));

                // write the new file to disk
                try
                {
                    // FileStream is IDisposable
                    using (FileStream fs = new FileStream(save_fn, FileMode.Create))
                    {
                        encoder.Save(fs);
                    }
                }
                catch (IOException)
                {
                    MessageBox.Show("fail to save color image");
                }
            }
                       
        }

    }
}
