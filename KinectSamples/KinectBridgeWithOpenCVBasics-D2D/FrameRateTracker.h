//-----------------------------------------------------------------------------
// <copyright file="FrameRateTracker.h" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------------

#pragma once

#include <ctime>
#include <algorithm>
#include <windows.h>

class FrameRateTracker {
public:
    // Functions:
    /// <summary>
    /// Constructor
    /// </summary>
    FrameRateTracker();

    /// <summary>
    /// Call once per frame to update the frame rate
    /// </summary>
    void Tick();

    /// <summary>
    /// Get the current frame rate
    /// </summary>
    /// <returns>The current frame rate</returns>
    const int CurrentFPS();

private:
    // Variables
    // The clock tick from the last time the fps was calculated
    clock_t m_previousClock;

    // The current frame count
    DWORD m_frameCount;

    // The previous frame count
    DWORD m_previousFrameCount;

    // The current frame rate
    int m_fps;
};