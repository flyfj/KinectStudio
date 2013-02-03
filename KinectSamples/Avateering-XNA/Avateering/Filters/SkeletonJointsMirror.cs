//------------------------------------------------------------------------------
// <copyright file="SkeletonJointsMirror.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Avateering.Filters
{
    using System;
    using Microsoft.Kinect;

    /// <summary>
    /// Filter to mirror skeleton joints so that avatar appears to mirror the user when displayed on screen.
    /// </summary>
    public static class SkeletonJointsMirror
    {
        /// <summary>
        /// Helper method to mirror the skeleton before calculating joint angles for the avatar.
        /// </summary>
        /// <param name="skeleton">The skeleton to mirror.</param>
        public static void MirrorSkeleton(Skeleton skeleton)
        {
            if (null == skeleton)
            {
                return;
            }

            SwapJoints(skeleton, JointType.ShoulderLeft, JointType.ShoulderRight);
            SwapJoints(skeleton, JointType.ElbowLeft, JointType.ElbowRight);
            SwapJoints(skeleton, JointType.WristLeft, JointType.WristRight);
            SwapJoints(skeleton, JointType.HandLeft, JointType.HandRight);

            SwapJoints(skeleton, JointType.HipLeft, JointType.HipRight);
            SwapJoints(skeleton, JointType.KneeLeft, JointType.KneeRight);
            SwapJoints(skeleton, JointType.AnkleLeft, JointType.AnkleRight);
            SwapJoints(skeleton, JointType.FootLeft, JointType.FootRight);

            Array jointTypeValues = Enum.GetValues(typeof(JointType));

            foreach (JointType j in jointTypeValues)
            {
                Joint joint = skeleton.Joints[j];
                SkeletonPoint mirroredjointPosition = joint.Position;

                // Here we negate the Z or X axis to change the skeleton to mirror the user's movements.
                // Note that this potentially requires us to re-position our camera
                mirroredjointPosition.X = -mirroredjointPosition.X;

                joint.Position = mirroredjointPosition;
                skeleton.Joints[j] = joint;
            }
        }

        /// <summary>
        /// Helper method to swap two joints in the skeleton when mirroring the avatar.
        /// </summary>
        /// <param name="skeleton">The skeleton to mirror.</param>
        /// <param name="left">The left joint type.</param>
        /// <param name="right">The right joint type.</param>
        private static void SwapJoints(Skeleton skeleton, JointType left, JointType right)
        {
            Joint jL = skeleton.Joints[left];
            Joint jR = skeleton.Joints[right];

            Microsoft.Kinect.SkeletonPoint tempPos = jL.Position;
            jL.Position = jR.Position;
            jR.Position = tempPos;

            JointTrackingState tempTs = jL.TrackingState;
            jL.TrackingState = jR.TrackingState;
            jR.TrackingState = tempTs;

            skeleton.Joints[left] = jL;
            skeleton.Joints[right] = jR;
        }
    }
}
