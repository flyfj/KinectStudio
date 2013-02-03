//------------------------------------------------------------------------------
// <copyright file="SkeletonJointsSelfIntersectionConstraint.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Avateering.Filters
{
    using System;
    using Microsoft.Kinect;
    using Microsoft.Xna.Framework;

    /// <summary>
    /// Filter to prevent skeleton arm joints from intersecting the "body".
    /// </summary>
    public static class SkeletonJointsSelfIntersectionConstraint
    {
        /// <summary>
        /// ConstrainSelfIntersection collides joints with the skeleton to keep the skeleton's hands and wrists from puncturing its body
        /// A cylinder is created to represent the torso. Intersecting joints have their positions changed to push them outside the torso.
        /// </summary>
        /// <param name="skeleton">The skeleton.</param>
        public static void Constrain(Skeleton skeleton)
        {
            if (null == skeleton)
            {
                return;
            }

            const float ShoulderExtend = 0.5f;
            const float HipExtend = 6.0f;
            const float CollisionTolerance = 1.01f;
            const float RadiusMultiplier = 1.3f; // increase for bulky avatars

            if (skeleton.Joints[JointType.ShoulderCenter].TrackingState != JointTrackingState.NotTracked
                && skeleton.Joints[JointType.HipCenter].TrackingState != JointTrackingState.NotTracked)
            {
                Vector3 shoulderDiffLeft = KinectHelper.VectorBetween(skeleton, JointType.ShoulderCenter, JointType.ShoulderLeft);
                Vector3 shoulderDiffRight = KinectHelper.VectorBetween(skeleton, JointType.ShoulderCenter, JointType.ShoulderRight);
                float shoulderLengthLeft = shoulderDiffLeft.Length();
                float shoulderLengthRight = shoulderDiffRight.Length();

                // The distance between shoulders is averaged for the radius
                float cylinderRadius = (shoulderLengthLeft + shoulderLengthRight) * 0.5f;
        
                // Calculate the shoulder center and the hip center.  Extend them up and down respectively.
                Vector3 shoulderCenter = KinectHelper.SkeletonPointToVector3(skeleton.Joints[JointType.ShoulderCenter].Position);
                Vector3 hipCenter = KinectHelper.SkeletonPointToVector3(skeleton.Joints[JointType.HipCenter].Position);
                Vector3 hipShoulder = hipCenter - shoulderCenter;
                hipShoulder.Normalize();

                shoulderCenter = shoulderCenter - (hipShoulder * (ShoulderExtend * cylinderRadius));
                hipCenter = hipCenter + (hipShoulder * (HipExtend * cylinderRadius));
        
                // Optionally increase radius to account for bulky avatars
                cylinderRadius *= RadiusMultiplier;
       
                // joints to collide
                JointType[] collisionIndices = { JointType.WristLeft, JointType.HandLeft, JointType.WristRight, JointType.HandRight };
        
                foreach (JointType j in collisionIndices)
                {
                    Vector3 collisionJoint = KinectHelper.SkeletonPointToVector3(skeleton.Joints[j].Position);
                    
                    Microsoft.Xna.Framework.Vector4 distanceNormal = KinectHelper.DistanceToLineSegment(shoulderCenter, hipCenter, collisionJoint);

                    Vector3 normal = new Vector3(distanceNormal.X, distanceNormal.Y, distanceNormal.Z);

                    // if distance is within the cylinder then push the joint out and away from the cylinder
                    if (distanceNormal.W < cylinderRadius)
                    {
                        collisionJoint += normal * ((cylinderRadius - distanceNormal.W) * CollisionTolerance);

                        Joint joint = skeleton.Joints[j];
                        joint.Position = KinectHelper.Vector3ToSkeletonPoint(collisionJoint);
                        skeleton.Joints[j] = joint;
                    }
                }
            }
        }
    }
}
