//------------------------------------------------------------------------------
// <copyright file="KinectHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Avateering
{
    using System;
    using Microsoft.Kinect;
    using Microsoft.Xna.Framework;

    /// <summary>
    /// A set of useful helper functions for using Kinect with XNA.
    /// </summary>
    public static class KinectHelper
    {
        /// <summary>
        /// Joint types representing the hierarchical model of the Kinect Skeleton bones.
        /// Specifies the starting joint for each bone (i.e. as the bones are indexed on 
        /// their end joint, this specifies the start joint of the current bone, or 
        /// (equivalently) the end joint of the parent bone.
        /// </summary>
        private static readonly JointType[] BoneJointParents =
        {
            JointType.HipCenter,        // HipCenter parent = none (points to itself)
            JointType.HipCenter,        // Spine parent = HipCenter (0)
            JointType.Spine,            // ShoulderCenter parent = Spine (1)
            JointType.ShoulderCenter,   // Head parent = ShoulderCenter (2)
            JointType.ShoulderCenter,   // ShoulderLeft parent = ShoulderCenter (2)
            JointType.ShoulderLeft,     // ElbowLeft parent = ShoulderLeft (4)
            JointType.ElbowLeft,        // WristLeft parent ElbowLeft (5)
            JointType.WristLeft,        // HandLeft parent WristLeft (6)
            JointType.ShoulderCenter,   // ShoulderRight parent = ShoulderCenter (2)
            JointType.ShoulderRight,    // ElbowRight parent = ShoulderRight (8)
            JointType.ElbowRight,       // WristRight parent = ElbowRight (9)
            JointType.WristRight,       // HandRight parent = WristRight (10)
            JointType.HipCenter,        // HipLeft parent = HipCenter (0)
            JointType.HipLeft,          // KneeLeft parent = HipLeft (12)
            JointType.KneeLeft,         // AnkleLeft parent = KneeLeft (13)
            JointType.AnkleLeft,        // FootLeft parent = AnkleLeft (14)
            JointType.HipCenter,        // HipRight parent = HipCenter (0)
            JointType.HipRight,         // KneeRight parent = HipRight (16)
            JointType.KneeRight,        // AnkleRight parent = KneeRight (17)
            JointType.AnkleRight        // FootRight parent = AnkleRight (18)
        };

        /// <summary>
        /// ParentBoneJoint returns the parent of the bone joint specified.
        /// </summary>
        /// <param name="jt">The current skeleton joint.</param>
        /// <returns>Returns the jointType of the parent joint.</returns>
        public static JointType ParentBoneJoint(JointType jt)
        {
            return BoneJointParents[(int)jt];
        }

        /// <summary>
        /// Position returns the Position at a requested skeleton joint as an XNA Vector3.
        /// </summary>
        /// <param name="skeleton">The skeleton.</param>
        /// <param name="jt">The joint index.</param>
        /// <returns>Returns true if joint is tracked or inferred.</returns>
        public static Vector3 Position(Skeleton skeleton, JointType jt)
        {
            if (skeleton == null)
            {
                return Vector3.Zero;
            }
            else
            {
                return SkeletonPointToVector3(skeleton.Joints[jt].Position);
            }
        }

        /// <summary>
        /// JointPositionIsValid checks whether a skeleton joint is all 0's which can indicate not valid.  
        /// </summary>
        /// <param name="jointPosition">The joint position.</param>
        /// <returns>Returns true if valid, false otherwise.</returns>
        public static bool JointPositionIsValid(Vector3 jointPosition)
        {
            return jointPosition.X != 0.0f || jointPosition.Y != 0.0f || jointPosition.Z != 0.0f;
        }

        /// <summary>
        /// BoneOrientationIsValid checks whether a skeleton bone rotation is NaN which can indicate an invalid rotation.  
        /// </summary>
        /// <param name="boneOrientation">The bone Orientation.</param>
        /// <returns>Returns true if valid, false otherwise.</returns>
        public static bool BoneOrientationIsValid(Quaternion boneOrientation)
        {
            return !(float.IsNaN(boneOrientation.X) || float.IsNaN(boneOrientation.Y) || float.IsNaN(boneOrientation.Z) || float.IsNaN(boneOrientation.W));
        }

        /// <summary>
        /// IsTracked checks whether the skeleton joint is tracked.
        /// </summary>
        /// <param name="skeleton">The skeleton.</param>
        /// <param name="jt">The joint index.</param>
        /// <returns>Returns true if joint is tracked or inferred.</returns>
        public static bool IsTracked(Skeleton skeleton, JointType jt)
        {
            if (null == skeleton)
            {
                return false;
            }

            return skeleton.Joints[jt].TrackingState == JointTrackingState.Tracked;
        }

        /// <summary>
        /// IsTrackedOrInferred checks whether the skeleton joint is tracked or inferred.
        /// </summary>
        /// <param name="skeleton">The skeleton.</param>
        /// <param name="jt">The joint index.</param>
        /// <returns>Returns true if joint is tracked or inferred.</returns>
        public static bool IsTrackedOrInferred(Skeleton skeleton, JointType jt)
        {
            if (null == skeleton)
            {
                return false;
            }

            return skeleton.Joints[jt].TrackingState != JointTrackingState.NotTracked;
        }

        /// <summary>
        /// VectorBetween calculates the XNA Vector3 from start to end == subtract start from end 
        /// (a->b = b - a)
        /// </summary>
        /// <param name="skeleton">The skeleton.</param>
        /// <param name="startJoint">Start NuiSkeleton joint.</param>
        /// <param name="endJoint">End NuiSkeleton joint.</param>
        /// <returns>Returns a Vector3</returns>
        public static Vector3 VectorBetween(Skeleton skeleton, JointType startJoint, JointType endJoint)
        {
            if (null == skeleton)
            {
                return Vector3.Zero;
            }

            Vector3 startPosition = new Vector3(skeleton.Joints[startJoint].Position.X, skeleton.Joints[startJoint].Position.Y, skeleton.Joints[startJoint].Position.Z);
            Vector3 endPosition = new Vector3(skeleton.Joints[endJoint].Position.X, skeleton.Joints[endJoint].Position.Y, skeleton.Joints[endJoint].Position.Z);
            return endPosition - startPosition;
        }

        /// <summary>
        /// GetShortestRotationBetweenVectors finds the shortest rotation between two vectors and return in quaternion form
        /// </summary>
        /// <param name="vector1">First Vector3</param>
        /// <param name="vector2">Second Vector3</param>
        /// <returns>Returns a Rotation Quaternion</returns>
        public static Quaternion GetShortestRotationBetweenVectors(Vector3 vector1, Vector3 vector2)
        {
            vector1.Normalize();
            vector2.Normalize();
            float angle = (float)Math.Acos(Vector3.Dot(vector1, vector2));
            Vector3 axis = Vector3.Cross(vector2, vector1);

            // Check to see if the angle is very small, in which case, the cross product becomes unstable,
            // so set the axis to a default.  It doesn't matter much what this axis is, as the rotation angle 
            // will be near zero anyway.
            if (angle < 0.001f)
            {
                axis = new Vector3(0.0f, 0.0f, 1.0f);
            }

            if (axis.Length() < .001f)
            {
                return Quaternion.Identity;
            }

            axis.Normalize();
            Quaternion rot = Quaternion.CreateFromAxisAngle(axis, angle);

            return rot;
        }

        /// <summary>
        /// SkeletonPointToVector3 converts Skeleton Point to Vector3.  
        /// </summary>
        /// <param name="pt">The Skeleton Point position.</param>
        /// <returns>Returns the Vector3 position.</returns>
        public static Vector3 SkeletonPointToVector3(SkeletonPoint pt)
        {
            return new Vector3(pt.X, pt.Y, pt.Z);
        }

        /// <summary>
        /// Vector3ToSkeletonPoint convert Vector3 to Skeleton Point.  
        /// </summary>
        /// <param name="pt">The Vector3 position.</param>
        /// <returns>Returns the Skeleton Point position.</returns>
        public static SkeletonPoint Vector3ToSkeletonPoint(Vector3 pt)
        {
            SkeletonPoint skelPt = new SkeletonPoint();
            skelPt.X = pt.X;
            skelPt.Y = pt.Y;
            skelPt.Z = pt.Z;
            return skelPt;
        }

        /// <summary>
        /// Matrix4ToXNAMatrix converts a Matrix4 object holding Joint Orientation to an XNA Matrix.
        /// Matrix is initialized to zeros, and the joint orientation only holds a 3x3 rotation matrix.
        /// </summary>
        /// <param name="mat">A Matrix4 object to be converted to an XNA Matrix.</param>
        /// <returns>Returns an XNA Matrix.</returns>
        public static Matrix Matrix4ToXNAMatrix(Microsoft.Kinect.Matrix4 mat)
        {
            Matrix converted = new Matrix();
            converted.M11 = mat.M11;
            converted.M12 = mat.M12;
            converted.M13 = mat.M13;
            converted.M21 = mat.M21;
            converted.M22 = mat.M22;
            converted.M23 = mat.M23;
            converted.M31 = mat.M31;
            converted.M32 = mat.M32;
            converted.M33 = mat.M33;
            converted.M44 = mat.M44;
            return converted;
        }

        /// <summary>
        /// XNAMatrixToMatrix4 converts a XNA Matrix object holding Joint Orientation to a Kinect Matrix4.
        /// Matrix is initialized to zeros, and the joint orientation only holds a 3x3 rotation matrix.
        /// </summary>
        /// <param name="mat">An XNA Matrix object to be converted to a Kinect Matrix4.</param>
        /// <returns>Returns a Kinect Matrix4.</returns>
        public static Microsoft.Kinect.Matrix4 XNAMatrixToMatrix4(Matrix mat)
        {
            Microsoft.Kinect.Matrix4 converted = new Microsoft.Kinect.Matrix4();
            converted.M11 = mat.M11;
            converted.M12 = mat.M12;
            converted.M13 = mat.M13;
            converted.M21 = mat.M21;
            converted.M22 = mat.M22;
            converted.M23 = mat.M23;
            converted.M31 = mat.M31;
            converted.M32 = mat.M32;
            converted.M33 = mat.M33;
            converted.M44 = mat.M44;
            return converted;
        }

        /// <summary>
        /// Vector4ToXNAQuaternion converts a Vector4 object holding Joint Orientation quaternion to an XNA Quaternion.
        /// </summary>
        /// <param name="quaternion">A Vector4 object to be converted to an XNA Quaternion.</param>
        /// <returns>Returns an XNA Quaternion.</returns>
        public static Quaternion Vector4ToXNAQuaternion(Microsoft.Kinect.Vector4 quaternion)
        {
            Quaternion converted = new Quaternion();
            converted.X = quaternion.X;
            converted.Y = quaternion.Y;
            converted.Z = quaternion.Z;
            converted.W = quaternion.W;
            return converted;
        }

        /// <summary>
        /// XNAQuaternionToVector4 converts an XNAQuaternion object holding Joint Orientation to a KinectSDK Vector4.
        /// </summary>
        /// <param name="quaternion">An XNA Quaternion object to be converted to a Kinect Vector4.</param>
        /// <returns>Returns a Kinect Vector4.</returns>
        public static Microsoft.Kinect.Vector4 XNAQuaternionToVector4(Quaternion quaternion)
        {
            Microsoft.Kinect.Vector4 converted = new Microsoft.Kinect.Vector4();
            converted.X = quaternion.X;
            converted.Y = quaternion.Y;
            converted.Z = quaternion.Z;
            converted.W = quaternion.W;
            return converted;
        }

        /// <summary>
        /// DecomposeMatRot decomposes a matrix into its component parts and return the quaternion rotation.
        /// </summary>
        /// <param name="inMat">Matrix to decompose.</param>
        /// <returns>Returns a quaternion rotation from the Matrix.</returns>
        public static Quaternion DecomposeMatRot(Matrix inMat)
        {
            Quaternion rot;
            Vector3 scale;
            Vector3 trans;
            inMat.Decompose(out scale, out rot, out trans);

            return rot;
        }

        /// <summary>
        /// QuaternionAngle returns the amount of rotation in the given quaternion, in radians.
        /// </summary>
        /// <param name="rotation">Input quaternion.</param>
        /// <returns>Returns rotation angle in radians</returns>
        public static float QuaternionAngle(Quaternion rotation)
        {
            rotation.Normalize();
            float angle = 2.0f * (float)Math.Acos(rotation.W);
            return angle;
        }

        /// <summary>
        /// EnsureQuaternionNeighborhood ensures that quaternions qA and quaternionB are in the same 3D sphere in 4D space.
        /// </summary>
        /// <param name="quaternionA">Input quaternion a</param>
        /// <param name="quaternionB">Input quaternion b</param>
        /// <returns>Returns quaternion B in same 3D sphere as quaternion A.</returns>
        public static Quaternion EnsureQuaternionNeighborhood(Quaternion quaternionA, Quaternion quaternionB)
        {
            if (Quaternion.Dot(quaternionA, quaternionB) < 0)
            {
                // Negate the second quaternion, to place it in the opposite 3D sphere.
                return -quaternionB;
            }

            return quaternionB;
        }

        /// <summary>
        /// RotationBetweenQuaternions returns a quaternion that represents a rotation qR such that qA * qR = quaternionB.
        /// </summary>
        /// <param name="quaternionA">Input quaternion a</param>
        /// <param name="quaternionB">Input quaternion b</param>
        /// <returns>Returns rotation quaternion between input quaternions.</returns>
        public static Quaternion RotationBetweenQuaternions(Quaternion quaternionA, Quaternion quaternionB)
        {
            Quaternion modifiedB = EnsureQuaternionNeighborhood(quaternionA, quaternionB);
            return Quaternion.Multiply(Quaternion.Inverse(quaternionA), modifiedB);
        }

        /// <summary>
        /// EnhancedQuaternionSlerp performs a quaternion Slerp, after placing both input quaternions in the same 3D sphere.
        /// </summary>
        /// <param name="quaternionA">Input quaternion a</param>
        /// <param name="quaternionB">Input quaternion b</param>
        /// <param name="amount">Amount to slerp</param>
        /// <returns>Slerped rotation quaternion between input quaternions.</returns>
        public static Quaternion EnhancedQuaternionSlerp(Quaternion quaternionA, Quaternion quaternionB, float amount)
        {
            Quaternion modifiedB = EnsureQuaternionNeighborhood(quaternionA, quaternionB);
            return Quaternion.Slerp(quaternionA, modifiedB, amount);
        }

        /// <summary>
        /// QuaternionAngle returns the axis and amount of rotation in the given quaternion, in radians.
        /// </summary>
        /// <param name="rotation">The input quaternion.</param>
        /// <returns>Returns the axis angle of rotation in a Vector4.</returns>
        public static Microsoft.Xna.Framework.Vector4 QuaternionToAxisAngle(Quaternion rotation)
        {
            rotation.Normalize();
            float angle = 2.0f * (float)Math.Acos(rotation.W);

            float s = (float)Math.Sqrt(1.0f - (rotation.W * rotation.W));

            // If the angle is very small, the direction is not important - set a default here
            Vector3 axis = new Vector3(rotation.X, rotation.Y, rotation.Z);

            // perform calculation if proper angle
            if (s >= 0.001f)
            {
                float oneOverS = 1.0f / s;
                axis.X = rotation.X * oneOverS; // normalize axis
                axis.Y = rotation.Y * oneOverS;
                axis.Z = rotation.Z * oneOverS;
            }

            axis.Normalize();
            return new Microsoft.Xna.Framework.Vector4(axis, angle);
        }

        /// <summary>
        /// CompareAgainstThreshold compares a value against min/max thresholds, with clamping.
        /// </summary>
        /// <param name="value">The value to compare.</param>
        /// <param name="minValue">The minimum threshold.</param>
        /// <param name="maxValue">The maximum threshold.</param>
        /// <returns>Returns comparison value or 0, 1 if clamped.</returns>
        public static float CompareAgainstThreshold(float value, float minValue, float maxValue)
        {
            float comparison = (value - minValue) / (maxValue - minValue);
            return Math.Max(0.0f, Math.Min(1.0f, comparison));
        }

        /// <summary>
        /// LerpVector modifies a vector with a lerp operation.
        /// </summary>
        /// <param name="sourceValue">The Vector3 position.</param>
        /// <param name="lerpValue">The new Vector3 value to lerp to.</param>
        /// <param name="amount">The lerp amount between current and new Vector3.</param>
        /// <returns>Returns vector3 Lerp Result</returns>
        public static Vector3 LerpVector(Vector3 sourceValue, Vector3 lerpValue, float amount)
        {
            Vector3 vec0 = sourceValue;

            // V0 + t * (V1 - V0)
            Vector3 length = Vector3.Subtract(lerpValue, vec0);
            Vector3 temp = Vector3.Multiply(length, amount);

            return Vector3.Add(temp, vec0);
        }

        /// <summary>
        /// LerpVector modifies a vector with a lerp operation.
        /// </summary>
        /// <param name="sourceValue">The Kinect Skeleton position.</param>
        /// <param name="lerpValue">The new Vector3 value to lerp to.</param>
        /// <param name="amount">The lerp amount between current and new Vector3.</param>
        /// <returns>Returns Skeleton Point Lerp Result</returns>
        public static SkeletonPoint LerpVector(SkeletonPoint sourceValue, Vector3 lerpValue, float amount)
        {
            return Vector3ToSkeletonPoint(
                LerpVector(SkeletonPointToVector3(sourceValue), lerpValue, amount));
        }

        /// <summary>
        /// LerpAndApply performs a Lerp and applies the Lerped vector to the skeleton joint.
        /// </summary>
        /// <param name="skeleton">The skeleton.</param>
        /// <param name="jt">The joint type.</param>
        /// <param name="newJointVector">The new Vector3 value to lerp to.</param>
        /// <param name="lerpValue">The lerp amount between current and new Vector3.</param>
        /// <param name="finalTrackingState">The tracking state of the joint to set after lerp.</param>
        public static void LerpAndApply(Skeleton skeleton, JointType jt, Vector3 newJointVector, float lerpValue, JointTrackingState finalTrackingState)
        {
            if (null == skeleton)
            {
                return;
            }

            Joint joint = skeleton.Joints[jt];
            SkeletonPoint pt = KinectHelper.LerpVector(joint.Position, newJointVector, lerpValue);
            joint.Position = pt;
            joint.TrackingState = finalTrackingState;
            skeleton.Joints[jt] = joint;
        }

        /// <summary>
        /// DistanceToLineSegment finds the distance from a point to a line.
        /// </summary>
        /// <param name="linePoint0">Line point0.</param>
        /// <param name="linePoint1">Line point1.</param>
        /// <param name="point">Point to get distance from.</param>
        /// <returns>Return the distance and normal for offset use.</returns>
        public static Microsoft.Xna.Framework.Vector4 DistanceToLineSegment(Vector3 linePoint0, Vector3 linePoint1, Vector3 point)
        {
            // find the vector from x0 to x1
            Vector3 lineVec = linePoint1 - linePoint0;
            float lineLength = lineVec.Length();
            Vector3 lineToPoint = point - linePoint0;

            const float Epsilon = 0.0001f;

            // if the line is too short skip
            if (lineLength > Epsilon)
            {
                float t = Vector3.Dot(lineVec, lineToPoint) / lineLength;

                // projection is longer than the line itself so find distance to end point of line
                if (t > lineLength)
                {
                    lineToPoint = point - linePoint1;
                }
                else if (t >= 0.0f)
                {
                    // find distance to line
                    Vector3 normalPoint = lineVec;

                    // Perform the float->vector conversion once by combining t/fLineLength
                    normalPoint *= t / lineLength;
                    normalPoint += linePoint0;
                    lineToPoint = point - normalPoint;
                }
            }

            // The distance is the size of the final computed line
            float distance = lineToPoint.Length();

            // The normal is the final line normalized
            Vector3 normal = lineToPoint / distance;

            return new Microsoft.Xna.Framework.Vector4(normal, distance);
        }

        /// <summary>
        /// CopySkeleton copies the data from another skeleton.
        /// </summary>
        /// <param name="source">The source skeleton.</param>
        /// <param name="destination">The destination skeleton.</param>
        public static void CopySkeleton(Skeleton source, Skeleton destination)
        {
            if (null == source)
            {
                return;
            }

            if (null == destination)
            {
                destination = new Skeleton();
            }

            destination.TrackingState = source.TrackingState;
            destination.TrackingId = source.TrackingId;
            destination.Position = source.Position;
            destination.ClippedEdges = source.ClippedEdges;

            Array jointTypeValues = Enum.GetValues(typeof(JointType));

            // This must copy before the joint orientations
            foreach (JointType j in jointTypeValues)
            {
                Joint temp = destination.Joints[j];
                temp.Position = source.Joints[j].Position;
                temp.TrackingState = source.Joints[j].TrackingState;
                destination.Joints[j] = temp;
            }

            if (null != source.BoneOrientations)
            {
                foreach (JointType j in jointTypeValues)
                {
                    BoneOrientation temp = destination.BoneOrientations[j];
                    temp.HierarchicalRotation.Matrix = source.BoneOrientations[j].HierarchicalRotation.Matrix;
                    temp.HierarchicalRotation.Quaternion = source.BoneOrientations[j].HierarchicalRotation.Quaternion;
                    temp.AbsoluteRotation.Matrix = source.BoneOrientations[j].AbsoluteRotation.Matrix;
                    temp.AbsoluteRotation.Quaternion = source.BoneOrientations[j].AbsoluteRotation.Quaternion;
                    destination.BoneOrientations[j] = temp;
                }
            }
        }
    }
}
