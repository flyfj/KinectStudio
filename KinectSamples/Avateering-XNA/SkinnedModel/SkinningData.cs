//------------------------------------------------------------------------------
// <copyright file="SkinningData.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace SkinnedModel
{
    using System.Collections.Generic;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Content;

    /// <summary>
    /// Combines all the data needed to render and animate a skinned object.
    /// This is typically stored in the Tag property of the Model being animated.
    /// </summary>
    public class SkinningData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SkinningData"/> class.
        /// </summary>
        public SkinningData(List<Matrix> bindPose, List<Matrix> inverseBindPose, List<int> skeletonHierarchy)
        {
            BindPose = bindPose;
            InverseBindPose = inverseBindPose;
            SkeletonHierarchy = skeletonHierarchy;
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="SkinningData"/> class from being created.
        /// Private constructor for use by the XNB serialization.
        /// </summary>
        private SkinningData()
        {
        }

        /// <summary>
        /// Bind pose matrices for each bone in the skeleton,
        /// relative to the parent bone.
        /// </summary>
        [ContentSerializer]
        public List<Matrix> BindPose { get; private set; }

        /// <summary>
        /// Vertex to bone space transforms for each bone in the skeleton.
        /// </summary>
        [ContentSerializer]
        public List<Matrix> InverseBindPose { get; private set; }

        /// <summary>
        /// For each bone in the skeleton, stores the index of the parent bone.
        /// </summary>
        [ContentSerializer]
        public List<int> SkeletonHierarchy { get; private set; }
    }
}
