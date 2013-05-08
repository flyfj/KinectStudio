// -----------------------------------------------------------------------
// <copyright file="CommandOnEventHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.InteractionGallery.Utilities
{
    using System;
    using System.Globalization;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Input;
    using Microsoft.Samples.Kinect.InteractionGallery.Properties;

    /// <summary>
    /// Helper that augments the behavior of an entity to execute commands when an event is signaled.
    /// Supports events of type EventHandler and EventHandler<T> where the event arguments derive from System.EventArgs
    /// </summary>
    public class CommandOnEventHelper : DependencyObject
    {
        /// <summary>
        /// Attached dependency property storing an association between an event and a command.
        /// </summary>
        public static readonly DependencyProperty AssociationEntryProperty = DependencyProperty.RegisterAttached(
            "AssociationEntry",
            typeof(CommandOnEventAssociation),
            typeof(CommandOnEventHelper),
            new PropertyMetadata(AssociationEntryPropertyChangedCallback));

        private static readonly MethodInfo HandlerMethodInfo = typeof(CommandOnEventHelper).GetMethod("OnEventHandler", BindingFlags.NonPublic | BindingFlags.Static);

        public static CommandOnEventAssociation GetAssociationEntry(DependencyObject obj)
        {
            if (null == obj)
            {
                throw new ArgumentNullException("obj", "Unable to get mapping entry from null dependency object.");
            }

            return (CommandOnEventAssociation)obj.GetValue(AssociationEntryProperty);
        }

        public static void SetAssociationEntry(DependencyObject obj, CommandOnEventAssociation value)
        {
            if (null == obj)
            {
                throw new ArgumentNullException("obj", "Unable to set mapping entry on null dependency object.");
            }

            obj.SetValue(AssociationEntryProperty, value);
        }

        /// <summary>
        /// Adds a handler calling the supplied command when the supplied event is signaled
        /// </summary>
        public static void AssociationEntryPropertyChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (null == sender)
            {
                throw new ArgumentNullException("sender", "Unable to configure event-to-command mapping with a null dependency object");
            }

            if (null != e.OldValue)
            {
                var entry = e.OldValue as CommandOnEventAssociation;
                var eventInfo = GetEventInfo(sender, entry.Event);
                eventInfo.RemoveEventHandler(sender, entry.Delegate);
                entry.Delegate = null;
            }

            if (null != e.NewValue)
            {
                var entry = e.NewValue as CommandOnEventAssociation;
                var eventInfo = GetEventInfo(sender, entry.Event);
                Delegate handlerDelegate = Delegate.CreateDelegate(eventInfo.EventHandlerType, null, HandlerMethodInfo);
                entry.Delegate = handlerDelegate;
                eventInfo.AddEventHandler(sender, handlerDelegate);
            }
        }

        /// <summary>
        /// Handler that checks whether the associated command can currently execute and calls it if so.
        /// </summary>
        private static void OnEventHandler(object sender, EventArgs e)
        {
            DependencyObject obj = sender as DependencyObject;
            var entry = obj.GetValue(AssociationEntryProperty) as CommandOnEventAssociation;
            if (entry.Command.CanExecute(e))
            {
                entry.Command.Execute(e);
            }
        }

        /// <summary>
        /// Gets the reflection information for the specified sender object and event name.
        /// </summary>
        /// <param name="sender">
        /// Object that contains event.
        /// </param>
        /// <param name="eventName">
        /// Name of event to return.
        /// </param>
        /// <returns>
        /// Reflection information for specified event.
        /// </returns>
        private static EventInfo GetEventInfo(object sender, string eventName)
        {
            EventInfo eventInfo = sender.GetType().GetEvent(eventName);
            if (null == eventInfo)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resources.CommandOnEventException, sender.GetType().FullName, eventName));
            }

            return eventInfo;
        }
    }
}