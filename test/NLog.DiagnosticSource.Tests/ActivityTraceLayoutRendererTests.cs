﻿using System;
using NLog.LayoutRenderers;
using Xunit;

namespace NLog.DiagnosticSource.Tests
{
    public class ActivityTraceLayoutRendererTests
    {
        [Fact]
        public void TestAllPropertiesWhenActivityNull()
        {
            bool orgThrowExceptions = LogManager.ThrowExceptions;

            try
            {
                LogManager.ThrowExceptions = true;
                System.Diagnostics.Activity.Current = null;

                var logEvent = LogEventInfo.CreateNullEvent();

                foreach (ActivityTraceProperty property in Enum.GetValues(typeof(ActivityTraceProperty)))
                {
                    ActivityTraceLayoutRenderer layoutRenderer = new ActivityTraceLayoutRenderer();
                    layoutRenderer.Property = property;
                    var result = layoutRenderer.Render(logEvent);
                    Assert.True(string.IsNullOrEmpty(result));
                }
            }
            finally
            {
                LogManager.ThrowExceptions = orgThrowExceptions;
            }
        }

        [Theory]
        [InlineData(ActivityTraceProperty.Id, false)]               // Id will never be empty
        [InlineData(ActivityTraceProperty.SpanId, false)]           // SpanId will never be empty
        [InlineData(ActivityTraceProperty.TraceId, false)]          // Will fallback to SpanId
        [InlineData(ActivityTraceProperty.OperationName, true)]
        [InlineData(ActivityTraceProperty.StartTimeUtc, true)]
        [InlineData(ActivityTraceProperty.Duration, true)]
        [InlineData(ActivityTraceProperty.DurationMs, true)]
        [InlineData(ActivityTraceProperty.Baggage, true)]
        [InlineData(ActivityTraceProperty.Tags, true)]
        [InlineData(ActivityTraceProperty.ParentId, true)]
        [InlineData(ActivityTraceProperty.TraceState, true)]
        [InlineData(ActivityTraceProperty.TraceFlags, true)]
        [InlineData(ActivityTraceProperty.Events, true)]
        [InlineData(ActivityTraceProperty.CustomProperty, true)]
        [InlineData(ActivityTraceProperty.SourceName, true)]
        [InlineData(ActivityTraceProperty.SourceVersion, true)]
        [InlineData(ActivityTraceProperty.ActivityKind, true)]
        public void TestAllPropertiesWhenActivityEmpty(ActivityTraceProperty property, bool empty)
        {
            bool orgThrowExceptions = LogManager.ThrowExceptions;

            try
            {
                LogManager.ThrowExceptions = true;

                using (var newActivity = new System.Diagnostics.Activity(null).Start())
                {
                    System.Diagnostics.Activity.Current.SetStartTime(new DateTime(0, DateTimeKind.Utc));

                    var logEvent = LogEventInfo.CreateNullEvent();

                    ActivityTraceLayoutRenderer layoutRenderer = new ActivityTraceLayoutRenderer();
                    layoutRenderer.Property = property;
                    var result = layoutRenderer.Render(logEvent);
                    Assert.True(string.IsNullOrEmpty(result) == empty);
                }
            }
            finally
            {
                LogManager.ThrowExceptions = orgThrowExceptions;
            }
        }

        [Theory]
        [InlineData(ActivityTraceProperty.Id, null, null)]            // Id will never be empty
        [InlineData(ActivityTraceProperty.SpanId, null, null)]        // SpanId will never be empty
        [InlineData(ActivityTraceProperty.TraceId, null, null)]       // Will fallback to SpanId
        [InlineData(ActivityTraceProperty.OperationName, null, "MyOperation")]
        [InlineData(ActivityTraceProperty.StartTimeUtc, "fff", "123")]
        [InlineData(ActivityTraceProperty.Duration, "hh", "01")]
        [InlineData(ActivityTraceProperty.Baggage, null, "")]
        [InlineData(ActivityTraceProperty.Tags, null, "")]
        [InlineData(ActivityTraceProperty.ParentId, null, "")]
        [InlineData(ActivityTraceProperty.TraceState, null, "")]
        [InlineData(ActivityTraceProperty.TraceFlags, null, "")]
        [InlineData(ActivityTraceProperty.Events, null, "")]
        [InlineData(ActivityTraceProperty.CustomProperty, null, "")]
        [InlineData(ActivityTraceProperty.SourceName, null, "")]
        [InlineData(ActivityTraceProperty.SourceVersion, null, "")]
        [InlineData(ActivityTraceProperty.ActivityKind, null, "")]
        public void TestAllPropertiesWhenActivityRunning(ActivityTraceProperty property, string format, string output)
        {
            bool orgThrowExceptions = LogManager.ThrowExceptions;

            try
            {
                DateTime startedTime = DateTime.UtcNow.Date.AddHours(DateTime.UtcNow.Hour - 1).AddMilliseconds(123);

                LogManager.ThrowExceptions = true;
                using (var newActivity = new System.Diagnostics.Activity("MyOperation").Start())
                {
                    System.Diagnostics.Activity.Current.SetStartTime(startedTime);

                    var logEvent = LogEventInfo.CreateNullEvent();

                    ActivityTraceLayoutRenderer layoutRenderer = new ActivityTraceLayoutRenderer();
                    layoutRenderer.Property = property;
                    layoutRenderer.Format = format;
                    var result = layoutRenderer.Render(logEvent);
                    if (output != null)
                        Assert.Equal(output, result);
                }
            }
            finally
            {
                LogManager.ThrowExceptions = orgThrowExceptions;
            }
        }

