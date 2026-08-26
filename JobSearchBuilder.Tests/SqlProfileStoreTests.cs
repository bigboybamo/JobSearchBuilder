using JobSearchBuilder.Models;
using JobSearchBuilder.Services;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Threading;

namespace JobSearchBuilder.Tests
{
    /// <summary>
    /// Tests for SqlProfileStore using an in-memory SQLite database.
    /// No SQL Server installation required — runs anywhere.
    /// </summary>
    [TestFixture]
    public class SqlProfileStoreTests
    {
        // Temporary file-based SQLite database — created once per test run,
        // deleted on teardown. Each test clears the rows in SetUp.
        private static readonly string DbPath =
            Path.Combine(Path.GetTempPath(), $"jsb_test_{Guid.NewGuid():N}.db");

        private static string ConnStr => $"Data Source={DbPath};Version=3;";

        private SqlProfileStore _store;

        // -------------------------------------------------------------------
        // Schema lifecycle
        // -------------------------------------------------------------------

        [OneTimeSetUp]
        public void CreateSchema()
        {
            Execute(@"CREATE TABLE IF NOT EXISTS SearchProfiles (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                Name      TEXT    NOT NULL,
                Seniority TEXT    NOT NULL DEFAULT 'Any',
                CreatedAt TEXT    NOT NULL,
                UpdatedAt TEXT    NOT NULL
            )");

            Execute(@"CREATE TABLE IF NOT EXISTS ProfileKeywords (
                Id        INTEGER PRIMARY KEY AUTOINCREMENT,
                ProfileId INTEGER NOT NULL REFERENCES SearchProfiles(Id) ON DELETE CASCADE,
                Category  TEXT    NOT NULL,
                Keyword   TEXT    NOT NULL
            )");

            Execute(@"CREATE TABLE IF NOT EXISTS ProfileSourceGroups (
                ProfileId     INTEGER NOT NULL REFERENCES SearchProfiles(Id) ON DELETE CASCADE,
                SourceGroupId INTEGER NOT NULL,
                PRIMARY KEY (ProfileId, SourceGroupId)
            )");
        }

        [OneTimeTearDown]
        public void DeleteDatabase()
        {
            if (File.Exists(DbPath))
                File.Delete(DbPath);
        }

        [SetUp]
        public void ClearData()
        {
            // Delete in dependency order so FK constraints are never violated.
            Execute("DELETE FROM ProfileSourceGroups");
            Execute("DELETE FROM ProfileKeywords");
            Execute("DELETE FROM SearchProfiles");

            _store = new SqlProfileStore(new SQLiteConnectionFactory());
        }

        // -------------------------------------------------------------------
        // Save — insert (Id == 0)
        // -------------------------------------------------------------------

        [Test]
        public void Save_NewProfile_AssignsNonZeroId()
        {
            // Arrange
            SearchProfile profile = new SearchProfile { Name = "Test" };

            // Act
            _store.Save(profile);

            // Assert
            Assert.That(profile.Id, Is.GreaterThan(0));
        }

        [Test]
        public void Save_NewProfile_AppearsInGetAll()
        {
            // Act
            _store.Save(new SearchProfile { Name = "Test" });

            // Assert
            Assert.That(_store.GetAll(), Has.Count.EqualTo(1));
        }

        [Test]
        public void Save_NewProfile_NullKeywordLists_DoesNotThrow()
        {
            // Arrange
            SearchProfile profile = new SearchProfile { Name = "Null Keywords" };
            profile.StackKeywords   = null;
            profile.RoleKeywords    = null;
            profile.LocationFilters = null;
            profile.VisaFilters     = null;
            profile.RemoteFilters   = null;

            // Act + Assert
            Assert.DoesNotThrow(() => _store.Save(profile));
            Assert.That(profile.Id, Is.GreaterThan(0));
        }

        [Test]
        public void Save_NewProfile_NullNameAndSeniority_NormalizesRequiredFields()
        {
            // Arrange
            SearchProfile profile = new SearchProfile
            {
                Name = null,
                Seniority = null
            };

            // Act
            Assert.DoesNotThrow(() => _store.Save(profile));
            SearchProfile loaded = _store.GetById(profile.Id);

            // Assert
            Assert.That(loaded.Name, Is.EqualTo(string.Empty));
            Assert.That(loaded.Seniority, Is.EqualTo("Any"));
        }

