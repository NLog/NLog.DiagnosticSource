using NLog.Config;
using NLog.LayoutRenderers;
using NLog.LayoutRenderers.Wrappers;
using Xunit;

namespace NLog.DiagnosticSource.Tests
{
    public class OnHasActivityLayoutRendererTests
    {
        [Fact]
        public void OnHasActivityNotActive()
        {
            // Arrange
            System.Diagnostics.Activity.Current = null;
            LayoutRenderer.Register("activity", typeof(ActivityTraceLayoutRenderer));
            LayoutRenderer.Register("onhasactivity", typeof(OnHasActivityTraceLayoutRendererWrapper));
            var logFactory = new LogFactory();
            var logConfig = new LoggingConfiguration(logFactory);
            var memTarget = new NLog.Targets.MemoryTarget("memory");
            logConfig.AddRuleForAllLevels(memTarget);
            memTarget.Layout = "${message} ${onhasactivity:inner=${activity:operationName}}";
            logFactory.Configuration = logConfig;
            var logger = logFactory.GetLogger(nameof(OnHasActivityNotActive));

            // Act
            logger.Info("Hello");

            // Assert
            Assert.Null(System.Diagnostics.Activity.Current);
            Assert.Single(memTarget.Logs);
            Assert.Equal("Hello ", memTarget.Logs[0]);
        }

        [Fact]
        public void OnHasActivityStarted()
        {
            // Arrange
            System.Diagnostics.Activity.Current = null;
            LayoutRenderer.Register("activity", typeof(ActivityTraceLayoutRenderer));
            LayoutRenderer.Register("onhasactivity", typeof(OnHasActivityTraceLayoutRendererWrapper));
            var logFactory = new LogFactory();
            var logConfig = new LoggingConfiguration(logFactory);
            var memTarget = new NLog.Targets.MemoryTarget("memory");
            logConfig.AddRuleForAllLevels(memTarget);
            memTarget.Layout = "${message} ${onhasactivity:inner=${activity:operationName}}";
            logFactory.Configuration = logConfig;
            var logger = logFactory.GetLogger(nameof(OnHasActivityNotActive));

            // Act
            var activity = new System.Diagnostics.Activity("World");
            try
            {
                activity.Start();
                logger.Info("Hello");

                // Assert
                Assert.NotNull(System.Diagnostics.Activity.Current);
                Assert.Single(memTarget.Logs);
                Assert.Equal("Hello World", memTarget.Logs[0]);
            }
            finally
            {
                activity.Stop();
            }
        }
    }
}
