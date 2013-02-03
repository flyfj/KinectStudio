//------------------------------------------------------------------------------
// <copyright file="SkeletonJointsFilterClippedLegs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Avateering.Filters
{
    using Microsoft.Kinect;
    using Microsoft.Xna.Framework;

    /// <summary>
    /// FilterClippedLegs smooths out leg joint positions when the skeleton is clipped
    /// by the bottom of the camera FOV.  Inferred joint positions from the skeletal tracker
    /// can occasionally be noisy or erroneous, based on limited depth image pixels from the
    /// parts of the legs in view.  This filter applies a lot of smoothing using a double
    /// exponential filter, letting through just enough leg movement to show a kick or high step.
    /// Based on the amount of leg that is clipped/inferred, the smoothed data is feathered into the
    /// skeleton output data.
    /// </summary>
    public class SkeletonJointsFilterClippedLegs
    {
        /// <summary>
        /// The blend weights when all leg joints are tracked.
        /// </summary>
        private readonly Vector3 allTracked;

        /// <summary>
        /// The blend weights when the foot is inferred or not tracked.
        /// </summary>
        private readonly Vector3 footInferred;

        /// <summary>
        /// The blend weights when ankle and below are inferred or not tracked.
        /// </summary>
        private readonly Vector3 ankleInferred;

        /// <summary>
        /// The blend weights when knee and below are inferred or not tracked.
        /// </summary>
        private readonly Vector3 kneeInferred;

        /// <summary>
        /// The joint position filter.
        /// </summary>
        private SkeletonJointsPositionDoubleExponentialFilter filterDoubleExp;

        /// <summary>
        /// The timed lerp for the left knee.
        /// </summary>
        private TimedLerp lerpLeftKnee;

        /// <summary>
        /// The timed lerp for the left ankle.
        /// </summary>
        private TimedLerp lerpLeftAnkle;

        /// <summary>
        /// The timed lerp for the left foot.
        /// </summary>
        private TimedLerp lerpLeftFoot;

        /// <summary>
        /// The timed lerp for the right knee.
        /// </summary>
        private TimedLerp lerpRightKnee;

        /// <summary>
        /// The timed lerp for the right ankle.
        /// </summary>
        private TimedLerp lerpRightAnkle;

        /// <summary>
        /// The timed lerp for the right foot.
        /// </summary>
        private TimedLerp lerpRightFoot;

        /// <summary>
        /// The local skeleton with leg filtering applied.
        /// </summary>
        private Skeleton filteredSkeleton;

        /// <summary>
        /// Initializes a new instance of the <see cref="SkeletonJointsFilterClippedLegs"/> class.
        /// </summary>
        public SkeletonJointsFilterClippedLegs()
        {
            this.lerpLeftKnee = new TimedLerp();
            this.lerpLeftAnkle = new TimedLerp();
            this.lerpLeftFoot = new TimedLerp();
            this.lerpRightKnee = new TimedLerp();
            this.lerpRightAnkle = new TimedLerp();
            this.lerpRightFoot = new TimedLerp();

            this.filteredSkeleton = new Skeleton();
            this.filterDoubleExp = new SkeletonJointsPositionDoubleExponentialFilter();

            // knee, ankle, foot blend amounts
            this.allTracked = new Vector3(0.0f, 0.0f, 0.0f); // All joints tracked
            this.footInferred = new Vector3(0.0f, 0.0f, 1.0f); // foot is inferred
            this.ankleInferred = new Vector3(0.5f, 1.0f, 1.0f);  // ankle is inferred
            this.kneeInferred = new Vector3(1.0f, 1.0f, 1.0f);   // knee is inferred

            this.Reset();
        }

        /// <summary>
        /// Name: FilterSkeleton - Implements the per-frame filter logic for the arms up patch.
        /// </summary>
        /// <param name="skeleton">The skeleton to filter.</param>
        /// <param name="deltaNuiTime">Time since the last filter update.</param>
        /// <returns>Returns true if filter runs, false if it did not run.</returns>
        public bool FilterSkeleton(Skeleton skeleton, float deltaNuiTime)
        {
            if (null == skeleton)
            {
                return false;
            }

            // exit early if we lose tracking on the entire skeleton
            if (skeleton.TrackingState != SkeletonTrackingState.Tracked)
            {
                this.filterDoubleExp.Reset();
            }

            KinectHelper.CopySkeleton(skeleton, this.filteredSkeleton);
            this.filterDoubleExp.UpdateFilter(this.filteredSkeleton);

            // Update lerp state with the current delta NUI time.
            this.lerpLeftKnee.Tick(deltaNuiTime);
            this.lerpLeftAnkle.Tick(deltaNuiTime);
            this.lerpLeftFoot.Tick(deltaNuiTime);
            this.lerpRightKnee.Tick(deltaNuiTime);
            this.lerpRightAnkle.Tick(deltaNuiTime);
            this.lerpRightFoot.Tick(deltaNuiTime);

            // Exit early if we do not have a valid body basis - too much of the skeleton is invalid.
            if ((!KinectHelper.IsTracked(skeleton, JointType.HipCenter)) || (!KinectHelper.IsTrackedOrInferred(skeleton, JointType.HipLeft)) || (!KinectHelper.IsTrackedOrInferred(skeleton, JointType.HipRight)))
            {
                return false;
            }

            // Determine if the skeleton is clipped by the bottom of the FOV.
            bool clippedBottom = skeleton.ClippedEdges == FrameEdges.Bottom;

            // Select a mask for the left leg depending on which joints are not tracked.
            // These masks define how much of the filtered joint positions should be blended
            // with the raw positions.  Based on the tracking state of the leg joints, we apply
            // more filtered data as more joints lose tracking.
            Vector3 leftLegMask = this.allTracked;

            if (!KinectHelper.IsTracked(skeleton, JointType.KneeLeft))
            {
                leftLegMask = this.kneeInferred;
            }
            else if (!KinectHelper.IsTracked(skeleton, JointType.AnkleLeft))
            {
                leftLegMask = this.ankleInferred;
            }
            else if (!KinectHelper.IsTracked(skeleton, JointType.FootLeft))
            {
                leftLegMask = this.footInferred;
            }

            // Select a mask for the right leg depending on which joints are not tracked.
            Vector3 rightLegMask = this.allTracked;

            if (!KinectHelper.IsTracked(skeleton, JointType.KneeRight))
            {
                rightLegMask = this.kneeInferred;
            }
            else if (!KinectHelper.IsTracked(skeleton, JointType.AnkleRight))
            {
                rightLegMask = this.ankleInferred;
            }
            else if (!KinectHelper.IsTracked(skeleton, JointType.FootRight))
            {
                rightLegMask = this.footInferred;
            }

            // If the skeleton is not clipped by the bottom of the FOV, cut the filtered data
            // blend in half.
            float clipMask = clippedBottom ? 1.0f : 0.5f;

            // Apply the mask values to the joints of each leg, by placing the mask values into the lerp targets.
            this.lerpLeftKnee.SetEnabled(leftLegMask.X * clipMask);
            this.lerpLeftAnkle.SetEnabled(leftLegMask.Y * clipMask);
            this.lerpLeftFoot.SetEnabled(leftLegMask.Z * clipMask);
            this.lerpRightKnee.SetEnabled(rightLegMask.X * clipMask);
            this.lerpRightAnkle.SetEnabled(rightLegMask.Y * clipMask);
            this.lerpRightFoot.SetEnabled(rightLegMask.Z * clipMask);

            // The bSkeletonUpdated flag tracks whether we have modified the output skeleton or not.
            bool skeletonUpdated = false;

            // Apply lerp to the left knee, which will blend the raw joint position with the filtered joint position based on the current lerp value.
            if (this.lerpLeftKnee.IsLerpEnabled())
            {
                KinectHelper.LerpAndApply(skeleton, JointType.KneeLeft, KinectHelper.SkeletonPointToVector3(this.filteredSkeleton.Joints[JointType.KneeLeft].Position), this.lerpLeftKnee.SmoothValue, JointTrackingState.Tracked);
                skeletonUpdated = true;
            }

            // Apply lerp to the left ankle.
            if (this.lerpLeftAnkle.IsLerpEnabled())
            {
                KinectHelper.LerpAndApply(skeleton, JointType.AnkleLeft, KinectHelper.SkeletonPointToVector3(this.filteredSkeleton.Joints[JointType.AnkleLeft].Position), this.lerpLeftAnkle.SmoothValue, JointTrackingState.Tracked);
                skeletonUpdated = true;
            }

            // Apply lerp to the left foot.
            if (this.lerpLeftFoot.IsLerpEnabled())
            {
                KinectHelper.LerpAndApply(skeleton, JointType.FootLeft, KinectHelper.SkeletonPointToVector3(this.filteredSkeleton.Joints[JointType.FootLeft].Position), this.lerpLeftFoot.SmoothValue, JointTrackingState.Inferred);
                skeletonUpdated = true;
            }

            // Apply lerp to the right knee.
            if (this.lerpRightKnee.IsLerpEnabled())
            {
                KinectHelper.LerpAndApply(skeleton, JointType.KneeRight, KinectHelper.SkeletonPointToVector3(this.filteredSkeleton.Joints[JointType.KneeRight].Position), this.lerpRightKnee.SmoothValue, JointTrackingState.Tracked);
                skeletonUpdated = true;
            }

            // Apply lerp to the right ankle.
            if (this.lerpRightAnkle.IsLerpEnabled())
            {
                KinectHelper.LerpAndApply(skeleton, JointType.AnkleRight, KinectHelper.SkeletonPointToVector3(this.filteredSkeleton.Joints[JointType.AnkleRight].Position), this.lerpRightAnkle.SmoothValue, JointTrackingState.Tracked);
                skeletonUpdated = true;
            }

            // Apply lerp to the right foot.
            if (this.lerpRightFoot.IsLerpEnabled())
            {
                KinectHelper.LerpAndApply(skeleton, JointType.FootRight, KinectHelper.SkeletonPointToVector3(this.filteredSkeleton.Joints[JointType.FootRight].Position), this.lerpRightFoot.SmoothValue, JointTrackingState.Inferred);
                skeletonUpdated = true;
            }

            return skeletonUpdated;
        }

        /// <summary>
        /// Resets filter state to defaults.
        /// </summary>
        public void Reset()
        {
            // set up a really floaty double exponential filter - we want maximum smoothness
            this.filterDoubleExp.Init(0.5f, 0.3f, 1.0f, 1.0f, 1.0f);

            this.lerpLeftKnee.Reset();
            this.lerpLeftAnkle.Reset();
            this.lerpLeftFoot.Reset();
            this.lerpRightKnee.Reset();
            this.lerpRightAnkle.Reset();
            this.lerpRightFoot.Reset();
        }
    }
}
