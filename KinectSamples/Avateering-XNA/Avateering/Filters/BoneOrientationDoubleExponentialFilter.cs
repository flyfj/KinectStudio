//------------------------------------------------------------------------------
// <copyright file="BoneOrientationDoubleExponentialFilter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Avateering.Filters
{
    using System;
    using Microsoft.Kinect;
    using Microsoft.Xna.Framework;

    /// <summary>
    /// Implementation of a Holt Double Exponential Smoothing filter for orientation. The double 
    /// exponential smooths the curve and predicts. There is also noise jitter removal. And maximum
    /// prediction bounds.  The parameters are commented in the Init function.
    /// </summary>
    public class BoneOrientationDoubleExponentialFilter
    {
        /// <summary>
        /// The previous filtered orientation data.
        /// </summary>
        private FilterDoubleExponentialData[] history;

        /// <summary>
        /// The transform smoothing parameters for this filter.
        /// </summary>
        private TransformSmoothParameters smoothParameters;

        /// <summary>
        /// True when the filter parameters are initialized.
        /// </summary>
        private bool init;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoneOrientationDoubleExponentialFilter"/> class.
        /// </summary>
        public BoneOrientationDoubleExponentialFilter()
        {
            this.init = false;
        }

        /// <summary>
        /// Initialize the filter with a default set of TransformSmoothParameters.
        /// </summary>
        public void Init()
        {
            // Set some reasonable defaults
            this.Init(0.5f, 0.8f, 0.75f, 0.1f, 0.1f);
        }

        /// <summary>
        /// Initialize the filter with a set of manually specified TransformSmoothParameters.
        /// </summary>
        /// <param name="smoothingValue">Smoothing = [0..1], lower values is closer to the raw data and more noisy.</param>
        /// <param name="correctionValue">Correction = [0..1], higher values correct faster and feel more responsive.</param>
        /// <param name="predictionValue">Prediction = [0..n], how many frames into the future we want to predict.</param>
        /// <param name="jitterRadiusValue">JitterRadius = The deviation angle in radians that defines jitter.</param>
        /// <param name="maxDeviationRadiusValue">MaxDeviation = The maximum angle in radians that filtered positions are allowed to deviate from raw data.</param>
        public void Init(float smoothingValue, float correctionValue, float predictionValue, float jitterRadiusValue, float maxDeviationRadiusValue)
        {
            this.smoothParameters = new TransformSmoothParameters();

            this.smoothParameters.MaxDeviationRadius = maxDeviationRadiusValue; // Size of the max prediction radius Can snap back to noisy data when too high
            this.smoothParameters.Smoothing = smoothingValue;                   // How much soothing will occur.  Will lag when too high
            this.smoothParameters.Correction = correctionValue;                 // How much to correct back from prediction.  Can make things springy
            this.smoothParameters.Prediction = predictionValue;                 // Amount of prediction into the future to use. Can over shoot when too high
            this.smoothParameters.JitterRadius = jitterRadiusValue;             // Size of the radius where jitter is removed. Can do too much smoothing when too high
            this.Reset();
            this.init = true;
        }

        /// <summary>
        /// Initialize the filter with a set of TransformSmoothParameters.
        /// </summary>
        /// <param name="smoothingParameters">The smoothing parameters to filter with.</param>
        public void Init(TransformSmoothParameters smoothingParameters)
        {
            this.smoothParameters = smoothingParameters;
            this.Reset();
            this.init = true;
        }

        /// <summary>
        /// Resets the filter to default values.
        /// </summary>
        public void Reset()
        {
            Array jointTypeValues = Enum.GetValues(typeof(JointType));
            this.history = new FilterDoubleExponentialData[jointTypeValues.Length];
        }

        /// <summary>
        /// DoubleExponentialJointOrientationFilter - Implements a double exponential smoothing filter on the skeleton bone orientation quaternions.
        /// </summary>
        /// <param name="skeleton">The Skeleton to filter.</param>
        public void UpdateFilter(Skeleton skeleton)
        {
            if (null == skeleton)
            {
                return;
            }

            if (skeleton.TrackingState != SkeletonTrackingState.Tracked)
            {
                return;
            }

            if (this.init == false)
            {
                this.Init(); // initialize with default parameters                
            }

            Array jointTypeValues = Enum.GetValues(typeof(JointType));

            TransformSmoothParameters tempSmoothingParams = new TransformSmoothParameters();

            // Check for divide by zero. Use an epsilon of a 10th of a millimeter
            this.smoothParameters.JitterRadius = Math.Max(0.0001f, this.smoothParameters.JitterRadius);

            tempSmoothingParams.Smoothing = this.smoothParameters.Smoothing;
            tempSmoothingParams.Correction = this.smoothParameters.Correction;
            tempSmoothingParams.Prediction = this.smoothParameters.Prediction;

            foreach (JointType jt in jointTypeValues)
            {
                // If not tracked, we smooth a bit more by using a bigger jitter radius
                // Always filter feet highly as they are so noisy
                if (skeleton.Joints[jt].TrackingState != JointTrackingState.Tracked || jt == JointType.FootLeft || jt == JointType.FootRight)
                {
                    tempSmoothingParams.JitterRadius *= 2.0f;
                    tempSmoothingParams.MaxDeviationRadius *= 2.0f;
                }
                else
                {
                    tempSmoothingParams.JitterRadius = this.smoothParameters.JitterRadius;
                    tempSmoothingParams.MaxDeviationRadius = this.smoothParameters.MaxDeviationRadius;
                }

                this.FilterJoint(skeleton, jt, tempSmoothingParams);
            }
        }

        /// <summary>
        /// Update the filter for one joint.  
        /// </summary>
        /// <param name="skeleton">The Skeleton to filter.</param>
        /// <param name="jt">The Skeleton Joint index to filter.</param>
        /// <param name="smoothingParameters">The Smoothing parameters to apply.</param>
        protected void FilterJoint(Skeleton skeleton, JointType jt, TransformSmoothParameters smoothingParameters)
        {
            if (null == skeleton)
            {
                return;
            }

            int jointIndex = (int)jt;

            Quaternion filteredOrientation;
            Quaternion trend;

            Quaternion rawOrientation = Quaternion.CreateFromRotationMatrix(KinectHelper.Matrix4ToXNAMatrix(skeleton.BoneOrientations[jt].HierarchicalRotation.Matrix));
            Quaternion prevFilteredOrientation = this.history[jointIndex].FilteredBoneOrientation;
            Quaternion prevTrend = this.history[jointIndex].Trend;
            Vector3 rawPosition = KinectHelper.SkeletonPointToVector3(skeleton.Joints[jt].Position);
            bool orientationIsValid = KinectHelper.JointPositionIsValid(rawPosition) && KinectHelper.IsTrackedOrInferred(skeleton, jt) && KinectHelper.BoneOrientationIsValid(rawOrientation);

            if (!orientationIsValid)
            {
                if (this.history[jointIndex].FrameCount > 0)
                {
                    rawOrientation = this.history[jointIndex].FilteredBoneOrientation;
                    this.history[jointIndex].FrameCount = 0;
                }
            }

            // Initial start values or reset values
            if (this.history[jointIndex].FrameCount == 0)
            {
                // Use raw position and zero trend for first value
                filteredOrientation = rawOrientation;
                trend = Quaternion.Identity;
            }
            else if (this.history[jointIndex].FrameCount == 1)
            {
                // Use average of two positions and calculate proper trend for end value
                Quaternion prevRawOrientation = this.history[jointIndex].RawBoneOrientation;
                filteredOrientation = KinectHelper.EnhancedQuaternionSlerp(prevRawOrientation, rawOrientation, 0.5f);

                Quaternion diffStarted = KinectHelper.RotationBetweenQuaternions(filteredOrientation, prevFilteredOrientation);
                trend = KinectHelper.EnhancedQuaternionSlerp(prevTrend, diffStarted, smoothingParameters.Correction);
            }
            else
            {
                // First apply a jitter filter
                Quaternion diffJitter = KinectHelper.RotationBetweenQuaternions(rawOrientation, prevFilteredOrientation);
                float diffValJitter = (float)Math.Abs(KinectHelper.QuaternionAngle(diffJitter));

                if (diffValJitter <= smoothingParameters.JitterRadius)
                {
                    filteredOrientation = KinectHelper.EnhancedQuaternionSlerp(prevFilteredOrientation, rawOrientation, diffValJitter / smoothingParameters.JitterRadius);
                }
                else
                {
                    filteredOrientation = rawOrientation;
                }

                // Now the double exponential smoothing filter
                filteredOrientation = KinectHelper.EnhancedQuaternionSlerp(filteredOrientation, Quaternion.Multiply(prevFilteredOrientation, prevTrend), smoothingParameters.Smoothing);

                diffJitter = KinectHelper.RotationBetweenQuaternions(filteredOrientation, prevFilteredOrientation);
                trend = KinectHelper.EnhancedQuaternionSlerp(prevTrend, diffJitter, smoothingParameters.Correction);
            }      

            // Use the trend and predict into the future to reduce latency
            Quaternion predictedOrientation = Quaternion.Multiply(filteredOrientation, KinectHelper.EnhancedQuaternionSlerp(Quaternion.Identity, trend, smoothingParameters.Prediction));

            // Check that we are not too far away from raw data
            Quaternion diff = KinectHelper.RotationBetweenQuaternions(predictedOrientation, filteredOrientation);
            float diffVal = (float)Math.Abs(KinectHelper.QuaternionAngle(diff));

            if (diffVal > smoothingParameters.MaxDeviationRadius)
            {
                predictedOrientation = KinectHelper.EnhancedQuaternionSlerp(filteredOrientation, predictedOrientation, smoothingParameters.MaxDeviationRadius / diffVal);
            }

            predictedOrientation.Normalize();
            filteredOrientation.Normalize();
            trend.Normalize();
              
            // Save the data from this frame
            this.history[jointIndex].RawBoneOrientation = rawOrientation;
            this.history[jointIndex].FilteredBoneOrientation = filteredOrientation;
            this.history[jointIndex].Trend = trend;
            this.history[jointIndex].FrameCount++;

            // Set the filtered and predicted data back into the bone orientation
            skeleton.BoneOrientations[jt].HierarchicalRotation.Quaternion = KinectHelper.XNAQuaternionToVector4(predictedOrientation);  // local rotation
            skeleton.BoneOrientations[jt].HierarchicalRotation.Matrix = KinectHelper.XNAMatrixToMatrix4(Matrix.CreateFromQuaternion(predictedOrientation));

            // HipCenter has no parent and is the root of our skeleton - leave the HipCenter absolute set as it is
            if (jt != JointType.HipCenter)
            {
                Quaternion parentRot = KinectHelper.Vector4ToXNAQuaternion(skeleton.BoneOrientations[KinectHelper.ParentBoneJoint(jt)].AbsoluteRotation.Quaternion);

                // create a new world rotation
                Quaternion worldRot = Quaternion.Multiply(parentRot, predictedOrientation);
                skeleton.BoneOrientations[jt].AbsoluteRotation.Quaternion = KinectHelper.XNAQuaternionToVector4(worldRot);
                skeleton.BoneOrientations[jt].AbsoluteRotation.Matrix = KinectHelper.XNAMatrixToMatrix4(Matrix.CreateFromQuaternion(worldRot));
            }
            else
            {
                // In the Hip Center root joint, absolute and relative are the same
                skeleton.BoneOrientations[jt].AbsoluteRotation.Quaternion = skeleton.BoneOrientations[jt].HierarchicalRotation.Quaternion;
                skeleton.BoneOrientations[jt].AbsoluteRotation.Matrix = skeleton.BoneOrientations[jt].HierarchicalRotation.Matrix;
            }
        }

        /// <summary>
        /// Historical Filter Data.  
        /// </summary>
        private struct FilterDoubleExponentialData
        {
            /// <summary>
            /// Gets or sets Historical Position.  
            /// </summary>
            public Quaternion RawBoneOrientation { get; set; }

            /// <summary>
            /// Gets or sets Historical Filtered Position.  
            /// </summary>
            public Quaternion FilteredBoneOrientation { get; set; }

            /// <summary>
            /// Gets or sets Historical Trend.  
            /// </summary>
            public Quaternion Trend { get; set; }

            /// <summary>
            /// Gets or sets Historical FrameCount.  
            /// </summary>
            public uint FrameCount { get; set; }
        }
    }
}