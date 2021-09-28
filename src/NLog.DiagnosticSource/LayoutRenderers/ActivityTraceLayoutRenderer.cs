﻿using System;
using System.Collections.Generic;
using System.Text;
using NLog.Config;
using NLog.Extensions.Logging;

namespace NLog.LayoutRenderers
{
    /// <summary>
    /// Layout renderer that can render properties from <see cref="System.Diagnostics.Activity.Current"/>
    /// </summary>
    [LayoutRenderer("activity")]
    [ThreadSafe]
    public sealed class ActivityTraceLayoutRenderer : LayoutRenderer
    {
        private static string[] DurationMsFormat = null;

        /// <summary>
        /// Gets or sets the property to retrieve.
        /// </summary>
        [DefaultParameter]
        public ActivityTraceProperty Property { get; set; } = ActivityTraceProperty.TraceId;

        /// <summary>
        /// Single item to extract from <see cref="System.Diagnostics.Activity.Baggage"/> or <see cref="System.Diagnostics.Activity.Tags"/> or with <see cref="System.Diagnostics.Activity.GetCustomProperty(string)"/>
        /// </summary>
        public string Item { get; set; }

        /// <summary>
        /// Control output formating of selected property (if supported)
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// Gets or sets the culture used for rendering.
        /// </summary>
        public System.Globalization.CultureInfo Culture { get; set; } = System.Globalization.CultureInfo.InvariantCulture;

        /// <summary>
        /// Retrieve the value from the parent activity
        /// </summary>
        public bool Parent { get; set; }

        /// <summary>
        /// Retrieve the value from the root activity (Containing TraceId from the active request)
        /// </summary>
        public bool Root { get; set; }

        /// <inheritdoc />
        protected override void InitializeLayoutRenderer()
        {
            if (Property == ActivityTraceProperty.DurationMs && DurationMsFormat == null)
            {
                System.Threading.Interlocked.CompareExchange(ref DurationMsFormat, System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(System.Linq.Enumerable.Range(0, 1000), i => i.ToString())), null);
            }

            base.InitializeLayoutRenderer();
        }

        /// <inheritdoc />
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            var activity = System.Diagnostics.Activity.Current;
            if (Parent)
            {
                activity = activity?.Parent;
            }

            if (Root)
            {
                var parent = activity?.Parent;
                while (parent != null)
                {
                    activity = parent;
                    parent = activity.Parent;
                }
            }

            if (activity == null)
                return;

