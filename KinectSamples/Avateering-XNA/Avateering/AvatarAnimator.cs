//------------------------------------------------------------------------------
// <copyright file="AvatarAnimator.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Avateering
{
    using System;
    using Microsoft.Kinect;
    using Microsoft.Samples.Kinect.Avateering.Filters;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;
    using SkinnedModel;

    /// <summary>
    /// A delegate method used by the animator to re-target the orientations to the model.
    /// </summary>
    /// <param name="skeleton">The Skeleton to retarget.</param>
    /// <param name="bindRoot">The bind root matrix of the avatar mesh.</param>
    /// <param name="boneTransforms">The avatar mesh rotation matrices.</param>
    public delegate void RetargetMatrixHierarchyToAvatarMesh(Skeleton skeleton, Matrix bindRoot, Matrix[] boneTransforms);

    /// <summary>
    /// This class is responsible for animating an avatar using a skeleton stream.
    /// </summary>
    [CLSCompliant(true)]
    public class AvatarAnimator : DrawableGameComponent
    {
        /// <summary>
        /// This is the map method called when re-targeting from
        /// Nui skeleton to the avatar mesh.
        /// </summary>
        private readonly RetargetMatrixHierarchyToAvatarMesh retargetMethod;

        /// <summary>
        /// This skeleton is the  first tracked skeleton in the frame is used for animation, with constraints and mirroring optionally applied.
        /// </summary>
        private Skeleton skeleton;

        /// <summary>
        /// This tracks the previous keyboard state.
        /// </summary>
        private KeyboardState previousKeyboard;

        /// <summary>
        /// The 3D avatar mesh.
        /// </summary>
        private Model currentModel;

        /// <summary>
        /// The avatar relative bone transformation matrices.
        /// </summary>
        private Matrix[] boneTransforms;

        /// <summary>
        /// The avatar "absolute" bone transformation matrices in the world coordinate system.
        /// These are in the warrior ("dude") format
        /// </summary>
        private Matrix[] worldTransforms;

        /// <summary>
        /// The avatar skin transformation matrices.
        /// </summary>
        private Matrix[] skinTransforms;

        /// <summary>
        /// Back link to the avatar model bind pose and skeleton hierarchy data.
        /// </summary>
        private SkinningData skinningDataValue;

        /// <summary>
        /// This is the coordinate cross we use to draw the local axes of the model.
        /// </summary>
        private CoordinateCross localAxes;

        /// <summary>
        /// Draws local joint axes inside the 3D avatar mesh if true.
        /// </summary>
        private bool drawLocalAxes;

        /// <summary>
        /// Enables avateering when true.
        /// </summary>
        private bool useKinectAvateering;

        /// <summary>
        /// Compensate the avatar joints for sensor tilt if true.
        /// </summary>
        private bool tiltCompensate;

        /// <summary>
        /// Compensate the avatar joints for sensor height and bring skeleton to floor level if true.
        /// </summary>
        private bool floorOffsetCompensate;

        /// <summary>
        /// Filter to compensate the avatar joints for sensor height and bring skeleton to floor level.
        /// </summary>
        private SkeletonJointsSensorOffsetCorrection sensorOffsetCorrection;

        /// <summary>
        /// Filter to prevent arm-torso self-intersections if true.
        /// </summary>
        private bool selfIntersectionConstraints;

        /// <summary>
        /// The timer for controlling Filter Lerp blends.
        /// </summary>
        private Timer frameTimer;

        /// <summary>
        /// The timer for controlling Filter Lerp blends.
        /// </summary>
        private float lastNuiTime;

        /// <summary>
        /// Filter clipped legs if true.
        /// </summary>
        private bool filterClippedLegs;

        /// <summary>
        /// The filter for clipped legs.
        /// </summary>
        private SkeletonJointsFilterClippedLegs clippedLegs;

        /// <summary>
        /// Mirrors the avatar when true.
        /// </summary>
        private bool mirrorView;

        /// <summary>
        /// Apply joint constraints to joint locations and orientations if true.
        /// </summary>
        private bool boneConstraints;

        /// <summary>
        /// The filter for bone orientations constraints.
        /// </summary>
        private BoneOrientationConstraints boneOrientationConstraints;

        /// <summary>
        /// Draw the Kinect line skeleton using the raw joint positions and joint constraint cones if true.
        /// </summary>
        private bool drawBoneConstraintsSkeleton;

        /// <summary>
        /// The world translation offset for the skeleton drawing in bone constraints.
        /// </summary>
        private Matrix kinectLineSkeletonWorldOffsetMatrix;

        /// <summary>
        /// The filter for joint positions.
        /// </summary>
        private SkeletonJointsPositionDoubleExponentialFilter jointPositionFilter;

        /// <summary>
        /// Filter bone orientations if true.
        /// </summary>
        private bool filterBoneOrientations;

        /// <summary>
        /// The filter for bone orientations.
        /// </summary>
        private BoneOrientationDoubleExponentialFilter boneOrientationFilter;

        /// <summary>
        /// Initializes a new instance of the AvatarAnimator class.
        /// </summary>
        /// <param name="game">The related game object.</param>
        /// <param name="retarget">The avatar mesh re-targeting method to convert from the Kinect skeleton.</param>
        public AvatarAnimator(Game game, RetargetMatrixHierarchyToAvatarMesh retarget, Vector3 skeletonTranslationScaleFactor)
            : base(game)
        {
            if (null == game)
            {
                return;
            }

            this.SkeletonTranslationScaleFactor = skeletonTranslationScaleFactor;
            this.retargetMethod = retarget;
            this.SkeletonDrawn = true;
            this.useKinectAvateering = true;
            this.AvatarHipCenterHeight = 0;

            // Create local axes inside the model to draw at each joint
            this.localAxes = new CoordinateCross(this.Game, 2f);
            this.drawLocalAxes = false;
            game.Components.Add(this.localAxes);

            // If we draw the Kinect 3D skeleton in BoneOrientationConstraints, we can offset it from the original 
            // hip center position, so as not to draw over the top of the Avatar. Offset defined in m.
            this.kinectLineSkeletonWorldOffsetMatrix = Matrix.CreateTranslation(this.SkeletonTranslationScaleFactor.X, 0.75f * this.SkeletonTranslationScaleFactor.Y, 0);
            this.drawBoneConstraintsSkeleton = false;

            // Skeleton fixups
            this.frameTimer = new Timer();
            this.lastNuiTime = 0;
            this.FloorClipPlane = new Tuple<float, float, float, float>(0, 0, 0, 0);
            this.clippedLegs = new SkeletonJointsFilterClippedLegs();
            this.sensorOffsetCorrection = new SkeletonJointsSensorOffsetCorrection();
            this.jointPositionFilter = new SkeletonJointsPositionDoubleExponentialFilter();
            this.boneOrientationConstraints = new BoneOrientationConstraints(game, this.SkeletonTranslationScaleFactor);
            this.boneOrientationFilter = new BoneOrientationDoubleExponentialFilter();

            this.filterClippedLegs = true;
            this.tiltCompensate = true;
            this.floorOffsetCompensate = false;
            this.selfIntersectionConstraints = true;
            this.mirrorView = true;
            this.boneConstraints = true;
            this.filterBoneOrientations = true;

            // For many applications we would enable the
            // automatic joint smoothing, however, in this
            // Avateering sample, we perform skeleton joint
            // position corrections, so we will manually
            // filter here after these are complete.

            // Typical smoothing parameters for the joints:
            var jointPositionSmoothParameters = new TransformSmoothParameters
            {
                Smoothing = 0.25f,
                Correction = 0.25f,
                Prediction = 0.75f,
                JitterRadius = 0.1f,
                MaxDeviationRadius = 0.04f
            };

            this.jointPositionFilter.Init(jointPositionSmoothParameters);
            
            // Setup the bone orientation constraint system
            this.boneOrientationConstraints.AddDefaultConstraints();
            game.Components.Add(this.boneOrientationConstraints);

            // Typical smoothing parameters for the bone orientations:
            var boneOrientationSmoothparameters = new TransformSmoothParameters
            {
                Smoothing = 0.5f,
                Correction = 0.8f,
                Prediction = 0.75f,
                JitterRadius = 0.1f,
                MaxDeviationRadius = 0.1f
            };

            this.boneOrientationFilter.Init(boneOrientationSmoothparameters);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the model has been drawn - this flag ensures we only request a frame once per update call
        /// across the entire application.
        /// </summary>
        public bool SkeletonDrawn { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this model is visible - this flag ensures we only update if there is visible data.
        /// </summary>
        public bool SkeletonVisible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the first tracked skeleton in the frame is used for animation.
        /// </summary>
        public Skeleton RawSkeleton { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether Store the floor plane to compensate the skeletons for any Kinect tilt.
        /// </summary>
        public System.Tuple<float, float, float, float> FloorClipPlane { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the height of the avatar Hip Center joint off the floor when standing upright.
        /// </summary>
        public float AvatarHipCenterHeight { get; set; }

        /// <summary>
        /// Gets the KinectChooser from the services.
        /// </summary>
        public KinectChooser Chooser
        {
            get
            {
                return (KinectChooser)Game.Services.GetService(typeof(KinectChooser));
            }
        }

        /// <summary>
        /// Gets or sets the Avatar 3D model to animate.
        /// </summary>
        public Model Avatar
        {
            get
            {
                return this.currentModel;
            }

            set
            {
                if (value == null)
                {
                    return;
                }

                this.currentModel = value;

                // Look up our custom skinning information.
                SkinningData skinningData = this.currentModel.Tag as SkinningData;
                if (null == skinningData)
                {
                    throw new InvalidOperationException("This model does not contain a Skinning Data tag.");
                }

                this.skinningDataValue = skinningData;

                // Bone matrices for the "dude" model
                this.boneTransforms = new Matrix[skinningData.BindPose.Count];
                this.worldTransforms = new Matrix[skinningData.BindPose.Count];
                this.skinTransforms = new Matrix[skinningData.BindPose.Count];

                // Initialize bone transforms to the bind pose.
                this.skinningDataValue.BindPose.CopyTo(this.boneTransforms, 0);
                this.UpdateWorldTransforms(Matrix.Identity);
                this.UpdateSkinTransforms();
            }
        }

        /// <summary>
        /// The model mesh can be defined at arbitrary non-human size, so re-scale the Kinect translation for drawing the Kinect 3D Skeleton.
        /// </summary>
        private Vector3 SkeletonTranslationScaleFactor { get; set; }

        /// <summary>
        /// Reset the tracking filters.
        /// </summary>
        public void Reset()
        {
            if (null != this.jointPositionFilter)
            {
                this.jointPositionFilter.Reset();
            }

            if (null != this.boneOrientationFilter)
            {
                this.boneOrientationFilter.Reset();
            }

            if (null != this.sensorOffsetCorrection)
            {
                this.sensorOffsetCorrection.Reset();
            }

            if (null != this.clippedLegs)
            {
                this.clippedLegs.Reset();
            }
        }

        /// <summary>
        /// This method copies a new skeleton locally so we can modify it.
        /// </summary>
        /// <param name="sourceSkeleton">The skeleton to copy.</param>
        public void CopySkeleton(Skeleton sourceSkeleton)
        {
            if (null == sourceSkeleton)
            {
                return;
            }

            if (null == this.skeleton)
            {
                this.skeleton = new Skeleton();
            }

            // Copy the raw Kinect skeleton so we can modify the joint data and apply constraints
            KinectHelper.CopySkeleton(sourceSkeleton, this.skeleton);
        }

        /// <summary>
        /// This method retrieves a new skeleton frame if necessary.
        /// </summary>
        /// <param name="gameTime">The elapsed game time.</param>
        public override void Update(GameTime gameTime)
        {
            // If the sensor is not found, not running, or not connected, return now
            if (null == this.Chooser || null == this.Chooser.Sensor || false == this.Chooser.Sensor.IsRunning || this.Chooser.Sensor.Status != KinectStatus.Connected)
            {
                return;
            }

            // If we have already drawn this skeleton, then we should retrieve a new frame
            // This prevents us from calling the next frame more than once per update
            if (false == this.SkeletonDrawn && null != this.skeleton && this.useKinectAvateering && this.SkeletonVisible)
            {
                // Copy all bind pose matrices to boneTransforms 
                // Note: most are identity, but the translation is important to describe bone length/the offset between bone drawing positions
                this.skinningDataValue.BindPose.CopyTo(this.boneTransforms, 0);
                this.UpdateWorldTransforms(Matrix.Identity);
                this.UpdateSkinTransforms();
                
                // If required, we should modify the joint positions before we access the bone orientations, as orientations are calculated
                // on the first access, and then whenever a joint position changes. Hence changing joint positions interleaved with accessing
                // rotations will cause unnecessary additional computation.
                float currentNuiTime = (float)this.frameTimer.AbsoluteTime;
                float deltaNuiTime = currentNuiTime - this.lastNuiTime;

                // Fixup Skeleton to improve avatar appearance.
                if (this.filterClippedLegs && null != this.clippedLegs && null != this.Chooser && !this.Chooser.SeatedMode)
                {
                    this.clippedLegs.FilterSkeleton(this.skeleton, deltaNuiTime);
                }

                if (this.selfIntersectionConstraints)
                {
                    // Constrain the wrist and hand joint positions to not intersect the torso
                    SkeletonJointsSelfIntersectionConstraint.Constrain(this.skeleton);
                }

                if (this.tiltCompensate)
                {
                    int elevationAngle = 0;
                    try
                    {
                        elevationAngle = this.Chooser.Sensor.ElevationAngle;
                    }
                    catch (InvalidOperationException)
                    {
                        // Ignore any exception accessing the sensor if it has just been disconnected
                    }

                    // Correct for sensor tilt if we have a valid floor plane or a sensor tilt value from the motor.
                    SkeletonJointsSensorTiltCorrection.CorrectSensorTilt(this.skeleton, this.FloorClipPlane, elevationAngle);
                }

                if (this.floorOffsetCompensate && 0.0f != this.AvatarHipCenterHeight)
                {
                    // Correct for the sensor height from the floor (moves the skeleton to the floor plane) if we have a valid plane, or feet visible in the image.
                    // Note that by default this will not run unless we have set a non-zero AvatarHipCenterHeight
                    this.sensorOffsetCorrection.CorrectSkeletonOffsetFromFloor(this.skeleton, this.FloorClipPlane, this.AvatarHipCenterHeight);
                }

                if (this.mirrorView)
                {
                    SkeletonJointsMirror.MirrorSkeleton(this.skeleton);
                }

                // Filter the joint positions manually, using a double exponential filter.
                this.jointPositionFilter.UpdateFilter(this.skeleton);

                if (this.boneConstraints && null != this.boneOrientationConstraints)
                {
                    // Constrain the joint positions to approximate range of human motion.
                    this.boneOrientationConstraints.Constrain(this.skeleton, this.mirrorView);
                }

                if (this.filterBoneOrientations && null != this.boneOrientationFilter)
                {
                    // Double Exponential Filtering of the joint orientations.
                    // Note: This updates the joint orientations directly in the skeleton.
                    // It should be performed after all joint position modifications.
                    this.boneOrientationFilter.UpdateFilter(this.skeleton);
                }

                if (null != this.retargetMethod)
                {
                    // Adapt the rotation matrices to the avatar mesh joint local coordinate systems
                    this.retargetMethod(this.skeleton, this.skinningDataValue.BindPose[0], this.boneTransforms);
                }

                // Calculate the Avatar world transforms from the relative bone transforms of Kinect skeleton
                this.UpdateWorldTransforms(Matrix.Identity);

                // Refresh the Avatar SkinTransforms data based on the transforms we just applied
                this.UpdateSkinTransforms();

                this.lastNuiTime = currentNuiTime;
            }

            this.HandleInput();

            base.Update(gameTime);
        }

        /// <summary>
        /// This method draws the skeleton frame data.
        /// </summary>
        /// <param name="gameTime">The elapsed game time.</param>
        /// <param name="world">The world matrix.</param>
        /// <param name="view">The view matrix.</param>
        /// <param name="projection">The projection matrix.</param>
        public void Draw(GameTime gameTime, Matrix world, Matrix view, Matrix projection)
        {
            // Render the 3D model skinned mesh with Skinned Effect.
            foreach (ModelMesh mesh in this.currentModel.Meshes)
            {
                foreach (SkinnedEffect effect in mesh.Effects)
                {
                    effect.SetBoneTransforms(this.skinTransforms);

                    effect.World = world;
                    effect.View = view;
                    effect.Projection = projection;

                    effect.EnableDefaultLighting();

                    effect.SpecularColor = new Vector3(0.25f);
                    effect.SpecularPower = 16;
                }

                mesh.Draw();
            }

            // Optionally draw local bone transforms with Basic Effect.
            if (this.drawLocalAxes && null != this.localAxes)
            {
                // Disable the depth buffer so we can render the Kinect skeleton inside the model
                Game.GraphicsDevice.DepthStencilState = DepthStencilState.None;

                foreach (Matrix boneWorldTrans in this.worldTransforms)
                {
                    // re-use the coordinate cross instance for each localAxes draw
                    this.localAxes.Draw(gameTime, boneWorldTrans * world, view, projection);
                }

                // Re-enable the depth buffer
                Game.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            }

            // Optionally draw the Kinect 3d line skeleton from the raw joint positions and the bone orientation constraint cones
            if (this.drawBoneConstraintsSkeleton && null != this.boneOrientationConstraints && null != this.Chooser)
            {
                this.boneOrientationConstraints.Draw(gameTime, this.skeleton, this.Chooser.SeatedMode, this.kinectLineSkeletonWorldOffsetMatrix * world, view, projection);                    
            }

            this.SkeletonDrawn = true;

            base.Draw(gameTime);
        }

        /// <summary>
        /// Handles input for avateering options.
        /// </summary>
        private void HandleInput()
        {
            KeyboardState currentKeyboard = Keyboard.GetState();

            // Mirror Avatar on/off toggle
            if (currentKeyboard.IsKeyDown(Keys.M))
            {
                // If not down last update, key has just been pressed.
                if (!this.previousKeyboard.IsKeyDown(Keys.M))
                {
                    this.mirrorView = !this.mirrorView;
                }
            }

            // Local Axes on/off toggle
            if (currentKeyboard.IsKeyDown(Keys.G))
            {
                if (!this.previousKeyboard.IsKeyDown(Keys.G))
                {
                    this.drawLocalAxes = !this.drawLocalAxes;
                }
            }

            // Avateering on/off toggle
            if (currentKeyboard.IsKeyDown(Keys.K))
            {
                if (!this.previousKeyboard.IsKeyDown(Keys.K))
                {
                    this.useKinectAvateering = !this.useKinectAvateering;
                }
            }

            // Tilt Compensation on/off toggle
            if (currentKeyboard.IsKeyDown(Keys.T))
            {
                if (!this.previousKeyboard.IsKeyDown(Keys.T))
                {
                    this.tiltCompensate = !this.tiltCompensate;
                }
            }

            // Compensate for sensor height, move skeleton to floor on/off toggle
            if (currentKeyboard.IsKeyDown(Keys.O))
            {
                if (!this.previousKeyboard.IsKeyDown(Keys.O))
                {
                    this.floorOffsetCompensate = !this.floorOffsetCompensate;
                }
            }

            // Torso self intersection constraints on/off toggle
            if (currentKeyboard.IsKeyDown(Keys.I))
            {
                if (!this.previousKeyboard.IsKeyDown(Keys.I))
                {
                    this.selfIntersectionConstraints = !this.selfIntersectionConstraints;
                }
            }

            // Filter Joint Orientation on/off toggle
            if (currentKeyboard.IsKeyDown(Keys.F))
            {
                if (!this.previousKeyboard.IsKeyDown(Keys.F))
                {
                    this.filterBoneOrientations = !this.filterBoneOrientations;
                }
            }

            // Constrain Bone orientations on/off toggle
            if (currentKeyboard.IsKeyDown(Keys.C))
            {
                if (!this.previousKeyboard.IsKeyDown(Keys.C))
                {
                    this.boneConstraints = !this.boneConstraints;
                }
            }

            // Draw Bones in bone orientation constraints on/off toggle
            if (currentKeyboard.IsKeyDown(Keys.B))
            {
                if (!this.previousKeyboard.IsKeyDown(Keys.B))
                {
                    this.drawBoneConstraintsSkeleton = !this.drawBoneConstraintsSkeleton;
                }
            }

            this.previousKeyboard = currentKeyboard;
        }

        /// <summary>
        /// Helper used by the Update method to refresh the WorldTransforms data.
        /// </summary>
        /// <param name="rootTransform">Matrix to modify the Avatar root transform with.</param>
        private void UpdateWorldTransforms(Matrix rootTransform)
        {
            // Root bone of model.
            this.worldTransforms[0] = this.boneTransforms[0] * rootTransform;

            // Child bones in bone hierarchy.
            for (int bone = 1; bone < this.worldTransforms.Length; bone++)
            {
                int parentBone = this.skinningDataValue.SkeletonHierarchy[bone];

                // Every bone world transform is calculated by multiplying it's relative transform by the world transform of it's parent. 
                this.worldTransforms[bone] = this.boneTransforms[bone] * this.worldTransforms[parentBone];
            }
        }

        /// <summary>
        /// Helper used by the Update method to refresh the SkinTransforms data.
        /// </summary>
        private void UpdateSkinTransforms()
        {
            for (int bone = 0; bone < this.skinTransforms.Length; bone++)
            {
                this.skinTransforms[bone] = this.skinningDataValue.InverseBindPose[bone] * this.worldTransforms[bone];
            }
        }
    }
}
