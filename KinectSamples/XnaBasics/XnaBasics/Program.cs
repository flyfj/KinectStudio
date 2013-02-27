//------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;

[assembly: CLSCompliant(true)]

namespace Microsoft.Samples.Kinect.XnaBasics
{
    /// <summary>
    /// The base Xna program.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// This method starts the game cycle.
        /// </summary>
        public static void Main()
        {
            using (XnaBasics game = new XnaBasics())
            {
                game.Run();
            }
        }
    }
}