// -----------------------------------------------------------------------
// <copyright file="UserActivityRecord.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.InteractionGallery.Utilities
{
    using System;

    using Microsoft.Kinect;

    /// <summary>
    /// Represents activity state and metrics for a single user.
    /// </summary>
    internal class UserActivityRecord
    {
        // Activity level, above which a user is considered to be in "active" state.
        private const double ActivityMetricThreshold = 0.1;

        private double activityLevel;

        public UserActivityRecord(SkeletonPoint position, int updateId, long timestamp)
        {
            this.ActivityLevel = 0.0;
            this.LastPosition = position;
            this.LastUpdateId = updateId;
            this.IsActive = false;
            this.StateTransitionTimestamp = timestamp;
        }

        /// <summary>
        /// User activity level metric being tracked.
        /// </summary>
        /// <remarks>
        /// Value is always in [0.0, 1.0] interval.
        /// </remarks>
        public double ActivityLevel
        {
            get
            {
                return this.activityLevel;
            }

            private set
            {
                this.activityLevel = Math.Max(0.0, Math.Min(1.0, value));
            }
        }

        /// <summary>
        /// Id of last update that touched this activity record.
        /// </summary>
        public int LastUpdateId { get; private set; }

        /// <summary>
        /// True if user activity is currently larger than the activity threshold.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Time when IsActive state last changed from true to false or vice versa.
        /// </summary>
        public long StateTransitionTimestamp { get; private set; }

        /// <summary>
        /// Last position where user was observed.
        /// </summary>
        public SkeletonPoint LastPosition { get; private set; }

        public void Update(SkeletonPoint position, int updateId, long timestamp)
        {
            // Movement magnitude gets scaled by this amount in order to get the current activity metric
            const double DeltaScalingFactor = 10.0;

            // Controls how quickly new values of the metric displace old values. 1.0 means that new values
            // for metric immediately replace old values, while smaller decay amounts mean that old metric
            // values influence the metric for a longer amount of time (i.e.: decay more slowly).
            const double ActivityDecay = 0.1;

            var delta = new SkeletonPoint
            {
                X = position.X - this.LastPosition.X,
                Y = position.Y - this.LastPosition.Y,
                Z = position.Z - this.LastPosition.Z
            };

            double deltaLengthSquared = (delta.X * delta.X) + (delta.Y * delta.Y) + (delta.Z * delta.Z);
            double newMetric = DeltaScalingFactor * Math.Sqrt(deltaLengthSquared);

            this.ActivityLevel = ((1.0 - ActivityDecay) * this.ActivityLevel) + (ActivityDecay * newMetric);

            bool newIsActive = this.ActivityLevel >= ActivityMetricThreshold;

            if (newIsActive != this.IsActive)
            {
                this.IsActive = newIsActive;
                this.StateTransitionTimestamp = timestamp;
            }

            this.LastPosition = position;
            this.LastUpdateId = updateId;
        }
    }
}