        [Test]
        public void Save_NewProfile_PersistsAllKeywordCategories()
        {
            // Arrange
            SearchProfile profile = new SearchProfile
            {
                Name            = "Full Profile",
                StackKeywords   = new List<string> { "C#", ".NET" },
                RoleKeywords    = new List<string> { "Developer" },
                LocationFilters = new List<string> { "London" },
                VisaFilters     = new List<string> { "visa sponsorship" },
                RemoteFilters   = new List<string> { "remote" }
            };

            // Act
            _store.Save(profile);
            SearchProfile loaded = _store.GetById(profile.Id);

            // Assert
            Assert.That(loaded.StackKeywords,   Is.EquivalentTo(new[] { "C#", ".NET" }));
            Assert.That(loaded.RoleKeywords,    Is.EquivalentTo(new[] { "Developer" }));
            Assert.That(loaded.LocationFilters, Is.EquivalentTo(new[] { "London" }));
            Assert.That(loaded.VisaFilters,     Is.EquivalentTo(new[] { "visa sponsorship" }));
            Assert.That(loaded.RemoteFilters,   Is.EquivalentTo(new[] { "remote" }));
        }

        [Test]
        public void GetById_UnknownKeywordCategory_IsIgnoredAndKnownCategoriesStillLoad()
        {
            // Arrange
            SearchProfile profile = new SearchProfile
            {
                Name          = "Unknown Category",
                StackKeywords = new List<string> { "C#" }
            };
            _store.Save(profile);

            // Simulate a row written by an older/newer build, or hand-edited data,
            // whose Category does not map to any chip section.
            Execute("INSERT INTO ProfileKeywords (ProfileId, Category, Keyword) VALUES ("
                + profile.Id + ", 'Bogus', 'should-not-appear')");

            // Act
            SearchProfile loaded = null;
            Assert.DoesNotThrow(() => loaded = _store.GetById(profile.Id));

            // Assert
            // The known category still loads...
            Assert.That(loaded.StackKeywords, Is.EquivalentTo(new[] { "C#" }));

            // ...and the unmappable keyword does not leak into any section.
            Assert.That(loaded.StackKeywords,   Does.Not.Contain("should-not-appear"));
            Assert.That(loaded.RoleKeywords,    Does.Not.Contain("should-not-appear"));
            Assert.That(loaded.LocationFilters, Does.Not.Contain("should-not-appear"));
            Assert.That(loaded.VisaFilters,     Does.Not.Contain("should-not-appear"));
            Assert.That(loaded.RemoteFilters,   Does.Not.Contain("should-not-appear"));
            Assert.That(loaded.TimezoneFilters, Does.Not.Contain("should-not-appear"));
            Assert.That(loaded.ExcludeKeywords, Does.Not.Contain("should-not-appear"));
        }

        [Test]
        public void Save_NewProfile_PersistsSourceGroupIds()
        {
            // Arrange
            SearchProfile profile = new SearchProfile
            {
                Name           = "With Groups",
                SourceGroupIds = new List<int> { 1, 3 }
            };

            // Act
            _store.Save(profile);
            SearchProfile loaded = _store.GetById(profile.Id);

            // Assert
            Assert.That(loaded.SourceGroupIds, Is.EquivalentTo(new[] { 1, 3 }));
        }

        [Test]
        public void Save_NewProfile_SetsCreatedAtAndUpdatedAt()
        {
            // Arrange
            DateTime before = DateTime.UtcNow.AddSeconds(-1);
            SearchProfile profile = new SearchProfile { Name = "Timestamped" };

            // Act
            _store.Save(profile);

            // Assert
            Assert.That(profile.CreatedAt, Is.GreaterThan(before));
            Assert.That(profile.UpdatedAt, Is.GreaterThan(before));
        }

        // -------------------------------------------------------------------
        // Save — update (Id > 0)
        // -------------------------------------------------------------------

        [Test]
        public void Save_ExistingProfile_UpdatesName()
        {
            // Arrange
            SearchProfile profile = new SearchProfile { Name = "Original" };
            _store.Save(profile);

            // Act
            profile.Name = "Updated";
            _store.Save(profile);

            // Assert
            Assert.That(_store.GetById(profile.Id).Name, Is.EqualTo("Updated"));
        }

        [Test]
        public void Save_ExistingProfile_DoesNotDuplicateRow()
        {
            // Arrange
            SearchProfile profile = new SearchProfile { Name = "Once" };
            _store.Save(profile);

            // Act
            profile.Name = "Twice";
            _store.Save(profile);

            // Assert
            Assert.That(_store.GetAll(), Has.Count.EqualTo(1));
        }

        [Test]
        public void Save_ExistingProfile_ReplacesKeywords()
        {
            // Arrange
            SearchProfile profile = new SearchProfile
            {
                Name          = "Keyword Test",
                StackKeywords = new List<string> { "C#" }
            };
            _store.Save(profile);

            // Act
            profile.StackKeywords = new List<string> { "Python", "Go" };
            _store.Save(profile);

            // Assert
            SearchProfile loaded = _store.GetById(profile.Id);
            Assert.That(loaded.StackKeywords, Is.EquivalentTo(new[] { "Python", "Go" }));
            Assert.That(loaded.StackKeywords, Does.Not.Contain("C#"));
        }

