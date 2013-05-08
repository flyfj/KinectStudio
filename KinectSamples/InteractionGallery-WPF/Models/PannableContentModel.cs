// -----------------------------------------------------------------------
// <copyright file="PannableContentModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.InteractionGallery.Models
{
    using System;

    /// <summary>
    /// Data model for an individual image to be displayed on pannable experience.
    /// </summary>
    public class PannableContentModel
    {
        /// <summary>
        /// Pack Uri to the image resource
        /// </summary>
        public Uri ImageUri { get; set; }
    }
}