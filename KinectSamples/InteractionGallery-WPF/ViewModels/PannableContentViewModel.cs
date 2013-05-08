// -----------------------------------------------------------------------
// <copyright file="PannableContentViewModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.InteractionGallery.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Xaml;

    using Microsoft.Samples.Kinect.InteractionGallery.Models;
    using Microsoft.Samples.Kinect.InteractionGallery.Navigation;
    using Microsoft.Samples.Kinect.InteractionGallery.Properties;

    [ExportNavigable(NavigableContextName = DefaultNavigableContexts.PannableContent)]
    public class PannableContentViewModel : ViewModelBase
    {
        /// <summary>
        /// The content that will be placed in the KinectScrollViewer
        /// </summary>
        public ImageSource PannableImage { get; private set; }

        /// <summary>
        /// Loads content to be placed in the KinectScrollViewer
        /// </summary>
        /// <param name="parameter">Uri pointing to a scrolling grid model resource</param>
        public override void Initialize(Uri parameter)
        {
            if (null == parameter)
            {
                throw new ArgumentNullException("parameter");
            }

            using (Stream contentStream = Application.GetResourceStream(parameter).Stream)
            {
                if (null == contentStream)
                {
                    throw new InvalidDataException(string.Format(CultureInfo.InvariantCulture, Resources.InvalidPanningContent, parameter.AbsolutePath));
                }

                var pannableItem = XamlServices.Load(contentStream) as PannableContentModel;
                if (null == pannableItem)
                {
                    throw new InvalidDataException(string.Format(CultureInfo.InvariantCulture, Resources.InvalidPanningContent, parameter.AbsolutePath));
                }

                this.PannableImage = new BitmapImage(pannableItem.ImageUri);
            }
        }
    }
}