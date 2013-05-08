// -----------------------------------------------------------------------
// <copyright file="EngagementStateManager.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.InteractionGallery.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Kinect;
    using Microsoft.Kinect.Toolkit.Controls;

    /// <summary>
    /// Helper class that keeps track of user engagement state for application.
    /// </summary>
    internal class EngagementStateManager
    {
        /// <summary>
        /// Defines an invalid tracking identifier.
        /// </summary>
        internal const int InvalidUserTrackingId = 0;

        /// <summary>
        /// Length (in milliseconds) of period of inactivity required
        /// before users become candidates for tracking.
        /// </summary>
        internal const long MinimumInactivityBeforeTracking = 500;

        private readonly UserActivityMeter activityMeter = new UserActivityMeter();

        public EngagementStateManager()
        {
            this.TrackedUserTrackingIds = new HashSet<int>();
        }

        public event EventHandler<EventArgs> TrackedUsersChanged;

        public event EventHandler<UserTrackingIdChangedEventArgs> EngagedUserChanged;

        public event EventHandler<UserTrackingIdChangedEventArgs> CandidateUserChanged;

        public event EventHandler<UserTrackingIdChangedEventArgs> PrimaryUserChanged;

        public HashSet<int> TrackedUserTrackingIds { get; private set; }

        public int EngagedUserTrackingId { get; internal set; }

        public int CandidateUserTrackingId { get; internal set; }

        public int PrimaryUserTrackingId { get; internal set; }

        /// <summary>
        /// Resets all state to the initial state, with no users remembered as engaged or tracked.
        /// </summary>
        public void Reset()
        {
            using (var section = new EventQueueSection())
            {
                this.activityMeter.Clear();
                this.TrackedUserTrackingIds.Clear();
                this.PrimaryUserTrackingId = InvalidUserTrackingId;
                this.SetCandidateUserTrackingId(InvalidUserTrackingId, section);
                this.SetEngagedUserTrackingId(InvalidUserTrackingId, section);
                this.SendTrackedUsersChanged(section);
            }
        }

        /// <summary>
        /// Determines which users should be tracked in the future, based on selection
        /// metrics and engagement state.
        /// </summary>
        /// <param name="frameSkeletons">
        /// Array of skeletons from which the appropriate user tracking Ids will be selected.
        /// </param>
        /// <param name="timestamp">
        /// Timestamp from skeleton frame.
        /// </param>
        /// <param name="chosenTrackingIds">
        /// Array that will contain the tracking Ids of users to track, sorted from most
        /// important to least important user to track.
        /// </param>
        public void ChooseTrackedUsers(Skeleton[] frameSkeletons, long timestamp, int[] chosenTrackingIds)
        {
            var availableSkeletons = new List<Skeleton>(
                from skeleton in frameSkeletons
                where
                    (skeleton.TrackingId != InvalidUserTrackingId)
                    &&
                    ((skeleton.TrackingState == SkeletonTrackingState.Tracked)
                     || (skeleton.TrackingState == SkeletonTrackingState.PositionOnly))
                select skeleton);
            var trackingCandidateSkeletons = new List<Skeleton>();

            // Update user activity metrics
            this.activityMeter.Update(availableSkeletons, timestamp);

            foreach (var skeleton in availableSkeletons)
            {
                UserActivityRecord record;
                if (this.activityMeter.TryGetActivityRecord(skeleton.TrackingId, out record)
                     &&
                     ((skeleton.TrackingId == this.EngagedUserTrackingId)
                       ||
                       (skeleton.TrackingId == this.CandidateUserTrackingId)
                       ||
                       (!record.IsActive && (record.StateTransitionTimestamp + MinimumInactivityBeforeTracking <= timestamp))))
                {
                    // The tracked skeletons only become candidate skeletons for tracking if they correspond to engaged or
                    // engagement candidate users, or if users are inactive for at least a threshold period of time.
                    trackingCandidateSkeletons.Add(skeleton);
                }
            }

            // sort the currently tracked skeletons according to our tracking choice criteria
            trackingCandidateSkeletons.Sort((left, right) => ComputeTrackingMetric(right).CompareTo(ComputeTrackingMetric(left)));

            for (int i = 0; i < chosenTrackingIds.Length; ++i)
            {
                chosenTrackingIds[i] = (i < trackingCandidateSkeletons.Count) ? trackingCandidateSkeletons[i].TrackingId : InvalidUserTrackingId;
            }
        }

        /// <summary>
        /// Handler for KinectRegion.QueryPrimaryUserTrackingIdCallback delegate.
        /// Chooses who the primary user should be.
        /// </summary>
        /// <param name="proposedTrackingId">
        /// Tracking Id of proposed primary user.
        /// </param>
        /// <param name="candidateHandPointers">
        /// Collection of user hand pointers from which client can choose a
        /// primary user.
        /// </param>
        /// <returns>
        /// The tracking Id of chosen primary user. 0 means that no user should be considered primary.
        /// </returns>
        public int QueryPrimaryUserCallback(int proposedTrackingId, IEnumerable<HandPointer> candidateHandPointers)
        {
            int chosenTrackingId = proposedTrackingId;

            // If we're not tracking an engaged user or a candidate user, there is
            // no information for us to choose a primary user.
            if ((this.EngagedUserTrackingId == InvalidUserTrackingId) && (this.CandidateUserTrackingId == InvalidUserTrackingId))
            {
                return chosenTrackingId;
            }

            foreach (var handPointer in candidateHandPointers)
            {
                if (this.EngagedUserTrackingId == handPointer.TrackingId)
                {
                    // We only override the default primary user logic when there's already
                    // an engaged user that is not the same as the recommended primary user,
                    // and the engaged user has an active primary hand.
                    if ((this.EngagedUserTrackingId != proposedTrackingId) && handPointer.IsPrimaryHandOfUser)
                    {
                        chosenTrackingId = this.EngagedUserTrackingId;
                    }
                }
            }

            return chosenTrackingId;
        }

        /// <summary>
        /// Called whenever the set of tracked hand pointers has changed.
        /// </summary>
        /// <param name="trackedHandPointers">
        /// Hand pointers from which we'll update the set of tracked users and the primary user.
        /// </param>
        public void UpdateHandPointers(IEnumerable<HandPointer> trackedHandPointers)
        {
            bool foundEngagedUser = false;
            bool foundCandidateUser = false;
            int primaryUserTrackingId = InvalidUserTrackingId;

            using (var section = new EventQueueSection())
            {
                this.TrackedUserTrackingIds.Clear();

                foreach (var handPointer in trackedHandPointers)
                {
                    if (handPointer.IsTracked && (handPointer.TrackingId != InvalidUserTrackingId))
                    {
                        // Only consider valid user tracking ids
                        this.TrackedUserTrackingIds.Add(handPointer.TrackingId);

                        if (this.EngagedUserTrackingId == handPointer.TrackingId)
                        {
                            foundEngagedUser = true;
                        }

                        if (this.CandidateUserTrackingId == handPointer.TrackingId)
                        {
                            foundCandidateUser = true;
                        }

                        if (handPointer.IsPrimaryUser)
                        {
                            primaryUserTrackingId = handPointer.TrackingId;
                        }
                    }
                }

                this.SendTrackedUsersChanged(section);

                // If engaged user was not found in list of candidate users, engaged user has become invalid.
                if (!foundEngagedUser)
                {
                    this.SetEngagedUserTrackingId(InvalidUserTrackingId, section);
                }

                // If candidate user was not found in list of candidate users, candidate user has become invalid.
                if (!foundCandidateUser)
                {
                    this.SetCandidateUserTrackingId(InvalidUserTrackingId, section);
                }

                this.SetPrimaryUserTrackingId(primaryUserTrackingId, section);
            }
        }

        /// <summary>
        /// Promote candidate user to be the engaged user.
        /// </summary>
        /// <param name="candidateTrackingId">
        /// Tracking Id of user to be promoted to engaged user.
        /// If tracking Id does not match the Id of current candidate user,
        /// no action is taken.
        /// </param>
        /// <returns>
        /// True if specified candidate could be confirmed as the new engaged user,
        /// false otherwise.
        /// </returns>
        public bool ConfirmCandidateEngagement(int candidateTrackingId)
        {
            bool isConfirmed = false;

            if ((candidateTrackingId != InvalidUserTrackingId) && (candidateTrackingId == this.CandidateUserTrackingId))
            {
                using (var section = new EventQueueSection())
                {
                    this.SetCandidateUserTrackingId(InvalidUserTrackingId, section);
                    this.SetEngagedUserTrackingId(candidateTrackingId, section);
                }

                isConfirmed = true;
            }

            return isConfirmed;
        }

        /// <summary>
        /// Tries to get the last position observed for the specified user tracking Id.
        /// </summary>
        /// <param name="trackingId">
        /// User tracking Id for which we're finding the last position observed.
        /// </param>
        /// <returns>
        /// Skeleton point, if last position is being tracked for specified
        /// tracking Id, null otherwise.
        /// </returns>
        public SkeletonPoint? TryGetLastPositionForId(int trackingId)
        {
            if (InvalidUserTrackingId == trackingId)
            {
                return null;
            }

            UserActivityRecord record;
            if (this.activityMeter.TryGetActivityRecord(trackingId, out record))
            {
                return record.LastPosition;
            }

            return null;
        }

        internal void SetPrimaryUserTrackingId(int newId, EventQueueSection section)
        {
            int oldId = this.PrimaryUserTrackingId;
            this.PrimaryUserTrackingId = newId;

            if (oldId != newId)
            {
                section.Enqueue(
                    () =>
                    {
                        if (this.PrimaryUserChanged != null)
                        {
                            var args = new UserTrackingIdChangedEventArgs(oldId, newId);
                            this.PrimaryUserChanged(this, args);
                        }
                    });

                // If the new primary user is the same as the engaged user, then there is no candidate user.
                // Otherwise, we have a new candidate user as long as the new primary user is a valid user.
                this.SetCandidateUserTrackingId(
                    (this.EngagedUserTrackingId != InvalidUserTrackingId) && (this.EngagedUserTrackingId == newId)
                        ? InvalidUserTrackingId
                        : newId,
                    section);
            }
        }

        /// <summary>
        /// Calculate how valuable it will be to keep tracking the specified skeleton.
        /// </summary>
        /// <param name="skeleton">
        /// Skeleton that is one of several candidates for tracking.
        /// </param>
        /// <returns>
        /// A non-negative metric that estimates how valuable it is to keep tracking
        /// the specified skeleton. The higher the value, the more valuable the skeleton
        /// is estimated to be.
        /// </returns>
        private double ComputeTrackingMetric(Skeleton skeleton)
        {
            const double MaxCameraDistance = 4.0;

            // Give preference to engaged users, then to candidate users, then to users
            // near the center of the Kinect Sensor's field of view that are also
            // closer (distance) to the KinectSensor and not moving around too much.
            const double EngagedWeight = 100.0;
            const double CandidateWeight = 50.0;
            const double AngleFromCenterWeight = 1.30;
            const double DistanceFromCameraWeight = 1.15;
            const double BodyMovementWeight = 0.05;

            double engagedMetric = (skeleton.TrackingId == this.EngagedUserTrackingId) ? 1.0 : 0.0;
            double candidateMetric = (skeleton.TrackingId == this.CandidateUserTrackingId) ? 1.0 : 0.0;
            double angleFromCenterMetric = (skeleton.Position.Z > 0.0) ? (1.0 - Math.Abs(2 * Math.Atan(skeleton.Position.X / skeleton.Position.Z) / Math.PI)) : 0.0;
            double distanceFromCameraMetric = (MaxCameraDistance - skeleton.Position.Z) / MaxCameraDistance;
            UserActivityRecord activityRecord;
            double bodyMovementMetric = this.activityMeter.TryGetActivityRecord(skeleton.TrackingId, out activityRecord)
                                            ? 1.0 - activityRecord.ActivityLevel
                                            : 0.0;
            return (EngagedWeight * engagedMetric) +
                (CandidateWeight * candidateMetric) +
                (AngleFromCenterWeight * angleFromCenterMetric) +
                (DistanceFromCameraWeight * distanceFromCameraMetric) +
                (BodyMovementWeight * bodyMovementMetric);
        }

        private void SendTrackedUsersChanged(EventQueueSection section)
        {
            section.Enqueue(
                () =>
                    {
                        if (this.TrackedUsersChanged != null)
                        {
                            this.TrackedUsersChanged(this, EventArgs.Empty);
                        }
                    });
        }

        private void SetEngagedUserTrackingId(int newId, EventQueueSection section)
        {
            int oldId = this.EngagedUserTrackingId;
            this.EngagedUserTrackingId = newId;

            if (oldId != newId)
            {
                section.Enqueue(
                    () =>
                        {
                            if (this.EngagedUserChanged != null)
                            {
                                var args = new UserTrackingIdChangedEventArgs(oldId, newId);
                                this.EngagedUserChanged(this, args);
                            }
                        });
            }
        }

        private void SetCandidateUserTrackingId(int newId, EventQueueSection section)
        {
            int oldId = this.CandidateUserTrackingId;
            this.CandidateUserTrackingId = newId;

            if (oldId != newId)
            {
                section.Enqueue(
                    () =>
                        {
                            if (this.CandidateUserChanged != null)
                            {
                                var args = new UserTrackingIdChangedEventArgs(oldId, newId);
                                this.CandidateUserChanged(this, args);
                            }
                        });
            }
        }
    }
}
