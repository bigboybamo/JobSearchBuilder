using JobSearchBuilder.Interfaces;
using JobSearchBuilder.Models;
using JobSearchBuilder.Services;
using JobSearchBuilder.Services.Providers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JobSearchBuilder.Tests
{
    [TestFixture]
    public class BatchProfileBuilderServiceTests
    {
        private string _tempRoot;
        private InMemoryLlmProvider _provider;
        private PromptLoader _promptLoader;
        private AppSettings _settings;
        private BatchProfileBuilderService _service;

        [SetUp]
        public void SetUp()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), "JobSearchBuilderTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(_tempRoot, "nl_profile_builder"));
            File.WriteAllText(Path.Combine(_tempRoot, "nl_profile_builder", "v3.xml"), "<prompt><instructions>test prompt</instructions></prompt>");

            _provider = new InMemoryLlmProvider();
            _provider.NextResponse = CreateValidLlmResponse("Developer");
            _promptLoader = new PromptLoader(_tempRoot);
            _settings = CreateSettings();
            _service = new BatchProfileBuilderService(_provider, _promptLoader, _settings);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, true);
        }

        [Test]
        public void BuildBatchAsync_NullDescriptions_ThrowsArgumentNullException()
        {
            Assert.ThrowsAsync<ArgumentNullException>(() => _service.BuildBatchAsync(null));
        }

        [Test]
        public async Task BuildBatchAsync_EmptyList_ReturnsEmptyList()
        {
            List<BatchProfileResult> results = await _service.BuildBatchAsync(new List<string>());

            Assert.That(results.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task BuildBatchAsync_TwoDescriptions_ReturnsTwoResults()
        {
            List<BatchProfileResult> results = await _service.BuildBatchAsync(new List<string> { "Senior .NET developer", "Lead platform engineer" });

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results[0].IsError, Is.False);
            Assert.That(results[1].IsError, Is.False);
        }

        [Test]
        public async Task BuildBatchAsync_ValidResponse_ParsesProfileCorrectly()
        {
            List<BatchProfileResult> results = await _service.BuildBatchAsync(new List<string> { "Senior .NET developer" });

            Assert.That(results[0].Profile.Role, Is.EqualTo("Developer"));
            Assert.That(results[0].Profile.Seniority, Is.EqualTo("Senior"));
            Assert.That(results[0].Profile.TechStack, Is.EqualTo(new[] { "C#", ".NET" }));
        }

        [Test]
        public async Task BuildBatchAsync_ProviderReturnsWrongTool_WrapsErrorInResult()
        {
            _provider.NextResponse = new LlmResponse { TextContent = "plain text" };

            List<BatchProfileResult> results = await _service.BuildBatchAsync(new List<string> { "Senior developer" });

            Assert.That(results[0].IsError, Is.True);
            Assert.That(results[0].ErrorMessage, Is.Not.Empty);
        }

        [Test]
        public async Task BuildBatchAsync_AnthropicProvider_PostsBatchWithCorrectRequestCount()
        {
            FakeBatchHandler handler = new FakeBatchHandler(CreateSucceededJsonl("0", "Engineer"));
            BatchProfileBuilderService service = CreateAnthropicService(handler);

            await service.BuildBatchAsync(new List<string> { "Engineer one", "Engineer two" });

            Assert.That(handler.LastBatchRequestBody, Does.Contain("\"custom_id\":\"0\""));
            Assert.That(handler.LastBatchRequestBody, Does.Contain("\"custom_id\":\"1\""));
        }

        [Test]
        public async Task BuildBatchAsync_AnthropicProvider_IncludesCorrectModel()
        {
            FakeBatchHandler handler = new FakeBatchHandler(CreateSucceededJsonl("0", "Engineer"));
            BatchProfileBuilderService service = CreateAnthropicService(handler);

            await service.BuildBatchAsync(new List<string> { "Engineer" });

            Assert.That(handler.LastBatchRequestBody, Does.Contain("claude-sonnet-4-5-20250929"));
        }

        [Test]
        public async Task BuildBatchAsync_AnthropicProvider_ForcesBuildQueryProfileTool()
        {
            FakeBatchHandler handler = new FakeBatchHandler(CreateSucceededJsonl("0", "Engineer"));
            BatchProfileBuilderService service = CreateAnthropicService(handler);

            await service.BuildBatchAsync(new List<string> { "Engineer" });

            Assert.That(handler.LastBatchRequestBody, Does.Contain("build_query_profile"));
            Assert.That(handler.LastBatchRequestBody, Does.Contain("\"type\":\"tool\""));
        }

        [Test]
        public async Task BuildBatchAsync_AnthropicProvider_ParsesSucceededJsonlResult()
        {
            FakeBatchHandler handler = new FakeBatchHandler(CreateSucceededJsonl("0", "Engineer"));
            BatchProfileBuilderService service = CreateAnthropicService(handler);

            List<BatchProfileResult> results = await service.BuildBatchAsync(new List<string> { "Engineer" });

            Assert.That(results[0].IsError, Is.False);
            Assert.That(results[0].Profile.Role, Is.EqualTo("Engineer"));
        }

        [Test]
        public async Task BuildBatchAsync_AnthropicProvider_ParsesErroredJsonlResult()
        {
            string jsonl = "{\"custom_id\":\"0\",\"result\":{\"type\":\"errored\",\"error\":{\"message\":\"rate limited\"}}}";
            FakeBatchHandler handler = new FakeBatchHandler(jsonl);
            BatchProfileBuilderService service = CreateAnthropicService(handler);

            List<BatchProfileResult> results = await service.BuildBatchAsync(new List<string> { "Engineer" });

            Assert.That(results[0].IsError, Is.True);
            Assert.That(results[0].ErrorMessage, Does.Contain("rate limited"));
        }

        [Test]
        public async Task BuildBatchAsync_AnthropicProvider_HandlesGapInResults()
        {
            FakeBatchHandler handler = new FakeBatchHandler(CreateSucceededJsonl("0", "Engineer"));
            BatchProfileBuilderService service = CreateAnthropicService(handler);

            List<BatchProfileResult> results = await service.BuildBatchAsync(new List<string> { "Engineer one", "Engineer two" });

            Assert.That(results[1].IsError, Is.True);
            Assert.That(results[1].ErrorMessage, Is.EqualTo("No result returned."));
        }

        private BatchProfileBuilderService CreateAnthropicService(FakeBatchHandler handler)
        {
            ILlmProvider anthropicProvider = new AnthropicProvider(_settings, new FakeNeverCalledHandler());
            return new BatchProfileBuilderService(anthropicProvider, _promptLoader, _settings, handler, 0);
        }

        private static AppSettings CreateSettings()
        {
            return new AppSettings
            {
                Ai = new AiSettings
                {
                    Provider = "Anthropic",
                    Models = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
                    {
                        {
                            "Anthropic",
                            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                            {
                                { "Balanced", "claude-sonnet-4-5-20250929" }
                            }
                        }
                    },
                    ApiKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "Anthropic", "test-key" }
                    }
                }
            };
        }

        private static LlmResponse CreateValidLlmResponse(string role)
        {
            return new LlmResponse
            {
                ToolCallName = "build_query_profile",
                ToolCallArguments = @"{
  ""role"": """ + role + @""",
  ""seniority"": ""Senior"",
  ""tech_stack"": [""C#"", "".NET""],
  ""remote_terms"": [""Fully Remote""],
  ""timezone_terms"": [""UTC+1""],
  ""exclude_terms"": [""security clearance""]
}"
            };
        }

        private static string CreateSucceededJsonl(string customId, string role)
        {
            return "{\"custom_id\":\"" + customId + "\",\"result\":{\"type\":\"succeeded\",\"message\":{\"content\":[{\"type\":\"tool_use\",\"input\":{\"role\":\"" + role + "\",\"seniority\":\"Senior\",\"tech_stack\":[\"C#\",\".NET\"],\"remote_terms\":[\"Fully Remote\"],\"timezone_terms\":[\"UTC+1\"],\"exclude_terms\":[\"security clearance\"]}}]}}}";
        }

        private class FakeBatchHandler : HttpMessageHandler
        {
            private readonly string _jsonl;

            public string LastBatchRequestBody { get; private set; }

            public FakeBatchHandler(string jsonl)
            {
                _jsonl = jsonl;
                LastBatchRequestBody = string.Empty;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                string url = request.RequestUri.ToString();
                if (request.Method == HttpMethod.Post)
                {
                    LastBatchRequestBody = request.Content == null
                        ? string.Empty
                        : await request.Content.ReadAsStringAsync().ConfigureAwait(false);

                    return CreateJsonResponse("{\"id\":\"msgbatch_test\",\"processing_status\":\"in_progress\"}");
                }

                if (url.EndsWith("/results", StringComparison.OrdinalIgnoreCase))
                    return CreateJsonResponse(_jsonl);

                return CreateJsonResponse("{\"processing_status\":\"ended\"}");
            }

            private static HttpResponseMessage CreateJsonResponse(string body)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json")
                };
            }
        }

        private class FakeNeverCalledHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                throw new InvalidOperationException("Provider SendAsync should not be called in the Anthropic batch path.");
            }
        }
    }
}
