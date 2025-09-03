namespace NLog.LayoutRenderers.Wrappers
{
    using System.Text;

    /// <summary>
    /// Only outputs the inner layout when <see cref="System.Diagnostics.Activity.Current"/> is active
    /// </summary>
    /// <example>
    /// ${onhasactivity:, ActivityTraceId\: ${activity:traceid}}
    /// </example>
    [LayoutRenderer("onhasactivity")]
    public sealed class OnHasActivityTraceLayoutRendererWrapper : WrapperLayoutRendererBase
    {
        /// <inheritdoc/>
        protected override void RenderInnerAndTransform(LogEventInfo logEvent, StringBuilder builder, int orgLength)
        {
            var currentActivity = System.Diagnostics.Activity.Current;
            if (currentActivity != null)
            {
                builder.Append(Inner?.Render(logEvent));
            }
        }

        /// <inheritdoc />
        protected override string Transform(string text)
        {
            return text;
        }
    }
}
