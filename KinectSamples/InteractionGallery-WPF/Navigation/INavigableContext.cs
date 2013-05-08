// -----------------------------------------------------------------------
// <copyright file="INavigableContext.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.InteractionGallery.Navigation
{
    using System;

    /// <summary>
    /// Interface which all classes that participate in navigation through the NavigationManager must implement.
    /// </summary>
    public interface INavigableContext
    {
        /// <summary>
        /// Method that is called to allow initialization with respect to data pointed to by the parameter
        /// </summary>
        /// <param name="parameter">Uri pointing to initialization data for this navigable context</param>
        void Initialize(Uri parameter);

        /// <summary>
        /// Method that is called when this navigation context is being transitioned to
        /// </summary>
        void OnNavigatedTo();

        /// <summary>
        /// Method that is called when this navigation context is being transitioned away from
        /// </summary>
        void OnNavigatedFrom();
    }
}
