//------------------------------------------------------------------------------
// <copyright file="SkeletonJointsSensorTiltCorrection.cs" company="Microsoft">
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
    /// Filter to correct the joint positions for camera tilt, which enables the sensor to be placed at different locations and un-tilt and skeleton to still appear upright.
    /// </summary>
    public static class SkeletonJointsSensorTiltCorrection
    {
        /// <summary>
        /// The running average of the floor normal position.
        /// </summary>
        private static Vector3 averagedFloorNormal = Vector3.Up;

        /// <summary>
        /// CorrectSensorTilt applies camera tilt correction to the skeleton data.
        /// </summary>
        /// <param name="skeleton">The skeleton to correct</param>
        /// <param name="floorPlane">The floor plane (consisting of up normal and sensor height) detected by skeleton tracking (if any).</param>
        /// <param name="sensorElevationAngle">The tilt of the sensor as detected by Kinect.</param>
        public static void CorrectSensorTilt(Skeleton skeleton, Tuple<float, float, float, float> floorPlane, int sensorElevationAngle)
        {
            if (null == skeleton)
            {
                return;
            }

            // To correct the tilt of the skeleton due to a tilted camera, we have three possible up vectors:
            // one from any floor plane detected in Skeleton Tracking, one from the gravity normal produced by the 3D accelerometer,
            // and one from the tilt value sensed by the camera motor.
            // The raw accelerometer value is not currently available in the Kinect for Windows SDK, so instead we use the 
            // the sensorElevationAngle, as the floor plane from skeletal tracking is typically only detected when the
            // camera is pointing down and sees the floor. 
            // Note: SensorElevationAngle value varies around +/- 60 degrees.
            Vector3 floorNormal = Vector3.Up; // default value (has no tilt effect)

            // Assume camera base is level, and use the tilt of the Kinect motor.
            // Rotate an up vector by the negated elevation angle around the X axis
            floorNormal = Vector3.Transform(
                floorNormal,
                Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), MathHelper.ToRadians(sensorElevationAngle)));
            
            if (floorPlane != null)
            {
                Vector4 floorPlaneVec = new Vector4(floorPlane.Item1, floorPlane.Item2, floorPlane.Item3, floorPlane.Item4);

                if (floorPlaneVec.Length() > float.Epsilon && (sensorElevationAngle == 0 || Math.Abs(sensorElevationAngle) > 50))
                {
                    // Use the floor plane for everything.
                    floorNormal = new Vector3(floorPlaneVec.X, floorPlaneVec.Y, floorPlaneVec.Z);
                }
            }

            Array jointTypeValues = Enum.GetValues(typeof(JointType));

            // Running average of floor normal
            averagedFloorNormal = (averagedFloorNormal * 0.9f) + (floorNormal * 0.1f);
            Quaternion rotationToRoomSpace = KinectHelper.GetShortestRotationBetweenVectors(Vector3.Up, averagedFloorNormal);

            Vector3 hipCenter = KinectHelper.SkeletonPointToVector3(skeleton.Joints[JointType.HipCenter].Position);

            // De-tilt.
            foreach (JointType j in jointTypeValues)
            {
                Joint joint = skeleton.Joints[j];
                SkeletonPoint pt = joint.Position;
                Vector3 pos = KinectHelper.SkeletonPointToVector3(pt);

                // Move it back to the origin to rotate
                pos -= hipCenter;

                Vector3 rotatedVec = Vector3.Transform(pos, rotationToRoomSpace);

                rotatedVec += hipCenter;

                joint.Position = KinectHelper.Vector3ToSkeletonPoint(rotatedVec);
                skeleton.Joints[j] = joint;
            }
        }
    }
}