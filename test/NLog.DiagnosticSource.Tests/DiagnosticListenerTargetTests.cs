using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using NLog.Config;
using NLog.Targets;
using Xunit;

namespace NLog.DiagnosticSource.Tests
{
    public class DiagnosticListenerTargetTests
    {
        [Fact]
        public void DiagnosticSource_WriteMessage()
        {
            // Arrange
            var logFactory = new LogFactory() { ThrowExceptions = true };
            var logConfig = new LoggingConfiguration(logFactory);
            var logTarget = new DiagnosticListenerTarget() { Name = nameof(DiagnosticSource_WriteMessage) };
            logConfig.AddRuleForAllLevels(logTarget);
            logFactory.Configuration = logConfig;
            var logger = logFactory.GetLogger("MyLogger");

            using (var observer = new Observer(nameof(DiagnosticSource_WriteMessage)))
            using (var subscription = DiagnosticListener.AllListeners.Subscribe(observer))
            {
                // Act
                logger.Info("Hello World");

                // Assert
                Assert.Equal(logger.Name, observer.LastEvent.Key);
                Assert.Equal("Hello World", observer.GetLastEventProperty("Message"));
                Assert.Null(observer.GetLastEventProperty("Exception"));
            }
        }

        [Fact]
        public void DiagnosticSource_WriteException()
        {
            // Arrange
            var logFactory = new LogFactory() { ThrowExceptions = true };
            var logConfig = new LoggingConfiguration(logFactory);
            var logTarget = new DiagnosticListenerTarget() { Name = nameof(DiagnosticSource_WriteException) };
            logConfig.AddRuleForAllLevels(logTarget);
            logFactory.Configuration = logConfig;
            var logger = logFactory.GetLogger("MyLogger");

            using (var observer = new Observer(nameof(DiagnosticSource_WriteException)))
            using (var subscription = DiagnosticListener.AllListeners.Subscribe(observer))
            {
                // Act
                logger.Error(new Exception("Boom!"), "Explosion");

                // Assert
                Assert.Equal(logger.Name, observer.LastEvent.Key);
                Assert.Equal("Explosion", observer.GetLastEventProperty("Message"));
                Assert.Equal("System.Exception: Boom!", observer.GetLastEventProperty("Exception")?.ToString());
            }
        }

        [Fact]
        public void DiagnosticSource_Shutdown()
        {
            // Arrange
            var logFactory = new LogFactory() { ThrowExceptions = true };
            var logConfig = new LoggingConfiguration(logFactory);
            var logTarget = new DiagnosticListenerTarget() { Name = nameof(DiagnosticSource_Shutdown) };
            logConfig.AddRuleForAllLevels(logTarget);
            logFactory.Configuration = logConfig;
            var logger = logFactory.GetLogger("MyLogger");

            using (var observer = new Observer(nameof(DiagnosticSource_Shutdown)))
            using (var subscription = DiagnosticListener.AllListeners.Subscribe(observer))
            {
                // Act
                logger.Info("Hello World");

                logFactory.Dispose();

                // Assert
            }
        }

        private sealed class Observer : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object>>, IDisposable
        {
            private readonly List<IDisposable> _subscriptions = new List<IDisposable>();

            public string SourceName { get; }

            public KeyValuePair<string, object> LastEvent { get; private set; }

            public object GetLastEventProperty(string name)
            {
                return LastEvent.Value.GetType().GetTypeInfo().GetDeclaredProperty(name)?.GetValue(LastEvent.Value);
            }

            public Observer(string sourceName)
            {
                SourceName = sourceName;
            }

            public void OnCompleted()
            {
                // Nothing to do
            }

            public void OnError(Exception error)
            {
                // Nothing to do
            }

            public void OnNext(DiagnosticListener value)
            {
                if (value.Name == SourceName)
                {
                    var subscription = value.Subscribe(this);
                    _subscriptions.Add(subscription);
                }
            }

            public void OnNext(KeyValuePair<string, object> value)
            {
                LastEvent = value;
            }

            public void Dispose()
            {
                foreach (var subscription in _subscriptions)
                    subscription.Dispose();
                _subscriptions.Clear();
            }
        }
    }
}
