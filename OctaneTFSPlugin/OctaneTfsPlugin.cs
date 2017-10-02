﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MicroFocus.Ci.Tfs.Octane;
using Microsoft.TeamFoundation.Build.Server;
using Microsoft.TeamFoundation.Framework.Server;

namespace MicroFocus.Ci.Tfs.Core
{
    public class OctaneTfsPlugin : ISubscriber
    {
        private static string PLUGIN_DISPLAY_NAME = "OctaneTfsPlugin";

        private readonly TimeSpan _initTimeout = new TimeSpan(0,0,0,5);

        private static OctaneManager _octaneManager =null;

        private static Task _octaneInitializationThread = null;
        private static readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public OctaneTfsPlugin()
        {
            if (_octaneInitializationThread == null)
            {
                _octaneInitializationThread =
                    Task.Factory.StartNew(() => InitializeOctaneManager(_cancellationTokenSource.Token),
                        TaskCreationOptions.LongRunning);
            }

        }

        private void InitializeOctaneManager(CancellationToken token)
        {
            while (!IsOctaneInitialized())
            {
                if (token.IsCancellationRequested)
                {
                    TeamFoundationApplicationCore.Log("Octane initialization thread was requested to quit!", 1, EventLogEntryType.Information);
                    break;
                }
                try
                {
                    _octaneManager = new OctaneManager(8080);
                    _octaneManager.Init();
                    
                }
                catch (Exception ex)
                {
                    TeamFoundationApplicationCore.Log($"Error initializing octane plugin! {ex.Message}",1,EventLogEntryType.Error);
                }

                //Sleep till next retry
                Thread.Sleep(_initTimeout);

            }
        }

        private static bool IsOctaneInitialized()
        {
            return _octaneManager != null && _octaneManager.IsInitialized;
        }
        

        public Type[] SubscribedTypes()
        {
            var subscribedEventsList = new List<Type>()
            {
                typeof(BuildCompletionEvent)                
            };

            return subscribedEventsList.ToArray();
        }

        public string Name => PLUGIN_DISPLAY_NAME;
        public SubscriberPriority Priority => SubscriberPriority.Normal;

        public EventNotificationStatus ProcessEvent(IVssRequestContext requestContext, NotificationType notificationType,
            object notificationEventArgs, out int statusCode, out string statusMessage,
            out Microsoft.TeamFoundation.Common.ExceptionPropertyCollection properties)
        {
            throw new NotImplementedException();
        }
    }
}
