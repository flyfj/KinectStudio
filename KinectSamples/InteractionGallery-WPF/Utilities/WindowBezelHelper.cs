// -----------------------------------------------------------------------
// <copyright file="WindowBezelHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.InteractionGallery.Utilities
{
    using System;
    using System.Windows;
    using System.Windows.Input;

    /// <summary>
    /// Helper class with utility methods used to hide/show a window bezel and cursor.
    /// </summary>
    public class WindowBezelHelper : DependencyObject
    {
        /// <summary>
        /// Show or hide the bezel and cursor for specified window.
        /// </summary>
        /// <param name="window">
        /// Window for which bezel will be hidden.
        /// </param>
        /// <param name="showBezel">
        /// true if window bezel should be shown, false if it should be hidden.
        /// </param>
        public static void UpdateBezel(Window window, bool showBezel)
        {
            if (window == null)
            {
                throw new ArgumentNullException("window");
            }

            if (showBezel)
            {
                window.WindowStyle = WindowStyle.SingleBorderWindow;
                window.Cursor = Cursors.Arrow;
            }
            else
            {
                window.WindowStyle = WindowStyle.None;

                // If the window is already full-screen, we must set it again else the window will appear under the Windows taskbar.
                if (window.WindowState == WindowState.Maximized)
                {
                    window.WindowState = WindowState.Normal;
                    window.WindowState = WindowState.Maximized;
                }

                window.Cursor = Cursors.None;
            }
        }
    }
}