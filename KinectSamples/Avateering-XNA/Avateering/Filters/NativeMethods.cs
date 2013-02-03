// -----------------------------------------------------------------------
// <copyright file="NativeMethods.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Avateering.Filters
{
    using System.Runtime.InteropServices;

    /// <summary>
    /// Native Methods.
    /// </summary>
    public static class NativeMethods
    {
        /// <summary>
        /// PInvoke Import of performance counter.
        /// </summary>
        /// <param name="performanceCount">Count of ticks.</param>
        /// <returns>Returns true on success, false otherwise.</returns>
        [DllImport("Kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool QueryPerformanceCounter(out long performanceCount);

        /// <summary>
        /// PInvoke Import of performance counter.
        /// </summary>
        /// <param name="frequency">Clock frequency in ticks per second.</param>
        /// <returns>Returns true on success, false otherwise.</returns>
        [DllImport("Kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool QueryPerformanceFrequency(out long frequency);
    }
}
