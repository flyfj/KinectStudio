//------------------------------------------------------------------------------
// <copyright file="KinectController.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.InteractionGallery.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.Linq;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Threading;

    using Microsoft.Kinect;
    using Microsoft.Kinect.Toolkit;
    using Microsoft.Kinect.Toolkit.Controls;
    using Microsoft.Samples.Kinect.InteractionGallery.Navigation;
    using Microsoft.Samples.Kinect.InteractionGallery.Utilities;

    /// <summary>
    /// Manages the lifetime of the Kinect sensor and calculates the current controlling user.
    /// </summary>
    [Export(typeof(KinectController))]
    public class KinectController : ViewModelBase
    {
        /// <summary>
        /// Duration of time interval (in seconds) over which the state of the engagement
        /// handoff prompts state remains in stasis after handoff is confirmed.
        /// </summary>
        private const double HandoffConfirmationStasisSeconds = 0.5;

        /// <summary>
        /// Component that manages finding a Kinect sensor
        /// </summary>
        private readonly KinectSensorChooser sensorChooser = new KinectSensorChooser();

        /// <summary>
        /// Component that keeps track of engagement state
        /// </summary>
        private readonly EngagementStateManager engagementStateManager = new EngagementStateManager();

        /// <summary>
        /// Duration of time interval after which application navigates back to attract
        /// screen when a user disengages when there IS another user to whom control
        /// could be handed off.
        /// </summary>
        private readonly TimeSpan disengagementHandoffNavigationTimeout = TimeSpan.FromSeconds(10.0);

        /// <summary>
        /// Duration of time interval after which application navigates back to attract
        /// screen when a user disengages when there are NO other users to whom control
        /// could be handed off.
        /// </summary>
        private readonly TimeSpan disengagementNoHandoffNavigationTimeout = TimeSpan.FromSeconds(2.0);

        /// <summary>
        /// Command that is executed on shutdown to cleanup
        /// </summary>
        private readonly RelayCommand shutdownCommand;

        /// <summary>
        /// Command that is executed when candidate user has confirmed intent to engage
        /// when there is no engaged user present.
        /// </summary>
        private readonly RelayCommand<RoutedEventArgs> engagementConfirmationCommand;

        /// <summary>
        /// Command that is executed when candidate user has confirmed intent to engage
        /// when there is already an engaged user present.
        /// </summary>
        private readonly RelayCommand<RoutedEventArgs> engagementHandoffConfirmationCommand;

        /// <summary>
        /// Ids of users we choose to track.
        /// </summary>
        private readonly int[] recommendedUserTrackingIds = new int[2];

        /// <summary>
        /// Timer used to keep track of time interval over which the state of the engagement
        /// handoff prompts state remains in stasis after handoff is confirmed.
        /// </summary>
        private readonly DispatcherTimer handoffConfirmationStasisTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(HandoffConfirmationStasisSeconds) };

        /// <summary>
        /// Timer used to keep track of time interval (in seconds) after which application
        /// navigates back to attract screen when a user disengages.
        /// </summary>
        private readonly DispatcherTimer disengagementNavigationTimer = new DispatcherTimer();

        /// <summary>
        /// Array of skeletons to process in each frame.
        /// </summary>
        private Skeleton[] skeletons;

        /// <summary>
        /// Boolean determining whether engagement state is currently being overridden.
        /// </summary>
        private bool isInEngagementOverrideMode;

        /// <summary>
        /// Boolean determining whether any user is currently engaged.
        /// </summary>
        private bool isUserEngaged;

        /// <summary>
        /// Boolean determining whether any user is currently a candidate for engagement.
        /// </summary>
        private bool isUserEngagementCandidate;

        /// <summary>
        /// Boolean determining whether any user is currently engaged or a candidate for engagement.
        /// </summary>
        private bool isUserActive;

        /// <summary>
        /// Boolean determining whether any user is currently being tracked.
        /// </summary>
        private bool isUserTracked;

        /// <summary>
        /// State of start banner prompt.
        /// </summary>
        private PromptState startBannerState = PromptState.Hidden;

        /// <summary>
        /// State of engagement confirmation prompt.
        /// </summary>
        private PromptState engagementConfirmationState = PromptState.Hidden;

        /// <summary>
        /// True if engagement handoff barrier should be enabled (to prevent
        /// candidate user from interacting with application UI), false otherwise.
        /// </summary>
        private bool isEngagementHandoffBarrierEnabled;

        /// <summary>
        /// State of handoff message for user on the left.
        /// </summary>
        private PromptState leftHandoffMessageState = PromptState.Hidden;

        /// <summary>
        /// Handoff message text for user on the left.
        /// </summary>
        private string leftHandoffMessageText;

        /// <summary>
        /// Handoff message background brush for user on the left.
        /// </summary>
        private Brush leftHandoffMessageBrush;

        /// <summary>
        /// State of handoff confirmation prompt for user on the left.
        /// </summary>
        private PromptState leftHandoffConfirmationState = PromptState.Hidden;

        /// <summary>
        /// State of handoff message for user on the right.
        /// </summary>
        private PromptState rightHandoffMessageState = PromptState.Hidden;

        /// <summary>
        /// Handoff message text for user on the right.
        /// </summary>
        private string rightHandoffMessageText;

        /// <summary>
        /// Handoff message background brush for user on the left.
        /// </summary>
        private Brush rightHandoffMessageBrush;

        /// <summary>
        /// State of handoff confirmation prompt for user on the right.
        /// </summary>
        private PromptState rightHandoffConfirmationState = PromptState.Hidden;

        public KinectController()
        {
            this.QueryPrimaryUserCallback = this.OnQueryPrimaryUserCallback;
            this.PreEngagementUserColors = new Dictionary<int, Color>();
            this.PostEngagementUserColors = new Dictionary<int, Color>();

            this.engagementStateManager.TrackedUsersChanged += this.OnEngagementManagerTrackedUsersChanged;
            this.engagementStateManager.CandidateUserChanged += this.OnEngagementManagerCandidateUserChanged;
            this.engagementStateManager.EngagedUserChanged += this.OnEngagementManagerEngagedUserChanged;
            this.engagementStateManager.PrimaryUserChanged += this.OnEngagementManagerPrimaryUserChanged;

            this.handoffConfirmationStasisTimer.Tick += this.OnHandoffConfirmationStasisTimerTick;
            this.disengagementNavigationTimer.Tick += this.OnDisengagementNavigationTick;

            this.shutdownCommand = new RelayCommand(this.Cleanup);
            this.engagementConfirmationCommand = new RelayCommand<RoutedEventArgs>(this.OnEngagementConfirmation);
            this.engagementHandoffConfirmationCommand = new RelayCommand<RoutedEventArgs>(this.OnEngagementHandoffConfirmation);

            this.sensorChooser.KinectChanged += this.SensorChooserOnKinectChanged;
            this.sensorChooser.Start();
        }

        public override NavigationManager NavigationManager
        {
            get
            {
                return base.NavigationManager;
            }
            
            protected set
            {
                if (null != base.NavigationManager)
                {
                    base.NavigationManager.PropertyChanged -= this.OnNavigationManagerPropertyChanged;
                }

                base.NavigationManager = value;

                if (null != base.NavigationManager)
                {
                    base.NavigationManager.PropertyChanged += this.OnNavigationManagerPropertyChanged;
                }
            }
        }

        /// <summary>
        /// Gets the KinectSensorChooser component
        /// </summary>
        public KinectSensorChooser KinectSensorChooser
        {
            get { return this.sensorChooser; }
        }

        /// <summary>
        /// Gets the shutdown command
        /// </summary>
        public ICommand ShutdownCommand
        {
            get { return this.shutdownCommand; }
        }

        /// <summary>
        /// Gets the command that is executed when candidate user has confirmed intent to engage
        /// when there is no engaged user present.
        /// </summary>
        public ICommand EngagementConfirmationCommand
        {
            get { return this.engagementConfirmationCommand; }
        }

        /// <summary>
        /// Gets the command that is executed when candidate user has confirmed intent to engage
        /// when there is already an engaged user present.
        /// </summary>
        public ICommand EngagementHandoffConfirmationCommand
        {
            get { return this.engagementHandoffConfirmationCommand; }
        }

        /// <summary>
        /// Callback that chooses who the primary user should be.
        /// </summary>
        public QueryPrimaryUserTrackingIdCallback QueryPrimaryUserCallback { get; private set; }

        /// <summary>
        /// Gets whether engagement state is currently being overridden.
        /// </summary>
        public bool IsInEngagementOverrideMode
        {
            get
            {
                return this.isInEngagementOverrideMode;
            }

            set
            {
                this.isInEngagementOverrideMode = value;

                this.UpdateUserEngaged();
                this.UpdateUserTracked();
            }
        }

        /// <summary>
        /// Gets whether any user is currently engaged with the application.
        /// Changes to this property cause the PropertyChanged event to be signaled.
        /// </summary>
        public bool IsUserEngaged
        {
            get
            {
                return this.isUserEngaged;
            }

            protected set
            {
                bool wasEngaged = this.isUserEngaged;

                this.isUserEngaged = value;
                this.OnPropertyChanged("IsUserEngaged");

                if (wasEngaged != this.isUserEngaged)
                {
                    this.PerformEngagementChangeNavigation();
                }

                this.UpdateCurrentNavigationContextState();
                this.UpdateUserActive();
                this.UpdateStartBannerState();
                this.UpdateEngagementHandoffBarrier();
            }
        }

        /// <summary>
        /// Gets whether any user is currently an engagement candidate.
        /// Changes to this property cause the PropertyChanged event to be signaled.
        /// </summary>
        public bool IsUserEngagementCandidate
        {
            get
            {
                return this.isUserEngagementCandidate;
            }

            protected set
            {
                this.isUserEngagementCandidate = value;
                this.OnPropertyChanged("IsUserEngagementCandidate");

                this.UpdateUserActive();
                this.UpdateStartBannerState();
                this.UpdateEngagementConfirmationState();
                this.UpdateEngagementHandoffBarrier();
            }
        }

        /// <summary>
        /// Gets whether any user is currently engaged with the application or is an
        /// engagement candidate.
        /// Changes to this property cause the PropertyChanged event to be signaled.
        /// </summary>
        public bool IsUserActive
        {
            get
            {
                return this.isUserActive;
            }

            protected set
            {
                this.isUserActive = value;
                this.OnPropertyChanged("IsUserActive");
            }
        }

        /// <summary>
        /// Gets whether any user is currently being tracked by the application.
        /// Changes to this property cause the PropertyChanged event to be signaled.
        /// </summary>
        public bool IsUserTracked
        {
            get
            {
                return this.isUserTracked;
            }

            protected set
            {
                this.isUserTracked = value;
                this.OnPropertyChanged("IsUserTracked");

                this.UpdateStartBannerState();
            }
        }

        /// <summary>
        /// Gets the current state of start banner prompt.
        /// Changes to this property cause the PropertyChanged event to be signaled.
        /// </summary>
        public PromptState StartBannerState
        {
            get
            {
                return this.startBannerState;
            }

            protected set
            {
                this.startBannerState = value;
                this.OnPropertyChanged("StartBannerState");
            }
        }

        /// <summary>
        /// Gets the current state of engagement confirmation prompt.
        /// Changes to this property cause the PropertyChanged event to be signaled.
        /// </summary>
        public PromptState EngagementConfirmationState
        {
            get
            {
                return this.engagementConfirmationState;
            }

            protected set
            {
                this.engagementConfirmationState = value;
                this.OnPropertyChanged("EngagementConfirmationState");
            }
        }

        /// <summary>
        /// Gets whether the engagement handoff confirmation barrier should be enabled.
        /// Changes to this property cause the PropertyChanged event to be signaled.
        /// </summary>
        public bool IsEngagementHandoffBarrierEnabled
        {
            get
            {
                return this.isEngagementHandoffBarrierEnabled;
            }

            protected set
            {
                this.isEngagementHandoffBarrierEnabled = value;
                this.OnPropertyChanged("IsEngagementHandoffBarrierEnabled");
            }
        }

        /// <summary>
        /// Gets the current state of handoff message for user on the left.
        /// Changes to this property cause the PropertyChanged event to be signaled.
        /// </summary>
        public PromptState LeftHandoffMessageState
        {
            get
            {
                return this.leftHandoffMessageState;
            }

            protected set
            {
                this.leftHandoffMessageState = value;
                this.OnPropertyChanged("LeftHandoffMessageState");
            }
        }

        /// <summary>
        /// Gets the current handoff message text for user on the left.
        /// Changes to this property cause the PropertyChanged event to be signaled.
        /// </summary>
        public string LeftHandoffMessageText
        {
            get
            {
                return this.leftHandoffMessageText;
            }

            protected set
            {
                this.leftHandoffMessageText = value;
                this.OnPropertyChanged("LeftHandoffMessageText");
            }
        }

        /// <summary>
        /// Gets the current handoff message background brush for user on the left.
        /// Changes to this property cause the PropertyChanged event to be signaled.
        /// </summary>
        public Brush LeftHandoffMessageBrush
        {
            get
            {
                return this.leftHandoffMessageBrush;
            }

            protected set
            {
                this.leftHandoffMessageBrush = value;
                this.OnPropertyChanged("LeftHandoffMessageBrush");
            }
        }

        /// <summary>
        /// Gets the current state of handoff confirmation prompt for user on the left.
        /// Changes to this property cause the PropertyChanged event to be signaled.
        /// </summary>
        public PromptState LeftHandoffConfirmationState
        {
            get
            {
                return this.leftHandoffConfirmationState;
            }

            protected set
            {
                this.leftHandoffConfirmationState = value;
                this.OnPropertyChanged("LeftHandoffConfirmationState");
            }
        }

        /// <summary>
        /// Gets the current state of handoff message for user on the right.
        /// Changes to this property cause the PropertyChanged event to be signaled.
        /// </summary>
        public PromptState RightHandoffMessageState
        {
            get
            {
                return this.rightHandoffMessageState;
            }

            protected set
            {
                this.rightHandoffMessageState = value;
                this.OnPropertyChanged("RightHandoffMessageState");
            }
        }

        /// <summary>
        /// Gets the current handoff message text for user on the right.
        /// Changes to this property cause the PropertyChanged event to be signaled.
        /// </summary>
        public string RightHandoffMessageText
        {
            get
            {
                return this.rightHandoffMessageText;
            }

            protected set
            {
                this.rightHandoffMessageText = value;
                this.OnPropertyChanged("RightHandoffMessageText");
            }
        }

        /// <summary>
        /// Gets the current handoff message background brush for user on the right.
        /// Changes to this property cause the PropertyChanged event to be signaled.
        /// </summary>
        public Brush RightHandoffMessageBrush
        {
            get
            {
                return this.rightHandoffMessageBrush;
            }

            protected set
            {
                this.rightHandoffMessageBrush = value;
                this.OnPropertyChanged("RightHandoffMessageBrush");
            }
        }

        /// <summary>
        /// Gets the current state of handoff confirmation prompt for user on the left.
        /// Changes to this property cause the PropertyChanged event to be signaled.
        /// </summary>
        public PromptState RightHandoffConfirmationState
        {
            get
            {
                return this.rightHandoffConfirmationState;
            }

            protected set
            {
                this.rightHandoffConfirmationState = value;
                this.OnPropertyChanged("RightHandoffConfirmationState");
            }
        }

        /// <summary>
        /// Color used to represent the engaged user.
        /// </summary>
        public Color EngagedUserColor { get; set; }

        /// <summary>
        /// Color used to represent non-engaged tracked users.
        /// </summary>
        public Color TrackedUserColor { get; set; }

        /// <summary>
        /// Brush used to paint the background of a message intended for the engaged user.
        /// </summary>
        public Brush EngagedUserMessageBrush { get; set; }

        /// <summary>
        /// Brush used to paint the background of a message intended for non-engaged tracked users.
        /// </summary>
        public Brush TrackedUserMessageBrush { get; set; }

        /// <summary>
        /// Dictionary mapping user tracking Ids to colors corresponding to those users in
        /// UI shown before initial engagement.
        /// </summary>
        public Dictionary<int, Color> PreEngagementUserColors { get; private set; }

        /// <summary>
        /// Dictionary mapping user tracking Ids to colors corresponding to those users in
        /// UI shown after initial engagement.
        /// </summary>
        public Dictionary<int, Color> PostEngagementUserColors { get; private set; }

        /// <summary>
        /// Should be called whenever the set of actively tracked hand pointers
        /// is updated.
        /// </summary>
        /// <param name="handPointers">
        /// Collection of hand pointers currently being tracked.
        /// </param>
        internal void OnHandPointersUpdated(ICollection<HandPointer> handPointers)
        {
            this.engagementStateManager.UpdateHandPointers(handPointers);
        }

        private void SensorChooserOnKinectChanged(object sender, KinectChangedEventArgs e)
        {
            KinectSensor oldSensor = e.OldSensor;
            KinectSensor newSensor = e.NewSensor;

            if (null != oldSensor)
            {
                try
                {
                    oldSensor.SkeletonFrameReady -= this.OnSkeletonFrameReady;
                    oldSensor.SkeletonStream.AppChoosesSkeletons = false;
                    oldSensor.DepthStream.Range = DepthRange.Default;
                    oldSensor.SkeletonStream.EnableTrackingInNearRange = false;
                    oldSensor.DepthStream.Disable();
                    oldSensor.SkeletonStream.Disable();
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }

                this.engagementStateManager.Reset();
            }

            if (null != newSensor)
            {
                try
                {
                    newSensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                    newSensor.SkeletonStream.Enable();

                    try
                    {
                        newSensor.DepthStream.Range = DepthRange.Near;
                        newSensor.SkeletonStream.EnableTrackingInNearRange = true;
                    }
                    catch (InvalidOperationException)
                    {
                        // Non Kinect for Windows devices do not support Near mode, so reset back to default mode.
                        newSensor.DepthStream.Range = DepthRange.Default;
                        newSensor.SkeletonStream.EnableTrackingInNearRange = false;
                    }

                    newSensor.SkeletonStream.AppChoosesSkeletons = true;
                    newSensor.SkeletonFrameReady += this.OnSkeletonFrameReady;
                }
                catch (InvalidOperationException)
                {
                    // KinectSensor might enter an invalid state while enabling/disabling streams or stream features.
                    // E.g.: sensor might be abruptly unplugged.
                }
            }

            // Whenever the Kinect sensor changes, we have no controlling user, so reset to attract screen
            this.NavigationManager.NavigateToHome(DefaultNavigableContexts.AttractScreen);
            this.IsUserEngaged = false;
        }

        private void Cleanup()
        {
            this.sensorChooser.Stop();
        }

        private void OnSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            bool haveSkeletons = false;
            long timestamp = 0;

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (null != skeletonFrame)
                {
                    if ((null == this.skeletons) || (this.skeletons.Length != skeletonFrame.SkeletonArrayLength))
                    {
                        this.skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    }

                    // Let engagement state manager choose which users to track.
                    skeletonFrame.CopySkeletonDataTo(this.skeletons);
                    timestamp = skeletonFrame.Timestamp;
                    haveSkeletons = true;
                }
            }

            if (haveSkeletons)
            {
                this.engagementStateManager.ChooseTrackedUsers(this.skeletons, timestamp, this.recommendedUserTrackingIds);

                var sensor = sender as KinectSensor;
                if (null != sensor)
                {
                    try
                    {
                        sensor.SkeletonStream.ChooseSkeletons(this.recommendedUserTrackingIds[0], this.recommendedUserTrackingIds[1]);
                    }
                    catch (InvalidOperationException)
                    {
                        // KinectSensor might enter an invalid state while choosing skeletons.
                        // E.g.: sensor might be abruptly unplugged.
                    }
                }
            }
        }

        /// <summary>
        /// Handler for KinectRegion.QueryPrimaryUserTrackingIdCallback.
        /// </summary>
        /// <param name="proposedTrackingId">
        /// Tracking Id of proposed primary user.
        /// </param>
        /// <param name="candidateHandPointers">
        /// Collection of information about hand pointers from which client can choose\
        /// a primary user.
        /// </param>
        /// <param name="timestamp">
        /// Time when delegate was called. Corresponds to InteractionStream and
        /// KinectSensor event timestamps.
        /// </param>
        /// <returns>
        /// The tracking Id of chosen primary user. 0 means that no user should be considered primary.
        /// </returns>
        private int OnQueryPrimaryUserCallback(int proposedTrackingId, IEnumerable<HandPointer> candidateHandPointers, long timestamp)
        {
            return this.engagementStateManager.QueryPrimaryUserCallback(proposedTrackingId, candidateHandPointers);
        }

        /// <summary>
        /// Handler for engagement confirmation command.
        /// </summary>
        /// <param name="e">
        /// Event arguments.
        /// </param>
        /// <remarks>
        /// If this event is triggered, it means that a user has confirmed the
        /// intent to engage while no other user was engaged.
        /// </remarks>
        private void OnEngagementConfirmation(RoutedEventArgs e)
        {
            if (this.engagementStateManager.ConfirmCandidateEngagement(this.engagementStateManager.CandidateUserTrackingId))
            {
                this.UpdateEngagementConfirmationState();
            }
        }

        /// <summary>
        /// Handler for engagement handoff confirmation command.
        /// </summary>
        /// <param name="e">
        /// Event arguments.
        /// </param>
        /// <remarks>
        /// If this event is triggered, it means that a user has confirmed the
        /// intent to engage when another user was already engaged.
        /// </remarks>
        private void OnEngagementHandoffConfirmation(RoutedEventArgs e)
        {
            if (this.engagementStateManager.ConfirmCandidateEngagement(this.engagementStateManager.CandidateUserTrackingId))
            {
                this.UpdateEngagementHandoffBarrier();
                this.UpdateEngagementHandoffState(true);
            }
        }

        /// <summary>
        /// Event handler for EngagementStateManager.TrackedUsersChanged.
        /// </summary>
        /// <param name="sender">
        /// Object that sent the event.
        /// </param>
        /// <param name="e">
        /// Event arguments.
        /// </param>
        private void OnEngagementManagerTrackedUsersChanged(object sender, EventArgs e)
        {
            this.UpdateUserTracked();
            this.UpdateUserColors();
            this.UpdateEngagementHandoffState(false);
        }

        /// <summary>
        /// Event handler for EngagementStateManager.EngagedUserChanged.
        /// </summary>
        /// <param name="sender">
        /// Object that sent the event.
        /// </param>
        /// <param name="e">
        /// Event arguments.
        /// </param>
        private void OnEngagementManagerEngagedUserChanged(object sender, UserTrackingIdChangedEventArgs e)
        {
            this.UpdateUserEngaged();
        }

        /// <summary>
        /// Event handler for EngagementStateManager.CandidateUserChanged.
        /// </summary>
        /// <param name="sender">
        /// Object that sent the event.
        /// </param>
        /// <param name="e">
        /// Event arguments.
        /// </param>
        private void OnEngagementManagerCandidateUserChanged(object sender, UserTrackingIdChangedEventArgs e)
        {
            this.IsUserEngagementCandidate = EngagementStateManager.InvalidUserTrackingId != e.NewValue;
        }

        /// <summary>
        /// Event handler for EngagementStateManager.PrimaryUserChanged.
        /// </summary>
        /// <param name="sender">
        /// Object that sent the event.
        /// </param>
        /// <param name="e">
        /// Event arguments.
        /// </param>
        private void OnEngagementManagerPrimaryUserChanged(object sender, UserTrackingIdChangedEventArgs e)
        {
            this.UpdateCurrentNavigationContextState();
        }

        /// <summary>
        /// Update pre-engagement and post-engagement colors to be displayed in user viewers
        /// based on tracked, engaged and candidate users.
        /// </summary>
        private void UpdateUserColors()
        {
            this.PreEngagementUserColors.Clear();
            this.PostEngagementUserColors.Clear();

            foreach (var trackingId in this.engagementStateManager.TrackedUserTrackingIds)
            {
                if (trackingId == this.engagementStateManager.EngagedUserTrackingId)
                {
                    this.PreEngagementUserColors[trackingId] = this.EngagedUserColor;
                    this.PostEngagementUserColors[trackingId] = this.EngagedUserColor;
                }
                else
                {
                    this.PreEngagementUserColors[trackingId] = this.EngagedUserColor;

                    if ((this.engagementStateManager.EngagedUserTrackingId == EngagementStateManager.InvalidUserTrackingId) ||
                        (this.engagementStateManager.EngagedUserTrackingId != this.engagementStateManager.PrimaryUserTrackingId))
                    {
                        // Differentiate tracked users from background users only if there is no
                        // engaged user currently interacting.
                        this.PostEngagementUserColors[trackingId] = this.TrackedUserColor;
                    }
                }
            }
        }

        /// <summary>
        /// Update value of IsUserEngaged property from other properties that affect it.
        /// </summary>
        private void UpdateUserEngaged()
        {
            this.IsUserEngaged = this.IsInEngagementOverrideMode
                                 || (EngagementStateManager.InvalidUserTrackingId != this.engagementStateManager.EngagedUserTrackingId);
        }

        /// <summary>
        /// Update value of IsUserActive property from other properties that affect it.
        /// </summary>
        private void UpdateUserActive()
        {
            this.IsUserActive = this.IsUserEngagementCandidate || this.IsUserEngaged;
        }

        /// <summary>
        /// Update value of IsUserTracked property from other properties that affect it.
        /// </summary>
        private void UpdateUserTracked()
        {
            this.IsUserTracked = this.IsInEngagementOverrideMode || (this.engagementStateManager.TrackedUserTrackingIds.Count > 0);
        }

        /// <summary>
        /// Update value of StartBannerState property from other properties that affect it.
        /// </summary>
        private void UpdateStartBannerState()
        {
            var state = PromptState.Hidden;

            if (this.IsUserTracked)
            {
                state = this.IsUserEngagementCandidate || this.IsUserEngaged ? PromptState.Dismissed : PromptState.Prompting;
            }

            this.StartBannerState = state;
        }

        /// <summary>
        /// Update value of EngagementConfirmationState property from other properties that affect it.
        /// </summary>
        private void UpdateEngagementConfirmationState()
        {
            var state = PromptState.Hidden;

            if (this.IsUserEngaged)
            {
                state = PromptState.Dismissed;
            }
            else if (this.IsUserEngagementCandidate)
            {
                state = PromptState.Prompting;
            }

            this.EngagementConfirmationState = state;
        }

        /// <summary>
        /// Update value of IsEngagementHandoffBarrierEnabled property from other properties that affect it.
        /// </summary>
        private void UpdateEngagementHandoffBarrier()
        {
            this.IsEngagementHandoffBarrierEnabled = this.IsUserEngaged && this.IsUserEngagementCandidate;
        }

        /// <summary>
        /// Update values of properties related to engagement handoff from the values of other properties that
        /// affect them.
        /// </summary>
        private void UpdateEngagementHandoffState(bool confirmHandoff)
        {
            if (this.handoffConfirmationStasisTimer.IsEnabled)
            {
                // If timer is already running, wait for it to finish
                return;
            }

            if (confirmHandoff)
            {
                // If confirming handoff, mark handoff confirmation prompts as
                // dismissed and start timer to re-update state later.
                this.ClearEngagementHandoff();
                this.LeftHandoffConfirmationState = PromptState.Dismissed;
                this.RightHandoffConfirmationState = PromptState.Dismissed;
                this.handoffConfirmationStasisTimer.Start();

                return;
            }

            if ((this.engagementStateManager.EngagedUserTrackingId == EngagementStateManager.InvalidUserTrackingId) ||
                (this.engagementStateManager.EngagedUserTrackingId == this.engagementStateManager.PrimaryUserTrackingId) ||
                (this.engagementStateManager.TrackedUserTrackingIds.Count < 2))
            {
                // If we're currently transitioning engagement states, if there is no engaged
                // user, if engaged user is actively interacting, or there is nobody besides the
                // engaged user, then there is no need for engagement handoff UI to be shown.
                this.ClearEngagementHandoff();
                return;
            }

            int nonEngagedId = this.engagementStateManager.TrackedUserTrackingIds.FirstOrDefault(trackingId => trackingId != this.engagementStateManager.EngagedUserTrackingId);

            SkeletonPoint? lastEngagedPosition =
                this.engagementStateManager.TryGetLastPositionForId(this.engagementStateManager.EngagedUserTrackingId);
            SkeletonPoint? lastNonEngagedPosition = this.engagementStateManager.TryGetLastPositionForId(nonEngagedId);

            if (!lastEngagedPosition.HasValue || !lastNonEngagedPosition.HasValue)
            {
                // If we can't determine the relative position of engaged and non-engaged user,
                // we don't show an engagement handoff prompt at all.
                this.ClearEngagementHandoff();
                return;
            }

            PromptState engagedMessageState = PromptState.Hidden;
            string engagedMessageText = string.Empty;
            Brush engagedBrush = this.EngagedUserMessageBrush;
            PromptState engagedConfirmationState = PromptState.Hidden;
            PromptState nonEngagedMessageState = PromptState.Prompting;
            string nonEngagedMessageText = Properties.Resources.EngagementHandoffGetStarted;
            Brush nonEngagedBrush = this.TrackedUserMessageBrush;
            PromptState nonEngagedConfirmationState = PromptState.Hidden;

            if ((EngagementStateManager.InvalidUserTrackingId != this.engagementStateManager.CandidateUserTrackingId) &&
                (nonEngagedId == this.engagementStateManager.CandidateUserTrackingId))
            {
                // If non-engaged user is an engagement candidate
                engagedMessageState = PromptState.Prompting;
                engagedMessageText = Properties.Resources.EngagementHandoffKeepControl;
                nonEngagedMessageText = string.Empty;
                nonEngagedConfirmationState = PromptState.Prompting;
            }

            bool isEngagedOnLeft = lastEngagedPosition.Value.X < lastNonEngagedPosition.Value.X;

            this.LeftHandoffMessageState = isEngagedOnLeft ? engagedMessageState : nonEngagedMessageState;
            this.LeftHandoffMessageText = isEngagedOnLeft ? engagedMessageText : nonEngagedMessageText;
            this.LeftHandoffMessageBrush = isEngagedOnLeft ? engagedBrush : nonEngagedBrush;
            this.LeftHandoffConfirmationState = isEngagedOnLeft ? engagedConfirmationState : nonEngagedConfirmationState;
            this.RightHandoffMessageState = isEngagedOnLeft ? nonEngagedMessageState : engagedMessageState;
            this.RightHandoffMessageText = isEngagedOnLeft ? nonEngagedMessageText : engagedMessageText;
            this.RightHandoffMessageBrush = isEngagedOnLeft ? nonEngagedBrush : engagedBrush;
            this.RightHandoffConfirmationState = isEngagedOnLeft ? nonEngagedConfirmationState : engagedConfirmationState;
        }

        /// <summary>
        /// Reset properties related to engagement handoff to their default values.
        /// </summary>
        private void ClearEngagementHandoff()
        {
            this.LeftHandoffMessageState = PromptState.Hidden;
            this.LeftHandoffMessageText = string.Empty;
            this.LeftHandoffConfirmationState = PromptState.Hidden;
            this.RightHandoffMessageState = PromptState.Hidden;
            this.RightHandoffMessageText = string.Empty;
            this.RightHandoffConfirmationState = PromptState.Hidden;
        }

        /// <summary>
        /// Update state of current navigation context based on current controller state.
        /// </summary>
        private void UpdateCurrentNavigationContextState()
        {
            var viewModel = NavigationManager.CurrentNavigationContext as ViewModelBase;
            if (null != viewModel)
            {
                int primaryUserTrackingId = this.engagementStateManager.PrimaryUserTrackingId;
                int engagedUserTrackingId = this.engagementStateManager.EngagedUserTrackingId;

                // Application views should only care about interaction state of currently engaged user
                viewModel.IsUserInteracting = this.IsInEngagementOverrideMode
                                              ||
                                              ((primaryUserTrackingId != EngagementStateManager.InvalidUserTrackingId)
                                               && (primaryUserTrackingId == engagedUserTrackingId));
            }
        }

        /// <summary>
        /// Start timer used to navigate back to attract screen after user disengagement, with a
        /// timeout dependent on whether there are still other users present that might want to
        /// engage and prevent navigation.
        /// </summary>
        private void StartDisengagementNavigationTimer()
        {
            bool isAnotherUserTracked = this.engagementStateManager.TrackedUserTrackingIds.Any(trackingId => trackingId != EngagementStateManager.InvalidUserTrackingId);

            this.disengagementNavigationTimer.Interval = isAnotherUserTracked ? this.disengagementHandoffNavigationTimeout : this.disengagementNoHandoffNavigationTimeout;
            this.disengagementNavigationTimer.Start();
        }

        /// <summary>
        /// Navigate to the appropriate view given a recent change in engagement state.
        /// </summary>
        private void PerformEngagementChangeNavigation()
        {
            if (this.disengagementNavigationTimer.IsEnabled)
            {
                this.disengagementNavigationTimer.Stop();

                if (!this.IsUserEngaged)
                {
                    // If disengagement timer was already started, and another user got disengaged, reset timer
                    this.StartDisengagementNavigationTimer();
                }
                //// Else if a user just became engaged while waiting for disengagement timer to fire, don't take
                //// any navigation actions
            }
            else if (!this.disengagementNavigationTimer.IsEnabled)
            {
                if (this.IsUserEngaged)
                {
                    // If there was no engaged user and now there is, initiate a navigation to the home screen.
                    this.NavigationManager.NavigateToHome(DefaultNavigableContexts.HomeScreen);
                }
                else
                {
                    // Wait until timeout period before navigating to attract scren
                    this.StartDisengagementNavigationTimer();
                }
            }
            //// Else If we have just changed between interacting users, no navigation action is undertaken
        }

        /// <summary>
        /// Event handler for the Tick event of the handoff confirmation stasis timer.
        /// </summary>
        /// <param name="sender">
        /// Object that sent the event.
        /// </param>
        /// <param name="e">
        /// Event arguments.
        /// </param>
        /// <remarks>
        /// If this timer fires, it means that stasis period has expired, so it is time
        /// to confirm engagement handoff state.
        /// </remarks>
        private void OnHandoffConfirmationStasisTimerTick(object sender, EventArgs e)
        {
            this.handoffConfirmationStasisTimer.Stop();
            this.UpdateEngagementHandoffState(false);
        }

        /// <summary>
        /// Event handler for the Tick event of the disengagement navigation timer.
        /// </summary>
        /// <param name="sender">
        /// Object that sent the event.
        /// </param>
        /// <param name="e">
        /// Event arguments.
        /// </param>
        /// <remarks>
        /// If this timer fires it means that nobody took control of application after
        /// the previously engaged user became disengaged.
        /// </remarks>
        private void OnDisengagementNavigationTick(object sender, EventArgs e)
        {
            // If a user disengaged and nobody took control before timer expired, go back to attract screen
            this.disengagementNavigationTimer.Stop();
            this.NavigationManager.NavigateToHome(DefaultNavigableContexts.AttractScreen);
        }

        /// <summary>
        /// Event handler for NavigationManager.PropertyChanged.
        /// </summary>
        /// <param name="sender">
        /// Object that sent the event.
        /// </param>
        /// <param name="e">
        /// Event arguments.
        /// </param>
        private void OnNavigationManagerPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ("CurrentNavigationContext".Equals(e.PropertyName))
            {
                this.UpdateCurrentNavigationContextState();
            }
        }
    }
}
