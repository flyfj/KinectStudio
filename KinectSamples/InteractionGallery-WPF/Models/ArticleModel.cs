// -----------------------------------------------------------------------
// <copyright file="ArticleModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.InteractionGallery.Models
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Data model for an article
    /// </summary>
    public class ArticleModel
    {
        private ICollection<Uri> imageUris;
        private ICollection<string> paragraphs;

        public ArticleModel()
        {
            Title = string.Empty;
            imageUris = new List<Uri>();
            paragraphs = new List<string>();
        }

        /// <summary>
        /// Title of the article
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Collection of image Uris associated with the article
        /// </summary>
        public ICollection<Uri> ImageUris
        {
            get { return this.imageUris; }
        }

        /// <summary>
        /// Collection of paragraphs composing the article content
        /// </summary>
        public ICollection<string> Paragraphs
        {
            get { return this.paragraphs; }
        }
    }
}