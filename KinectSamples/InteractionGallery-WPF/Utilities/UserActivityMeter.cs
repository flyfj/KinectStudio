// -----------------------------------------------------------------------
// <copyright file="UserActivityMeter.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.InteractionGallery.Utilities
{
    using System.Collections.Generic;
    using Microsoft.Kinect;

    /// <summary>
    /// Helper class used to measure user activity.
    /// </summary>
    internal class UserActivityMeter
    {
        private readonly Dictionary<int, UserActivityRecord> activityRecords = new Dictionary<int, UserActivityRecord>();
        private int totalUpdatesSoFar;

        /// <summary>
        /// Clears all user activity metrics.
        /// </summary>
        public void Clear()
        {
            this.activityRecords.Clear();
        }

        /// <summary>
        /// Update user activity metrics with data from a collection of skeletons.
        /// </summary>
        /// <param name="skeletons">
        /// Collection of skeletons to use to update activity metrics.
        /// </param>
        /// <param name="timestamp">
        /// Time when skeleton array was received for processing.
        /// </param>
        /// <remarks>
        /// UserActivityMeter assumes that this method is called regularly, e.g.: once
        /// per skeleton frame received by application, so if a user whose activity was
        /// previously measured is now absent, activity record will be removed.
        /// </remarks>
        public void Update(ICollection<Skeleton> skeletons, long timestamp)
        {
            foreach (var skeleton in skeletons)
            {
                UserActivityRecord record;

                if (this.activityRecords.TryGetValue(skeleton.TrackingId, out record))
                {
                    record.Update(skeleton.Position, this.totalUpdatesSoFar, timestamp);
                }
                else
                {
                    record = new UserActivityRecord(skeleton.Position, this.totalUpdatesSoFar, timestamp);
                    this.activityRecords[skeleton.TrackingId] = record;
                }
            }

            // Remove activity records corresponding to users that are no longer being tracked
            var idsToRemove = new List<int>();
            foreach (var record in this.activityRecords)
            {
                if (record.Value.LastUpdateId != this.totalUpdatesSoFar)
                {
                    idsToRemove.Add(record.Key);
                }
            }

            foreach (var id in idsToRemove)
            {
                this.activityRecords.Remove(id);
            }

            ++this.totalUpdatesSoFar;
        }

        /// <summary>
        /// Gets the activity record associated with the specified user.
        /// </summary>
        /// <param name="userTrackingId">
        /// Skeleton tracking Id of user associated with the activity record to
        /// retrieve.
        /// </param>
        /// <param name="record">
        /// [out] When this method returns, contains the record associated with the
        /// specified user tracking Id, if the appropriate activity record is found.
        /// This parameter is passed uninitialized.
        /// </param>
        /// <returns>
        /// <code>true</code> if the UserActivityMeter contains an activity record
        /// for the specified user tracking Id; otherwise, <code>false</code>.
        /// </returns>
        public bool TryGetActivityRecord(int userTrackingId, out UserActivityRecord record)
        {
            return this.activityRecords.TryGetValue(userTrackingId, out record);
        }
    }
}
