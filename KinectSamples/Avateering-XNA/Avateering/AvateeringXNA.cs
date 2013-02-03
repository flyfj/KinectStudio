//------------------------------------------------------------------------------
// <copyright file="AvateeringXNA.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Avateering
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Kinect;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;

    /// <summary>
    /// Sample game showing how to display skinned character and avateer with Kinect for Windows.
    /// </summary>
    public class AvateeringXNA : Microsoft.Xna.Framework.Game
    {
        #region Fields

        /// <summary>
        /// This is used to adjust the window size. The height is set automatically from the width using a 4:3 ratio.
        /// </summary>
        private const int WindowedWidth = 800;

        /// <summary>
        /// This is used to adjust the fullscreen window size. Only valid resolutions can be set.
        /// </summary>
        private const int FullScreenWidth = 1280;

        /// <summary>
        /// This is used to adjust the fullscreen window size. Only valid resolutions can be set.
        /// </summary>
        private const int FullScreenHeight = 1024;

        /// <summary>
        /// Camera Arc Increment value.
        /// </summary>
        private const float CameraArcIncrement = 0.1f;

        /// <summary>
        /// Camera Arc angle limit value.
        /// </summary>
        private const float CameraArcAngleLimit = 90.0f;

        /// <summary>
        /// Camera Zoom Increment value.
        /// </summary>
        private const float CameraZoomIncrement = 0.25f;

        /// <summary>
        /// Camera Max Distance value.
        /// </summary>
        private const float CameraMaxDistance = 500.0f;

        /// <summary>
        /// Camera Min Distance value.
        /// </summary>
        private const float CameraMinDistance = 10.0f;

        /// <summary>
        /// Camera starting Distance value.
        /// </summary>
        private const float CameraHeight = 40.0f;

        /// <summary>
        /// Camera starting Distance value.
        /// </summary>
        private const float CameraStartingTranslation = 90.0f;

        /// <summary>
        /// The "Dude" model mesh is defined at an arbitrary size in centimeters.
        /// Here we re-scale the Kinect translation so the model appears to walk more correctly on the ground.
        /// </summary>
        private static readonly Vector3 SkeletonTranslationScaleFactor = new Vector3(40.0f, 40.0f, 40.0f);

        /// <summary>
        /// The graphics device manager provided by XNA.
        /// </summary>
        private readonly GraphicsDeviceManager graphics;

        /// <summary>
        /// This control selects a sensor, and displays a notice if one is
        /// not connected.
        /// </summary>
        private readonly KinectChooser chooser;

        /// <summary>
        /// This manages the rendering of the depth stream.
        /// </summary>
        private readonly DepthStreamRenderer depthStream;

        /// <summary>
        /// This manages the rendering of the skeleton over the depth stream.
        /// </summary>
        private readonly SkeletonStreamRenderer skeletonStream;

        /// <summary>
        /// This is the XNA Basic Effect used in drawing.
        /// </summary>
        private BasicEffect effect;    

        /// <summary>
        /// This is the SpriteBatch used for rendering the header/footer.
        /// </summary>
        private SpriteBatch spriteBatch;

        /// <summary>
        /// This is used when toggling between windowed and fullscreen mode.
        /// </summary>
        private bool fullscreenMode = false;

        /// <summary>
        /// This tracks the previous keyboard state.
        /// </summary>
        private KeyboardState previousKeyboard;

        /// <summary>
        /// This tracks the current keyboard state.
        /// </summary>
        private KeyboardState currentKeyboard;

        /// <summary>
        /// This is the texture for the header.
        /// </summary>
        private Texture2D header;

        /// <summary>
        /// This is the coordinate cross we use to draw the world coordinate system axes.
        /// </summary>
        private CoordinateCross worldAxes;

        /// <summary>
        /// The 3D avatar mesh.
        /// </summary>
        private Model currentModel;

        /// <summary>
        /// Store the mapping between the NuiJoint and the Avatar Bone index.
        /// </summary>
        private Dictionary<JointType, int> nuiJointToAvatarBoneIndex;

        /// <summary>
        /// The 3D avatar mesh animator.
        /// </summary>
        private AvatarAnimator animator;

        /// <summary>
        /// Viewing Camera arc.
        /// </summary>
        private float cameraArc = 0;

        /// <summary>
        /// Viewing Camera current rotation.
        /// The virtual camera starts where Kinect is looking i.e. looking along the Z axis, with +X left, +Y up, +Z forward
        /// </summary>
        private float cameraRotation = 0; 

        /// <summary>
        /// Viewing Camera distance from origin.
        /// The "Dude" model is defined in centimeters, hence all the units we use here are cm.
        /// </summary>
        private float cameraDistance = CameraStartingTranslation;

        /// <summary>
        /// Viewing Camera view matrix.
        /// </summary>
        private Matrix view;

        /// <summary>
        /// Viewing Camera projection matrix.
        /// </summary>
        private Matrix projection;

        /// <summary>
        /// Draw the simple planar grid for avatar to stand on if true.
        /// </summary>
        private bool drawGrid;

        /// <summary>
        /// Simple planar grid for avatar to stand on.
        /// </summary>
        private GridXz planarXzGrid;

        /// <summary>
        /// Draw the avatar only when the player skeleton is detected in the depth image.
        /// </summary>
        private bool drawAvatarOnlyWhenPlayerDetected;

        /// <summary>
        /// Flag for first detection of skeleton.
        /// </summary>
        private bool skeletonDetected;

        /// <summary>
        /// Sets a seated posture when Seated Mode is on.
        /// </summary>
        private bool setSeatedPostureInSeatedMode;

        /// <summary>
        /// Fix the avatar hip center draw height.
        /// </summary>
        private bool fixAvatarHipCenterDrawHeight;

        /// <summary>
        /// Avatar hip center draw height.
        /// </summary>
        private float avatarHipCenterDrawHeight;

        /// <summary>
        /// Adjust Avatar lean when leaning back to reduce lean.
        /// </summary>
        private bool leanAdjust; 

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes a new instance of the AvateeringXNA class.
        /// </summary>
        public AvateeringXNA()
        {
            this.Window.Title = "Avateering";
            this.IsFixedTimeStep = false;
            this.IsMouseVisible = true;

            // Setup the graphics device for rendering
            this.graphics = new GraphicsDeviceManager(this);
            this.SetScreenMode();
            this.graphics.PreparingDeviceSettings += this.GraphicsDevicePreparingDeviceSettings;
            this.graphics.SynchronizeWithVerticalRetrace = true;

            Content.RootDirectory = "Content";

            // The Kinect sensor will use 640x480 for the color stream (default) and 320x240 for depth
            this.chooser = new KinectChooser(this, ColorImageFormat.RgbResolution640x480Fps30, DepthImageFormat.Resolution320x240Fps30);
            this.Services.AddService(typeof(KinectChooser), this.chooser);

            // Optionally set near mode for close range avateering (0.4m up to 3m)
            this.chooser.NearMode = false;

            // Optionally set seated mode for upper-body only tracking here (typically used with near mode for close to camera tracking)
            this.chooser.SeatedMode = false;

            // Adding these objects as XNA Game components enables automatic calls to the overridden LoadContent, Update, etc.. methods
            this.Components.Add(this.chooser);

            // Create a ground plane for the model to stand on
            this.planarXzGrid = new GridXz(this, new Vector3(0, 0, 0), new Vector2(500, 500), new Vector2(10, 10), Color.Black);
            this.Components.Add(this.planarXzGrid);
            this.drawGrid = true;

            this.worldAxes = new CoordinateCross(this, 500);
            this.Components.Add(this.worldAxes);

            // Create the avatar animator
            this.animator = new AvatarAnimator(this, this.RetargetMatrixHierarchyToAvatarMesh, AvateeringXNA.SkeletonTranslationScaleFactor);
            this.Components.Add(this.animator);

            // Drawing options
            this.setSeatedPostureInSeatedMode = true;
            this.drawAvatarOnlyWhenPlayerDetected = true;
            this.skeletonDetected = false;
            this.leanAdjust = true;

            // Here we can force the avatar to be drawn at fixed height in the XNA virtual world.
            // The reason we may use this is because the sensor height above the physical floor
            // and the feet locations are not always known. Hence the avatar cannot be correctly 
            // placed on the ground plane or will be very jumpy.
            // Note: this will prevent the avatar from jumping and crouching.
            this.fixAvatarHipCenterDrawHeight = true;
            this.avatarHipCenterDrawHeight = 0.8f;  // in meters

            // Setup the depth stream
            this.depthStream = new DepthStreamRenderer(this);

            // Setup the skeleton stream the same as depth stream 
            this.skeletonStream = new SkeletonStreamRenderer(this, this.SkeletonToDepthMap);
            
            // Update Depth and Skeleton Stream size and location based on the back-buffer
            this.UpdateStreamSizeAndLocation();

            this.previousKeyboard = Keyboard.GetState();
        }

        /// <summary>
        /// Gets the KinectChooser from the services.
        /// </summary>
        public KinectChooser Chooser
        {
            get
            {
                return (KinectChooser)this.Services.GetService(typeof(KinectChooser));
            }
        }

        /// <summary>
        /// Gets the SpriteBatch from the services.
        /// </summary>
        public SpriteBatch SharedSpriteBatch
        {
            get
            {
                return (SpriteBatch)this.Services.GetService(typeof(SpriteBatch));
            }
        }

        /// <summary>
        /// Gets or sets the last frames skeleton data.
        /// </summary>
        private static Skeleton[] SkeletonData { get; set; }

        /// <summary>
        /// Load the graphics content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create the spritebatch to draw the 3D items
            this.spriteBatch = new SpriteBatch(this.GraphicsDevice);
            this.Services.AddService(typeof(SpriteBatch), this.spriteBatch);

            this.header = Content.Load<Texture2D>("Header");

            // Create the XNA Basic Effect for line drawing
            this.effect = new BasicEffect(GraphicsDevice);
            if (null == this.effect)
            {
                throw new InvalidOperationException("Cannot load Basic Effect");
            }

            this.effect.VertexColorEnabled = true;

            // Load the model.
            this.currentModel = Content.Load<Model>("dude");
            if (null == this.currentModel)
            {
                throw new InvalidOperationException("Cannot load 3D avatar model");
            }

            // Add the model to the avatar animator
            this.animator.Avatar = this.currentModel;
            this.animator.AvatarHipCenterHeight = this.avatarHipCenterDrawHeight;

            // Set the Nui joint to model mapping for this avatar
            this.BuildJointHierarchy();

            base.LoadContent();
        }

        /// <summary>
        /// This function configures the mapping between the Nui Skeleton bones/joints and the Avatar bones/joints
        /// </summary>
        protected void BuildJointHierarchy()
        {
            // "Dude.fbx" bone index definitions
            // These are described as the "bone" that the transformation affects.
            // The rotation values are stored at the start joint before the bone (i.e. at the shared joint with the end of the parent bone).
            // 0 = root node
            // 1 = pelvis
            // 2 = spine
            // 3 = spine1
            // 4 = spine2
            // 5 = spine3
            // 6 = neck
            // 7 = head
            // 8-11 = eyes
            // 12 = Left clavicle (joint between spine and shoulder)
            // 13 = Left upper arm (joint at left shoulder)
            // 14 = Left forearm
            // 15 = Left hand
            // 16-30 = Left hand finger bones
            // 31 = Right clavicle (joint between spine and shoulder)
            // 32 = Right upper arm (joint at left shoulder)
            // 33 = Right forearm
            // 34 = Right hand
            // 35-49 = Right hand finger bones
            // 50 = Left Thigh
            // 51 = Left Knee
            // 52 = Left Ankle
            // 53 = Left Ball
            // 54 = Right Thigh
            // 55 = Right Knee
            // 56 = Right Ankle
            // 57 = Right Ball

            // For the Kinect NuiSkeleton, the joint at the end of the bone describes the rotation to get there, 
            // and the root orientation is in HipCenter. This is different to the Avatar skeleton described above.
            if (null == this.nuiJointToAvatarBoneIndex)
            {
                this.nuiJointToAvatarBoneIndex = new Dictionary<JointType, int>();
            }

            // Note: the actual hip center joint in the Avatar mesh has a root node (index 0) as well, which we ignore here for rotation.
            this.nuiJointToAvatarBoneIndex.Add(JointType.HipCenter, 1);
            this.nuiJointToAvatarBoneIndex.Add(JointType.Spine, 4);
            this.nuiJointToAvatarBoneIndex.Add(JointType.ShoulderCenter, 6);
            this.nuiJointToAvatarBoneIndex.Add(JointType.Head, 7);
            this.nuiJointToAvatarBoneIndex.Add(JointType.ElbowLeft, 13);
            this.nuiJointToAvatarBoneIndex.Add(JointType.WristLeft, 14);
            this.nuiJointToAvatarBoneIndex.Add(JointType.HandLeft, 15);
            this.nuiJointToAvatarBoneIndex.Add(JointType.ElbowRight, 32);
            this.nuiJointToAvatarBoneIndex.Add(JointType.WristRight, 33);
            this.nuiJointToAvatarBoneIndex.Add(JointType.HandRight, 34);
            this.nuiJointToAvatarBoneIndex.Add(JointType.KneeLeft, 50);
            this.nuiJointToAvatarBoneIndex.Add(JointType.AnkleLeft, 51);
            this.nuiJointToAvatarBoneIndex.Add(JointType.FootLeft, 52);
            this.nuiJointToAvatarBoneIndex.Add(JointType.KneeRight, 54);
            this.nuiJointToAvatarBoneIndex.Add(JointType.AnkleRight, 55);
            this.nuiJointToAvatarBoneIndex.Add(JointType.FootRight, 56);
        }

        #endregion

        #region Update

        /// <summary>
        /// Allows the game to run logic.
        /// </summary>
        /// <param name="gameTime">The gametime.</param>
        protected override void Update(GameTime gameTime)
        {
            // Update saved state.
            this.previousKeyboard = this.currentKeyboard;

            // If the sensor is not found, not running, or not connected, stop now
            if (null == this.chooser || null == this.Chooser.Sensor || false == this.Chooser.Sensor.IsRunning || this.Chooser.Sensor.Status != KinectStatus.Connected)
            {
                return;
            }

            bool newFrame = false;

            using (var skeletonFrame = this.Chooser.Sensor.SkeletonStream.OpenNextFrame(0))
            {
                // Sometimes we get a null frame back if no data is ready
                if (null != skeletonFrame)
                {
                    newFrame = true;

                    // Reallocate if necessary
                    if (null == SkeletonData || SkeletonData.Length != skeletonFrame.SkeletonArrayLength)
                    {
                        SkeletonData = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    }

                    skeletonFrame.CopySkeletonDataTo(SkeletonData);

                    // Select the first tracked skeleton we see to avateer
                    Skeleton rawSkeleton =
                        (from s in SkeletonData
                         where s != null && s.TrackingState == SkeletonTrackingState.Tracked
                         select s).FirstOrDefault();

                    if (null != this.animator)
                    {
                        if (null != rawSkeleton)
                        {
                            this.animator.CopySkeleton(rawSkeleton);
                            this.animator.FloorClipPlane = skeletonFrame.FloorClipPlane;

                            // Reset the filters if the skeleton was not seen before now
                            if (this.skeletonDetected == false)
                            {
                                this.animator.Reset();
                            }

                            this.skeletonDetected = true;
                            this.animator.SkeletonVisible = true;
                        }
                        else
                        {
                            this.skeletonDetected = false;
                            this.animator.SkeletonVisible = false;
                        }
                    }
                }
            }

            if (newFrame)
            {
                // Call the stream update manually as they are not a game component
                if (null != this.depthStream && null != this.skeletonStream)
                {
                    this.depthStream.Update(gameTime);
                    this.skeletonStream.Update(gameTime, SkeletonData);
                }

                // Update the avatar renderer
                if (null != this.animator)
                {
                    this.animator.SkeletonDrawn = false;
                }
            }

            this.HandleInput();
            this.UpdateCamera(gameTime);

            base.Update(gameTime);
        }

        /// <summary>
        /// Create the viewing camera.
        /// </summary>
        protected void UpdateViewingCamera()
        {
            GraphicsDevice device = this.graphics.GraphicsDevice;

            // Compute camera matrices.
            this.view = Matrix.CreateTranslation(0, -CameraHeight, 0) *
                          Matrix.CreateRotationY(MathHelper.ToRadians(this.cameraRotation)) *
                          Matrix.CreateRotationX(MathHelper.ToRadians(this.cameraArc)) *
                          Matrix.CreateLookAt(
                                                new Vector3(0, 0, -this.cameraDistance),
                                                new Vector3(0, 0, 0), 
                                                Vector3.Up);

            // Kinect vertical FOV in degrees
            float nominalVerticalFieldOfView = 45.6f;

            if (null != this.chooser && null != this.Chooser.Sensor && this.Chooser.Sensor.IsRunning && KinectStatus.Connected == this.Chooser.Sensor.Status)
            {
                nominalVerticalFieldOfView = this.chooser.Sensor.DepthStream.NominalVerticalFieldOfView;
            }

            this.projection = Matrix.CreatePerspectiveFieldOfView(
                                                                nominalVerticalFieldOfView * (float)Math.PI / 180.0f,
                                                                device.Viewport.AspectRatio,
                                                                1,
                                                                10000);
        }

        #endregion

        #region Draw
        
        /// <summary>
        /// This method renders the current state.
        /// </summary>
        /// <param name="gameTime">The elapsed game time.</param>
        protected override void Draw(GameTime gameTime)
        {
            // Clear the screen
            GraphicsDevice.Clear(Color.White);

            this.UpdateViewingCamera();

            // Render the depth and skeleton stream
            if (null != this.depthStream && null != this.skeletonStream)
            {
                this.depthStream.Draw(gameTime);
                this.skeletonStream.Draw(gameTime);
            }

            // Optionally draw a ground plane grid and world axes that the avatar stands on.
            // For our axes, red is +X, green is +Y, blue is +Z
            if (this.drawGrid && null != this.planarXzGrid && null != this.worldAxes)
            {
                this.planarXzGrid.Draw(gameTime, Matrix.Identity, this.view, this.projection);
                this.worldAxes.Draw(gameTime, Matrix.Identity, this.view, this.projection);
            }

            // Draw the actual avatar
            if (null != this.animator && (!this.drawAvatarOnlyWhenPlayerDetected || (this.drawAvatarOnlyWhenPlayerDetected && this.skeletonDetected)))
            {
                this.animator.Draw(gameTime, Matrix.Identity, this.view, this.projection);
            }

            // Render header/footer image
            this.SharedSpriteBatch.Begin();
            this.SharedSpriteBatch.Draw(this.header, Vector2.Zero, null, Color.White);
            this.SharedSpriteBatch.End();

            base.Draw(gameTime);
        }
        
        #endregion

        #region Handle Input

        /// <summary>
        /// Handles input for quitting the game.
        /// </summary>
        private void HandleInput()
        {
            this.currentKeyboard = Keyboard.GetState();

            // Check for exit.
            if (this.currentKeyboard.IsKeyDown(Keys.Escape))
            {
                this.Exit();
            }

            // Fullscreen on/off toggle
            if (this.currentKeyboard.IsKeyDown(Keys.F11))
            {
                // If not down last update, key has just been pressed.
                if (!this.previousKeyboard.IsKeyDown(Keys.F11))
                {
                    this.fullscreenMode = !this.fullscreenMode;
                    this.SetScreenMode();
                }
            }

            // Draw avatar when not detected on/off toggle
            if (this.currentKeyboard.IsKeyDown(Keys.V))
            {
                if (!this.previousKeyboard.IsKeyDown(Keys.V))
                {
                    this.drawAvatarOnlyWhenPlayerDetected = !this.drawAvatarOnlyWhenPlayerDetected;
                }
            }

            // Seated and near mode on/off toggle
            if (this.currentKeyboard.IsKeyDown(Keys.N))
            {
                if (!this.previousKeyboard.IsKeyDown(Keys.N))
                {
                    this.chooser.SeatedMode = !this.chooser.SeatedMode;
                    this.skeletonDetected = false;

                    // Set near mode to accompany seated mode
                    this.chooser.NearMode = this.chooser.SeatedMode;
                }
            }

            // Fix the avatar hip center draw height on/off toggle
            if (this.currentKeyboard.IsKeyDown(Keys.H))
            {
                if (!this.previousKeyboard.IsKeyDown(Keys.H))
                {
                    this.fixAvatarHipCenterDrawHeight = !this.fixAvatarHipCenterDrawHeight;
                }
            }

            // Fix the avatar leaning back too much on/off toggle
            if (this.currentKeyboard.IsKeyDown(Keys.L))
            {
                if (!this.previousKeyboard.IsKeyDown(Keys.L))
                {
                    this.leanAdjust = !this.leanAdjust;
                }
            }

            // Reset the avatar filters (also resets camera)
            if (this.currentKeyboard.IsKeyDown(Keys.R))
            {
                if (!this.previousKeyboard.IsKeyDown(Keys.R))
                {
                    if (null != this.animator)
                    {
                        this.animator.Reset();
                    }
                }
            }
        }

        /// <summary>
        /// Toggle between fullscreen and windowed mode
        /// </summary>
        private void SetScreenMode()
        {
            // This sets the display resolution or window size to the desired size
            // If windowed, it also forces a 4:3 ratio for height and adds 110 for header/footer
            if (this.fullscreenMode)
            {
                foreach (DisplayMode mode in GraphicsAdapter.DefaultAdapter.SupportedDisplayModes)
                {
                    // Check our requested FullScreenWidth and Height against each supported display mode and set if valid
                    if ((mode.Width == FullScreenWidth) && (mode.Height == FullScreenHeight))
                    {
                        this.graphics.PreferredBackBufferWidth = FullScreenWidth;
                        this.graphics.PreferredBackBufferHeight = FullScreenHeight;
                        this.graphics.IsFullScreen = true;
                        this.graphics.ApplyChanges();
                    }
                }
            }
            else
            {
                if (WindowedWidth <= GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width)
                {
                    this.graphics.PreferredBackBufferWidth = WindowedWidth;
                    this.graphics.PreferredBackBufferHeight = ((WindowedWidth / 4) * 3) + 110;
                    this.graphics.IsFullScreen = false;
                    this.graphics.ApplyChanges();
                }
            }

            this.UpdateStreamSizeAndLocation();
        }

        /// <summary>
        /// Update the depth and skeleton stream rendering position and size based on the backbuffer resolution.
        /// </summary>
        private void UpdateStreamSizeAndLocation()
        {
            int depthStreamWidth = this.graphics.PreferredBackBufferWidth / 4;
            Vector2 size = new Vector2(depthStreamWidth, (depthStreamWidth / 4) * 3);
            Vector2 pos = new Vector2(this.graphics.PreferredBackBufferWidth - depthStreamWidth - 10, 85);

            if (null != this.depthStream)
            {
                this.depthStream.Size = size;
                this.depthStream.Position = pos;
            }

            if (null != this.skeletonStream)
            {
                this.skeletonStream.Size = size;
                this.skeletonStream.Position = pos;
            }
        }

        /// <summary>
        /// Handles camera input.
        /// </summary>
        /// <param name="gameTime">The gametime.</param>
        private void UpdateCamera(GameTime gameTime)
        {
            float time = (float)gameTime.ElapsedGameTime.TotalMilliseconds;

            // Check for input to rotate the camera up and down around the model.
            if (this.currentKeyboard.IsKeyDown(Keys.Up) ||
                this.currentKeyboard.IsKeyDown(Keys.W))
            {
                this.cameraArc += time * CameraArcIncrement;
            }
            
            if (this.currentKeyboard.IsKeyDown(Keys.Down) ||
                this.currentKeyboard.IsKeyDown(Keys.S))
            {
                this.cameraArc -= time * CameraArcIncrement;
            }

            // Limit the arc movement.
            if (this.cameraArc > CameraArcAngleLimit)
            {
                this.cameraArc = CameraArcAngleLimit;
            }
            else if (this.cameraArc < -CameraArcAngleLimit)
            {
                this.cameraArc = -CameraArcAngleLimit;
            }

            // Check for input to rotate the camera around the model.
            if (this.currentKeyboard.IsKeyDown(Keys.Right) ||
                this.currentKeyboard.IsKeyDown(Keys.D))
            {
                this.cameraRotation += time * CameraArcIncrement;
            }

            if (this.currentKeyboard.IsKeyDown(Keys.Left) ||
                this.currentKeyboard.IsKeyDown(Keys.A))
            {
                this.cameraRotation -= time * CameraArcIncrement;
            }

            // Check for input to zoom camera in and out.
            if (this.currentKeyboard.IsKeyDown(Keys.Z))
            {
                this.cameraDistance += time * CameraZoomIncrement;
            }

            if (this.currentKeyboard.IsKeyDown(Keys.X))
            {
                this.cameraDistance -= time * CameraZoomIncrement;
            }

            // Limit the camera distance from the origin.
            if (this.cameraDistance > CameraMaxDistance)
            {
                this.cameraDistance = CameraMaxDistance;
            }
            else if (this.cameraDistance < CameraMinDistance)
            {
                this.cameraDistance = CameraMinDistance;
            }

            if (this.currentKeyboard.IsKeyDown(Keys.R))
            {
                this.cameraArc = 0;
                this.cameraRotation = 0;
                this.cameraDistance = CameraStartingTranslation;
            }
        }

        #endregion

        #region AvatarRetargeting

        /// <summary>
        /// 3D avatar models typically have varying bone structures and joint orientations, depending on how they are built.
        /// Here we adapt the calculated hierarchical relative rotation matrices to work with our avatar and set these into the 
        /// boneTransforms array. This array is then later converted to world transforms and then skinning transforms for the
        /// XNA skinning processor to draw the mesh.
        /// The "Dude.fbx" model defines more bones/joints (57 in total) and in different locations and orientations to the 
        /// Nui Skeleton. Many of the bones/joints have no direct equivalent - e.g. with Kinect we cannot currently recover 
        /// the fingers pose. Bones are defined relative to each other, hence unknown bones will be left as identity relative
        /// transformation in the boneTransforms array, causing them to take their parent's orientation in the world coordinate system.
        /// </summary>
        /// <param name="skeleton">The Kinect skeleton.</param>
        /// <param name="bindRoot">The bind root matrix of the avatar mesh.</param>
        /// <param name="boneTransforms">The avatar mesh rotation matrices.</param>
        private void RetargetMatrixHierarchyToAvatarMesh(Skeleton skeleton, Matrix bindRoot, Matrix[] boneTransforms)
        {
            if (null == skeleton)
            {
                return;
            }

            // Set the bone orientation data in the avatar mesh
            foreach (BoneOrientation bone in skeleton.BoneOrientations)
            {
                // If any of the joints/bones are not tracked, skip them
                // Note that if we run filters on the raw skeleton data, which fix tracking problems,
                // We should set the tracking state from NotTracked to Inferred.
                if (skeleton.Joints[bone.EndJoint].TrackingState == JointTrackingState.NotTracked)
                {
                    continue;
                }

                this.SetJointTransformation(bone, skeleton, bindRoot, ref boneTransforms);
            }

            // If seated mode is on, sit the avatar down
            if (this.Chooser.SeatedMode && this.setSeatedPostureInSeatedMode)
            {
                this.SetSeatedPosture(ref boneTransforms);
            }

            // Set the world position of the avatar
            this.SetAvatarRootWorldPosition(skeleton, ref boneTransforms);
        }

        /// <summary>
        /// Set the bone transform in the avatar mesh.
        /// </summary>
        /// <param name="bone">Nui Joint/bone orientation</param>
        /// <param name="skeleton">The Kinect skeleton.</param>
        /// <param name="bindRoot">The bind root matrix of the avatar mesh.</param>
        /// <param name="boneTransforms">The avatar mesh rotation matrices.</param>
        private void SetJointTransformation(BoneOrientation bone, Skeleton skeleton, Matrix bindRoot, ref Matrix[] boneTransforms)
        {
            // Always look at the skeleton root
            if (bone.StartJoint == JointType.HipCenter && bone.EndJoint == JointType.HipCenter)
            {
                // Unless in seated mode, the hip center is special - it is the root of the NuiSkeleton and describes the skeleton orientation in the world
                // (camera) coordinate system. All other bones/joint orientations in the hierarchy have hip center as one of their parents.
                // However, if in seated mode, the shoulder center then holds the skeleton orientation in the world (camera) coordinate system.
                bindRoot.Translation = Vector3.Zero;
                Matrix invBindRoot = Matrix.Invert(bindRoot);

                Matrix hipOrientation = KinectHelper.Matrix4ToXNAMatrix(bone.HierarchicalRotation.Matrix);

                // Here we create a rotation matrix for the hips from the inverse of the bind pose
                // for the pelvis rotation and the inverse of the bind pose for the root node (0) in the Dude model.
                // This multiplication effectively removes the initial 90 degree rotations set in the first two model nodes.
                Matrix pelvis = boneTransforms[1];
                pelvis.Translation = Vector3.Zero; // Ensure pure rotation as we explicitly set world translation from the Kinect camera below.
                Matrix invPelvis = Matrix.Invert(pelvis);

                Matrix combined = (invBindRoot * hipOrientation) * invPelvis;

                this.ReplaceBoneMatrix(JointType.HipCenter, combined, true, ref boneTransforms);
            }
            else if (bone.EndJoint == JointType.ShoulderCenter)
            {
                // This contains an absolute rotation if we are in seated mode, or the hip center is not tracked, as the HipCenter will be identity
                if (this.chooser.SeatedMode || (this.Chooser.SeatedMode == false && skeleton.Joints[JointType.HipCenter].TrackingState == JointTrackingState.NotTracked))
                {
                    bindRoot.Translation = Vector3.Zero;
                    Matrix invBindRoot = Matrix.Invert(bindRoot);

                    Matrix hipOrientation = KinectHelper.Matrix4ToXNAMatrix(bone.HierarchicalRotation.Matrix);

                    // We can use the same method as in HipCenter above to invert the root and pelvis bind pose,
                    // however, alternately we can also explicitly swap axes and adjust the rotations to get from
                    // the Kinect rotation to the model hip orientation, similar to what we do for the following joints/bones.

                    // Kinect = +X left, +Y up, +Z forward in body coordinate system
                    // Avatar = +Z left, +X up, +Y forward
                    Quaternion kinectRotation = KinectHelper.DecomposeMatRot(hipOrientation);    // XYZ
                    Quaternion avatarRotation = new Quaternion(kinectRotation.Y, kinectRotation.Z, kinectRotation.X, kinectRotation.W); // transform from Kinect to avatar coordinate system
                    Matrix combined = Matrix.CreateFromQuaternion(avatarRotation);

                    // Add a small adjustment rotation to manually correct for the rotation in the parent bind
                    // pose node in the model mesh - this can be found by looking in the FBX or in 3DSMax/Maya.
                    Matrix adjustment = Matrix.CreateRotationY(MathHelper.ToRadians(-90));
                    combined *= adjustment;
                    Matrix adjustment2 = Matrix.CreateRotationZ(MathHelper.ToRadians(-90));
                    combined *= adjustment2;

                    // Although not strictly correct, we apply this to the hip center, as all other bones are children of this joint.
                    // Application at the spine or shoulder center instead would require manually updating of the bone orientations below for the whole body to move when the shoulders twist or tilt.
                    this.ReplaceBoneMatrix(JointType.HipCenter, combined, true, ref boneTransforms);
                }
            }
            else if (bone.EndJoint == JointType.Spine)
            {
                Matrix tempMat = KinectHelper.Matrix4ToXNAMatrix(bone.HierarchicalRotation.Matrix);

                // The Dude appears to lean back too far compared to a real person, so here we adjust this lean.
                this.CorrectBackwardsLean(skeleton, ref tempMat);

                // Also add a small constant adjustment rotation to correct for the hip center to spine bone being at a rear-tilted angle in the Kinect skeleton.
                // The dude should now look more straight ahead when avateering
                Matrix adjustment = Matrix.CreateRotationX(MathHelper.ToRadians(20));  // 20 degree rotation around the local Kinect x axis for the spine bone.
                tempMat *= adjustment;

                // Kinect = +X left, +Y up, +Z forward in body coordinate system
                // Avatar = +Z left, +X up, +Y forward
                Quaternion kinectRotation = KinectHelper.DecomposeMatRot(tempMat);    // XYZ
                Quaternion avatarRotation = new Quaternion(kinectRotation.Y, kinectRotation.Z, kinectRotation.X, kinectRotation.W); // transform from Kinect to avatar coordinate system
                tempMat = Matrix.CreateFromQuaternion(avatarRotation);

                // Set the corresponding matrix in the avatar using the translation table we specified.
                // Note for the spine and shoulder center rotations, we could also try to spread the angle
                // over all the Avatar skeleton spine joints, causing a more curved back, rather than apply
                // it all to one joint, as we do here.
                this.ReplaceBoneMatrix(bone.EndJoint, tempMat, false, ref boneTransforms);
            }
            else if (bone.EndJoint == JointType.Head)
            {
                Matrix tempMat = KinectHelper.Matrix4ToXNAMatrix(bone.HierarchicalRotation.Matrix);

                // Add a small adjustment rotation to correct for the avatar skeleton head bones being defined pointing looking slightly down, not vertical.
                // The dude should now look more straight ahead when avateering
                Matrix adjustment = Matrix.CreateRotationX(MathHelper.ToRadians(-30));  // -30 degree rotation around the local Kinect x axis for the head bone.
                tempMat *= adjustment;

                // Kinect = +X left, +Y up, +Z forward in body coordinate system
                // Avatar = +Z left, +X up, +Y forward
                Quaternion kinectRotation = KinectHelper.DecomposeMatRot(tempMat);    // XYZ
                Quaternion avatarRotation = new Quaternion(kinectRotation.Y, kinectRotation.Z, kinectRotation.X, kinectRotation.W); // transform from Kinect to avatar coordinate system
                tempMat = Matrix.CreateFromQuaternion(avatarRotation);

                // Set the corresponding matrix in the avatar using the translation table we specified
                this.ReplaceBoneMatrix(bone.EndJoint, tempMat, false, ref boneTransforms);
            }
            else if (bone.EndJoint == JointType.ElbowLeft || bone.EndJoint == JointType.WristLeft)
            {
                Matrix tempMat = KinectHelper.Matrix4ToXNAMatrix(bone.HierarchicalRotation.Matrix);

                if (bone.EndJoint == JointType.ElbowLeft)
                {
                    // Add a small adjustment rotation to correct for the avatar skeleton shoulder/upper arm bones.
                    // The dude should now be able to have arms correctly down at his sides when avateering
                    Matrix adjustment = Matrix.CreateRotationZ(MathHelper.ToRadians(-15));  // -15 degree rotation around the local Kinect z axis for the upper arm bone.
                    tempMat *= adjustment;
                }

                // Kinect = +Y along arm, +X down, +Z forward in body coordinate system
                // Avatar = +X along arm, +Y down, +Z backwards
                Quaternion kinectRotation = KinectHelper.DecomposeMatRot(tempMat);    // XYZ
                Quaternion avatarRotation = new Quaternion(kinectRotation.Y, -kinectRotation.Z, -kinectRotation.X, kinectRotation.W); // transform from Kinect to avatar coordinate system
                tempMat = Matrix.CreateFromQuaternion(avatarRotation);

                this.ReplaceBoneMatrix(bone.EndJoint, tempMat, false, ref boneTransforms);
            }
            else if (bone.EndJoint == JointType.HandLeft)
            {
                Matrix tempMat = KinectHelper.Matrix4ToXNAMatrix(bone.HierarchicalRotation.Matrix);

                // Add a small adjustment rotation to correct for the avatar skeleton wist/hand bone.
                // The dude should now have the palm of his hands toward his body when arms are straight down
                Matrix adjustment = Matrix.CreateRotationY(MathHelper.ToRadians(-90));  // -90 degree rotation around the local Kinect y axis for the wrist-hand bone.
                tempMat *= adjustment;

                // Kinect = +Y along arm, +X down, +Z forward in body coordinate system
                // Avatar = +X along arm, +Y down, +Z backwards
                Quaternion kinectRotation = KinectHelper.DecomposeMatRot(tempMat);    // XYZ
                Quaternion avatarRotation = new Quaternion(kinectRotation.Y, kinectRotation.X, -kinectRotation.Z, kinectRotation.W);
                tempMat = Matrix.CreateFromQuaternion(avatarRotation);

                this.ReplaceBoneMatrix(bone.EndJoint, tempMat, false, ref boneTransforms);
            }
            else if (bone.EndJoint == JointType.ElbowRight || bone.EndJoint == JointType.WristRight)
            {
                Matrix tempMat = KinectHelper.Matrix4ToXNAMatrix(bone.HierarchicalRotation.Matrix);

                if (bone.EndJoint == JointType.ElbowRight)
                {
                    // Add a small adjustment rotation to correct for the avatar skeleton shoulder/upper arm bones.
                    // The dude should now be able to have arms correctly down at his sides when avateering
                    Matrix adjustment = Matrix.CreateRotationZ(MathHelper.ToRadians(15));  // 15 degree rotation around the local Kinect  z axis for the upper arm bone.
                    tempMat *= adjustment;
                }

                // Kinect = +Y along arm, +X up, +Z forward in body coordinate system
                // Avatar = +X along arm, +Y back, +Z down
                Quaternion kinectRotation = KinectHelper.DecomposeMatRot(tempMat);    // XYZ
                Quaternion avatarRotation = new Quaternion(kinectRotation.Y, -kinectRotation.Z, -kinectRotation.X, kinectRotation.W); // transform from Kinect to avatar coordinate system
                tempMat = Matrix.CreateFromQuaternion(avatarRotation);

                this.ReplaceBoneMatrix(bone.EndJoint, tempMat, false, ref boneTransforms);
            }
            else if (bone.EndJoint == JointType.HandRight)
            {
                Matrix tempMat = KinectHelper.Matrix4ToXNAMatrix(bone.HierarchicalRotation.Matrix);

                // Add a small adjustment rotation to correct for the avatar skeleton wist/hand bone.
                // The dude should now have the palm of his hands toward his body when arms are straight down
                Matrix adjustment = Matrix.CreateRotationY(MathHelper.ToRadians(90));  // -90 degree rotation around the local Kinect y axis for the wrist-hand bone.
                tempMat *= adjustment;

                // Kinect = +Y along arm, +X up, +Z forward in body coordinate system
                // Avatar = +X along arm, +Y down, +Z forwards
                Quaternion kinectRotation = KinectHelper.DecomposeMatRot(tempMat);    // XYZ
                Quaternion avatarRotation = new Quaternion(kinectRotation.Y, -kinectRotation.X, kinectRotation.Z, kinectRotation.W); // transform from Kinect to avatar coordinate system
                tempMat = Matrix.CreateFromQuaternion(avatarRotation);

                this.ReplaceBoneMatrix(bone.EndJoint, tempMat, false, ref boneTransforms);
            }
            else if (bone.EndJoint == JointType.KneeLeft)
            {
                // Combine the two joint rotations from the hip and knee
                Matrix hipLeft = KinectHelper.Matrix4ToXNAMatrix(skeleton.BoneOrientations[JointType.HipLeft].HierarchicalRotation.Matrix);
                Matrix kneeLeft = KinectHelper.Matrix4ToXNAMatrix(bone.HierarchicalRotation.Matrix);
                Matrix combined = kneeLeft * hipLeft;

                this.SetLegMatrix(bone.EndJoint, combined, ref boneTransforms);
            }
            else if (bone.EndJoint == JointType.AnkleLeft || bone.EndJoint == JointType.AnkleRight)
            {
                Matrix tempMat = KinectHelper.Matrix4ToXNAMatrix(bone.HierarchicalRotation.Matrix);
                this.SetLegMatrix(bone.EndJoint, tempMat, ref boneTransforms);
            }
            else if (bone.EndJoint == JointType.KneeRight)
            {
                // Combine the two joint rotations from the hip and knee
                Matrix hipRight = KinectHelper.Matrix4ToXNAMatrix(skeleton.BoneOrientations[JointType.HipRight].HierarchicalRotation.Matrix);
                Matrix kneeRight = KinectHelper.Matrix4ToXNAMatrix(bone.HierarchicalRotation.Matrix);
                Matrix combined = kneeRight * hipRight;

                this.SetLegMatrix(bone.EndJoint, combined, ref boneTransforms);
            }
            else if (bone.EndJoint == JointType.FootLeft || bone.EndJoint == JointType.FootRight)
            {
                // Only set this if we actually have a good track on this and the parent
                if (skeleton.Joints[bone.EndJoint].TrackingState == JointTrackingState.Tracked && skeleton.Joints[skeleton.BoneOrientations[bone.EndJoint].StartJoint].TrackingState == JointTrackingState.Tracked)
                {
                    Matrix tempMat = KinectHelper.Matrix4ToXNAMatrix(bone.HierarchicalRotation.Matrix);

                    // Add a small adjustment rotation to correct for the avatar skeleton foot bones being defined pointing down at 45 degrees, not horizontal
                    Matrix adjustment = Matrix.CreateRotationX(MathHelper.ToRadians(-45));
                    tempMat *= adjustment;

                    // Kinect = +Y along foot (fwd), +Z up, +X right in body coordinate system
                    // Avatar = +X along foot (fwd), +Y up, +Z right
                    Quaternion kinectRotation = KinectHelper.DecomposeMatRot(tempMat); // XYZ
                    Quaternion avatarRotation = new Quaternion(kinectRotation.Y, kinectRotation.Z, kinectRotation.X, kinectRotation.W); // transform from Kinect to avatar coordinate system
                    tempMat = Matrix.CreateFromQuaternion(avatarRotation);

                    this.ReplaceBoneMatrix(bone.EndJoint, tempMat, false, ref boneTransforms);
                }
            }            
        }

        /// <summary>
        /// Correct the spine rotation when leaning back to reduce lean.
        /// </summary>
        /// <param name="skeleton">The Kinect skeleton.</param>
        /// <param name="spineMat">The spine orientation.</param>
        private void CorrectBackwardsLean(Skeleton skeleton, ref Matrix spineMat)
        {
            Matrix hipOrientation = KinectHelper.Matrix4ToXNAMatrix(skeleton.BoneOrientations[JointType.HipCenter].HierarchicalRotation.Matrix);

            Vector3 hipZ = new Vector3(hipOrientation.M31, hipOrientation.M32, hipOrientation.M33);   // Z (forward) vector
            Vector3 boneY = new Vector3(spineMat.M21, spineMat.M22, spineMat.M23);   // Y (up) vector

            hipZ *= -1;
            hipZ.Normalize();
            boneY.Normalize();

            // Dot product the hip center forward vector with our spine bone up vector.
            float cosAngle = Vector3.Dot(hipZ, boneY);

            // If it's negative (i.e. greater than 90), we are leaning back, so reduce this lean.
            if (cosAngle < 0 && this.leanAdjust)
            {
                float angle = (float)Math.Acos(cosAngle);
                float correction = (angle / 2) * -(cosAngle / 2);
                Matrix leanAdjustment = Matrix.CreateRotationX(correction);  // reduce the lean by up to half, scaled by how far back we are leaning
                spineMat *= leanAdjustment;
            }
        }

        /// <summary>
        /// Helper used for leg bones.
        /// </summary>
        /// <param name="joint">Nui Joint index</param>
        /// <param name="legRotation">Matrix containing a leg joint rotation.</param>
        /// <param name="boneTransforms">The avatar mesh rotation matrices.</param>
        private void SetLegMatrix(JointType joint, Matrix legRotation, ref Matrix[] boneTransforms)
        {
            // Kinect = +Y along leg (down), +Z fwd, +X right in body coordinate system
            // Avatar = +X along leg (down), +Y fwd, +Z right
            Quaternion kinectRotation = KinectHelper.DecomposeMatRot(legRotation);  // XYZ
            Quaternion avatarRotation = new Quaternion(kinectRotation.Y, kinectRotation.Z, kinectRotation.X, kinectRotation.W); // transform from Kinect to avatar coordinate system
            legRotation = Matrix.CreateFromQuaternion(avatarRotation);

            this.ReplaceBoneMatrix(joint, legRotation, false, ref boneTransforms);
        }

        /// <summary>
        /// Set the avatar root position in world coordinates.
        /// </summary>
        /// <param name="skeleton">The Kinect skeleton.</param>
        /// <param name="boneTransforms">The avatar mesh rotation matrices.</param>
        private void SetAvatarRootWorldPosition(Skeleton skeleton, ref Matrix[] boneTransforms)
        {
            // Get XNA world position of skeleton.
            Matrix worldTransform = this.GetModelWorldTranslation(skeleton.Joints, this.chooser.SeatedMode); 

            // set root translation
            boneTransforms[0].Translation = worldTransform.Translation;
        }

        /// <summary>
        /// This function sets the mapping between the Nui Skeleton bones/joints and the Avatar bones/joints
        /// </summary>
        /// <param name="joint">Nui Joint index</param>
        /// <param name="boneMatrix">Matrix to set in joint/bone.</param>
        /// <param name="replaceTranslationInExistingBoneMatrix">set Boolean true to replace the translation in the original bone matrix with the one passed in boneMatrix (i.e. at root), false keeps the original (default).</param>
        /// <param name="boneTransforms">The avatar mesh rotation matrices.</param>
        private void ReplaceBoneMatrix(JointType joint, Matrix boneMatrix, bool replaceTranslationInExistingBoneMatrix, ref Matrix[] boneTransforms)
        {
            int meshJointId;
            bool success = this.nuiJointToAvatarBoneIndex.TryGetValue(joint, out meshJointId);

            if (success)
            {
                Vector3 offsetTranslation = boneTransforms[meshJointId].Translation;
                boneTransforms[meshJointId] = boneMatrix;

                if (replaceTranslationInExistingBoneMatrix == false)
                {
                    // overwrite any new boneMatrix translation with the original one
                    boneTransforms[meshJointId].Translation = offsetTranslation;   // re-set the translation
                }
            }
        }

        /// <summary>
        /// Helper used to get the world translation for the root.
        /// </summary>
        /// <param name="joints">Nui Joint collection.</param>
        /// <param name="seatedMode">Boolean true if seated mode.</param>
        /// <returns>Returns a Matrix containing the translation.</returns>
        private Matrix GetModelWorldTranslation(JointCollection joints, bool seatedMode)
        {
            Vector3 transVec = Vector3.Zero;

            if (seatedMode && joints[JointType.ShoulderCenter].TrackingState != JointTrackingState.NotTracked)
            {
                transVec = KinectHelper.SkeletonPointToVector3(joints[JointType.ShoulderCenter].Position);
            }
            else
            {
                if (joints[JointType.HipCenter].TrackingState != JointTrackingState.NotTracked)
                {
                    transVec = KinectHelper.SkeletonPointToVector3(joints[JointType.HipCenter].Position);
                }
                else if (joints[JointType.ShoulderCenter].TrackingState != JointTrackingState.NotTracked)
                {
                    // finally try shoulder center if this is tracked while hip center is not
                    transVec = KinectHelper.SkeletonPointToVector3(joints[JointType.ShoulderCenter].Position);
                }
            }

            if (this.fixAvatarHipCenterDrawHeight)
            {
                transVec.Y = this.avatarHipCenterDrawHeight;
            }

            // Here we scale the translation, as the "Dude" avatar mesh is defined in centimeters, and the Kinect skeleton joint positions in meters.
            return Matrix.CreateTranslation(transVec * SkeletonTranslationScaleFactor);
        }

        /// <summary>
        /// Sets the Avatar in a seated posture - useful for seated mode.
        /// </summary>
        /// <param name="boneTransforms">The relative bone transforms of the avatar mesh.</param>
        private void SetSeatedPosture(ref Matrix[] boneTransforms)
        {
            // In the Kinect coordinate system, we first rotate from the local avatar 
            // root orientation with +Y up to +Y down for the leg bones (180 around Z)
            // then pull the knees up for a seated posture.
            Matrix rot180 = Matrix.CreateRotationZ(MathHelper.ToRadians(180));
            Matrix rot90 = Matrix.CreateRotationX(MathHelper.ToRadians(90));
            Matrix rotMinus90 = Matrix.CreateRotationX(MathHelper.ToRadians(-90));
            Matrix combinedHipRotation = rot90 * rot180;

            this.SetLegMatrix(JointType.KneeLeft, combinedHipRotation, ref boneTransforms);
            this.SetLegMatrix(JointType.KneeRight, combinedHipRotation, ref boneTransforms);
            this.SetLegMatrix(JointType.AnkleLeft, rotMinus90, ref boneTransforms);
            this.SetLegMatrix(JointType.AnkleRight, rotMinus90, ref boneTransforms);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// This method ensures that we can render to the back buffer without
        /// losing the data we already had in our previous back buffer.  This
        /// is necessary for the SkeletonStreamRenderer.
        /// </summary>
        /// <param name="sender">The sending object.</param>
        /// <param name="e">The event args.</param>
        private void GraphicsDevicePreparingDeviceSettings(object sender, PreparingDeviceSettingsEventArgs e)
        {
            // This is necessary because we are rendering to back buffer/render targets and we need to preserve the data
            e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
        }

        /// <summary>
        /// This method maps a SkeletonPoint to the depth frame.
        /// </summary>
        /// <param name="point">The SkeletonPoint to map.</param>
        /// <returns>A Vector2 of the location on the depth frame.</returns>
        private Vector2 SkeletonToDepthMap(SkeletonPoint point)
        {
            // This is used to map a skeleton point to the depth image location
            if (null == this.chooser || null == this.Chooser.Sensor || true != this.Chooser.Sensor.IsRunning || this.Chooser.Sensor.Status != KinectStatus.Connected)
            {
                return Vector2.Zero;
            }

            var depthPt = this.chooser.Sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(point, this.chooser.Sensor.DepthStream.Format);

            // scale to current depth image display size and add any position offset
            float x = (depthPt.X * this.skeletonStream.Size.X) / this.chooser.Sensor.DepthStream.FrameWidth;
            float y = (depthPt.Y * this.skeletonStream.Size.Y) / this.chooser.Sensor.DepthStream.FrameHeight;

            return new Vector2(x + this.skeletonStream.Position.X, y + this.skeletonStream.Position.Y);
        }

        #endregion
    }
}