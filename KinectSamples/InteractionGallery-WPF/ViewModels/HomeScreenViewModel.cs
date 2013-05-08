// -----------------------------------------------------------------------
// <copyright file="HomeScreenViewModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.InteractionGallery.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Xaml;
    using Microsoft.Kinect.Toolkit;
    using Microsoft.Samples.Kinect.InteractionGallery.Models;
    using Microsoft.Samples.Kinect.InteractionGallery.Navigation;
    using Microsoft.Samples.Kinect.InteractionGallery.Properties;
    using Microsoft.Samples.Kinect.InteractionGallery.Utilities;

    [ExportNavigable(NavigableContextName = DefaultNavigableContexts.HomeScreen)]
    public class HomeScreenViewModel : ViewModelBase
    {
        /// <summary>
        /// Path to the default model content resource
        /// </summary>
        internal const string DefaultHomeScreenModelContent = "Content/HomeScreen/HomeScreenContent.xaml";

        /// <summary>
        /// Command that is executed when an experience option is selected
        /// </summary>
        private RelayCommand<RoutedEventArgs> experienceSelected;

        /// <summary>
        /// Initializes a new instance of the HomeScreenViewModel class and loads model content from the default resource path
        /// </summary>
        public HomeScreenViewModel()
            : this(PackUriHelper.CreatePackUri(DefaultHomeScreenModelContent))
        {
        }

        /// <summary>
        /// Initializes a new instance of the HomeScreenViewModel class that loads model content from the given Uri
        /// </summary>
        /// <param name="modelContentUri">Uri to the collection of AttractScreenImage models to be loaded</param>
        public HomeScreenViewModel(Uri modelContentUri)
            : base()
        {
            this.experienceSelected = new RelayCommand<RoutedEventArgs>(this.OnExperienceSelected);

            using (Stream experienceModelsStream = Application.GetResourceStream(modelContentUri).Stream)
            {
                var experiences = XamlServices.Load(experienceModelsStream) as IList<ExperienceOptionModel>;
                if (null == experiences)
                {
                    throw new InvalidDataException();
                }

                this.Experiences = new ObservableCollection<ExperienceOptionModel>(experiences);
            }
        }

        /// <summary>
        /// Gets the experience selected command
        /// </summary>
        public ICommand ExperienceSelectedCommand
        {
            get { return this.experienceSelected; }
        }

        /// <summary>
        /// Gets the observable collection of experiences selectable from the home screen
        /// </summary>
        public ObservableCollection<ExperienceOptionModel> Experiences { get; private set; }

        /// <summary>
        /// Invoked when the ExperienceSelectedCommand is executed. Navigates to the selected experience
        /// </summary>
        private void OnExperienceSelected(RoutedEventArgs e)
        {
            ExperienceOptionModel selected = ((ContentControl)e.OriginalSource).Content as ExperienceOptionModel;
            if (null == selected)
            {
                throw new InvalidOperationException(Resources.HomeScreenInvalidExperienceSelected);
            }

            if (null != selected.NavigableContextName)
            {
                NavigationManager.NavigateTo(selected.NavigableContextName, selected.NavigableContextParameter);
            }
        }
    }
}