using JobSearchBuilder.Models;
using JobSearchBuilder.Services;
using NUnit.Framework;
using System;
using System.IO;

namespace JobSearchBuilder.Tests
{
    [TestFixture]
    public class LoggingSettingsTests
    {
        [Test]
        public void ParseLoggingSettings_ValidSection_ReadsValues()
        {
            LoggingSettings settings = AppSettingsLoader.ParseLoggingSettings(
                "{ \"Logging\": { \"MinimumLevel\": \"error\", \"Directory\": \"C:\\\\Logs\", \"MaxFileSizeKb\": 50, \"RetainedFileCount\": 0 } }");

            Assert.That(settings.MinimumLevel, Is.EqualTo(LogLevel.Error));
            Assert.That(settings.Directory, Is.EqualTo("C:\\Logs"));
            Assert.That(settings.MaxFileSizeKb, Is.EqualTo(50));
            Assert.That(settings.RetainedFileCount, Is.EqualTo(0));
        }

        [TestCase("not json")]
        [TestCase("")]
        [TestCase(null)]
        [TestCase("{ }")]
        [TestCase("{ \"Logging\": \"nope\" }")]
        public void ParseLoggingSettings_MissingOrMalformed_UsesDefaults(string json)
        {
            LoggingSettings settings = AppSettingsLoader.ParseLoggingSettings(json);

            Assert.That(settings.MinimumLevel, Is.EqualTo(LogLevel.Information));
            Assert.That(settings.MaxFileSizeKb, Is.EqualTo(1024));
            Assert.That(settings.RetainedFileCount, Is.EqualTo(5));
        }

        [TestCase("{ \"Logging\": { \"MinimumLevel\": \"Loud\", \"MaxFileSizeKb\": 0, \"RetainedFileCount\": -1 } }")]
        [TestCase("{ \"Logging\": { \"MinimumLevel\": \"99\", \"MaxFileSizeKb\": \"big\", \"RetainedFileCount\": 1.5 } }")]
        public void ParseLoggingSettings_InvalidValues_UsesDefaults(string json)
        {
            LoggingSettings settings = AppSettingsLoader.ParseLoggingSettings(json);

            Assert.That(settings.MinimumLevel, Is.EqualTo(LogLevel.Information));
            Assert.That(settings.MaxFileSizeKb, Is.EqualTo(1024));
            Assert.That(settings.RetainedFileCount, Is.EqualTo(5));
        }

        [Test]
        public void ResolveDirectory_Empty_UsesLocalAppData()
        {
            string expected = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JobSearchBuilder", "logs");

            Assert.That(new LoggingSettings().ResolveDirectory(), Is.EqualTo(expected));
        }
    }
}
