//------------------------------------------------------------------------------
// <copyright file="Object2D.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.XnaBasics
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    /// <summary>
    /// A very basic game component to track common values.
    /// </summary>
    public class Object2D : DrawableGameComponent
    {
        /// <summary>
        /// Initializes a new instance of the Object2D class.
        /// </summary>
        /// <param name="game">The related game object.</param>
        public Object2D(Game game)
            : base(game)
        {
        }

        /// <summary>
        /// Gets or sets the position of the object.
        /// </summary>
        public Vector2 Position { get; set; }

        /// <summary>
        /// Gets or sets the size of the object.
        /// </summary>
        public Vector2 Size { get; set; }

        /// <summary>
        /// Gets the KinectChooser from the services.
        /// </summary>
        public KinectChooser Chooser
        {
            get
            {
                return (KinectChooser)this.Game.Services.GetService(typeof(KinectChooser));
            }
        }

        /// <summary>
        /// Gets the SpriteBatch from the services.
        /// </summary>
        public SpriteBatch SharedSpriteBatch
        {
            get
            {
                return (SpriteBatch)this.Game.Services.GetService(typeof(SpriteBatch));
            }
        }
    }
}
