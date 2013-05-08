using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Microsoft.Kinect;

namespace KinectMotionAnalyzer.Processors
{
    public class BarbellAssess
    {


        /// <summary>
        /// determine the frames at which maximum and minimum height of barbell occur, their max and min Y values, and those frames' associated skeletons
        /// </summary>
        static public TopBottomPoints DetermineTopandBottom(List<Skeleton> data)
        {
            Skeleton[] skeldata = data.ToArray();
            TopBottomPoints topbottompoints = new TopBottomPoints(skeldata);

            for (int i = 0; i < skeldata.Length; i++)
            {
                Skeleton skeleton = skeldata[i];
                float leftwristY = skeleton.Joints[JointType.WristLeft].Position.Y;
                float rightwristY = skeleton.Joints[JointType.WristRight].Position.Y;
                float avewristdist = (leftwristY + rightwristY) / 2;  //compute average Y-value for this frames left and right wrist positions
                if (avewristdist > topbottompoints.TopY)  //determine maximum wrist Y-value in all frames
                {
                    topbottompoints.TopY = avewristdist;
                    topbottompoints.TopFrame = i;
                }
                if (avewristdist < topbottompoints.BottomY)  //determine minimum wrist Y-value in all frames
                {
                    topbottompoints.BottomY = avewristdist;
                    topbottompoints.BottomFrame = i;
                }

            }
            topbottompoints.BottomSkeleton = skeldata[topbottompoints.BottomFrame];
            topbottompoints.TopSkeleton = skeldata[topbottompoints.TopFrame];
            return topbottompoints;
        }

        /// <summary>
        /// a class used in various functions that contains the skeleton and Y-value info for the top and bottom Y-value frames along with the frame #'s
        /// </summary>
        public class TopBottomPoints
        {
            public float TopY;
            public int TopFrame;  //frame at which Y-value was highest, representing the peak point of exercise
            public Skeleton TopSkeleton;
            public float BottomY;
            public int BottomFrame;  //frame at which Y-value was lowest, representing the low point of exercise at beginning or end
            public Skeleton BottomSkeleton;
            int NumberOfFrames;

            public TopBottomPoints(Skeleton[] skelarray)
            {
                TopY = float.MinValue;
                TopFrame = 0;
                BottomY = float.MaxValue;
                BottomFrame = 0;
                NumberOfFrames = skelarray.Length;
            }
        }

        /// <summary>
        /// determine if user did not bring barbell up high enough
        /// </summary>
        static public ExerciseError GetTopError(TopBottomPoints topbottompoints)
        {
            ExerciseError ErrorTop = new ExerciseError("Make sure you bring the bar all the way up.");  //initialize ExerciseError with this problem's advice

            if (topbottompoints.TopY < topbottompoints.TopSkeleton.Joints[JointType.ShoulderLeft].Position.Y)
            {
                ErrorTop.WasError = true;  //if user didn't bring bar up to shoulder, they made an error  
            }
            return ErrorTop;
        }

        /// <summary>
        /// determine if user did not allow barbell to go low enough
        /// </summary>
        static public ExerciseError GetBottomError(TopBottomPoints topbottompoints)
        {
            ExerciseError ErrorBottom = new ExerciseError("Make sure you allow the bar to go all the way down to your starting position.");  //initialize ExerciseError with this problem's advice

            if (topbottompoints.BottomY > topbottompoints.TopSkeleton.Joints[JointType.HipCenter].Position.Y)
            {
                ErrorBottom.WasError = true;  //if user didn't bring bar below hip, they made an error
            }
            return ErrorBottom;
        }

