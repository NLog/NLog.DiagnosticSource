using System.Diagnostics;

namespace NLog.Extensions.Logging
{
    /// <summary>
    /// Helpers for getting the right values from Activity no matter the format (w3c or hierarchical)
    /// </summary>
    internal static class ActivityExtensions
    {
        private static readonly string EmptySpanId = default(System.Diagnostics.ActivitySpanId).ToHexString();
        private static readonly string EmptyTraceId = default(System.Diagnostics.ActivityTraceId).ToHexString();

        public static string GetSpanId(this Activity activity)
        {
            return activity.IdFormat == ActivityIdFormat.W3C ?
                CoalesceSpanId(activity.SpanId.ToHexString()) :
                activity.Id;
        }

        public static string GetTraceId(this Activity activity)
        {
            return activity.IdFormat == ActivityIdFormat.W3C ?
                CoalesceTraceId(activity.TraceId.ToHexString()) : 
                activity.RootId;
        }

        public static string GetParentId(this Activity activity)
        {
            return activity.IdFormat == ActivityIdFormat.W3C ?
                CoalesceTraceId(activity.ParentSpanId.ToHexString()) :
                activity.ParentId;
        }

        private static string CoalesceSpanId(string spanId)
        {
            if (EmptySpanId == spanId)
                return string.Empty;
            else
                return spanId;
        }

        private static string CoalesceTraceId(string traceId)
        {
            if (EmptyTraceId == traceId)
                return string.Empty;
            else
                return traceId;
        }
    }
}