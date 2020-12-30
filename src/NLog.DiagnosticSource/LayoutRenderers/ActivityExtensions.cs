using System.Diagnostics;

namespace NLog.Extensions.Logging
{
    /// <summary>
    /// Helpers for getting the right values from Activity no matter the format (w3c or hierarchical)
    /// </summary>
    internal static class ActivityExtensions
    {
        private static readonly System.Diagnostics.ActivitySpanId EmptySpanId = default(System.Diagnostics.ActivitySpanId);
        private static readonly System.Diagnostics.ActivityTraceId EmptyTraceId = default(System.Diagnostics.ActivityTraceId);

        public static string GetSpanId(this Activity activity)
        {
            return activity.IdFormat == ActivityIdFormat.W3C ?
                SpanIdToHexString(activity.SpanId) :
                activity.Id;
        }

        public static string GetTraceId(this Activity activity)
        {
            return activity.IdFormat == ActivityIdFormat.W3C ?
                TraceIdToHexString(activity.TraceId) : 
                activity.RootId;
        }

        public static string GetParentId(this Activity activity)
        {
            return activity.IdFormat == ActivityIdFormat.W3C ?
                SpanIdToHexString(activity.ParentSpanId) :
                activity.ParentId;
        }

        private static string SpanIdToHexString(ActivitySpanId spanId)
        {
            if (EmptySpanId.Equals(spanId))
                return string.Empty;
            else
                return spanId.ToHexString();
        }

        private static string TraceIdToHexString(ActivityTraceId traceId)
        {
            if (EmptyTraceId.Equals(traceId))
                return string.Empty;
            else
                return traceId.ToHexString();
        }
    }
}