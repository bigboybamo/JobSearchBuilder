using JobSearchBuilder.Services;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace JobSearchBuilder.Tests
{
    [TestFixture]
    public class CountryServiceTests
    {
        private string _tempCache;

        [SetUp]
        public void SetUp()
        {
            _tempCache = Path.Combine(Path.GetTempPath(), $"countries_test_{Guid.NewGuid()}.json");
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_tempCache))
                File.Delete(_tempCache);
        }

        // -------------------------------------------------------------------
        // ParseApiResponse — pure JSON parsing logic
        // -------------------------------------------------------------------

        [Test]
        public void ParseApiResponse_ValidJson_ExtractsCommonNames()
        {
            string json = @"[
                { ""name"": { ""common"": ""Ghana"", ""official"": ""Republic of Ghana"" } },
                { ""name"": { ""common"": ""Kiribati"", ""official"": ""Independent and Sovereign Republic of Kiribati"" } }
            ]";

            List<string> result = CountryService.ParseApiResponse(json);

            Assert.That(result, Is.EquivalentTo(new[] { "Ghana", "Kiribati" }));
        }

        [Test]
        public void ParseApiResponse_ResultIsSortedAlphabetically()
        {
            string json = @"[
                { ""name"": { ""common"": ""Zimbabwe"" } },
                { ""name"": { ""common"": ""Albania"" } },
                { ""name"": { ""common"": ""Mexico"" } }
            ]";

            List<string> result = CountryService.ParseApiResponse(json);

            Assert.That(result, Is.EqualTo(new[] { "Albania", "Mexico", "Zimbabwe" }));
        }

        [Test]
        public void ParseApiResponse_SortingIsCaseInsensitive()
        {
            string json = @"[
                { ""name"": { ""common"": ""zimbabwe"" } },
                { ""name"": { ""common"": ""Albania"" } }
            ]";

            List<string> result = CountryService.ParseApiResponse(json);

            Assert.That(result[0], Is.EqualTo("Albania"));
        }

        [Test]
        public void ParseApiResponse_SkipsEntriesWithNullCommonName()
        {
            string json = @"[
                { ""name"": { ""common"": ""Ghana"" } },
                { ""name"": { ""official"": ""No common field here"" } },
                { ""name"": { ""common"": null } }
            ]";

            List<string> result = CountryService.ParseApiResponse(json);

            Assert.That(result, Is.EqualTo(new[] { "Ghana" }));
        }

        [Test]
        public void ParseApiResponse_SkipsEntriesWithWhitespaceCommonName()
        {
            string json = @"[
                { ""name"": { ""common"": ""   "" } },
                { ""name"": { ""common"": ""Canada"" } }
            ]";

            List<string> result = CountryService.ParseApiResponse(json);

            Assert.That(result, Is.EqualTo(new[] { "Canada" }));
        }

        [Test]
        public void ParseApiResponse_EmptyArray_ReturnsEmptyList()
        {
            List<string> result = CountryService.ParseApiResponse("[]");

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void ParseApiResponse_InvalidJson_ThrowsException()
        {
            Assert.Throws<Newtonsoft.Json.JsonReaderException>(
                () => CountryService.ParseApiResponse("not valid json"));
        }

        // -------------------------------------------------------------------
        // ParseCache — round-trip with SaveToCache
        // -------------------------------------------------------------------

        [Test]
        public void ParseCache_ValidJson_ReturnsNames()
        {
            string json = new JArray("Albania", "Ghana", "Mexico").ToString();

            List<string> result = CountryService.ParseCache(json);

            Assert.That(result, Is.EqualTo(new[] { "Albania", "Ghana", "Mexico" }));
        }

        [Test]
        public void ParseCache_EmptyArray_ReturnsEmptyList()
        {
            List<string> result = CountryService.ParseCache("[]");

            Assert.That(result, Is.Empty);
        }

        // -------------------------------------------------------------------
        // GetCountries — cache hit / miss behaviour
        // -------------------------------------------------------------------

        [Test]
        public void GetCountries_CacheFileExists_ReturnsCachedDataWithoutCallingApi()
        {
            // Write a known cache file
            File.WriteAllText(_tempCache, new JArray("Testland", "Mockistan").ToString());

            bool apiCalled = false;
            CountryService service = new CountryService(_tempCache, () =>
            {
                apiCalled = true;
                return "[]";
            });

            List<string> result = service.GetCountries();

            Assert.That(apiCalled, Is.False, "API should not be called when cache exists");
            Assert.That(result, Is.EqualTo(new[] { "Testland", "Mockistan" }));
        }

        [Test]
        public void GetCountries_NoCacheFile_CallsApiAndCreatesCache()
        {
            string apiJson = @"[
                { ""name"": { ""common"": ""Canada"" } },
                { ""name"": { ""common"": ""Australia"" } }
            ]";

            CountryService service = new CountryService(_tempCache, () => apiJson);

            List<string> result = service.GetCountries();

            Assert.That(File.Exists(_tempCache), Is.True, "Cache file should be created after API call");
            Assert.That(result, Is.EqualTo(new[] { "Australia", "Canada" }));
        }

        [Test]
        public void GetCountries_NoCacheFile_CachedFileContainsCorrectJson()
        {
            string apiJson = @"[{ ""name"": { ""common"": ""France"" } }]";
            CountryService service = new CountryService(_tempCache, () => apiJson);

            service.GetCountries();

            List<string> fromCache = CountryService.ParseCache(File.ReadAllText(_tempCache));
            Assert.That(fromCache, Is.EqualTo(new[] { "France" }));
        }

        [Test]
        public void GetCountries_SecondCall_UsesCacheNotApi()
        {
            string apiJson = @"[{ ""name"": { ""common"": ""France"" } }]";
            int apiCallCount = 0;
            CountryService service = new CountryService(_tempCache, () =>
            {
                apiCallCount++;
                return apiJson;
            });

            service.GetCountries(); // first call — hits API, creates cache
            service.GetCountries(); // second call — should use cache

            Assert.That(apiCallCount, Is.EqualTo(1));
        }

        [Test]
        public void GetCountries_ApiReturnsNoBuildableNames_CreatesEmptyCacheFile()
        {
            string apiJson = @"[{ ""name"": { ""official"": ""Only official"" } }]";
            CountryService service = new CountryService(_tempCache, () => apiJson);

            List<string> result = service.GetCountries();

            Assert.That(result, Is.Empty);
            Assert.That(File.Exists(_tempCache), Is.True);
        }
    }
}
