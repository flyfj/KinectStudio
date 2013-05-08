// -----------------------------------------------------------------------
// <copyright file="CommandOnEventAssociation.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.InteractionGallery.Utilities
{
    using System;
    using System.Windows;
    using System.Windows.Input;

    public class CommandOnEventAssociation : Freezable
    {
        /// <summary>
        /// Dependency property storing the current command to execute when the associated event is signaled.
        /// </summary>
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            "Command",
            typeof(ICommand),
            typeof(CommandOnEventAssociation),
            new PropertyMetadata(null));

        /// <summary>
        /// Dependency property storing the name of the event to hook.
        /// </summary>
        public static readonly DependencyProperty EventProperty = DependencyProperty.Register(
            "Event",
            typeof(string),
            typeof(CommandOnEventAssociation),
            new PropertyMetadata(string.Empty));

        public ICommand Command
        {
            get
            {
                return (ICommand)this.GetValue(CommandProperty);
            }

            set
            {
                this.SetValue(CommandProperty, value);
            }
        }

        public string Event
        {
            get
            {
                return (string)this.GetValue(EventProperty);
            }

            set
            {
                this.SetValue(EventProperty, value);
            }
        }

        internal Delegate Delegate { get; set; }

        protected override Freezable CreateInstanceCore()
        {
            throw new NotImplementedException();
        }
    }
}
