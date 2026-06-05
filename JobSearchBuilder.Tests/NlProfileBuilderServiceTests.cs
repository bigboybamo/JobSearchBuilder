using JobSearchBuilder.Models;
using JobSearchBuilder.Services;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace JobSearchBuilder.Tests
{
    [TestFixture]
    public class NlProfileBuilderServiceTests
    {
        private string _tempRoot;
        private InMemoryLlmProvider _provider;
        private NlProfileBuilderService _service;

        [SetUp]
        public void SetUp()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), "JobSearchBuilderTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_tempRoot, "nl_profile_builder"));
            File.WriteAllText(Path.Combine(_tempRoot, "nl_profile_builder", "v2.xml"), "<prompt><instructions>test prompt</instructions></prompt>");

            _provider = new InMemoryLlmProvider();
            _provider.NextResponse = new LlmResponse
            {
                ToolCallName = "build_query_profile",
                ToolCallArguments = @"{
  ""role"": ""Developer"",
  ""seniority"": ""Senior"",
  ""tech_stack"": [""C#"", "".NET""],
  ""remote_terms"": [""Fully Remote""],
  ""timezone_terms"": [""UTC+1""],
  ""exclude_terms"": [""security clearance""]
}"
            };

            _service = new NlProfileBuilderService(_provider, new PromptLoader(_tempRoot));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, true);
        }

        [Test]
        public async Task BuildAsync_ValidDescription_SendsForcedBalancedCachedToolRequest()
        {
            await _service.BuildAsync("Senior .NET developer, fully remote");

            Assert.That(_provider.LastRequest, Is.Not.Null);
            Assert.That(_provider.LastRequest.SystemPrompt, Does.Contain("test prompt"));
            Assert.That(_provider.LastRequest.UserMessage, Is.EqualTo("Senior .NET developer, fully remote"));
            Assert.That(_provider.LastRequest.ModelTier, Is.EqualTo("Balanced"));
            Assert.That(_provider.LastRequest.EnableCaching, Is.True);
            Assert.That(_provider.LastRequest.ForceToolName, Is.EqualTo("build_query_profile"));
            Assert.That(_provider.LastRequest.Tools.Count, Is.EqualTo(1));
            Assert.That(_provider.LastRequest.Tools[0].Name, Is.EqualTo("build_query_profile"));
            Assert.That(_provider.LastRequest.Tools[0].InputSchema, Does.Contain("tech_stack"));
        }

        [Test]
        public async Task BuildAsync_ToolResponse_ReturnsQueryProfileResult()
        {
            QueryProfileResult result = await _service.BuildAsync("Senior .NET developer, fully remote");

            Assert.That(result.Role, Is.EqualTo("Developer"));
            Assert.That(result.Seniority, Is.EqualTo("Senior"));
            Assert.That(result.TechStack, Is.EqualTo(new[] { "C#", ".NET" }));
            Assert.That(result.RemoteTerms, Is.EqualTo(new[] { "Fully Remote" }));
            Assert.That(result.TimezoneTerms, Is.EqualTo(new[] { "UTC+1" }));
            Assert.That(result.ExcludeTerms, Is.EqualTo(new[] { "security clearance" }));
        }

        [Test]
        public void BuildAsync_EmptyDescription_ThrowsArgumentException()
        {
            Assert.ThrowsAsync<ArgumentException>(() => _service.BuildAsync(" "));
        }

        [Test]
        public void BuildAsync_MissingToolCall_ThrowsInvalidOperationException()
        {
            _provider.NextResponse = new LlmResponse
            {
                TextContent = "plain text"
            };

            Assert.ThrowsAsync<InvalidOperationException>(() => _service.BuildAsync("Senior developer"));
        }
    }
}
