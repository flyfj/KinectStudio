

using System.IO;
using System;
using System.Xml;
using System.Globalization;
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

    /// <summary>
    /// notifier class used to notify ui when data is updated
    /// </summary>
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
    /// used to fetch data and update UI
    /// </summary>
    public class KinectDataManager
    {

        // sensor reference
        public KinectSensor sensor_ref;

        // tool object
        //public KinectRecorder recorder;

        // gesture data: DUMMY
        public List<Skeleton> gesture = new List<Skeleton>();

        // visualization data
        public WriteableBitmap ColorStreamBitmap;
        public WriteableBitmap DepthStreamBitmap;

        // intermediate storage for color image pixel data
        private byte[] colorPixelData;
        private byte[] depthPixelData;

        // Intermediate storage for the depth data received from the camera
        public DepthImagePixel[] depthPixels;

        // current skeleton data
        public Skeleton[] skeletons;

        // current joint status dictionary
        public Dictionary<JointType, JointStatus> cur_joint_status;
        public bool ifShowJointStatus = false;


        /// <summary>
        /// Skeleton drawing params
        /// </summary>
        private const float RenderWidth = 640.0f;
        private const float RenderHeight = 480.0f;

        private const double JointThickness = 3;
        private const double BodyCenterThickness = 10;
        private const double ClipBoundsThickness = 10;

        private readonly Brush centerPointBrush = Brushes.Blue;
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Drawing group for skeleton rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        public DrawingImage skeletonImageSource;



        public KinectDataManager(ref KinectSensor sensor)
        {
            if(sensor == null)
            {
                throw new System.ArgumentException("Sensor cannot be null");
            }

            sensor_ref = sensor;

            // Create the drawing group we'll use for drawing skeleton
            this.drawingGroup = new DrawingGroup();

            // Create an skeleton image source that we can use in our image control
            this.skeletonImageSource = new DrawingImage(this.drawingGroup);
        }



#region visualization_functions

        public void UpdateColorData(ColorImageFrame frame)
        {
            // update property value
            if (colorPixelData == null)
            {
                colorPixelData = new byte[frame.PixelDataLength];    // always BGR32
            }

            frame.CopyPixelDataTo(colorPixelData);

            if (ColorStreamBitmap == null)
            {
                ColorStreamBitmap = new WriteableBitmap(frame.Width, frame.Height, 96, 96,
                    PixelFormats.Bgr32, null);
            }

            int stride = ColorStreamBitmap.PixelWidth * sizeof(int);
            Int32Rect drawRect = new Int32Rect(0, 0, ColorStreamBitmap.PixelWidth, ColorStreamBitmap.PixelHeight);
            ColorStreamBitmap.WritePixels(drawRect, colorPixelData, stride, 0);
        }

        public void UpdateDepthData(DepthImageFrame frame)
        {

            if (depthPixelData == null)
            {
                depthPixelData = new byte[frame.Width * frame.Height * sizeof(int)];    // always BGR32
            }

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
                // Get the depth for this pixel (millimeter)
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
                depthPixelData[colorPixelIndex++] = intensity;

                // Write out green byte
                depthPixelData[colorPixelIndex++] = intensity;

                // Write out red byte                        
                depthPixelData[colorPixelIndex++] = intensity;

                // We're outputting BGR, the last byte in the 32 bits is unused so skip it
                // If we were outputting BGRA, we would write alpha here.
                ++colorPixelIndex;
            }


            if (DepthStreamBitmap == null)
            {
                DepthStreamBitmap = new WriteableBitmap(frame.Width, frame.Height, 96, 96,
                    PixelFormats.Bgr32, null);
            }

            // write to bitmap
            int stride = frame.Width * sizeof(int);
            Int32Rect drawRect = new Int32Rect(0, 0, frame.Width, frame.Height);
            DepthStreamBitmap.WritePixels(drawRect, depthPixelData, stride, 0);
        }

        public void UpdateSkeletonData(SkeletonFrame frame, bool ifRecording = false)
        {
            // get skeleton data
            skeletons = new Skeleton[frame.SkeletonArrayLength];
            frame.CopySkeletonDataTo(skeletons);

            UpdateSkeletonData(skeletons);
        }

        public void UpdateSkeletonData(Skeleton[] skeletons)
        {
            if (skeletons == null)
                return;

            // draw skeletons
            using (DrawingContext dc = this.drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        RenderClippedEdges(skel, dc);

                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            this.DrawBonesAndJoints(skel, dc);
                        }
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawEllipse(
                            this.centerPointBrush,
                            null,
                            this.SkeletonPointToScreen(skel.Position),
                            BodyCenterThickness,
                            BodyCenterThickness);
                        }
                    }
                }

                // prevent drawing outside of our render area
                this.drawingGroup.ClipGeometry =
                    new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
        }

        public void UpdateSkeletonData(Skeleton ske)
        {
            if (ske == null)
                return;

            // draw skeletons
            using (DrawingContext dc = this.drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));
                
                RenderClippedEdges(ske, dc);

                if (ske.TrackingState == SkeletonTrackingState.Tracked)
                {
                    this.DrawBonesAndJoints(ske, dc);
                }
                else if (ske.TrackingState == SkeletonTrackingState.PositionOnly)
                {
                    dc.DrawEllipse(
                    this.centerPointBrush,
                    null,
                    this.SkeletonPointToScreen(ske.Position),
                    BodyCenterThickness,
                    BodyCenterThickness);
                }

                // prevent drawing outside of our render area
                this.drawingGroup.ClipGeometry =
                    new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
        }

        #region Skeleton_drawing_functions

        /// <summary>
        /// Draws indicators to show which edges are clipping skeleton data
        /// </summary>
        private static void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
        {
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, RenderHeight));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(RenderWidth - ClipBoundsThickness, 0, ClipBoundsThickness, RenderHeight));
            }
        }

        /// <summary>
        /// Draws a skeleton's bones and joints
        /// </summary>
        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
            // Render Torso
            this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            // Left Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);

            // Render Joints
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    Point joint2DPos = SkeletonPointToScreen(joint.Position);
                    drawingContext.DrawEllipse(
                        drawBrush, null, joint2DPos, 
                        JointThickness, JointThickness);

                    // draw status
                    if (ifShowJointStatus && cur_joint_status != null)
                    {
                        //joint.JointType == JointType.ElbowLeft ||
                        //    joint.JointType == JointType.ElbowRight ||
                        //    joint.JointType == JointType.KneeLeft ||
                        //    joint.JointType == JointType.KneeRight ||
                        //    joint.JointType == JointType.Spine ||
                        // selectively draw joint status
                        if (
                            joint.JointType == JointType.ShoulderLeft)
                        {
                            FormattedText formattedText = new FormattedText(
                            cur_joint_status[joint.JointType].abs_speed.ToString("F2") + "m/s\n" +
                            cur_joint_status[joint.JointType].angle.ToString("F2") + "°",
                            CultureInfo.GetCultureInfo("en-us"),
                            FlowDirection.LeftToRight,
                            new Typeface("Verdana"),
                            20,
                            Brushes.Yellow);

                            drawingContext.DrawText(formattedText, joint2DPos);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        /// </summary>
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = sensor_ref.CoordinateMapper.MapSkeletonPointToDepthPoint(
                skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        /// <summary>
        /// Draws a bone line between two joints
        /// </summary>
        /// <param name="jointType0">joint to start drawing from</param>
        /// <param name="jointType1">joint to end drawing at</param>
        private void DrawBone(
            Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = this.trackedBonePen;
            }

            drawingContext.DrawLine(drawPen,
                SkeletonPointToScreen(joint0.Position),
                SkeletonPointToScreen(joint1.Position));
        }

        #endregion

#endregion
        

#region data_recording_and_retreival

        public bool SaveKinectData(object data, string path, string type)
        {
            if (type == "COLOR" && data != null)    // save color image
            {
                WriteableBitmap img = data as WriteableBitmap;

                // create a png bitmap encoder which knows how to save a .png file
                BitmapEncoder encoder = new PngBitmapEncoder();

                // create frame from the writable bitmap and add to encoder
                encoder.Frames.Add(BitmapFrame.Create(img));

                // write the new file to disk
                try
                {
                    using (FileStream fs = new FileStream(path, FileMode.Create))
                    {
                        encoder.Save(fs);
                    }
                }
                catch (IOException)
                {
                    throw new ArgumentException("Error saving file to: " + path);
                }

            }
            if (type == "DEPTH" && data != null)    // save depth data to text file
            {
                DepthImagePixel[] dpixels = data as DepthImagePixel[];

                // save depth value
                FileInfo t = new FileInfo(path);
                StreamWriter writer = t.CreateText();
                for (int i = 0; i < dpixels.Length; i++)
                {
                    writer.Write(dpixels[i].Depth);
                    writer.Write(" ");
                }

                writer.Close();

            }
            if (type == "SKELETON" && data != null) // save skeleton data to text file
            {
                Skeleton[] skeletons = data as Skeleton[];

                FileInfo t = new FileInfo(path);
                StreamWriter writer = t.CreateText();
                writer.Write("Tracking id # Tracking state (tracked-joints/position_only) # Joints number # ");
                writer.WriteLine("Joint0 type # Joint0 state # Joint0 point # ...\n");
                for (int i = 0; i < skeletons.Length; i++)
                {
                    writer.WriteLine(skeletons[i].TrackingId + " " +
                    skeletons[i].TrackingState + " " + skeletons[i].Joints.Count);

                    if (skeletons[i].TrackingState == SkeletonTrackingState.Tracked)
                    {
                        // print all joint data
                        foreach (Joint joint in skeletons[i].Joints)
                        {
                            writer.WriteLine(joint.JointType + " " + joint.Position.X + " "
                                + joint.Position.Y + " " + joint.Position.Z);
                        }
                    }
                    else if (skeletons[i].TrackingState == SkeletonTrackingState.PositionOnly)
                    {
                        writer.WriteLine(skeletons[i].Position.X + " " +
                            skeletons[i].Position.Y + " " + skeletons[i].Position.Z);
                    }

                }

                writer.Close();

            }

            return true;
        }

#endregion


        
    }
}
