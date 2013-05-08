// -----------------------------------------------------------------------
// <copyright file="EventQueueSection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.InteractionGallery.Utilities
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Utility class that encapsulates queuing events triggered while within the
    /// section to be sent all together when we exit the lock.  Its purpose in life
    /// is to delay calling event handlers until all related state has been changed,
    /// so that when a client receives an event related to a specific state 
    /// property, the data for all related properties is guaranteed to be
    /// consistent.
    /// </summary>
    internal class EventQueueSection : IDisposable
    {
        private readonly Queue<ExitEventHandler> eventHandlerQueue = new Queue<ExitEventHandler>();

        public delegate void ExitEventHandler();

        internal int ItemCount
        {
            get
            {
                return this.eventHandlerQueue.Count;
            }
        }

        public void Enqueue(ExitEventHandler handler)
        {
            this.eventHandlerQueue.Enqueue(handler);
        }

        public void Dispose()
        {
            while (this.eventHandlerQueue.Count > 0)
            {
                var handler = this.eventHandlerQueue.Dequeue();
                handler();
            }
        }
    }
}
