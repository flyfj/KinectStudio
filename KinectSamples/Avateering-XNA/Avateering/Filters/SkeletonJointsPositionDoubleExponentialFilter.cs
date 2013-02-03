//------------------------------------------------------------------------------
// <copyright file="SkeletonJointsPositionDoubleExponentialFilter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Avateering.Filters
{
    using System;
    using Microsoft.Kinect;
    using Microsoft.Xna.Framework;

    /// <summary>
    /// Implementation of a Holt Double Exponential Smoothing filter. The double exponential
    /// smooths the curve and predicts.  There is also noise jitter removal. And maximum
    /// prediction bounds.  The parameters are commented in the Init function.
    /// </summary>
    public class SkeletonJointsPositionDoubleExponentialFilter
    {
        /// <summary>
        /// The previous data.
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
        /// Initializes a new instance of the <see cref="SkeletonJointsPositionDoubleExponentialFilter"/> class.
        /// </summary>
        public SkeletonJointsPositionDoubleExponentialFilter()
        {
            this.init = false;
        }

        /// <summary>
        /// Initialize the filter with a default set of TransformSmoothParameters.
        /// </summary>
        public void Init()
        {
            // Specify some defaults
            this.Init(0.25f, 0.25f, 0.25f, 0.03f, 0.05f);
        }

        /// <summary>
        /// Initialize the filter with a set of manually specified TransformSmoothParameters.
        /// </summary>
        /// <param name="smoothingValue">Smoothing = [0..1], lower values is closer to the raw data and more noisy.</param>
        /// <param name="correctionValue">Correction = [0..1], higher values correct faster and feel more responsive.</param>
        /// <param name="predictionValue">Prediction = [0..n], how many frames into the future we want to predict.</param>
        /// <param name="jitterRadiusValue">JitterRadius = The deviation distance in m that defines jitter.</param>
        /// <param name="maxDeviationRadiusValue">MaxDeviation = The maximum distance in m that filtered positions are allowed to deviate from raw data.</param>
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
        /// Update the filter with a new frame of data and smooth.
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
                this.Init();    // initialize with default parameters                
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
                if (skeleton.Joints[jt].TrackingState != JointTrackingState.Tracked)
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

            Vector3 filteredPosition;
            Vector3 diffvec;
            Vector3 trend;
            float diffVal;

            Vector3 rawPosition = KinectHelper.SkeletonPointToVector3(skeleton.Joints[jt].Position);
            Vector3 prevFilteredPosition = this.history[jointIndex].FilteredPosition;
            Vector3 prevTrend = this.history[jointIndex].Trend;
            Vector3 prevRawPosition = this.history[jointIndex].RawPosition;
            bool jointIsValid = KinectHelper.JointPositionIsValid(rawPosition);

            // If joint is invalid, reset the filter
            if (!jointIsValid)
            {
                this.history[jointIndex].FrameCount = 0;
            }

            // Initial start values
            if (this.history[jointIndex].FrameCount == 0)
            {
                filteredPosition = rawPosition;
                trend = Vector3.Zero;
            }
            else if (this.history[jointIndex].FrameCount == 1)
            {
                filteredPosition = Vector3.Multiply(Vector3.Add(rawPosition, prevRawPosition), 0.5f);
                diffvec = Vector3.Subtract(filteredPosition, prevFilteredPosition);
                trend = Vector3.Add(Vector3.Multiply(diffvec, smoothingParameters.Correction), Vector3.Multiply(prevTrend, 1.0f - smoothingParameters.Correction));
            }
            else
            {              
                // First apply jitter filter
                diffvec = Vector3.Subtract(rawPosition, prevFilteredPosition);
                diffVal = Math.Abs(diffvec.Length());

                if (diffVal <= smoothingParameters.JitterRadius)
                {
                    filteredPosition = Vector3.Add(Vector3.Multiply(rawPosition, diffVal / smoothingParameters.JitterRadius), Vector3.Multiply(prevFilteredPosition, 1.0f - (diffVal / smoothingParameters.JitterRadius)));
                }
                else
                {
                    filteredPosition = rawPosition;
                }

                // Now the double exponential smoothing filter
                filteredPosition = Vector3.Add(Vector3.Multiply(filteredPosition, 1.0f - smoothingParameters.Smoothing), Vector3.Multiply(Vector3.Add(prevFilteredPosition, prevTrend), smoothingParameters.Smoothing));

                diffvec = Vector3.Subtract(filteredPosition, prevFilteredPosition);
                trend = Vector3.Add(Vector3.Multiply(diffvec, smoothingParameters.Correction), Vector3.Multiply(prevTrend, 1.0f - smoothingParameters.Correction));
            }      

            // Predict into the future to reduce latency
            Vector3 predictedPosition = Vector3.Add(filteredPosition, Vector3.Multiply(trend, smoothingParameters.Prediction));

            // Check that we are not too far away from raw data
            diffvec = Vector3.Subtract(predictedPosition, rawPosition);
            diffVal = Math.Abs(diffvec.Length());

            if (diffVal > smoothingParameters.MaxDeviationRadius)
            {
                predictedPosition = Vector3.Add(Vector3.Multiply(predictedPosition, smoothingParameters.MaxDeviationRadius / diffVal), Vector3.Multiply(rawPosition, 1.0f - (smoothingParameters.MaxDeviationRadius / diffVal)));
            }

            // Save the data from this frame
            this.history[jointIndex].RawPosition = rawPosition;
            this.history[jointIndex].FilteredPosition = filteredPosition;
            this.history[jointIndex].Trend = trend;
            this.history[jointIndex].FrameCount++;
            
            // Set the filtered data back into the joint
            Joint j = skeleton.Joints[jt];
            j.Position = KinectHelper.Vector3ToSkeletonPoint(predictedPosition);
            skeleton.Joints[jt] = j;
        }

        /// <summary>
        /// Historical Filter Data.  
        /// </summary>
        private struct FilterDoubleExponentialData
        {
            /// <summary>
            /// Gets or sets Historical Position.  
            /// </summary>
            public Vector3 RawPosition { get; set; }

            /// <summary>
            /// Gets or sets Historical Filtered Position.  
            /// </summary>
            public Vector3 FilteredPosition { get; set; }

            /// <summary>
            /// Gets or sets Historical Trend.  
            /// </summary>
            public Vector3 Trend { get; set; }

            /// <summary>
            /// Gets or sets Historical FrameCount.  
            /// </summary>
            public uint FrameCount { get; set; }
        }
    }
}