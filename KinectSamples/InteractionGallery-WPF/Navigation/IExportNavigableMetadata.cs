// -----------------------------------------------------------------------
// <copyright file="IExportNavigableMetadata.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.InteractionGallery.Navigation
{
    /// <summary>
    /// Interface for an INavigable exported part
    /// </summary>
    public interface IExportNavigableMetadata
    {
        /// <summary>
        /// Name of the navigation context.
        /// </summary>
        string NavigableContextName { get; }
    }
}