        /// <summary>
        /// determine if user was too fast or too slow on the uptake motion of exercise
        /// </summary>
        static public ExerciseError GetSpeedErrorUptake(TopBottomPoints topbottompoints)
        {

            ExerciseError SpeedUp = new ExerciseError("Try to speed up a bit when pulling the weight up towards you.  If this is too difficult, try lowering the amount of weight you are using.");
            ExerciseError SlowDown = new ExerciseError("Try to slow down a bit when pulling the weight up towards you.");  //initialize ExerciseError with this problem's advice

            int SlowFrameAmount = 40;  //maximum allowed frame length for uptake of exercise
            int FastFrameAmount = 20;  //minimum allowed frame length for uptake exercise
            int upframeamount = topbottompoints.TopFrame - topbottompoints.BottomFrame;
            if (upframeamount < FastFrameAmount)
            {
                SpeedUp.WasError = true;  //if user's uptake motion had less frames than allowed, they went too fast
                return SpeedUp;
            }
            else if (upframeamount > SlowFrameAmount)
            {
                SlowDown.WasError = true;  //if user's uptake motion had more frames than allowed, they went too slow
                return SlowDown;
            }
            else
            {
                SpeedUp.WasError = false;  //otherwise, user made no uptake speed error
                return SpeedUp;
            }

        }

        /// <summary>
        /// determine if user was too fast or too slow on the downtake motion of exercise
        /// </summary>
        static public ExerciseError GetSpeedErrorDowntake(TopBottomPoints topbottompoints, int skellength)
        {

            ExerciseError SpeedUp = new ExerciseError("Try to speed up a bit when letting the weight back down");  //initialize ExerciseError with this problem's advice
            ExerciseError SlowDown = new ExerciseError("Try to slow down a bit when letting the weight back down.  If this is too difficult, try lowering the amount of weight you are using.");
            int SlowFrameAmount = 40;  //maximum allowed frame length for exercise
            int FastFrameAmount = 20;  //minimum allowed frame length for exercise
            int downframeamount = skellength - topbottompoints.TopFrame;

            if (downframeamount < FastFrameAmount)
            {
                SpeedUp.WasError = true;  //if user's downtake motion had less frames than allowed, they went too fast
                return SpeedUp;
            }
            else if (downframeamount > SlowFrameAmount)
            {
                SlowDown.WasError = true;  //if user's downtake motion had more frames than allowed, they went too slow
                return SlowDown;
            }
            else
            {
                SpeedUp.WasError = false;  //otherwise, user made no downtake speed error
                return SpeedUp;
            }
        }

        /// <summary>
        /// determine if user was too fast or too slow on the downtake motion of exercise
        /// </summary>
        static public ExerciseError GetBodyRockError(Skeleton[] skelarray)
        {
            float MaxAllowedZDistance = 1.0F;  // maximum amount of Z-coord difference between Hip and Shoulder points
            ExerciseError BodyRockError = new ExerciseError("Keep your back straight while performing exercise");  //initialize ExerciseError with this problem's advice
            float MaxMeasuredZDistance = 0;
            for (int i = 0; i < skelarray.Length; i++)
            {
                float ShoulderHipZDistance = skelarray[i].Joints[JointType.ShoulderCenter].Position.Z - skelarray[i].Joints[JointType.HipCenter].Position.Z;
                if (ShoulderHipZDistance > MaxMeasuredZDistance)
                {
                    MaxMeasuredZDistance = ShoulderHipZDistance;  //determine maximum amount of Z-coord difference between Hip and Shoulder points in all frames
                }
            }
            if (MaxMeasuredZDistance > MaxAllowedZDistance)
            {
                BodyRockError.WasError = true;  //if user's Hip to Shoulder Z-coord difference is too off, they made an error
            }
            return BodyRockError;
        }


        /// <summary>
        /// class that represent if an error took place, and the advice associated with that error
        /// </summary>
        public class ExerciseError
        {
            public Boolean WasError;
            public String ErrorAdvice;

            public ExerciseError(String advice)  //initialize a new ExerciseError object with the advice for that particular error
            {
                WasError = false;  //each error starts off as not having occured, specific error functions do checks and change this if error occured
                ErrorAdvice = advice;
            }
        }
    }
}
