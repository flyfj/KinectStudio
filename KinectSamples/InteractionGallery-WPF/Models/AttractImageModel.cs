// -----------------------------------------------------------------------
// <copyright file="AttractImageModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.InteractionGallery.Models
{
    using System;

    /// <summary>
    /// Data model for an individual image to be displayed on the attract screen
    /// </summary>
    public class AttractImageModel
    {
        /// <summary>
        /// Pack Uri to the image resource
        /// </summary>
        public Uri ImageUri { get; set; }
    }
}