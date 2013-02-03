//------------------------------------------------------------------------------
// <copyright file="TimedLerp.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Avateering.Filters
{
    using System;

    /// <summary>
    /// TimedLerp - Maintains a time-based lerp between 0 and a upper limit between 0 and 1.
    /// The lerp speed parameter is in units of inverse time - therefore, a speed of 2.0
    /// means that the lerp completes a full transition (0 to 1) in 0.5 seconds.
    /// </summary>
    public class TimedLerp
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimedLerp"/> class.
        /// </summary>
        public TimedLerp()
        {
            this.Enabled = 0.0f;
            this.Value = 0.0f;
            this.EaseInSpeed = 1.0f;
            this.EaseOutSpeed = 1.0f;
        }

        /// <summary>
        /// Gets LinearValue. Returns a raw, linearly interpolated value between 0 and the Math.Maximum value.
        /// </summary> 
        /// <returns>Returns a linear Lerped value.</returns>
        public float LinearValue
        {
            get
            {
                return this.Value;
            }
        }

        /// <summary>
        /// Gets SmoothValue. Returns the value between 0 and the Math.Maximum value, but applies a cosine-shaped smoothing function.
        /// </summary>
        /// <returns>Returns a smoothed value.</returns>
        public float SmoothValue
        {
            get
            {
                return 0.5f - (0.5f * (float)Math.Cos(this.LinearValue * Math.PI));
            }
        }

        /// <summary>
        /// Gets or sets Enabled value.
        /// </summary>
        protected float Enabled { get; set; }

        /// <summary>
        /// Gets or sets The Value.
        /// </summary>
        protected float Value { get; set; }

        /// <summary>
        /// Gets or sets Ease in speed.
        /// </summary>
        protected float EaseInSpeed { get; set; }

        /// <summary>
        /// Gets or sets Ease out speed.
        /// </summary>
        protected float EaseOutSpeed { get; set; }

        /// <summary>
        /// Set speeds.
        /// </summary>
        public void SetSpeed()
        {
            this.SetSpeed(0.5f, 0.0f);
        }

        /// <summary>
        /// Set speeds.
        /// </summary>
        /// <param name="easeInSpeed">Ease in speed value.</param>
        /// <param name="easeOutSpeed">Ease out speed value.</param>
        public void SetSpeed(float easeInSpeed, float easeOutSpeed) 
        {
            this.EaseInSpeed = easeInSpeed;
            if (easeOutSpeed <= 0.0f)
            {
                this.EaseOutSpeed = easeInSpeed;
            }
            else
            {
                this.EaseOutSpeed = easeOutSpeed;
            }
        }

        /// <summary>
        /// Set whether the Lerp is enabled.
        /// </summary>
        /// <param name="isEnabled">Enable or Disable Lerp.</param>
        public void SetEnabled(bool isEnabled)
        {
            this.Enabled = isEnabled ? 1.0f : 0.0f;
        }

        /// <summary>
        /// Set the Lerp enable value.
        /// </summary>
        /// <param name="enabled">Set enable value.</param>
        public void SetEnabled(float enabled)
        {
            this.Enabled = Math.Max(0.0f, Math.Min(1.0f, enabled));
        }

        /// <summary>
        /// ReSet the Lerp.
        /// </summary>
        public void Reset()
        {
            this.Enabled = 0.0f;
            this.Value = 0.0f;
        }

        /// <summary>
        /// IsEnabled reflects whether the target value is 0 or not.
        /// </summary>
        /// <returns>Returns true if enabled.</returns>
        public bool IsEnabled()
        {
            return this.Enabled > 0.0f;
        }

        /// <summary>
        /// IsLerpEnabled reflects whether the current value is 0 or not.
        /// </summary>
        /// <returns>Returns true if enabled and value greater than 0, false otherwise.</returns>
        public bool IsLerpEnabled()
        {
            return this.IsEnabled() || (this.Value > 0.0f);
        }

        /// <summary>
        /// Tick needs to be called once per frame.
        /// </summary>
        /// <param name="deltaTime">The time difference between frames.</param>
        public void Tick(float deltaTime)
        {
            float speed = this.EaseInSpeed;
            if (this.Value > this.Enabled)
            {
                speed = this.EaseOutSpeed;
            }

            float delta = speed * deltaTime;
            if (this.Enabled > 0.0f)
            {
                this.Value = Math.Min(this.Enabled, this.Value + delta);
            }
            else
            {
                this.Value = Math.Max(0.0f, this.Value - delta);
            }
        }
    }
}
