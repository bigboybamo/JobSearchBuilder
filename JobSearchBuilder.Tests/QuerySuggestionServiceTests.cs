using JobSearchBuilder.Models;
using JobSearchBuilder.Services;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace JobSearchBuilder.Tests
{
    [TestFixture]
    public class QuerySuggestionServiceTests
    {
        private string _tempRoot;
        private InMemoryLlmProvider _provider;
        private QuerySuggestionService _service;

        [SetUp]
        public void SetUp()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), "JobSearchBuilderTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_tempRoot, "query_suggestions"));
            File.WriteAllText(Path.Combine(_tempRoot, "query_suggestions", "v1.xml"), "<prompt>suggestion prompt</prompt>");

            _provider = new InMemoryLlmProvider();
            _provider.NextResponse = new LlmResponse
            {
                ToolCallName = "suggest_keywords",
                ToolCallArguments = @"{ ""suggestions"": [""Azure Functions"", ""Azure App Service""] }"
            };

            _service = new QuerySuggestionService(_provider, new PromptLoader(_tempRoot));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, true);
        }

        [Test]
        public async Task SuggestAsync_ValidCategory_SendsBalancedCachedForcedToolRequest()
        {
            await _service.SuggestAsync("Tech Stack", new List<string> { "C#", ".NET" }, "azure");

            Assert.That(_provider.LastRequest, Is.Not.Null);
            Assert.That(_provider.LastRequest.SystemPrompt, Does.Contain("suggestion prompt"));
            Assert.That(_provider.LastRequest.UserMessage, Does.Contain("Category: Tech Stack"));
            Assert.That(_provider.LastRequest.UserMessage, Does.Contain("Already added: C#, .NET"));
            Assert.That(_provider.LastRequest.UserMessage, Does.Contain("Partial input: azure"));
            Assert.That(_provider.LastRequest.ModelTier, Is.EqualTo("Balanced"));
            Assert.That(_provider.LastRequest.EnableCaching, Is.True);
            Assert.That(_provider.LastRequest.ForceToolName, Is.EqualTo("suggest_keywords"));
            Assert.That(_provider.LastRequest.Tools.Count, Is.EqualTo(1));
            Assert.That(_provider.LastRequest.Tools[0].Name, Is.EqualTo("suggest_keywords"));
            Assert.That(_provider.LastRequest.Tools[0].InputSchema, Does.Contain("suggestions"));
        }

        [Test]
        public async Task SuggestAsync_ToolResponse_ReturnsParsedList()
        {
            List<string> result = await _service.SuggestAsync("Tech Stack", new List<string> { "C#" }, "azure");

            Assert.That(result, Is.EqualTo(new[] { "Azure Functions", "Azure App Service" }));
        }

        [Test]
        public void SuggestAsync_NullCategory_ThrowsArgumentException()
        {
            Assert.ThrowsAsync<ArgumentException>(() => _service.SuggestAsync(null, new List<string>(), string.Empty));
        }
    }
}
