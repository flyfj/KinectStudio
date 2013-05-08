// -----------------------------------------------------------------------
// <copyright file="PromptControl.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.InteractionGallery.Controls
{
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Threading;

    using Microsoft.Samples.Kinect.InteractionGallery.Utilities;

    /// <summary>
    /// Represents a control that responds to property changes by transitioning between
    /// "Hidden", "Prompting" and "Dismissed" VisualStateManager states.
    /// </summary>
    /// <remarks>
    /// Time delay between state transitions is configurable.
    /// </remarks>
    public class PromptControl : ContentControl
    {
        /// <summary>
        /// Default value for minimum time (in seconds) we should spend in each visual state.
        /// </summary>
        public const double DefaultMinimumStateDurationSeconds = 0.6;

        /// <summary>
        /// Default value for amount of time (in seconds) by which we defer state transitions,
        /// even if we've already spent the minimum necessary time in the current visual state.
        /// </summary>
        public const double DefaultStateTransitionDelaySeconds = 0.4;

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(PromptControl), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty MinimumStateDurationProperty =
            DependencyProperty.Register("MinimumStateDuration", typeof(TimeSpan), typeof(PromptControl), new PropertyMetadata(TimeSpan.FromSeconds(DefaultMinimumStateDurationSeconds)));

        public static readonly DependencyProperty StateTransitionDelayProperty =
            DependencyProperty.Register("StateTransitionDelay", typeof(TimeSpan), typeof(PromptControl), new PropertyMetadata(TimeSpan.FromSeconds(DefaultStateTransitionDelaySeconds)));

        public static readonly DependencyProperty StateProperty =
            DependencyProperty.Register("State", typeof(PromptState), typeof(PromptControl), new PropertyMetadata(PromptState.Hidden, OnStateChanged));

        /// <summary>
        /// Name of visual state that represents a hidden prompt.
        /// </summary>
        private const string HiddenState = "Hidden";

        /// <summary>
        /// Name of visual state that represents a prompt that needs to be shown.
        /// </summary>
        private const string PromptingState = "Prompting";

        /// <summary>
        /// Name of visual state that represents a prompt that was shown and then dismissed.
        /// </summary>
        private const string DismissedState = "Dismissed";

        /// <summary>
        /// Represents a time duration of zero seconds.
        /// </summary>
        private readonly TimeSpan zeroDuration = TimeSpan.FromSeconds(0.0);

        /// <summary>
        /// Timer used to ensure that prompt states last at least as long as the minimum state duration.
        /// </summary>
        private readonly DispatcherTimer minimumStateDurationTimer = new DispatcherTimer();

        /// <summary>
        /// Timer used to delay the transition between states.
        /// </summary>
        private readonly DispatcherTimer stateTransitionDelayTimer = new DispatcherTimer();

        /// <summary>
        /// The current visual state.
        /// </summary>
        private PromptState? currentVisualState = null;

        /// <summary>
        /// The time when we entered the current visual state.
        /// </summary>
        private DateTime timeEnteredVisualState;

        public PromptControl()
        {
            this.DefaultStyleKey = typeof(PromptControl);

            minimumStateDurationTimer.Tick += OnMinimumStateDurationTimerTick;
            stateTransitionDelayTimer.Tick += OnStateTransitionDelayTimerTick;
        }

        /// <summary>
        /// Prompt message to display.
        /// </summary>
        public string Text
        {
            get
            {
                return (string)this.GetValue(TextProperty);
            }

            set
            {
                this.SetValue(TextProperty, value);
            }
        }

        /// <summary>
        /// Minimum time prompt should spend in each visual state.
        /// </summary>
        public TimeSpan MinimumStateDuration
        {
            get
            {
                return (TimeSpan)this.GetValue(MinimumStateDurationProperty);
            }

            set
            {
                this.SetValue(MinimumStateDurationProperty, value);
            }
        }

        /// <summary>
        /// Amount of time by which we defer state transitions, even if we've already spent
        /// the minimum necessary time in the current visual state.
        /// </summary>
        public TimeSpan StateTransitionDelay
        {
            get
            {
                return (TimeSpan)this.GetValue(StateTransitionDelayProperty);
            }

            set
            {
                this.SetValue(StateTransitionDelayProperty, value);
            }
        }

        /// <summary>
        /// Logical state of prompt UI experience.
        /// </summary>
        /// <remarks>
        /// Value changes are used as a trigger to transition between visual states.
        /// </remarks>
        public PromptState State
        {
            get
            {
                return (PromptState)this.GetValue(StateProperty);
            }

            set
            {
                this.SetValue(StateProperty, value);
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.GoToVisualState(PromptState.Hidden, false, false);
        }

        /// <summary>
        /// Figures out the appropriate visual state name corresponding to the state
        /// trigger values.
        /// </summary>
        /// <param name="state">
        /// Logical state.
        /// </param>
        /// <returns>
        /// String name of appropriate visual state.
        /// </returns>
        private static string GetVisualStateName(PromptState state)
        {
            switch (state)
            {
                case PromptState.Hidden:
                    return HiddenState;

                case PromptState.Prompting:
                    return PromptingState;

                case PromptState.Dismissed:
                    return DismissedState;
            }

            return HiddenState;
        }

        private static void OnStateChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var control = sender as PromptControl;

            if (control != null)
            {
                control.GoToVisualState((PromptState)e.NewValue, true);
            }
        }

        /// <summary>
        /// Transition to the specified visual state, taking delays and other transition logic
        /// into account.
        /// </summary>
        /// <param name="state">
        /// New state to transition into.
        /// </param>
        /// <param name="useTransitions">
        /// True to use a VisualTransition to transition between states, false otherwise.
        /// </param>
        /// <param name="delayTransition">
        /// True if transition should be delayed even if minimum amount of time has already
        /// been spent in current state, false otherwise.
        /// </param>
        private void GoToVisualState(PromptState state, bool useTransitions, bool delayTransition)
        {
            var currentTime = DateTime.UtcNow;
            bool isStateTransition = !this.currentVisualState.HasValue || (state != this.currentVisualState.Value);
            this.minimumStateDurationTimer.Stop();
            this.stateTransitionDelayTimer.Stop();

            if (!isStateTransition)
            {
                // If we're not transitioning states, there is no work to do.
                return;
            }

            if (this.currentVisualState.HasValue)
            {
                // If current state is valid and new state is different from current state,
                // verify that we've spent at least the minimum amount of time required in
                // the current state
                var timeInCurrentState = currentTime.Subtract(this.timeEnteredVisualState);
                var timeRemaining = this.MinimumStateDuration.Subtract(timeInCurrentState);

                if (timeRemaining.CompareTo(this.zeroDuration) > 0)
                {
                    // If we need to spend more time in current state before transitioning,
                    // defer transition until enough time passes.
                    this.minimumStateDurationTimer.Interval = timeRemaining;
                    this.minimumStateDurationTimer.Start();
                    return;
                }
            }

            if (delayTransition)
            {
                // If state transition is to be delayed even after spending minimum time
                // in current state, start transition delay timer.
                this.stateTransitionDelayTimer.Interval = this.StateTransitionDelay;
                this.stateTransitionDelayTimer.Start();
                return;
            }

            if (VisualStateManager.GoToState(this, GetVisualStateName(state), useTransitions))
            {
                // If state transition was successful, remember state we transitioned to
                // and time of transition.
                this.currentVisualState = state;
                this.timeEnteredVisualState = currentTime;
            }
        }

        private void GoToVisualState(PromptState state, bool useTransitions)
        {
            this.GoToVisualState(state, useTransitions, true);
        }

        private void OnMinimumStateDurationTimerTick(object sender, EventArgs e)
        {
            this.minimumStateDurationTimer.Stop();
            this.GoToVisualState(this.State, true);
        }

        private void OnStateTransitionDelayTimerTick(object sender, EventArgs e)
        {
            this.stateTransitionDelayTimer.Stop();
            this.GoToVisualState(this.State, true, false);
        }
    }
}
