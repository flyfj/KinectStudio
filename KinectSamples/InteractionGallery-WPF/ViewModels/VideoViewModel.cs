// -----------------------------------------------------------------------
// <copyright file="VideoViewModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.InteractionGallery.ViewModels
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using Microsoft.Kinect.Toolkit;
    using Microsoft.Samples.Kinect.InteractionGallery.Navigation;

    [ExportNavigable(NavigableContextName = DefaultNavigableContexts.VideoPlayer)]
    public class VideoViewModel : ViewModelBase
    {
        /// <summary>
        /// Uri pointing to the video to play
        /// </summary>
        private Uri video;

        /// <summary>
        /// Current state of the video player
        /// </summary>
        private State currentState = State.Disabled;

        /// <summary>
        /// Command that resumes play of the video
        /// </summary>
        private RelayCommand playCommand;

        /// <summary>
        /// Command that pauses the video
        /// </summary>
        private RelayCommand pauseCommand;

        /// <summary>
        /// Command that notifies the view model that the video has reset.
        /// </summary>
        private RelayCommand videoResetNotification;

        /// <summary>
        /// Initializes a new instance of the VideoViewModel class
        /// </summary>
        public VideoViewModel()
            : base()
        {
            this.playCommand = new RelayCommand(() => { this.CurrentState = State.Playing; }, () => { return this.CanResume || this.CanReplay; });
            this.pauseCommand = new RelayCommand(() => { this.CurrentState = State.Paused; }, () => { return this.CanPause; });
            this.videoResetNotification = new RelayCommand(() => { this.CurrentState = State.Stopped; });
        }

        /// <summary>
        /// Enumeration defining the mutually exclusive states of the video playback view
        /// </summary>
        private enum State
        {
            Disabled,
            Playing,
            Paused,
            Stopped,
        }

        /// <summary>
        /// Gets the Uri to the video. Changes to this property cause the PropertyChanged event to be signaled.
        /// </summary>
        public Uri Video
        {
            get 
            { 
                return this.video; 
            }

            private set
            {
                this.video = value;
                this.OnPropertyChanged("Video");
            }
        }

        /// <summary>
        /// Returns true if the video can be resumed from a currently paused state. False otherwise.
        /// </summary>
        public bool CanResume
        {
            get { return State.Paused == this.CurrentState; }
        }

        /// <summary>
        /// Returns true if the video can be paused from a currently playing state. False otherwise.
        /// </summary>
        public bool CanPause
        {
            get { return State.Playing == this.CurrentState; }
        }

        /// <summary>
        /// Returns true if the video has been reset and can be replayed from the beginning. False otherwise.
        /// </summary>
        public bool CanReplay
        {
            get { return State.Stopped == this.CurrentState; }
        }

        /// <summary>
        /// Gets the command to resume play of the video.
        /// </summary>
        public ICommand PlayCommand
        {
            get { return this.playCommand; }
        }

        /// <summary>
        /// Gets the command to pause the video.
        /// </summary>
        public ICommand PauseCommand
        {
            get { return this.pauseCommand; }
        }

        /// <summary>
        /// Gets the command that notifies the view model that the video has been reset
        /// </summary>
        public ICommand VideoResetNotification
        {
            get { return this.videoResetNotification; }
        }

        /// <summary>
        /// Maintains the current state of the video player. Used to update the mutually exclusive
        /// CanPlay/CanPause/CanReplay states
        /// </summary>
        private State CurrentState
        {
            get
            {
                return this.currentState;
            }

            set
            {
                this.currentState = value;
                this.OnPropertyChanged("CanResume");
                this.OnPropertyChanged("CanPause");
                this.OnPropertyChanged("CanReplay");

                this.playCommand.InvokeCanExecuteChanged();
                this.pauseCommand.InvokeCanExecuteChanged();
            }
        }

        /// <summary>
        /// Loads a video from the supplied Uri. This cannot be a Resource Uri as MediaElement does not support them.
        /// </summary>
        /// <param name="parameter">Uri pointing to a video. This cannot be a Resource Uri.</param>
        public override void Initialize(Uri parameter)
        {
            if (null == parameter)
            {
                throw new ArgumentNullException("parameter");
            }

            this.Video = parameter;
        }

        /// <summary>
        /// Automatically start playing the video when this view is being navigated to
        /// </summary>
        public override void OnNavigatedTo()
        {
            base.OnNavigatedTo();
            this.CurrentState = State.Playing;
        }

        /// <summary>
        /// Stops the video when the view is being navigated away from
        /// </summary>
        public override void OnNavigatedFrom()
        {
            base.OnNavigatedFrom();
            this.CurrentState = State.Disabled;
        }
    }
}