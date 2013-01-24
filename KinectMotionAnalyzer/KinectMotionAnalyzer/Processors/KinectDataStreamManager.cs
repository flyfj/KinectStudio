using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using Microsoft.Kinect;
using System.Linq.Expressions;


namespace KinectMotionAnalyzer.Processors
{

    public abstract class Notifier: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged<T>(Expression<Func<T>> propertyExpression)
        {
            if (propertyExpression == null)
                return;
 
            var body = propertyExpression.Body as MemberExpression;
            if (body == null)
                return;

            string propertyname = body.Member.Name;
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyname));
            }

        }

    }

    /// <summary>
    /// used for fetch data and update UI
    /// </summary>
    public class KinectDataStreamManager: Notifier
    {
        // data property
        public WriteableBitmap StreamDataBitmap;

        // intermediate storage for color image pixel data
        private byte[] colorPixelData;

        // Intermediate storage for the depth data received from the camera
        private DepthImagePixel[] depthPixels;

        public void UpdateColorData(ColorImageFrame frame)
        {
            // update property value
            if (colorPixelData == null)
            {
                colorPixelData = new byte[frame.PixelDataLength];
            }

            frame.CopyPixelDataTo(colorPixelData);

            if (StreamDataBitmap == null)
            {
                StreamDataBitmap = new WriteableBitmap(frame.Width, frame.Height, 96, 96,
                    PixelFormats.Bgr32, null);
            }

            int stride = frame.Width * sizeof(int);
            Int32Rect drawRect = new Int32Rect(0, 0, frame.Width, frame.Height);
            StreamDataBitmap.WritePixels(drawRect, colorPixelData, stride, 0);

            // notify...
            RaisePropertyChanged(() => StreamDataBitmap);

        }

        public void UpdateDepthData(DepthImageFrame frame)
        {
            if (depthPixels == null)
            {
                depthPixels = new DepthImagePixel[frame.PixelDataLength];
            }

            frame.CopyDepthImagePixelDataTo(depthPixels);

            // Get the min and max reliable depth for the current frame
            int minDepth = frame.MinDepth;
            int maxDepth = frame.MaxDepth;

            // Convert the depth to RGB
            int colorPixelIndex = 0;
            for (int i = 0; i < this.depthPixels.Length; ++i)
            {
                // Get the depth for this pixel
                short depth = depthPixels[i].Depth;

                // To convert to a byte, we're discarding the most-significant
                // rather than least-significant bits.
                // We're preserving detail, although the intensity will "wrap."
                // Values outside the reliable depth range are mapped to 0 (black).

                // Note: Using conditionals in this loop could degrade performance.
                // Consider using a lookup table instead when writing production code.
                // See the KinectDepthViewer class used by the KinectExplorer sample
                // for a lookup table example.
                byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);

                // Write out blue byte
                colorPixelData[colorPixelIndex++] = intensity;

                // Write out green byte
                colorPixelData[colorPixelIndex++] = intensity;

                // Write out red byte                        
                colorPixelData[colorPixelIndex++] = intensity;

                // We're outputting BGR, the last byte in the 32 bits is unused so skip it
                // If we were outputting BGRA, we would write alpha here.
                ++colorPixelIndex;
            }


            if (StreamDataBitmap == null)
            {
                StreamDataBitmap = new WriteableBitmap(frame.Width, frame.Height, 96, 96,
                    PixelFormats.Bgr32, null);
            }

            // write to bitmap
            int stride = frame.Width * sizeof(int);
            Int32Rect drawRect = new Int32Rect(0, 0, frame.Width, frame.Height);
            StreamDataBitmap.WritePixels(drawRect, colorPixelData, stride, 0);
        }

        public void UpdateSkeletonData(SkeletonFrame frame)
        {

        }



    }
}
