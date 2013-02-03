// -----------------------------------------------------------------------
// <copyright file="SkeletonJointsSensorOffsetCorrection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Avateering.Filters
{
    using System;
    using Microsoft.Kinect;
    using Microsoft.Samples.Kinect.Avateering;
    using Microsoft.Xna.Framework;
    using Vector4 = Microsoft.Xna.Framework.Vector4;

    /// <summary>
    /// Filter to correct the skeleton position for the sensor height above the floor, to enable an avatar to be placed with feet on the ground plane. 
    /// </summary>
    public class SkeletonJointsSensorOffsetCorrection
    {
        /// <summary>
        /// The running average of the sensor to floor offset.
        /// </summary>
        private float averageFloorOffset;

        /// <summary>
        /// Initializes a new instance of the <see cref="SkeletonJointsSensorOffsetCorrection"/> class.
        /// </summary>
        public SkeletonJointsSensorOffsetCorrection()
        {
            this.averageFloorOffset = 0;
        }

        /// <summary>
        /// Resets the average floor offset.
        /// </summary>
        public void Reset()
        {
            this.averageFloorOffset = 0;
        }

        /// <summary>
        /// CorrectSkeletonOffsetFromFloor moves the skeleton to the floor.
        /// If no floor found in Skeletal Tracking, we can try and use the foot position
        /// but this can be very noisy, which causes the skeleton to bounce up and down.
        /// Note: Using the foot positions will reduce the visual effect of jumping when
        /// an avateer jumps, as we perform a running average.
        /// </summary>
        /// <param name="skeleton">The skeleton to correct.</param>
        /// <param name="floorPlane">The floor plane (consisting of up normal and sensor height) detected by skeleton tracking (if any).</param>
        /// <param name="avatarHipCenterHeight">The height of the avatar Hip Center joint.</param>
        public void CorrectSkeletonOffsetFromFloor(Skeleton skeleton, Tuple<float, float, float, float> floorPlane, float avatarHipCenterHeight)
        {
            if (skeleton == null || skeleton.TrackingState != SkeletonTrackingState.Tracked)
            {
                return;
            }

            Vector4 floorPlaneVec = Vector4.Zero;
            bool haveFloor = false;

            if (null != floorPlane)
            {
                floorPlaneVec = new Vector4(floorPlane.Item1, floorPlane.Item2, floorPlane.Item3, floorPlane.Item4);
                haveFloor = floorPlaneVec.Length() > float.Epsilon;
            }

            // If there's no floor found, try to use the lower foot position, if visible.
            Vector3 hipCenterPosition = KinectHelper.SkeletonPointToVector3(skeleton.Joints[JointType.HipCenter].Position);
            bool haveLeftFoot = KinectHelper.IsTrackedOrInferred(skeleton, JointType.FootLeft);
            bool haveLeftAnkle = KinectHelper.IsTracked(skeleton, JointType.AnkleLeft);
            bool haveRightFoot = KinectHelper.IsTrackedOrInferred(skeleton, JointType.FootRight);
            bool haveRightAnkle = KinectHelper.IsTracked(skeleton, JointType.AnkleRight);

            if (haveLeftFoot || haveLeftAnkle || haveRightFoot || haveRightAnkle)
            {
                // As this runs after de-tilt of the skeleton, so the floor-camera offset will
                // be the foot to camera 0 height in meters as the foot is at the floor plane.
                // Jumping is enabled to some extent due to the running average, but will appear reduced in height.
                Vector3 leftFootPosition = KinectHelper.SkeletonPointToVector3(skeleton.Joints[JointType.FootLeft].Position);
                Vector3 rightFootPosition = KinectHelper.SkeletonPointToVector3(skeleton.Joints[JointType.FootRight].Position);

                Vector3 leftAnklePosition = KinectHelper.SkeletonPointToVector3(skeleton.Joints[JointType.AnkleLeft].Position);
                Vector3 rightAnklePosition = KinectHelper.SkeletonPointToVector3(skeleton.Joints[JointType.AnkleRight].Position);

                // Average the foot and ankle if we have it
                float leftFootAverage = (haveLeftFoot && haveLeftAnkle) ? (leftFootPosition.Y + leftAnklePosition.Y) * 0.5f : haveLeftFoot ? leftFootPosition.Y : leftAnklePosition.Y;
                float rightFootAverage = (haveRightFoot && haveRightAnkle) ? (rightFootPosition.Y + rightAnklePosition.Y) * 0.5f : haveRightFoot ? rightFootPosition.Y : rightAnklePosition.Y;

                // We assume the lowest foot is placed on the floor
                float lowestFootPosition = 0;

                if ((haveLeftFoot || haveLeftAnkle) && (haveRightFoot || haveRightAnkle))
                {
                    // Negate, as we are looking for the camera height above the floor plane
                    lowestFootPosition = Math.Min(leftFootAverage, rightFootAverage);
                }
                else if (haveLeftFoot || haveLeftAnkle)
                {
                    lowestFootPosition = leftFootAverage;
                }
                else
                {
                    lowestFootPosition = rightFootAverage;
                }

                // Running average of floor position
                this.averageFloorOffset = (this.averageFloorOffset * 0.9f) + (lowestFootPosition * 0.1f);
            }
            else if (haveFloor)
            {
                // Get the detected height of the camera off the floor in meters.
                if (0.0f == this.averageFloorOffset)
                {
                    // If it's the initial frame of detection, just set the floor plane directly.
                    this.averageFloorOffset = -floorPlaneVec.W;
                }
                else
                {
                    // Running average of floor position
                    this.averageFloorOffset = (this.averageFloorOffset * 0.9f) + (-floorPlaneVec.W * 0.1f);
                }
            }
            else
            {
                // Just set the avatar offset directly
                this.averageFloorOffset = hipCenterPosition.Y - avatarHipCenterHeight;
            }

            Array jointTypeValues = Enum.GetValues(typeof(JointType));

            // Move to the floor.
            foreach (JointType j in jointTypeValues)
            {
                Joint joint = skeleton.Joints[j];
                SkeletonPoint pt = joint.Position;

                pt.Y = pt.Y - this.averageFloorOffset;

                joint.Position = pt;
                skeleton.Joints[j] = joint;
            }
        }
    }
}
