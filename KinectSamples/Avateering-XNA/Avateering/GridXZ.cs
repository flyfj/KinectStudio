//------------------------------------------------------------------------------
// <copyright file="GridXZ.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Avateering
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    /// <summary>
    /// GridXZ Class - draws a grid in XZ plane
    /// </summary>
    public class GridXz : DrawableGameComponent
    {
        /// <summary>
        /// This is the array of 3D vertices with associated colors.
        /// </summary>
        private VertexPositionColor[] gridVertices;

        /// <summary>
        /// This is the number of vertices to draw.
        /// </summary>
        private int numberGridVertices;

        /// <summary>
        /// This is the XNA BasicEffect we use to draw.
        /// </summary>
        private BasicEffect effect;

        /// <summary>
        /// Initializes a new instance of the GridXz class.
        /// </summary>
        /// <param name="game">The related game object.</param>
        /// <param name="origin">The origin of the grid is 3D world coordinates.</param>
        /// <param name="gridSize">The size of the grid.</param>
        /// <param name="numberOfRowsAndColumns">The number of rows in the grid in X and number of columns in Y.</param>
        /// <param name="gridColor">The color of the grid lines.</param>
        public GridXz(Game game, Vector3 origin, Vector2 gridSize, Vector2 numberOfRowsAndColumns, Color gridColor)
            : base(game)
        {
            this.CreateGrid(origin, gridSize, numberOfRowsAndColumns, gridColor);
        }

        /// <summary>
        /// Create the Grid.
        /// </summary>
        /// <param name="origin">The origin of the grid is 3D world coordinates.</param>
        /// <param name="gridSize">The size of the grid.</param>
        /// <param name="numberOfRowsAndColumns">The number of rows in the grid in X and number of columns in Y.</param>
        /// <param name="gridColor">The color of the grid lines.</param>
        public void CreateGrid(Vector3 origin, Vector2 gridSize, Vector2 numberOfRowsAndColumns, Color gridColor)
        {
            if (0 == gridSize.X || 0 == gridSize.Y || 0 >= numberOfRowsAndColumns.X || 0 >= numberOfRowsAndColumns.Y)
            {
                return;
            }

            this.numberGridVertices = (int)((numberOfRowsAndColumns.X + 1) + (numberOfRowsAndColumns.Y + 1)) * 2;   // +1 for inclusive

            this.gridVertices = new VertexPositionColor[this.numberGridVertices];

            float diffX = gridSize.X / numberOfRowsAndColumns.Y;
            float diffZ = gridSize.Y / numberOfRowsAndColumns.X;
            float startX = origin.X - (gridSize.X / 2f);
            float startY = origin.Y;
            float startZ = origin.Z - (gridSize.Y / 2f);

            // Add lines for rows and columns of grid
            int index = 0;
            for (int i = 0; i <= numberOfRowsAndColumns.X; i++)
            {
                index = i * 2;
                this.gridVertices[index] = new VertexPositionColor(new Vector3(startX + (i * diffX), startY, startZ), gridColor);
                this.gridVertices[index + 1] = new VertexPositionColor(new Vector3(startX + (i * diffX), startY, startZ + gridSize.Y), gridColor);
            }

            for (int j = 0; j <= numberOfRowsAndColumns.Y; j++)
            {
                index = ((int)(numberOfRowsAndColumns.X + 1) * 2) + (j * 2);
                this.gridVertices[index] = new VertexPositionColor(new Vector3(startX, startY, startZ + (j * diffZ)), gridColor);
                this.gridVertices[index + 1] = new VertexPositionColor(new Vector3(startX + gridSize.X, startY, startZ + (j * diffZ)), gridColor);
            }
        }

        /// <summary>
        /// This method renders the current state.
        /// </summary>
        /// <param name="gameTime">The elapsed game time.</param>
        /// <param name="world">The world matrix.</param>
        /// <param name="view">The view matrix.</param>
        /// <param name="projection">The projection matrix.</param>
        public void Draw(GameTime gameTime, Matrix world, Matrix view, Matrix projection)
        {
            if (null == this.gridVertices || 0 == this.numberGridVertices)
            {
                return;
            }

            // Optionally draw ground plane grid with Basic Effect.
            this.effect.World = world;
            this.effect.View = view;
            this.effect.Projection = projection;

            foreach (EffectPass pass in this.effect.CurrentTechnique.Passes)
            {
                pass.Apply();

                // Draw grid vertices as line list
                GraphicsDevice.DrawUserPrimitives<VertexPositionColor>(
                    PrimitiveType.LineList,
                    this.gridVertices,
                    0,
                    this.numberGridVertices / 2);
            }

            base.Draw(gameTime);
        }

        /// <summary>
        /// This method loads the basic effect used for drawing.
        /// </summary>
        protected override void LoadContent()
        {
            this.effect = new BasicEffect(this.Game.GraphicsDevice);
            if (null == this.effect)
            {
                throw new InvalidOperationException("Error creating Basic Effect shader.");
            }

            this.effect.VertexColorEnabled = true;

            base.LoadContent();
        }
    }
}