        [Test]
        public void Save_ExistingProfile_ReplacesSourceGroupIds()
        {
            // Arrange
            SearchProfile profile = new SearchProfile
            {
                Name           = "Group Test",
                SourceGroupIds = new List<int> { 1, 2 }
            };
            _store.Save(profile);

            // Act
            profile.SourceGroupIds = new List<int> { 3 };
            _store.Save(profile);

            // Assert
            Assert.That(_store.GetById(profile.Id).SourceGroupIds, Is.EquivalentTo(new[] { 3 }));
        }

        [Test]
        public void Save_ExistingProfile_NullNameAndSeniority_NormalizesRequiredFields()
        {
            // Arrange
            SearchProfile profile = new SearchProfile { Name = "Original", Seniority = "Senior" };
            _store.Save(profile);

            // Act
            profile.Name = null;
            profile.Seniority = null;
            Assert.DoesNotThrow(() => _store.Save(profile));
            SearchProfile loaded = _store.GetById(profile.Id);

            // Assert
            Assert.That(loaded.Name, Is.EqualTo(string.Empty));
            Assert.That(loaded.Seniority, Is.EqualTo("Any"));
        }

        [Test]
        public void Save_ExistingProfile_AdvancesUpdatedAt()
        {
            // Arrange
            SearchProfile profile = new SearchProfile { Name = "Time Test" };
            _store.Save(profile);
            DateTime firstUpdatedAt = profile.UpdatedAt;

            // Act
            Thread.Sleep(20);
            profile.Name = "Time Test 2";
            _store.Save(profile);

            // Assert
            Assert.That(profile.UpdatedAt, Is.GreaterThan(firstUpdatedAt));
        }

        // -------------------------------------------------------------------
        // Delete
        // -------------------------------------------------------------------

        [Test]
        public void Delete_ExistingProfile_RemovesFromGetAll()
        {
            // Arrange
            SearchProfile profile = new SearchProfile { Name = "ToDelete" };
            _store.Save(profile);

            // Act
            _store.Delete(profile.Id);

            // Assert
            Assert.That(_store.GetAll(), Is.Empty);
        }

        [Test]
        public void Delete_ExistingProfile_MakesGetByIdReturnNull()
        {
            // Arrange
            SearchProfile profile = new SearchProfile { Name = "Gone" };
            _store.Save(profile);
            int savedId = profile.Id;

            // Act
            _store.Delete(savedId);

            // Assert
            Assert.That(_store.GetById(savedId), Is.Null);
        }

        [Test]
        public void Delete_NonExistentId_DoesNotThrow()
        {
            // Act + Assert
            Assert.DoesNotThrow(() => _store.Delete(999));
        }

        // -------------------------------------------------------------------
        // GetById
        // -------------------------------------------------------------------

        [Test]
        public void GetById_ExistingId_ReturnsCorrectProfile()
        {
            // Arrange
            SearchProfile profile = new SearchProfile { Name = "FindMe", Seniority = "Senior" };
            _store.Save(profile);

            // Act
            SearchProfile loaded = _store.GetById(profile.Id);

            // Assert
            Assert.That(loaded,           Is.Not.Null);
            Assert.That(loaded.Name,      Is.EqualTo("FindMe"));
            Assert.That(loaded.Seniority, Is.EqualTo("Senior"));
        }

        [Test]
        public void GetById_UnknownId_ReturnsNull()
        {
            // Act + Assert
            Assert.That(_store.GetById(999), Is.Null);
        }

        // -------------------------------------------------------------------
        // GetAll
        // -------------------------------------------------------------------

        [Test]
        public void GetAll_EmptyStore_ReturnsEmptyList()
        {
            // Act + Assert
            Assert.That(_store.GetAll(), Is.Empty);
        }

        [Test]
        public void GetAll_MultipleProfiles_ReturnsAll()
        {
            // Arrange
            _store.Save(new SearchProfile { Name = "A" });
            _store.Save(new SearchProfile { Name = "B" });
            _store.Save(new SearchProfile { Name = "C" });

            // Act + Assert
            Assert.That(_store.GetAll(), Has.Count.EqualTo(3));
        }

        // -------------------------------------------------------------------
        // SQLite implementation of IDbConnectionFactory
        // -------------------------------------------------------------------

        private class SQLiteConnectionFactory : IDbConnectionFactory
        {
            public IDbConnection CreateOpenConnection()
            {
                SQLiteConnection conn = new SQLiteConnection(ConnStr);
                conn.Open();
                return conn;
            }

            public string LastInsertIdSql => "SELECT last_insert_rowid()";
        }

        // -------------------------------------------------------------------
        // Helper
        // -------------------------------------------------------------------

        private static void Execute(string sql)
        {
            using (SQLiteConnection conn = new SQLiteConnection(ConnStr))
            {
                conn.Open();
                using (SQLiteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
