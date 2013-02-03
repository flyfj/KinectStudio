//------------------------------------------------------------------------------
// <copyright file="SkinnedModelProcessor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.SkinnedModelPipeline
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Content.Pipeline;
    using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
    using Microsoft.Xna.Framework.Content.Pipeline.Processors;
    using Microsoft.Xna.Framework.Graphics;
    using SkinnedModel;
    
    /// <summary>
    /// Custom processor extends the built in framework ModelProcessor class,
    /// adding animation support.
    /// </summary>
    [ContentProcessor]
    public class SkinnedModelProcessor : ModelProcessor
    {
        /// <summary>
        /// Force all the materials to use our skinned model effect.
        /// </summary>
        [DefaultValue(MaterialProcessorDefaultEffect.SkinnedEffect)]
        public override MaterialProcessorDefaultEffect DefaultEffect
        {
            get { return MaterialProcessorDefaultEffect.SkinnedEffect; }
            set { }
        }

        /// <summary>
        /// Makes sure this mesh contains the kind of data we know how to animate.
        /// </summary>
        public static void ValidateMesh(NodeContent node, ContentProcessorContext context, string parentBoneName)
        {
            MeshContent mesh = node as MeshContent;

            if (mesh != null)
            {
                // Validate the mesh.
                if (parentBoneName != null)
                {
                    context.Logger.LogWarning(
                        null,
                        null,
                        "Mesh {0} is a child of bone {1}. SkinnedModelProcessor does not correctly handle meshes that are children of bones.",
                        mesh.Name,
                        parentBoneName);
                }

                if (!MeshHasSkinning(mesh))
                {
                    context.Logger.LogWarning(
                        null,
                        null,
                        "Mesh {0} has no skinning information, so it has been deleted.",
                        mesh.Name);

                    mesh.Parent.Children.Remove(mesh);
                    return;
                }
            }
            else if (node is BoneContent)
            {
                // If this is a bone, remember that we are now looking inside it.
                parentBoneName = node.Name;
            }

            // Recurse (iterating over a copy of the child collection,
            // because validating children may delete some of them).
            foreach (NodeContent child in new List<NodeContent>(node.Children))
            {
                ValidateMesh(child, context, parentBoneName);
            }
        }

        /// <summary>
        /// Checks whether a mesh contains skinning information.
        /// </summary>
        /// <returns>Returns true if mesh has skin</returns>
        public static bool MeshHasSkinning(MeshContent mesh)
        {
            foreach (GeometryContent geometry in mesh.Geometry)
            {
                if (!geometry.Vertices.Channels.Contains(VertexChannelNames.Weights()))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Bakes unwanted transforms into the model geometry,
        /// so everything ends up in the same coordinate system.
        /// </summary>
        public static void FlattenTransforms(NodeContent node, BoneContent skeleton)
        {
            foreach (NodeContent child in node.Children)
            {
                // Don't process the skeleton, because that is special.
                if (child == skeleton)
                {
                    continue;
                }

                // Bake the local transform into the actual geometry.
                MeshHelper.TransformScene(child, child.Transform);

                // Having baked it, we can now set the local
                // coordinate system back to identity.
                child.Transform = Matrix.Identity;

                // Recurse.
                FlattenTransforms(child, skeleton);
            }
        }

        /// <summary>
        /// The main Process method converts an intermediate format content pipeline
        /// NodeContent tree to a ModelContent object with embedded animation data.
        /// </summary>
        /// <param name="input">NodeContent input</param>
        /// <param name="context">ContentProcessorContext input</param>
        /// <returns>Model Content</returns>
        public override ModelContent Process(NodeContent input, ContentProcessorContext context)
        {
            ValidateMesh(input, context, null);

            // Find the skeleton.
            BoneContent skeleton = MeshHelper.FindSkeleton(input);

            if (skeleton == null)
            {
                throw new InvalidContentException("Input skeleton not found.");
            }

            // We don't want to have to worry about different parts of the model being
            // in different local coordinate systems, so let's just bake everything.
            FlattenTransforms(input, skeleton);

            // Read the bind pose and skeleton hierarchy data.
            IList<BoneContent> bones = MeshHelper.FlattenSkeleton(skeleton);

            if (bones.Count > SkinnedEffect.MaxBones)
            {
                throw new InvalidContentException(
                    string.Format(
                    "Skeleton has {0} bones, but the maximum supported is {1}.",
                    bones.Count, 
                    SkinnedEffect.MaxBones));
            }

            List<Matrix> bindPose = new List<Matrix>();
            List<Matrix> inverseBindPose = new List<Matrix>();
            List<int> skeletonHierarchy = new List<int>();

            foreach (BoneContent bone in bones)
            {
                bindPose.Add(bone.Transform);
                inverseBindPose.Add(Matrix.Invert(bone.AbsoluteTransform));
                skeletonHierarchy.Add(bones.IndexOf(bone.Parent as BoneContent));
            }

            // Chain to the base ModelProcessor class so it can convert the model data.
            ModelContent model = base.Process(input, context);

            // Store our custom animation data in the Tag property of the model.
            model.Tag = new SkinningData(bindPose, inverseBindPose, skeletonHierarchy);

            return model;
        }
    }
}
