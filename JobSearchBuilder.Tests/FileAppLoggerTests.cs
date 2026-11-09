using JobSearchBuilder.Models;
using JobSearchBuilder.Services;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

namespace JobSearchBuilder.Tests
{
    [TestFixture]
    public class FileAppLoggerTests
    {
        private string _directory;
        private static readonly DateTime Today = new DateTime(2026, 2, 12, 10, 30, 0);
        private string LogPath => Path.Combine(_directory, "applog_12_02_26.log");

        private string[] ReadEntries()
        {
            return File.ReadAllLines(LogPath).Where(line => !line.StartsWith("___")).ToArray();
        }

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "JobSearchBuilderLogTests", Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, true);
        }

        [Test]
        public void Log_Entry_WritesStructuredJsonLine()
        {
            FileAppLogger logger = new FileAppLogger(_directory, LogLevel.Information, 1024 * 1024, 2, () => Today);

            logger.Log(LogLevel.Warning, "AI", "ReviewQuery", "Started.", null);

            JObject entry = JObject.Parse(ReadEntries().Single());
            Assert.That((string)entry["level"], Is.EqualTo("Warning"));
            Assert.That((string)entry["component"], Is.EqualTo("AI"));
            Assert.That((string)entry["operation"], Is.EqualTo("ReviewQuery"));
            Assert.That((string)entry["message"], Is.EqualTo("Started."));
            Assert.That(entry["timestamp"], Is.Not.Null);
            Assert.That(entry["exception"], Is.Null);
        }

        [Test]
        public void Log_TwoEntries_FollowsEachWithSeparatorLine()
        {
            FileAppLogger logger = new FileAppLogger(_directory, LogLevel.Information, 1024 * 1024, 2, () => Today);

            logger.Log(LogLevel.Information, "AI", "Send", "first", null);
            logger.Log(LogLevel.Information, "AI", "Send", "second", null);

            string[] lines = File.ReadAllLines(LogPath);
            string separator = new string('_', 35);
            Assert.That(lines, Has.Length.EqualTo(4));
            Assert.That(lines[0], Does.Contain("first"));
            Assert.That(lines[1], Is.EqualTo(separator));
            Assert.That(lines[2], Does.Contain("second"));
            Assert.That(lines[3], Is.EqualTo(separator));
        }

        [Test]
        public void Log_BelowMinimumLevel_WritesNothing()
        {
            FileAppLogger logger = new FileAppLogger(_directory, LogLevel.Warning, 1024 * 1024, 2, () => Today);

            logger.Log(LogLevel.Information, "AI", "ReviewQuery", "Started.", null);

            Assert.That(File.Exists(LogPath), Is.False);
        }

        [Test]
        public void Log_Exception_RecordsTypeAndStackInformation()
        {
            FileAppLogger logger = new FileAppLogger(_directory, LogLevel.Information, 1024 * 1024, 2, () => Today);
            Exception captured = null;
            try
            {
                throw new InvalidOperationException("boom");
            }
            catch (Exception ex)
            {
                captured = ex;
            }

            logger.Log(LogLevel.Error, "ProfileStore", "Save", "Failed.", captured);

            JObject entry = JObject.Parse(ReadEntries().Single());
            Assert.That((string)entry["exceptionType"], Is.EqualTo("System.InvalidOperationException"));
            Assert.That((string)entry["exception"], Does.Contain("boom"));
            Assert.That((string)entry["exception"], Does.Contain(nameof(Log_Exception_RecordsTypeAndStackInformation)));
        }

        [Test]
        public void Log_SensitiveMessageAndException_AreRedactedOnDisk()
        {
            FileAppLogger logger = new FileAppLogger(_directory, LogLevel.Information, 1024 * 1024, 2, () => Today);

            logger.Log(LogLevel.Error, "AI", "Send", "key sk-ant-api03-abcdefghijklmnop1234",
                new InvalidOperationException("Server=secrethost;Password=hunter2;"));

            string content = File.ReadAllText(LogPath);
            Assert.That(content, Does.Not.Contain("sk-ant-api03"));
            Assert.That(content, Does.Not.Contain("secrethost"));
            Assert.That(content, Does.Not.Contain("hunter2"));
        }

        [Test]
        public void Log_MultilineMessage_StaysOnOneLine()
        {
            FileAppLogger logger = new FileAppLogger(_directory, LogLevel.Information, 1024 * 1024, 2, () => Today);

            logger.Log(LogLevel.Information, "AI", "Send", "first\r\nsecond", null);

            Assert.That(ReadEntries(), Has.Length.EqualTo(1));
        }

        [Test]
        public void Log_FileName_UsesLocalDate()
        {
            FileAppLogger logger = new FileAppLogger(_directory, LogLevel.Information, 1024 * 1024, 2, () => Today);

            logger.Log(LogLevel.Information, "AI", "Send", "message", null);

            Assert.That(Directory.GetFiles(_directory).Select(Path.GetFileName), Is.EqualTo(new[] { "applog_12_02_26.log" }));
        }

        [Test]
        public void Log_DateChangesWhileRunning_StartsNewFile()
        {
            DateTime now = Today;
            FileAppLogger logger = new FileAppLogger(_directory, LogLevel.Information, 1024 * 1024, 2, () => now);

            logger.Log(LogLevel.Information, "AI", "Send", "first day", null);
            now = Today.AddDays(1);
            logger.Log(LogLevel.Information, "AI", "Send", "second day", null);

            Assert.That(File.ReadAllText(LogPath), Does.Contain("first day").And.Not.Contain("second day"));
            Assert.That(File.ReadAllText(Path.Combine(_directory, "applog_13_02_26.log")), Does.Contain("second day"));
        }

        [Test]
        public void Log_ExceedsMaxSize_RollsToSingleOverflowFile()
        {
            const int maxBytes = 1024;
            FileAppLogger logger = new FileAppLogger(_directory, LogLevel.Information, maxBytes, 2, () => Today);

            for (int i = 0; i < 200; i++)
                logger.Log(LogLevel.Information, "AI", "Send", "entry " + i + " " + new string('x', 80), null);

            string[] names = Directory.GetFiles(_directory).Select(Path.GetFileName).OrderBy(n => n).ToArray();
            Assert.That(names, Is.EqualTo(new[] { "applog_12_02_26.1.log", "applog_12_02_26.log" }));
            foreach (string file in Directory.GetFiles(_directory))
                Assert.That(new FileInfo(file).Length, Is.LessThanOrEqualTo(maxBytes));
        }

        [Test]
        public void Log_MoreDaysThanRetained_RemovesOldestDaysByDateNotName()
        {
            Directory.CreateDirectory(_directory);
            // By name 31_01_26 sorts last, but it is the oldest day here.
            foreach (string name in new[] { "applog_31_01_26.log", "applog_31_01_26.1.log", "applog_01_02_26.log", "applog_11_02_26.log" })
                File.WriteAllText(Path.Combine(_directory, name), "old");
            FileAppLogger logger = new FileAppLogger(_directory, LogLevel.Information, 1024 * 1024, 2, () => Today);

            logger.Log(LogLevel.Information, "AI", "Send", "message", null);

            string[] names = Directory.GetFiles(_directory).Select(Path.GetFileName).OrderBy(n => n).ToArray();
            Assert.That(names, Is.EqualTo(new[] { "applog_01_02_26.log", "applog_11_02_26.log", "applog_12_02_26.log" }));
        }

        [Test]
        public void Log_ZeroRetainedDays_KeepsOnlyToday()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(Path.Combine(_directory, "applog_11_02_26.log"), "old");
            FileAppLogger logger = new FileAppLogger(_directory, LogLevel.Information, 1024 * 1024, 0, () => Today);

            logger.Log(LogLevel.Information, "AI", "Send", "message", null);

            Assert.That(Directory.GetFiles(_directory).Select(Path.GetFileName), Is.EqualTo(new[] { "applog_12_02_26.log" }));
        }

        [Test]
        public void Log_UnrelatedFilesInDirectory_AreLeftAlone()
        {
            Directory.CreateDirectory(_directory);
            File.WriteAllText(Path.Combine(_directory, "applog_notes.log"), "keep");
            FileAppLogger logger = new FileAppLogger(_directory, LogLevel.Information, 1024 * 1024, 0, () => Today);

            logger.Log(LogLevel.Information, "AI", "Send", "message", null);

            Assert.That(File.Exists(Path.Combine(_directory, "applog_notes.log")), Is.True);
        }

        [Test]
        public void Log_UnwritableDirectory_DoesNotThrow()
        {
            string filePath = Path.GetTempFileName();
            try
            {
                FileAppLogger logger = new FileAppLogger(filePath, LogLevel.Information, 1024, 1);

                Assert.DoesNotThrow(() => logger.Log(LogLevel.Error, "AI", "Send", "message", null));
            }
            finally
            {
                File.Delete(filePath);
            }
        }
    }
}
