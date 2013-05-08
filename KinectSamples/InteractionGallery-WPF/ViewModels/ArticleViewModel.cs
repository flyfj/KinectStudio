// -----------------------------------------------------------------------
// <copyright file="ArticleViewModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.InteractionGallery.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Xaml;
    using Microsoft.Samples.Kinect.InteractionGallery.Models;
    using Microsoft.Samples.Kinect.InteractionGallery.Navigation;
    using Microsoft.Samples.Kinect.InteractionGallery.Properties;

    [ExportNavigable(NavigableContextName = DefaultNavigableContexts.ArticleScreen)]
    public class ArticleViewModel : ViewModelBase
    {
        private string title = string.Empty;

        public ArticleViewModel()
            : base()
        {
            this.Paragraphs = new ObservableCollection<string>();
            this.Images = new ObservableCollection<ImageSource>();
        }

        /// <summary>
        /// Gets the title of the article. Changes to this property 
        /// cause the PropertyChanged event to be signaled
        /// </summary>
        public string Title
        {
            get
            {
                return this.title;
            }

            protected set
            {
                this.title = value;
                this.OnPropertyChanged("Title");
            }
        }

        /// <summary>
        /// Gets the collection of paragraphs composing the content of the article.
        /// Changes to this property cause the PropertyChanged event to be signaled.
        /// </summary>
        public ObservableCollection<string> Paragraphs { get; private set; }

        /// <summary>
        /// Gets the collection of images associated with the article. 
        /// Changes to the paragraphs cause the CollectionChanged event to be signaled.
        /// </summary>
        public ObservableCollection<ImageSource> Images { get; private set; }

        /// <summary>
        /// Loads an article from the supplied Uri
        /// </summary>
        /// <param name="parameter">Uri pointing to an ArticleModel</param>
        public override void Initialize(Uri parameter)
        {
            if (null == parameter)
            {
                throw new ArgumentNullException("parameter");
            }

            using (Stream articleStream = Application.GetResourceStream(parameter).Stream)
            {
                if (null == articleStream)
                {
                    throw new InvalidDataException(string.Format(CultureInfo.InvariantCulture, Resources.InvalidArticle, parameter.AbsolutePath));
                }

                var article = XamlServices.Load(articleStream) as ArticleModel;
                if (null == article)
                {
                    throw new InvalidDataException(string.Format(CultureInfo.InvariantCulture, Resources.InvalidArticle, parameter.AbsolutePath));
                }

                this.Title = article.Title;

                this.Paragraphs.Clear();
                article.Paragraphs.ToList<string>().ForEach(this.Paragraphs.Add);

                this.Images.Clear();
                new List<ImageSource>(from imageUri in article.ImageUris
                                      select new BitmapImage(imageUri)).ForEach(this.Images.Add);
            }
        }
    }
}