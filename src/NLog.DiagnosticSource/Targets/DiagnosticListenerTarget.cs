﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NLog.Layouts;

namespace NLog.Targets
{
    /// <summary>
    /// Redirects NLog LogEvents to named <see cref="DiagnosticSource"/> using <see cref="DiagnosticListener"/>
    /// </summary>
    [Target("DiagnosticListener")]
    public class DiagnosticListenerTarget : TargetWithContext
    {
        private readonly Dictionary<string, DiagnosticListener> _diagnostiSources = new Dictionary<string, DiagnosticListener>(StringComparer.Ordinal);

        /// <summary>
        /// Source Name for <see cref="DiagnosticSource"/>
        /// </summary>
        public Layout SourceName { get; set; }

        /// <summary>
        /// Event Name for <see cref="DiagnosticSource.Write(string, object)"/>
        /// </summary>
        public Layout EventName { get; set; } = "${logger}";

        /// <summary>
        /// Value for Event Payload Level-Property 
        /// </summary>
        public Layout LevelName { get; set; } = "${level}";

        /// <summary>
        /// Initializes a new instance of the <see cref="DiagnosticListenerTarget"/> class.
        /// </summary>
        public DiagnosticListenerTarget()
        {
            Layout = "${message}";
        }

        /// <inheritdoc />
        protected override void Write(LogEventInfo logEvent)
        {
            var sourceName = RenderLogEvent(SourceName, logEvent);
            if (string.IsNullOrEmpty(sourceName))
                sourceName = Name;

            var eventName = RenderLogEvent(EventName, logEvent);
            if (string.IsNullOrEmpty(eventName))
                eventName = Name;

            DiagnosticSource diagnosticSource = LookupDiagnosticSource(sourceName);
            if (diagnosticSource.IsEnabled(eventName))
            {
                var eventData = CreateEventData(logEvent);
                diagnosticSource.Write(eventName, eventData);
            }
        }

        private object CreateEventData(LogEventInfo logEvent)
        {
            var message = RenderLogEvent(Layout, logEvent);
            IDictionary<string, object> properties = null;
            if (ShouldIncludeProperties(logEvent) || ContextProperties?.Count > 0)
            {
                properties = GetAllProperties(logEvent);
            }

            var levelName = RenderLogEvent(LevelName, logEvent);

            if (logEvent.Exception != null)
            {
                if (properties != null)
                    return new { Message = message, Level = levelName, Exception = logEvent.Exception, Properties = properties };
                else
                    return new { Message = message, Level = levelName, Exception = logEvent.Exception };
            }
            else
            {
                if (properties != null)
                    return new { Message = message, Level = levelName, Properties = properties };
                else
                    return new { Message = message, Level = levelName };
            }
        }

        private DiagnosticSource LookupDiagnosticSource(string sourceName)
        {
            if (!_diagnostiSources.TryGetValue(sourceName, out var diagnosticListener))
            {
                diagnosticListener = new DiagnosticListener(sourceName);
                _diagnostiSources[sourceName] = diagnosticListener;
            }

            return diagnosticListener;
        }

        /// <inheritdoc />
        protected override void CloseTarget()
        {
            if (_diagnostiSources.Count > 0)
            {
                var subscriptions = _diagnostiSources.Values.ToList();
                _diagnostiSources.Clear();
                foreach (var subscription in subscriptions)
                    subscription.Dispose();
            }

            base.CloseTarget();
        }
    }
}
