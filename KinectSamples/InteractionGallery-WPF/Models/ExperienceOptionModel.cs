// -----------------------------------------------------------------------
// <copyright file="ExperienceOptionModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.InteractionGallery.Models
{
    using System;

    /// <summary>
    /// Data model representing a single experience option on the home screen
    /// </summary>
    public class ExperienceOptionModel
    {
        /// <summary>
        /// Name of the experience option
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Pack Uri to the image resource
        /// </summary>
        public Uri ImageUri { get; set; }

        /// <summary>
        /// Pack Uri to an optional overlay image resource. If there is
        /// no overlay image, this property is set to null.
        /// </summary>
        public Uri OverlayImageUri { get; set; }

        /// <summary>
        /// MEF exported name of the experience option
        /// </summary>
        public string NavigableContextName { get; set; }

        /// <summary>
        /// Uri to initialization data for the experience option
        /// </summary>
        public Uri NavigableContextParameter { get; set; } 
    }
}