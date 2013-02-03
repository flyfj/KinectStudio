//------------------------------------------------------------------------------
// <copyright file="BoneOrientationConstraints.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.Avateering.Filters
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Kinect;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    /// <summary>
    /// Filter to correct the joint locations and joint orientations to constraint to range of viable human motion.
    /// </summary>
    public class BoneOrientationConstraints : DrawableGameComponent
    {
        /// <summary>
        /// Number of lines to draw in a circle for constraint cones.
        /// </summary>
        private const int Tesselation = 36;

        /// <summary>
        /// Line length scaling for constraint cones.
        /// </summary>
        private const float LineScale = 0.25f;

        /// <summary>
        /// The Joint Constraints.  
        /// </summary>
        private readonly List<BoneOrientationConstraint> jointConstraints = new List<BoneOrientationConstraint>();

        /// <summary>
        /// Set true if the bone constraints are mirrored.  
        /// </summary>
        private bool constraintsMirrored;

        /// <summary>
        /// Coordinate Cross to draw local axes.
        /// </summary>
        private CoordinateCross localJointCoordinateSystemCrosses;

        /// <summary>
        /// This is the array of 3D vertices with associated colors.
        /// </summary>
        private List<VertexPositionColor> lineVertices;

        /// <summary>
        /// This is the array of line vertices to draw.
        /// </summary>
        private List<short> lineIndices;

        /// <summary>
        /// This is the XNA BasicEffect we use to draw.
        /// </summary>
        private BasicEffect effect;

        /// <summary>
        /// Draw the Kinect 3D Skeleton constraint cones if true.
        /// </summary>
        private bool drawKinectSkeletonConstraintCones;

        /// <summary>
        /// Initializes a new instance of the BoneOrientationConstraints class.
        /// </summary>
        /// <param name="game">The related game object.</param>
        public BoneOrientationConstraints(Game game, Vector3 skeletonTranslationScaleFactor)
            : base(game)
        {
            this.drawKinectSkeletonConstraintCones = true;
            this.SkeletonTranslationScaleFactor = skeletonTranslationScaleFactor;

            this.localJointCoordinateSystemCrosses = new CoordinateCross(this.Game, 2f);
            this.Game.Components.Add(this.localJointCoordinateSystemCrosses);
        }

        /// <summary>
        /// The model mesh can be defined at arbitrary non-human size, so re-scale the Kinect translation for drawing the Kinect 3D Skeleton.
        /// </summary>
        private Vector3 SkeletonTranslationScaleFactor { get; set; }

        /// <summary>
        /// AddJointConstraint - Adds a joint constraint to the system.  
        /// </summary>
        /// <param name="joint">The skeleton joint/bone.</param>
        /// <param name="dir">The absolute dir for the center of the constraint cone.</param>
        /// <param name="angle">The angle of the constraint cone that the bone can move in.</param>
        public void AddBoneOrientationConstraint(JointType joint, Vector3 dir, float angle)
        {
            this.constraintsMirrored = false;
            BoneOrientationConstraint jc = new BoneOrientationConstraint(joint, dir, angle);
            this.jointConstraints.Add(jc);
        }

        /// <summary>
        /// AddDefaultConstraints - Adds a set of default joint constraints for normal human poses.  
        /// This is a reasonable set of constraints for plausible human bio-mechanics.
        /// </summary>
        public void AddDefaultConstraints()
        {
            // Constraints indexed on end joint (i.e. constrains the bone between start and end), but constraint applies at the start joint (i.e. the parent)
            // The constraint is defined in the local coordinate system of the parent bone, relative to the parent bone direction 

            // Acts at Hip Center (constrains hip center to spine bone)
            this.AddBoneOrientationConstraint(JointType.Spine, new Vector3(0.0f, 1.0f, 0.3f), 90.0f);

            // Acts at Spine (constrains spine to shoulder center bone)
            this.AddBoneOrientationConstraint(JointType.ShoulderCenter, new Vector3(0.0f, 1.0f, 0.0f), 50.0f);

            // Acts at Shoulder Center (constrains shoulder center to head bone)
            this.AddBoneOrientationConstraint(JointType.Head, new Vector3(0.0f, 1.0f, 0.3f), 45.0f);

            // Acts at Shoulder Joint (constraints shoulder-elbow bone)
            this.AddBoneOrientationConstraint(JointType.ElbowLeft, new Vector3(0.1f, 0.7f, 0.7f), 80.0f);   // along the bone, (i.e +Y), and forward +Z, enable 80 degrees rotation away from this
            this.AddBoneOrientationConstraint(JointType.ElbowRight, new Vector3(-0.1f, 0.7f, 0.7f), 80.0f);

            // Acts at Elbow Joint (constraints elbow-wrist bone)
            this.AddBoneOrientationConstraint(JointType.WristLeft, new Vector3(0.0f, 0.0f, 1.0f), 90.0f);   // +Z (i.e. so rotates up or down with twist, when arm bent, stop bending backwards)
            this.AddBoneOrientationConstraint(JointType.WristRight, new Vector3(0.0f, 0.0f, 1.0f), 90.0f);

            // Acts at Wrist Joint (constrains wrist-hand bone)
            this.AddBoneOrientationConstraint(JointType.HandLeft, new Vector3(0.0f, 1.0f, 0.0f), 45.0f);    // +Y is along the bone
            this.AddBoneOrientationConstraint(JointType.HandRight, new Vector3(0.0f, 1.0f, 0.0f), 45.0f);

            // Acts at Hip Joint (constrains hip-knee bone)
            this.AddBoneOrientationConstraint(JointType.KneeLeft, new Vector3(0.5f, 0.7f, 0.1f), 65.0f);
            this.AddBoneOrientationConstraint(JointType.KneeRight, new Vector3(-0.5f, 0.7f, 0.1f), 65.0f);

            // Acts at Knee Joint (constrains knee-ankle bone)
            this.AddBoneOrientationConstraint(JointType.AnkleRight, new Vector3(0.0f, 0.7f, -1.0f), 65.0f); // enable bending backwards with -Z
            this.AddBoneOrientationConstraint(JointType.AnkleLeft, new Vector3(0.0f, 0.7f, -1.0f), 65.0f);

            // Acts at Ankle Joint (constrains ankle-foot bone)
            this.AddBoneOrientationConstraint(JointType.FootRight, new Vector3(0.0f, 0.3f, 0.5f), 40.0f);
            this.AddBoneOrientationConstraint(JointType.FootLeft, new Vector3(0.0f, 0.3f, 0.5f), 40.0f);
        }

        /// <summary>
        /// ApplyBoneOrientationConstraints and constrain rotations.
        /// </summary>
        /// <param name="skeleton">The skeleton to correct.</param>
        /// <param name="mirrorView">Set this true if the skeleton joints are mirrored.</param>
        public void Constrain(Skeleton skeleton, bool mirrorView)
        {
            if (null == skeleton || skeleton.TrackingState != SkeletonTrackingState.Tracked)
            {
                return;
            }

            if (this.jointConstraints.Count == 0)
            {
                this.AddDefaultConstraints();
            }

            if (mirrorView != this.constraintsMirrored)
            {
                this.MirrorConstraints();
            }

            // Constraints are defined as a vector with respect to the parent bone vector, and a constraint angle, 
            // which is the maximum angle with respect to the constraint axis that the bone can move through.

            // Calculate constraint values. 0.0-1.0 means the bone is within the constraint cone. Greater than 1.0 means 
            // it lies outside the constraint cone.
            for (int i = 0; i < this.jointConstraints.Count; i++)
            {
                BoneOrientationConstraint jc = this.jointConstraints[i];

                if (skeleton.Joints[jc.Joint].TrackingState == JointTrackingState.NotTracked || jc.Joint == JointType.HipCenter) 
                {
                    // End joint is not tracked or Hip Center has no parent to perform this calculation with.
                    continue;
                }

                // If the joint has a parent, constrain the bone direction to be within the constraint cone
                JointType parentIdx = skeleton.BoneOrientations[jc.Joint].StartJoint;

                // Local bone orientation relative to parent
                Matrix boneOrientationRelativeToParent = KinectHelper.Matrix4ToXNAMatrix(skeleton.BoneOrientations[jc.Joint].HierarchicalRotation.Matrix);
                Quaternion boneOrientationRelativeToParentQuat = KinectHelper.Vector4ToXNAQuaternion(skeleton.BoneOrientations[jc.Joint].HierarchicalRotation.Quaternion);

                // Local bone direction is +Y vector in parent coordinate system
                Vector3 boneRelDirVecLs = new Vector3(boneOrientationRelativeToParent.M21, boneOrientationRelativeToParent.M22, boneOrientationRelativeToParent.M23);
                boneRelDirVecLs.Normalize();

                // Constraint is relative to the parent orientation, which is +Y/identity relative rotation
                Vector3 constraintDirLs = jc.Dir;
                constraintDirLs.Normalize();

                // Test this against the vector of the bone to find angle
                float boneDotConstraint = Vector3.Dot(boneRelDirVecLs, constraintDirLs);

                // Calculate the constraint value. 0.0 is in the center of the constraint cone, 1.0 and above are outside the cone.
                jc.Constraint = (float)Math.Acos(boneDotConstraint) / MathHelper.ToRadians(jc.Angle);

                this.jointConstraints[i] = jc;

                // Slerp between identity and the inverse of the current constraint rotation by the amount over the constraint amount
                if (jc.Constraint > 1)
                {
                    Quaternion inverseRotation = Quaternion.Inverse(boneOrientationRelativeToParentQuat);
                    Quaternion slerpedInverseRotation = Quaternion.Slerp(Quaternion.Identity, inverseRotation, jc.Constraint - 1);
                    Quaternion constrainedRotation = boneOrientationRelativeToParentQuat * slerpedInverseRotation;

                    // Put it back into the bone orientations
                    skeleton.BoneOrientations[jc.Joint].HierarchicalRotation.Quaternion = KinectHelper.XNAQuaternionToVector4(constrainedRotation);
                    skeleton.BoneOrientations[jc.Joint].HierarchicalRotation.Matrix = KinectHelper.XNAMatrixToMatrix4(Matrix.CreateFromQuaternion(constrainedRotation));
                }
            }

            // Recalculate the absolute rotations from the hierarchical relative rotations
            Array jointTypeValues = Enum.GetValues(typeof(JointType));

            foreach (JointType j in jointTypeValues)
            {
                if (j != JointType.HipCenter)
                {
                    JointType parentIdx = skeleton.BoneOrientations[j].StartJoint;

                    // Calculate the absolute/world equivalents of the hierarchical rotation
                    Quaternion parentRotation = KinectHelper.Vector4ToXNAQuaternion(skeleton.BoneOrientations[parentIdx].AbsoluteRotation.Quaternion);
                    Quaternion relativeRotation = KinectHelper.Vector4ToXNAQuaternion(skeleton.BoneOrientations[j].HierarchicalRotation.Quaternion);

                    // Create a new world rotation
                    Quaternion worldRotation = Quaternion.Multiply(parentRotation, relativeRotation);

                    skeleton.BoneOrientations[j].AbsoluteRotation.Quaternion = KinectHelper.XNAQuaternionToVector4(worldRotation);
                    skeleton.BoneOrientations[j].AbsoluteRotation.Matrix = KinectHelper.XNAMatrixToMatrix4(Matrix.CreateFromQuaternion(worldRotation));
                }
            }
        }

        /// <summary>
        /// This method draws the skeleton frame data.
        /// </summary>
        /// <param name="gameTime">The elapsed game time.</param>
        /// <param name="skeleton">The skeleton to correct.</param>
        /// <param name="seatedMode">Set true if in seated mode.</param>
        /// <param name="world">The world matrix.</param>
        /// <param name="view">The view matrix.</param>
        /// <param name="projection">The projection matrix.</param>
        public void Draw(GameTime gameTime, Skeleton skeleton, bool seatedMode, Matrix world, Matrix view, Matrix projection)
        {
            // If we don't have data, lets leave
            if (null == skeleton || null == this.Game || null == this.effect)
            {
                return;
            }

            GraphicsDevice device = this.Game.GraphicsDevice;

            // Disable the depth buffer so we can render the Kinect skeleton inside the model
            device.DepthStencilState = DepthStencilState.None;

            // Update the Kinect skeleton in the display
            this.CreateKinectSkeleton(skeleton, seatedMode);

            if (this.drawKinectSkeletonConstraintCones)
            {
                this.AddConstraintDirectionCones(skeleton, seatedMode);
            }

            this.effect.World = world;
            this.effect.View = view;
            this.effect.Projection = projection;
            this.effect.VertexColorEnabled = true;

            foreach (EffectPass pass in this.effect.CurrentTechnique.Passes)
            {
                pass.Apply();

                // Draw Kinect Skeleton vertices as Line List
                device.DrawUserIndexedPrimitives<VertexPositionColor>(
                    PrimitiveType.LineList,
                    this.lineVertices.ToArray(),
                    0,
                    this.lineVertices.Count,
                    this.lineIndices.ToArray(),
                    0,
                    this.lineIndices.Count / 2);
            }

            if (null != this.localJointCoordinateSystemCrosses)
            {
                Array jointTypeValues = Enum.GetValues(typeof(JointType));

                foreach (JointType j in jointTypeValues)
                {
                    if (KinectHelper.IsTrackedOrInferred(skeleton, j))
                    {
                        Matrix localWorld = KinectHelper.Matrix4ToXNAMatrix(skeleton.BoneOrientations[j].AbsoluteRotation.Matrix);
                        Vector3 jointPosVec = KinectHelper.Position(skeleton, j);

                        jointPosVec *= this.SkeletonTranslationScaleFactor; // This will scale and optionally mirror the skeleton
                        localWorld.Translation = jointPosVec; // set the translation into the rotation matrix

                        this.localJointCoordinateSystemCrosses.Draw(gameTime, localWorld * world, view, projection);
                    }
                }
            }

            // Re-enable the depth buffer
            device.DepthStencilState = DepthStencilState.Default;
        }

        /// <summary>
        /// This method loads the avatar model mesh and sets the bind pose.
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

        /// <summary>
        /// Helper method computes a point on a circle.
        /// </summary>
        /// <param name="index">The index of the vertex around the circle.</param>
        /// <param name="tessellation">The number of vertices in total to calculate around the circle.</param>
        /// <returns>Returns a Vector3 vertex around the circle on the x,z plane.</returns>
        private static Vector3 GetCircleVector(int index, int tessellation)
        {
            float angle = index * MathHelper.TwoPi / tessellation;

            float dx = (float)Math.Cos(angle);
            float dz = (float)Math.Sin(angle);

            return new Vector3(dx, 0, dz);
        }

        /// <summary>
        /// Helper method to swap mirror the skeleton bone constraints when the skeleton is mirrored.
        /// </summary>
        private void MirrorConstraints()
        {
            this.SwapJointTypes(JointType.ShoulderLeft, JointType.ShoulderRight);
            this.SwapJointTypes(JointType.ElbowLeft, JointType.ElbowRight);
            this.SwapJointTypes(JointType.WristLeft, JointType.WristRight);
            this.SwapJointTypes(JointType.HandLeft, JointType.HandRight);

            this.SwapJointTypes(JointType.HipLeft, JointType.HipRight);
            this.SwapJointTypes(JointType.KneeLeft, JointType.KneeRight);
            this.SwapJointTypes(JointType.AnkleLeft, JointType.AnkleRight);
            this.SwapJointTypes(JointType.FootLeft, JointType.FootRight);

            for (int i = 0; i < this.jointConstraints.Count; i++)
            {
                BoneOrientationConstraint jc = this.jointConstraints[i];

                // Here we negate the X axis to change the skeleton to mirror the user's movements.
                jc.Dir.X = -jc.Dir.X;

                this.jointConstraints[i] = jc;
            }

            this.constraintsMirrored = !this.constraintsMirrored;
        }

        /// <summary>
        /// Helper method to swap two joint types in the skeleton when mirroring the avatar.
        /// </summary>
        /// <param name="left">The left joint type.</param>
        /// <param name="right">The right joint type.</param>
        private void SwapJointTypes(JointType left, JointType right)
        {
            for (int i = 0; i < this.jointConstraints.Count; i++)
            {
                BoneOrientationConstraint jc = this.jointConstraints[i];

                if (jc.Joint == left)
                {
                    jc.Joint = right;
                }
                else if (jc.Joint == right)
                {
                    jc.Joint = left;
                }

                this.jointConstraints[i] = jc;
            }
        }

        /// <summary>
        /// Helper method to add a line between two joints to the list of lines for drawing.
        /// </summary>
        /// <param name="skeleton">The skeleton.</param>
        /// <param name="vertex1">The joint type of the first vertex.</param>
        /// <param name="vertex2">The joint type of the second vertex.</param>
        private void AddLine(Skeleton skeleton, JointType vertex1, JointType vertex2)
        {
            // Do not draw bone if not tracked
            if (!KinectHelper.IsTrackedOrInferred(skeleton, vertex1)
                || !KinectHelper.IsTrackedOrInferred(skeleton, vertex2))
            {
                return;
            }

            Color color = Color.Green;

            // Change color of bone to black if at least one joint is inferred
            if (!KinectHelper.IsTracked(skeleton, vertex1)
                || !KinectHelper.IsTracked(skeleton, vertex2))
            {
                color = Color.Black;
            }

            // Change color of bone to red if constrained
            for (int i = 0; i < this.jointConstraints.Count; i++)
            {
                if (this.jointConstraints[i].Joint == vertex2 && this.jointConstraints[i].Constraint > 1)
                {
                    color = Color.Red;
                }
            }

            Vector3 jointPosition1 = KinectHelper.Position(skeleton, vertex1);
            Vector3 jointPosition2 = KinectHelper.Position(skeleton, vertex2);

            jointPosition1 *= this.SkeletonTranslationScaleFactor;  // This will scale and optionally mirror the skeleton
            jointPosition2 *= this.SkeletonTranslationScaleFactor;

            this.lineVertices.Add(new VertexPositionColor(jointPosition1, color));
            this.lineIndices.Add((short)(this.lineVertices.Count - 1));
            this.lineVertices.Add(new VertexPositionColor(jointPosition2, color));
            this.lineIndices.Add((short)(this.lineVertices.Count - 1));
        }

        /// <summary>
        /// Helper method to add the lines of the Kinect skeleton for drawing.
        /// </summary>
        /// <param name="skeleton">The skeleton.</param>
        /// <param name="seatedMode">Set true if in seated mode.</param>
        private void CreateKinectSkeleton(Skeleton skeleton, bool seatedMode)
        {
            if (null == this.lineVertices)
            {
                this.lineVertices = new List<VertexPositionColor>();
                this.lineIndices = new List<short>();
            }
            else
            {
                this.lineVertices.Clear();
                this.lineIndices.Clear();
            }

            // Upper Torso
            this.AddLine(skeleton, JointType.Head, JointType.ShoulderCenter);
            this.AddLine(skeleton, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.AddLine(skeleton, JointType.ShoulderCenter, JointType.ShoulderRight);

            // Left Arm
            this.AddLine(skeleton, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.AddLine(skeleton, JointType.ElbowLeft, JointType.WristLeft);
            this.AddLine(skeleton, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.AddLine(skeleton, JointType.ShoulderRight, JointType.ElbowRight);
            this.AddLine(skeleton, JointType.ElbowRight, JointType.WristRight);
            this.AddLine(skeleton, JointType.WristRight, JointType.HandRight);

            if (!seatedMode)
            {
                // Torso
                this.AddLine(skeleton, JointType.ShoulderCenter, JointType.Spine);
                this.AddLine(skeleton, JointType.Spine, JointType.HipCenter);
                this.AddLine(skeleton, JointType.HipCenter, JointType.HipLeft);
                this.AddLine(skeleton, JointType.HipCenter, JointType.HipRight);

                // Left Leg
                this.AddLine(skeleton, JointType.HipLeft, JointType.KneeLeft);
                this.AddLine(skeleton, JointType.KneeLeft, JointType.AnkleLeft);
                this.AddLine(skeleton, JointType.AnkleLeft, JointType.FootLeft);

                // Right Leg
                this.AddLine(skeleton, JointType.HipRight, JointType.KneeRight);
                this.AddLine(skeleton, JointType.KneeRight, JointType.AnkleRight);
                this.AddLine(skeleton, JointType.AnkleRight, JointType.FootRight);
            }
        }

        /// <summary>
        /// Helper method to add the constraint cone for drawing.
        /// </summary>
        /// <param name="skeleton">The skeleton.</param>
        /// <param name="seatedMode">Set true if in seated mode.</param>
        private void AddConstraintDirectionCones(Skeleton skeleton, bool seatedMode)
        {
            // Calculate constraint values.  0.0-1.0 means the bone is within the constraint cone.  Greater than 1.0 means 
            // it lies outside the constraint cone.
            for (int i = 0; i < this.jointConstraints.Count; i++)
            {
                BoneOrientationConstraint jc = this.jointConstraints[i];

                JointType joint = this.jointConstraints[i].Joint;

                // Do not draw bone if not tracked
                if (skeleton.Joints[joint].TrackingState == JointTrackingState.NotTracked)
                {
                    continue;
                }

                // Do not draw the following bones if in seated mode
                if (
                    seatedMode && 
                    (JointType.HipCenter == joint 
                    || JointType.Spine == joint
                    || JointType.ShoulderCenter == joint
                    || JointType.HipLeft == joint 
                    || JointType.KneeLeft == joint 
                    || JointType.AnkleLeft == joint 
                    || JointType.FootLeft == joint 
                    || JointType.HipRight == joint 
                    || JointType.KneeRight == joint 
                    || JointType.AnkleRight == joint 
                    || JointType.FootRight == joint))
                {
                    continue;
                }

                // Get the constraint direction in world space from the parent orientation
                Matrix matConstraintLocalToWorld = KinectHelper.Matrix4ToXNAMatrix(skeleton.BoneOrientations[skeleton.BoneOrientations[jc.Joint].StartJoint].AbsoluteRotation.Matrix);

                Vector3 constraintDirWs = jc.Dir;
                constraintDirWs.Normalize();
                Vector3 rotatedConstraintDirWs = Vector3.Transform(constraintDirWs, matConstraintLocalToWorld);
                rotatedConstraintDirWs.Normalize();

                // Draw the constraint direction line itself
                rotatedConstraintDirWs *= 0.5f;
                this.AddRotatedLine(rotatedConstraintDirWs, skeleton, i, Color.DeepPink);

                // Get the bone direction in world space
                Vector3 boneDirWs = KinectHelper.VectorBetween(
                    skeleton,
                    skeleton.BoneOrientations[jc.Joint].StartJoint,
                    skeleton.BoneOrientations[jc.Joint].EndJoint);
                boneDirWs.Normalize();

                Quaternion constraintRotation = KinectHelper.GetShortestRotationBetweenVectors(constraintDirWs, Vector3.Up);

                for (int c = 0; c < Tesselation; c++)
                {
                    Vector3 circlePoint = GetCircleVector(c, Tesselation);

                    // Calculate distance based on known radius (opposite side) of 1 and angle
                    circlePoint.Y = 1.0f / (float)Math.Tan(MathHelper.ToRadians(jc.Angle));

                    circlePoint.Normalize();
                    Vector3 tranCirclePoint = Vector3.Transform(circlePoint, constraintRotation);

                    // now transform this by the parent rotation
                    Vector3 rotatedConstraintConePointWs = Vector3.Transform(tranCirclePoint, matConstraintLocalToWorld);
                    rotatedConstraintConePointWs *= LineScale;   // scale

                    this.AddRotatedLine(rotatedConstraintConePointWs, skeleton, i, Color.HotPink);
                }
            }
        }

        /// <summary>
        /// Helper method to calculate a rotated line for the constraint cones.
        /// </summary>
        private void AddRotatedLine(Vector3 rotatedLine, Skeleton skeleton, int i, Color color)
        {
            BoneOrientationConstraint jc = this.jointConstraints[i];
            Vector3 coneTipVertex = KinectHelper.Position(skeleton, KinectHelper.ParentBoneJoint(jc.Joint));

            // Scale dir vector for display
            rotatedLine *= LineScale;

            Vector3 coneBaseVertex = coneTipVertex + rotatedLine;

            coneTipVertex *= this.SkeletonTranslationScaleFactor; // This will scale and optionally mirror the skeleton
            coneBaseVertex *= this.SkeletonTranslationScaleFactor;

            this.lineVertices.Add(new VertexPositionColor(coneTipVertex, color));
            this.lineIndices.Add((short)(this.lineVertices.Count - 1));
            this.lineVertices.Add(new VertexPositionColor(coneBaseVertex, color));
            this.lineIndices.Add((short)(this.lineVertices.Count - 1));
        }

        /// <summary>
        /// Joint Constraint structure to hold the constraint axis, angle and cone direction and associated joint.
        /// </summary>
        private struct BoneOrientationConstraint
        {
            /// <summary>
            /// Constraint cone direction
            /// </summary>
            public Vector3 Dir;

            /// <summary>
            /// Skeleton joint
            /// </summary>
            public JointType Joint;

            /// <summary>
            /// Constraint cone angle
            /// </summary>
            public float Angle;

            /// <summary>
            /// Calculated dynamic value of constraint
            /// </summary>
            public float Constraint;

            /// <summary>
            /// Initializes a new instance of the <see cref="BoneOrientationConstraint"/> struct.
            /// </summary>
            /// <param name="joint">The joint/bone the constraint refers to.</param>
            /// <param name="dir">The constraint cone center direction.</param>
            /// <param name="angle">The constraint cone angle from the center direction.</param>
            public BoneOrientationConstraint(JointType joint, Vector3 dir, float angle)
            {
                this.Joint = joint;
                this.Dir = dir;
                this.Angle = angle;
                this.Constraint = 0;
            }
        }
    }
}
