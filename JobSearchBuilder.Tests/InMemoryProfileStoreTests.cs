using JobSearchBuilder.Models;
using JobSearchBuilder.Services;
using NUnit.Framework;
using System.Collections.Generic;

namespace JobSearchBuilder.Tests
{
    [TestFixture]
    public class InMemoryProfileStoreTests
    {
        private InMemoryProfileStore _store;

        [SetUp]
        public void SetUp()
        {
            _store = new InMemoryProfileStore();
        }

        // -------------------------------------------------------------------
        // Save — new profiles
        // -------------------------------------------------------------------

        [Test]
        public void Save_NewProfile_AssignsNonZeroId()
        {
            SearchProfile profile = new SearchProfile { Name = "Test" };
            _store.Save(profile);
            Assert.That(profile.Id, Is.GreaterThan(0));
        }

        [Test]
        public void Save_MultipleNewProfiles_AssignsUniqueIncrementingIds()
        {
            SearchProfile p1 = new SearchProfile { Name = "A" };
            SearchProfile p2 = new SearchProfile { Name = "B" };
            SearchProfile p3 = new SearchProfile { Name = "C" };

            _store.Save(p1);
            _store.Save(p2);
            _store.Save(p3);

            Assert.That(p1.Id, Is.LessThan(p2.Id));
            Assert.That(p2.Id, Is.LessThan(p3.Id));
        }

        [Test]
        public void Save_NewProfile_AppearsInGetAll()
        {
            SearchProfile profile = new SearchProfile { Name = "Test" };
            _store.Save(profile);

            IReadOnlyList<SearchProfile> all = _store.GetAll();
            Assert.That(all, Has.Count.EqualTo(1));
            Assert.That(all[0].Name, Is.EqualTo("Test"));
        }

        // -------------------------------------------------------------------
        // Save — update existing profiles
        // -------------------------------------------------------------------

        [Test]
        public void Save_ExistingProfile_UpdatesWithoutDuplicating()
        {
            SearchProfile profile = new SearchProfile { Name = "Original" };
            _store.Save(profile);

            profile.Name = "Updated";
            _store.Save(profile);

            IReadOnlyList<SearchProfile> all = _store.GetAll();
            Assert.That(all, Has.Count.EqualTo(1));
            Assert.That(all[0].Name, Is.EqualTo("Updated"));
        }

        [Test]
        public void Save_NonExistentId_ThrowsInvalidOperationException()
        {
            SearchProfile phantom = new SearchProfile { Id = 999, Name = "Ghost" };
            Assert.Throws<System.InvalidOperationException>(() => _store.Save(phantom));
        }

        // -------------------------------------------------------------------
        // Delete
        // -------------------------------------------------------------------

        [Test]
        public void Delete_ExistingProfile_RemovesItFromStore()
        {
            SearchProfile profile = new SearchProfile { Name = "ToDelete" };
            _store.Save(profile);

            _store.Delete(profile.Id);

            Assert.That(_store.GetAll(), Is.Empty);
        }

        [Test]
        public void Delete_NonExistentId_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _store.Delete(999));
        }

        // -------------------------------------------------------------------
        // GetById
        // -------------------------------------------------------------------

        [Test]
        public void GetById_ExistingId_ReturnsCorrectProfile()
        {
            SearchProfile profile = new SearchProfile { Name = "FindMe" };
            _store.Save(profile);

            SearchProfile found = _store.GetById(profile.Id);
            Assert.That(found, Is.Not.Null);
            Assert.That(found.Name, Is.EqualTo("FindMe"));
        }

        [Test]
        public void GetById_UnknownId_ReturnsNull()
        {
            SearchProfile result = _store.GetById(999);
            Assert.That(result, Is.Null);
        }

        // -------------------------------------------------------------------
        // GetAll
        // -------------------------------------------------------------------

        [Test]
        public void GetAll_EmptyStore_ReturnsEmptyList()
        {
            Assert.That(_store.GetAll(), Is.Empty);
        }

        [Test]
        public void GetAll_AfterMultipleSaves_ReturnsAllProfiles()
        {
            _store.Save(new SearchProfile { Name = "A" });
            _store.Save(new SearchProfile { Name = "B" });
            _store.Save(new SearchProfile { Name = "C" });

            Assert.That(_store.GetAll(), Has.Count.EqualTo(3));
        }

        // -------------------------------------------------------------------
        // SeedDefaults
        // -------------------------------------------------------------------

        [Test]
        public void SeedDefaults_EmptyStore_CreatesOneProfile()
        {
            _store.SeedDefaults();
            Assert.That(_store.GetAll(), Has.Count.EqualTo(1));
        }

        [Test]
        public void SeedDefaults_WhenProfilesExist_DoesNothing()
        {
            _store.Save(new SearchProfile { Name = "Existing" });
            _store.SeedDefaults();

            Assert.That(_store.GetAll(), Has.Count.EqualTo(1));
        }

        [Test]
        public void SeedDefaults_DefaultProfile_HasExpectedName()
        {
            _store.SeedDefaults();
            Assert.That(_store.GetAll()[0].Name, Is.EqualTo("UK Sponsorship .NET"));
        }
    }
}
