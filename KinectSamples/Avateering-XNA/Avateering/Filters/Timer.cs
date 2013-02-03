//------------------------------------------------------------------------------
// <copyright file="Timer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Avateering.Filters
{
    /// <summary>
    /// Class Timer is a helper class to perform timer operations
    ///       For stop-watch timer functionality, use:
    ///          Start()           - To start the timer
    ///          Stop()            - To stop (or pause) the timer
    ///          Reset()           - To reset the timer
    ///          Time         - Returns current time or last stopped time
    ///       For app-timing and per-frame updates, use:
    ///          AbsoluteTime - To get the absolute system time
    ///          GetAppTime()      - To get the running time since construction
    ///                              (which is usually the start of the app)
    ///          GetElapsedTime()  - To get the time that elapsed since the previous call
    ///                              GetElapsedTime() call
    ///          SingleStep()      - To advance the timer by a time delta
    /// </summary>
    public class Timer
    {
        /// <summary>
        ///  Whether the timer is initialized and can be used.
        /// </summary>
        private readonly bool init;

        /// <summary>
        ///  The clock frequency in ticks per second.
        /// </summary>
        private readonly long frequency;

        /// <summary>
        ///  The Last elapsed absolute time.
        /// </summary>
        private double lastElapsedAbsoluteTime;

        /// <summary>
        ///  The start elapsed time.
        /// </summary>
        private double baseTime;

        /// <summary>
        ///  The timer end time.
        /// </summary>
        private double stopTime;

        /// <summary>
        ///  True if timer is stopped, false otherwise.
        /// </summary>
        private bool timerStopped;

        /// <summary>
        ///  Initializes a new instance of the <see cref="Timer"/> class.
        /// </summary>
        public Timer()
        {
            if (NativeMethods.QueryPerformanceFrequency(out this.frequency) == false)
            {
                // Frequency not supported - cannot get time!
                this.init = false;
                return;
            }

            this.init = true;
            double absTime = this.AbsoluteTime;
            this.lastElapsedAbsoluteTime = absTime;

            this.baseTime = absTime;
            this.stopTime = 0.0;
            this.timerStopped = false;
        }

        /// <summary>
        ///  Gets the stop time if timer stopped, otherwise the absolute time.
        /// </summary>
        /// <returns>Returns the absolute time in s.</returns>
        public double Time
        {
            get
            {
                // Get either the current time or the stop time, depending
                // on whether we're stopped and what command was sent
                return (this.stopTime != 0.0) ? this.stopTime : this.AbsoluteTime;
            }
        }

        /// <summary>
        ///  Gets the absolute time.
        /// </summary>
        /// <returns>Returns the absolute time in s.</returns>
        public double AbsoluteTime
        {
            get
            {
                if (this.init == false)
                {
                    return 0;
                }

                long theTime;
                NativeMethods.QueryPerformanceCounter(out theTime);
                double realTime = (double)theTime / (double)this.frequency;
                return realTime;
            }
        }

        /// <summary>
        ///  Gets the elapsed time since the last call.
        /// </summary>
        /// <returns>Returns the absolute time in s.</returns>
        public double ElapsedTime
        {
            get
            {
                double theTime = this.AbsoluteTime;

                double elapsedAbsoluteTime = theTime - this.lastElapsedAbsoluteTime;
                this.lastElapsedAbsoluteTime = theTime;
                return elapsedAbsoluteTime;
            }
        }
        
        /// <summary>
        /// Gets the current time since the computer clock started.
        /// </summary>
        /// <returns>Returns the absolute time in s.</returns>
        public double AppTime
        {
            get
            {
                return this.Time - this.baseTime;
            }
        }

        /// <summary>
        /// Reset the timer.
        /// </summary>
        /// <returns>Returns the absolute stopped time (0) in s.</returns>
        public double Reset()
        {
            double theTime = this.Time;

            this.baseTime = theTime;
            this.stopTime = 0;
            this.timerStopped = false;
            return 0.0;
        }
        
        /// <summary>
        /// Start the timer.
        /// </summary>
        public void Start()
        {
            double theTime = this.AbsoluteTime;

            if (this.timerStopped)
            {
                this.baseTime += theTime - this.stopTime;
            }

            this.stopTime = 0.0;
            this.timerStopped = false;
        }

        /// <summary>
        /// Stop the timer.
        /// </summary>
        public void Stop()
        {
            double theTime = this.Time;

            if (!this.timerStopped)
            {
                this.stopTime = theTime;
                this.timerStopped = true;
            }
        }

        /// <summary>
        /// Advance the timer by a specified amount (e.g. 1/10th second)
        /// </summary>
        /// <param name="timeAdvance">Time to advance by.</param>
        public void SingleStep(double timeAdvance)
        {
            this.stopTime += timeAdvance;
        }
    }
}
