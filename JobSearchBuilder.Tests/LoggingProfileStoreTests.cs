using JobSearchBuilder.Models;
using JobSearchBuilder.Services;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JobSearchBuilder.Tests
{
    [TestFixture]
    public class LoggingProfileStoreTests
    {
        private class FailingProfileStore : IProfileStore
        {
            public readonly InvalidOperationException Failure = new InvalidOperationException("database down");

            public IReadOnlyList<SearchProfile> GetAll() { throw Failure; }
            public SearchProfile GetById(int id) { throw Failure; }
            public SearchProfile Save(SearchProfile profile) { throw Failure; }
            public void Delete(int id) { throw Failure; }
        }

        private RecordingAppLogger _logger;

        [SetUp]
        public void SetUp()
        {
            _logger = new RecordingAppLogger();
            AppLog.Configure(_logger);
        }

        [TearDown]
        public void TearDown()
        {
            AppLog.Configure(null);
        }

        [Test]
        public void Save_Success_ReturnsInnerResultAndLogsCompletion()
        {
            LoggingProfileStore store = new LoggingProfileStore(new InMemoryProfileStore());

            SearchProfile saved = store.Save(new SearchProfile { Name = "Private Profile Name" });

            Assert.That(saved.Id, Is.GreaterThan(0));
            RecordingAppLogger.Entry entry = _logger.Entries.Single();
            Assert.That(entry.Level, Is.EqualTo(LogLevel.Information));
            Assert.That(entry.Operation, Is.EqualTo("Save"));
            Assert.That(entry.Message, Does.Contain("Completed"));
        }

        [Test]
        public void Save_Success_DoesNotLogProfileName()
        {
            LoggingProfileStore store = new LoggingProfileStore(new InMemoryProfileStore());

            store.Save(new SearchProfile { Name = "Private Profile Name" });

            Assert.That(_logger.Entries.Single().Message, Does.Not.Contain("Private Profile Name"));
        }

        [Test]
        public void GetAll_Success_LogsAtDebugLevel()
        {
            LoggingProfileStore store = new LoggingProfileStore(new InMemoryProfileStore());

            store.GetAll();

            Assert.That(_logger.Entries.Single().Level, Is.EqualTo(LogLevel.Debug));
        }

        [Test]
        public void Delete_InnerThrows_LogsErrorWithContextAndRethrowsSameException()
        {
            FailingProfileStore inner = new FailingProfileStore();
            LoggingProfileStore store = new LoggingProfileStore(inner);

            Exception thrown = Assert.Throws<InvalidOperationException>(() => store.Delete(7));

            Assert.That(thrown, Is.SameAs(inner.Failure));
            RecordingAppLogger.Entry entry = _logger.Entries.Single();
            Assert.That(entry.Level, Is.EqualTo(LogLevel.Error));
            Assert.That(entry.Operation, Is.EqualTo("Delete"));
            Assert.That(entry.Message, Does.Contain("profileId=7"));
            Assert.That(entry.Exception, Is.SameAs(inner.Failure));
        }

        [Test]
        public void Constructor_NullInner_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new LoggingProfileStore(null));
        }
    }
}
