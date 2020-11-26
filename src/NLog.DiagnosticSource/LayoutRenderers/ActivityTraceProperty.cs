namespace NLog.LayoutRenderers
{
    /// <summary>
    /// Properties from <see cref="System.Diagnostics.Activity"/> available for <see cref="ActivityTraceLayoutRenderer"/> 
    /// </summary>
    public enum ActivityTraceProperty
    {
        /// <summary>
        /// Identifier for the current activity
        /// </summary>
        SpanId,
        /// <summary>
        /// Identifier for the parent activity
        /// </summary>
        ParentId,
        /// <summary>
        /// Identifier for the root activity
        /// </summary>
        TraceId,
        /// <summary>
        /// Operation name.
        /// </summary>
        OperationName,
        /// <summary>
        /// Time when the operation started.
        /// </summary>
        StartTimeUtc,
        /// <summary>
        /// Duration of the operation.
        /// </summary>
        Duration,
        /// <summary>
        /// Parent Operation name.
        /// </summary>
        ParentOperationName,
        /// <summary>
        /// Parent Operation start time.
        /// </summary>
        ParentStartTimeUtc,
        /// <summary>
        /// Parent Operation duration.
        /// </summary>
        ParentDuration,
        /// <summary>
        /// Collection of key/value pairs that are passed to children of this Activity.
        /// </summary>
        Baggage,
        /// <summary>
        /// Collection of key/value pairs that are NOT passed to children of this Activity
        /// </summary>
        Tags,
        /// <summary>
        /// W3C tracestate header.
        /// </summary>
        TraceState,
        /// <summary>
        /// <see cref="System.Diagnostics.ActivityTraceFlags"/> for activity (defined by the W3C ID specification) 
        /// </summary>
        TraceFlags,
        /// <summary>
        /// Collection of <see cref="System.Diagnostics.ActivityEvent"/> events attached to this activity
        /// </summary>
        Events,
        /// <summary>
        /// Custom property object value
        /// </summary>
        CustomProperty,
        /// <summary>
        /// Name of the activity source associated with this activity
        /// </summary>
        SourceName,
        /// <summary>
        /// Version of the activity source associated with this activity
        /// </summary>
        SourceVersion,
        /// <summary>
        /// Relationship kind between the activity, its parents, and its children
        /// </summary>
        ActivityKind,
    }
}