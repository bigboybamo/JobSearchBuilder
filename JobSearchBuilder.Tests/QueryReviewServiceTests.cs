using JobSearchBuilder.Models;
using JobSearchBuilder.Services;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace JobSearchBuilder.Tests
{
    [TestFixture]
    public class QueryReviewServiceTests
    {
        private string _tempRoot;
        private InMemoryLlmProvider _provider;
        private QueryReviewService _service;

        [SetUp]
        public void SetUp()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), "JobSearchBuilderTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_tempRoot, "query_review"));
            File.WriteAllText(Path.Combine(_tempRoot, "query_review", "v1.xml"), "<prompt><instructions>review prompt</instructions></prompt>");

            _provider = new InMemoryLlmProvider();
            _provider.NextResponse = new LlmResponse
            {
                TextContent = @"{ ""issues"": [], ""suggestions"": [] }"
            };

            _service = new QueryReviewService(_provider, new PromptLoader(_tempRoot));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, true);
        }

        [Test]
        public async Task ReviewAsync_ValidQuery_SendsBalancedCachedRequest()
        {
            string query = "site:greenhouse.io/jobs \"Senior .NET Developer\" remote";

            await _service.ReviewAsync(query);

            Assert.That(_provider.LastRequest, Is.Not.Null);
            Assert.That(_provider.LastRequest.SystemPrompt, Does.Contain("review prompt"));
            Assert.That(_provider.LastRequest.UserMessage, Is.EqualTo(query));
            Assert.That(_provider.LastRequest.ModelTier, Is.EqualTo("Balanced"));
            Assert.That(_provider.LastRequest.EnableCaching, Is.True);
            Assert.That(_provider.LastRequest.ForceToolName, Is.Null.Or.Empty);
            Assert.That(_provider.LastRequest.Tools, Is.Null.Or.Empty);
        }

        [Test]
        public async Task ReviewAsync_CleanResponse_ReturnsEmptyIssues()
        {
            QueryReviewResult result = await _service.ReviewAsync("site:greenhouse.io/jobs \"Developer\"");

            Assert.That(result.Failed, Is.False);
            Assert.That(result.ErrorMessage, Is.Empty);
            Assert.That(result.Issues, Is.Empty);
            Assert.That(result.Suggestions, Is.Empty);
        }

        [Test]
        public async Task ReviewAsync_IssuesResponse_ReturnsPopulatedLists()
        {
            _provider.NextResponse = new LlmResponse
            {
                TextContent = @"{
  ""issues"": [""Query mixes intern and senior terms.""],
  ""suggestions"": [""Pick one seniority level.""]
}"
            };

            QueryReviewResult result = await _service.ReviewAsync("site:lever.co/jobs intern senior");

            Assert.That(result.Failed, Is.False);
            Assert.That(result.ErrorMessage, Is.Empty);
            Assert.That(result.Issues, Is.EqualTo(new[] { "Query mixes intern and senior terms." }));
            Assert.That(result.Suggestions, Is.EqualTo(new[] { "Pick one seniority level." }));
        }

        [Test]
        public async Task ReviewAsync_MarkdownFencedJson_ParsesCorrectly()
        {
            _provider.NextResponse = new LlmResponse
            {
                TextContent = "```json\n{ \"issues\": [\"Too broad.\"], \"suggestions\": [\"Add a role.\"] }\n```"
            };

            QueryReviewResult result = await _service.ReviewAsync("site:ashbyhq.com jobs");

            Assert.That(result.Failed, Is.False);
            Assert.That(result.ErrorMessage, Is.Empty);
            Assert.That(result.Issues, Is.EqualTo(new[] { "Too broad." }));
            Assert.That(result.Suggestions, Is.EqualTo(new[] { "Add a role." }));
        }

        [Test]
        public async Task ReviewAsync_ProviderThrows_ReturnsFailedResult()
        {
            _provider.NextException = new InvalidOperationException("API key is missing.");

            QueryReviewResult result = await _service.ReviewAsync("site:greenhouse.io/jobs \"Developer\"");

            Assert.That(result.Failed, Is.True);
            Assert.That(result.ErrorMessage, Does.Contain("API key is missing."));
            Assert.That(result.Issues, Is.Empty);
            Assert.That(result.Suggestions, Is.Empty);
        }

        [Test]
        public async Task ReviewAsync_InvalidJson_ReturnsFailedResult()
        {
            _provider.NextResponse = new LlmResponse
            {
                TextContent = "not json"
            };

            QueryReviewResult result = await _service.ReviewAsync("site:greenhouse.io/jobs \"Developer\"");

            Assert.That(result.Failed, Is.True);
            Assert.That(result.ErrorMessage, Is.Not.Empty);
            Assert.That(result.Issues, Is.Empty);
            Assert.That(result.Suggestions, Is.Empty);
        }

        [Test]
        public async Task ReviewAsync_EmptyProviderResponse_ReturnsFailedResult()
        {
            _provider.NextResponse = new LlmResponse
            {
                TextContent = string.Empty
            };

            QueryReviewResult result = await _service.ReviewAsync("site:greenhouse.io/jobs \"Developer\"");

            Assert.That(result.Failed, Is.True);
            Assert.That(result.ErrorMessage, Does.Contain("empty review response"));
            Assert.That(result.Issues, Is.Empty);
            Assert.That(result.Suggestions, Is.Empty);
        }

        [Test]
        public void ReviewAsync_NullQuery_ThrowsArgumentException()
        {
            Assert.ThrowsAsync<ArgumentException>(() => _service.ReviewAsync(null));
        }

        [Test]
        public void ReviewAsync_WhitespaceQuery_ThrowsArgumentException()
        {
            Assert.ThrowsAsync<ArgumentException>(() => _service.ReviewAsync("   "));
        }
    }
}
