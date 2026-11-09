using JobSearchBuilder.Interfaces;
using JobSearchBuilder.Models;
using JobSearchBuilder.Services;
using NUnit.Framework;
using System;

namespace JobSearchBuilder.Tests
{
    [TestFixture]
    public class AppLogTests
    {
        private class ThrowingLogger : IAppLogger
        {
            public void Log(LogLevel level, string component, string operation, string message, Exception exception)
            {
                throw new InvalidOperationException("logger failed");
            }
        }

        [TearDown]
        public void TearDown()
        {
            AppLog.Configure(null);
        }

        [Test]
        public void Error_ConfiguredLogger_ForwardsEvent()
        {
            RecordingAppLogger logger = new RecordingAppLogger();
            AppLog.Configure(logger);
            Exception exception = new InvalidOperationException("boom");

            AppLog.Error("AI", "ReviewQuery", "Failed.", exception);

            Assert.That(logger.Entries, Has.Count.EqualTo(1));
            Assert.That(logger.Entries[0].Level, Is.EqualTo(LogLevel.Error));
            Assert.That(logger.Entries[0].Operation, Is.EqualTo("ReviewQuery"));
            Assert.That(logger.Entries[0].Exception, Is.SameAs(exception));
        }

        [Test]
        public void Info_NotConfigured_DoesNotThrow()
        {
            AppLog.Configure(null);

            Assert.DoesNotThrow(() => AppLog.Info("AI", "Send", "message"));
        }

        [Test]
        public void Info_LoggerThrows_DoesNotPropagate()
        {
            AppLog.Configure(new ThrowingLogger());

            Assert.DoesNotThrow(() => AppLog.Info("AI", "Send", "message"));
        }
    }
}