            if (Property == ActivityTraceProperty.Baggage && string.IsNullOrEmpty(Item))
            {
                if (Format == "@")
                {
                    RenderStringDictionaryJson(activity.Baggage, builder);
                }
                else
                {
                    RenderStringDictionaryFlat(activity.Baggage, builder);
                }
            }
            else if (Property == ActivityTraceProperty.Tags && string.IsNullOrEmpty(Item))
            {
                if (Format == "@")
                {
                    RenderStringDictionaryJson(activity.TagObjects, builder);
                }
                else
                {
                    RenderStringDictionaryFlat(activity.TagObjects, builder);
                }
            }
            else if (Property == ActivityTraceProperty.Events)
            {
                if (Format == "@")
                {
                    RenderActivityEventsJson(builder, activity.Events);
                }
                else
                {
                    RenderActivityEventsFlat(builder, activity.Events);
                }
            }
            else if (Property == ActivityTraceProperty.DurationMs)
            {
                RenderDurationMs(builder, activity);
            }
            else
            {
                var value = GetValue(activity);
                builder.Append(value);
            }
        }

        private void RenderDurationMs(StringBuilder builder, System.Diagnostics.Activity activity)
        {
            var durationMs = GetDurationMs(activity);
            if (durationMs.HasValue)
            {
                if (ReferenceEquals(Culture, System.Globalization.CultureInfo.InvariantCulture) && string.IsNullOrEmpty(Format))
                {
                    var truncateMs = (long)durationMs.Value;
                    if (DurationMsFormat != null && truncateMs >= 0 && truncateMs <= DurationMsFormat.Length)
                    {
                        builder.Append(DurationMsFormat[truncateMs]);
                    }
                    else
                    {
                        builder.Append(truncateMs);
                    }
                    var preciseMs = (int)((durationMs.Value - truncateMs) * 1000.0);
                    if (preciseMs > 0)
                    {
                        builder.Append('.');
                        if (preciseMs < 100)
                            builder.Append('0');
                        if (preciseMs < 10)
                            builder.Append('0');

                        if (DurationMsFormat != null && preciseMs <= DurationMsFormat.Length)
                        {
                            builder.Append(DurationMsFormat[preciseMs]);
                        }
                        else
                        {
                            builder.Append(preciseMs);
                        }
                    }
                    else
                    {
                        builder.Append(".0");
                    }
                }
                else
                {
                    builder.Append(durationMs.Value.ToString(Format, Culture));
                }
            }
        }

        private string GetValue(System.Diagnostics.Activity activity)
        {
            switch (Property)
            {
                case ActivityTraceProperty.Id: return activity.Id;
                case ActivityTraceProperty.SpanId: return activity.GetSpanId();
                case ActivityTraceProperty.TraceId: return activity.GetTraceId();
                case ActivityTraceProperty.OperationName: return activity.OperationName;
                case ActivityTraceProperty.StartTimeUtc:return activity.StartTimeUtc > DateTime.MinValue ? activity.StartTimeUtc.ToString(Format, Culture) : string.Empty;
                case ActivityTraceProperty.Duration: return GetDuration(activity);
                case ActivityTraceProperty.ParentId: return activity.GetParentId();
                case ActivityTraceProperty.TraceState: return activity.TraceStateString;
                case ActivityTraceProperty.TraceFlags: return activity.ActivityTraceFlags == System.Diagnostics.ActivityTraceFlags.None && string.IsNullOrEmpty(Format) ? string.Empty : activity.ActivityTraceFlags.ToString(Format);
                case ActivityTraceProperty.Baggage: return GetCollectionItem(Item, activity.Baggage);
                case ActivityTraceProperty.Tags: return GetCollectionItem(Item, activity.TagObjects);
                case ActivityTraceProperty.CustomProperty: return string.IsNullOrEmpty(Item) ? string.Empty : (activity.GetCustomProperty(Item)?.ToString() ?? activity.Parent?.GetCustomProperty(Item)?.ToString() ?? string.Empty);
                case ActivityTraceProperty.SourceName: return activity.Source?.Name;
                case ActivityTraceProperty.SourceVersion: return activity.Source?.Version;
                case ActivityTraceProperty.ActivityKind: return ConvertToString(activity.Kind);
                default: return string.Empty;
            }
        }

        private string GetDuration(System.Diagnostics.Activity activity)
        {
            var duration = GetDurationTimeSpan(activity);
            if (duration.HasValue)
                return duration.Value.ToString(Format, Culture);
            else
                return string.Empty;
        }

        private double? GetDurationMs(System.Diagnostics.Activity activity)
        {
            var duration = GetDurationTimeSpan(activity);
            if (duration.HasValue)
                return duration.Value.TotalMilliseconds;
            else
                return default(double?);
        }

        private TimeSpan? GetDurationTimeSpan(System.Diagnostics.Activity activity)
        {
            var startTimeUtc = activity.StartTimeUtc;
            if (startTimeUtc > DateTime.MinValue)
            {
                var duration = activity.Duration;
                if (duration == TimeSpan.Zero)
                {
                    // Not ended yet
                    var endTimeUtc = DateTime.UtcNow;
                    duration = endTimeUtc - startTimeUtc;
                    if (duration < TimeSpan.Zero)
                        duration = TimeSpan.FromTicks(1);
                }
                
                return duration;
            }

            return default(TimeSpan?);
        }

        private static string GetCollectionItem<T>(string item, IEnumerable<KeyValuePair<string, T>> collection) where T : class
        {
            if (collection is ICollection<KeyValuePair<string, T>> emptyCollection && emptyCollection.Count == 0)
                return string.Empty; // Skip allocation of enumerator

            foreach (var keyValue in collection)
            {
                if (string.CompareOrdinal(keyValue.Key, item)==0)
                {
                    return ConvertToString(keyValue.Value);
                }
            }

            return string.Empty;    // Not found
        }

        private static void RenderStringDictionaryFlat<T>(IEnumerable<KeyValuePair<string, T>> collection, StringBuilder builder) where T : class
        {
            if (collection is ICollection<KeyValuePair<string, T>> emptyCollection && emptyCollection.Count == 0)
                return; // Skip allocation of enumerator

            var firstItem = true;
            foreach (var keyValue in collection)
            {
                if (!firstItem)
                    builder.Append(',');
                firstItem = false;
                builder.Append(keyValue.Key);

                string stringValue = ConvertToString(keyValue.Value);
                if (stringValue != null)
                {
                    builder.Append('=');
                    builder.Append(stringValue);
                }
            }
        }

        private static void RenderStringDictionaryJson<T>(IEnumerable<KeyValuePair<string, T>> collection, StringBuilder builder, string dictionaryPrefix = "{ ") where T : class
        {
            if (collection is ICollection<KeyValuePair<string, T>> emptyCollection && emptyCollection.Count == 0)
                return; // Skip allocation of enumerator

            var firstItem = true;
            foreach (var keyValue in collection)
            {
                if (string.IsNullOrWhiteSpace(keyValue.Key))
                    continue;

                if (firstItem)
                    builder.Append(dictionaryPrefix);
                else
                    builder.Append(", ");
                firstItem = false;
                builder.Append('"');
                builder.Append(EscapeStringQuotes(keyValue.Key));

                string stringValue = ConvertToString(keyValue.Value);
                if (stringValue == null)
                    builder.Append("\": null");
                else
                    builder.Append("\": \"").Append(EscapeStringQuotes(stringValue)).Append('"');
            }

            if (!firstItem)
                builder.Append(" }");
        }

        private static void RenderActivityEventsFlat(StringBuilder builder, IEnumerable<System.Diagnostics.ActivityEvent> activityEvents)
        {
            if (activityEvents is ICollection<System.Diagnostics.ActivityEvent> emptyCollection && emptyCollection.Count == 0)
                return; // Skip allocation of enumerator

            var firstItem = true;
            foreach (var item in activityEvents)
            {
                if (!firstItem)
                    builder.Append(", ");
                builder.Append(item.Name);
                firstItem = false;
            }
        }

        private static void RenderActivityEventsJson(StringBuilder builder, IEnumerable<System.Diagnostics.ActivityEvent> activityEvents)
        {
            if (activityEvents is ICollection<System.Diagnostics.ActivityEvent> emptyCollection && emptyCollection.Count == 0)
                return; // Skip allocation of enumerator

            var firstItem = true;
            foreach (var item in activityEvents)
            {
                if (string.IsNullOrWhiteSpace(item.Name))
                    continue;

                if (firstItem)
                    builder.Append("[ ");
                else
                    builder.Append(", ");

                firstItem = false;
                builder.Append("{ \"name\": \"");
                builder.Append(EscapeStringQuotes(item.Name));
                builder.Append("\", \"timestamp\": \"");
                builder.Append(item.Timestamp.ToString("yyyy-MM-dd HH:mm:ss zzz", System.Globalization.CultureInfo.InvariantCulture));
                RenderStringDictionaryJson(item.Tags, builder, ", \"tags\"={ ");
                builder.Append("\" }");
            }

            if (!firstItem)
                builder.Append(" ]");
        }

        private static string ConvertToString(System.Diagnostics.ActivityKind activityKind)
        {
            switch (activityKind)
            {
                case System.Diagnostics.ActivityKind.Internal: return string.Empty; // unassigned so not interesting
                case System.Diagnostics.ActivityKind.Server: return nameof(System.Diagnostics.ActivityKind.Server);
                case System.Diagnostics.ActivityKind.Client: return nameof(System.Diagnostics.ActivityKind.Client);
                case System.Diagnostics.ActivityKind.Producer: return nameof(System.Diagnostics.ActivityKind.Producer);
                case System.Diagnostics.ActivityKind.Consumer: return nameof(System.Diagnostics.ActivityKind.Consumer);
                default: return activityKind.ToString();
            }
        }

        private static string EscapeStringQuotes(string stringValue)
        {
            return stringValue.Replace("\"", "\\\"");
        }

        private static string ConvertToString(object objectValue)
        {
            try
            {
                if (objectValue == null)
                    return null;

                return Convert.ToString(objectValue, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}