        [Theory]
        [InlineData(@"${activity:Duration:format=hh\\\:mm\\\:ss\\.fff}", "01:02:03.004")]
        [InlineData(@"${activity:DurationMs}", "3723004.056")]
        public void TestDurationSingleItem(string inputLayout, string expectedResult)
        {
            var logFactory = new LogFactory();
            var xmlStream = new System.IO.StringReader($@"<nlog throwConfigExceptions='true'>
                <targets>
                    <target name='Memory' type='Memory' layout='{inputLayout}' />
                </targets>
                <rules>
                    <logger name='*' writeTo='Memory' />
                </rules></nlog>");
            var xmlReader = System.Xml.XmlReader.Create(xmlStream);
            var logConfig = new NLog.Config.XmlLoggingConfiguration(xmlReader, null);
            logFactory.Configuration = logConfig;
            var memTarget = logFactory.Configuration.FindTargetByName<NLog.Targets.MemoryTarget>("Memory");
            
            var logger = logFactory.GetCurrentClassLogger();
            using (var newActivity = new System.Diagnostics.Activity("MyOperation").Start())
            {
                var dateTime = DateTime.UtcNow.Date;
                newActivity.SetStartTime(dateTime);
                newActivity.SetEndTime(dateTime.AddHours(1).AddMinutes(2).AddSeconds(3).AddTicks(40567));
                logger.Info("Hello");
                Assert.Equal(expectedResult, System.Linq.Enumerable.FirstOrDefault(memTarget.Logs));
            }
        }

        [Fact]
        public void TestBaggageSingleItem()
        {
            // Arrange
            var logEvent = LogEventInfo.CreateNullEvent();
            ActivityTraceLayoutRenderer layoutRenderer = new ActivityTraceLayoutRenderer();
            layoutRenderer.Property = ActivityTraceProperty.Baggage;

            // Act
            using (var newActivity = new System.Diagnostics.Activity("MyOperation").Start().AddBaggage("myitem1", "myvalue1"))
            {
                var result = layoutRenderer.Render(logEvent);

                // Assert
                Assert.Equal("myitem1=myvalue1", result);
            }
        }

        [Fact]
        public void TestBaggageSingleItemJson()
        {
            // Arrange
            var logEvent = LogEventInfo.CreateNullEvent();
            ActivityTraceLayoutRenderer layoutRenderer = new ActivityTraceLayoutRenderer();
            layoutRenderer.Property = ActivityTraceProperty.Baggage;
            layoutRenderer.Format = "@";

            // Act
            using (var newActivity = new System.Diagnostics.Activity("MyOperation").Start().AddBaggage("myitem1", "myvalue1"))
            {
                var result = layoutRenderer.Render(logEvent);

                // Assert
                Assert.Equal("{ \"myitem1\": \"myvalue1\" }", result);
            }
        }

        [Fact]
        public void TestBaggageDoubleItem()
        {
            // Arrange
            var logEvent = LogEventInfo.CreateNullEvent();
            ActivityTraceLayoutRenderer layoutRenderer = new ActivityTraceLayoutRenderer();
            layoutRenderer.Property = ActivityTraceProperty.Baggage;

            // Act
            using (var newActivity = new System.Diagnostics.Activity("MyOperation").Start().AddBaggage("myitem1", "myvalue1").AddBaggage("myitem2", "myvalue2"))
            {
                var result = layoutRenderer.Render(logEvent);

                // Assert
                Assert.Contains("myitem1=myvalue1", result);
                Assert.Contains("myitem2=myvalue2", result);
            }
        }

        [Fact]
        public void TestBaggageDoubleItemJson()
        {
            // Arrange
            var logEvent = LogEventInfo.CreateNullEvent();
            ActivityTraceLayoutRenderer layoutRenderer = new ActivityTraceLayoutRenderer();
            layoutRenderer.Property = ActivityTraceProperty.Baggage;
            layoutRenderer.Format = "@";

            // Act
            using (var newActivity = new System.Diagnostics.Activity("MyOperation").Start().AddBaggage("myitem1", "myvalue1").AddBaggage("myitem2", "myvalue2"))
            {
                var result = layoutRenderer.Render(logEvent);

                var jsonElement = (System.Text.Json.JsonElement)System.Text.Json.JsonSerializer.Deserialize(result, typeof(object));
                Assert.Equal("myvalue1", jsonElement.GetProperty("myitem1").GetString());
                Assert.Equal("myvalue2", jsonElement.GetProperty("myitem2").GetString());
            }
        }

        [Fact]
        public void TestTagsSingleItem()
        {
            // Arrange
            var logEvent = LogEventInfo.CreateNullEvent();
            ActivityTraceLayoutRenderer layoutRenderer = new ActivityTraceLayoutRenderer();
            layoutRenderer.Property = ActivityTraceProperty.Tags;

            // Act
            using (var newActivity = new System.Diagnostics.Activity("MyOperation").Start().AddTag("myitem1", "myvalue1"))
            {
                var result = layoutRenderer.Render(logEvent);

                // Assert
                Assert.Equal("myitem1=myvalue1", result);
            }
        }

        [Fact]
        public void TestTagsDoubleItem()
        {
            // Arrange
            var logEvent = LogEventInfo.CreateNullEvent();
            ActivityTraceLayoutRenderer layoutRenderer = new ActivityTraceLayoutRenderer();
            layoutRenderer.Property = ActivityTraceProperty.Tags;

            // Act
            using (var newActivity = new System.Diagnostics.Activity("MyOperation").Start().AddTag("myitem1", "myvalue1").AddTag("myitem2", "myvalue2"))
            {
                var result = layoutRenderer.Render(logEvent);

                // Assert
                Assert.Contains("myitem1=myvalue1", result);
                Assert.Contains("myitem2=myvalue2", result);
            }
        }

        [Fact]
        public void TestTagsDoubleItemJson()
        {
            // Arrange
            var logEvent = LogEventInfo.CreateNullEvent();
            ActivityTraceLayoutRenderer layoutRenderer = new ActivityTraceLayoutRenderer();
            layoutRenderer.Property = ActivityTraceProperty.Tags;
            layoutRenderer.Format = "@";

            // Act
            using (var newActivity = new System.Diagnostics.Activity("MyOperation").Start().AddTag("myitem1", "myvalue1").AddTag("myitem2", "myvalue2"))
            {
                var result = layoutRenderer.Render(logEvent);

                var jsonElement = (System.Text.Json.JsonElement)System.Text.Json.JsonSerializer.Deserialize(result, typeof(object));
                Assert.Equal("myvalue1", jsonElement.GetProperty("myitem1").GetString());
                Assert.Equal("myvalue2", jsonElement.GetProperty("myitem2").GetString());
            }
        }

        [Fact]
        public void TestEventsSingleItem()
        {
            // Arrange
            var logEvent = LogEventInfo.CreateNullEvent();
            ActivityTraceLayoutRenderer layoutRenderer = new ActivityTraceLayoutRenderer();
            layoutRenderer.Property = ActivityTraceProperty.Events;

            // Act
            using (var newActivity = new System.Diagnostics.Activity("MyOperation").Start().AddEvent(new System.Diagnostics.ActivityEvent("myevent1")))
            {
                var result = layoutRenderer.Render(logEvent);

                // Assert
                Assert.Equal("myevent1", result);
            }
        }

        [Fact]
        public void TestEventsDoubleItem()
        {
            // Arrange
            var logEvent = LogEventInfo.CreateNullEvent();
            ActivityTraceLayoutRenderer layoutRenderer = new ActivityTraceLayoutRenderer();
            layoutRenderer.Property = ActivityTraceProperty.Events;

            // Act
            using (var newActivity = new System.Diagnostics.Activity("MyOperation").Start().AddEvent(new System.Diagnostics.ActivityEvent("myevent1")).AddEvent(new System.Diagnostics.ActivityEvent("myevent2")))
            {
                var result = layoutRenderer.Render(logEvent);

                // Assert
                Assert.Contains("myevent1, myevent2", result);
            }
        }

        [Fact]
        public void TestEventsDoubleItemJson()
        {
            // Arrange
            var logEvent = LogEventInfo.CreateNullEvent();
            ActivityTraceLayoutRenderer layoutRenderer = new ActivityTraceLayoutRenderer();
            layoutRenderer.Property = ActivityTraceProperty.Events;
            layoutRenderer.Format = "@";

            // Act
            using (var newActivity = new System.Diagnostics.Activity("MyOperation").Start().AddEvent(new System.Diagnostics.ActivityEvent("myevent1")).AddEvent(new System.Diagnostics.ActivityEvent("myevent2")))
            {
                var result = layoutRenderer.Render(logEvent);

                var jsonElement = (System.Text.Json.JsonElement[])System.Text.Json.JsonSerializer.Deserialize(result, typeof(System.Text.Json.JsonElement[]));
                Assert.Equal(2, jsonElement.Length);
                Assert.Equal("myevent1", jsonElement[0].GetProperty("name").GetString());
                Assert.Equal("myevent2", jsonElement[1].GetProperty("name").GetString());
            }
        }
    }
}