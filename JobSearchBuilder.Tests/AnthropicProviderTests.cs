using JobSearchBuilder.Models;
using JobSearchBuilder.Services;
using JobSearchBuilder.Services.Providers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JobSearchBuilder.Tests
{
    [TestFixture]
    public class AnthropicProviderTests
    {
        private AppSettings CreateSettings()
        {
            return new AppSettings
            {
                Ai = new AiSettings
                {
                    Provider = "Anthropic",
                    Models = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
                    {
                        {
                            "Anthropic", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
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

        [Test]
        public async Task SendAsync_SimpleTextRequest_ReturnsTextContent()
        {
            // Arrange
            string json = "{\"content\":[{\"type\":\"text\",\"text\":\"hello world\"}],\"usage\":{\"input_tokens\":10,\"output_tokens\":4}}";
            AnthropicProvider provider = new AnthropicProvider(CreateSettings(), new FakeHandler(json));

            // Act
            LlmResponse response = await provider.SendAsync(new LlmRequest
            {
                ModelTier = "Balanced",
                SystemPrompt = "You are helpful",
                UserMessage = "Say hi"
            });

            // Assert
            Assert.That(response.TextContent, Is.EqualTo("hello world"));
            Assert.That(response.InputTokens, Is.EqualTo(10));
            Assert.That(response.OutputTokens, Is.EqualTo(4));
        }

        [Test]
        public async Task SendAsync_ToolUseResponse_ReturnsToolCallDetails()
        {
            // Arrange
            string json = "{\"content\":[{\"type\":\"tool_use\",\"name\":\"build_query_profile\",\"input\":{\"role\":\"Engineer\"}}]}";
            AnthropicProvider provider = new AnthropicProvider(CreateSettings(), new FakeHandler(json));

            // Act
            LlmResponse response = await provider.SendAsync(new LlmRequest
            {
                ModelTier = "Balanced",
                SystemPrompt = "system",
                UserMessage = "input"
            });

            // Assert
            Assert.That(response.ToolCallName, Is.EqualTo("build_query_profile"));
            Assert.That(response.ToolCallArguments, Does.Contain("Engineer"));
        }

        [Test]
        public async Task SendAsync_CacheHit_ReportsCacheReadTokens()
        {
            // Arrange
            string json = "{\"content\":[{\"type\":\"text\",\"text\":\"ok\"}],\"usage\":{\"cache_read_input_tokens\":123,\"cache_creation_input_tokens\":45}}";
            AnthropicProvider provider = new AnthropicProvider(CreateSettings(), new FakeHandler(json));

            // Act
            LlmResponse response = await provider.SendAsync(new LlmRequest
            {
                ModelTier = "Balanced",
                SystemPrompt = "system",
                UserMessage = "input"
            });

            // Assert
            Assert.That(response.CacheReadTokens, Is.EqualTo(123));
            Assert.That(response.CacheWriteTokens, Is.EqualTo(45));
        }

        [Test]
        public async Task SendAsync_EnableCaching_IncludesCacheControlInRequest()
        {
            // Arrange
            FakeCapturingHandler handler = new FakeCapturingHandler("{\"content\":[]}");
            AnthropicProvider provider = new AnthropicProvider(CreateSettings(), handler);

            // Act
            await provider.SendAsync(new LlmRequest
            {
                ModelTier = "Balanced",
                SystemPrompt = "stable system prompt",
                UserMessage = "hello",
                EnableCaching = true
            });

            // Assert
            Assert.That(handler.LastRequestBody, Does.Contain("cache_control"));
            Assert.That(handler.LastRequestBody, Does.Contain("ephemeral"));
        }

        [Test]
        public async Task SendAsync_DisabledCaching_OmitsCacheControl()
        {
            // Arrange
            FakeCapturingHandler handler = new FakeCapturingHandler("{\"content\":[]}");
            AnthropicProvider provider = new AnthropicProvider(CreateSettings(), handler);

            // Act
            await provider.SendAsync(new LlmRequest
            {
                ModelTier = "Balanced",
                SystemPrompt = "stable system prompt",
                UserMessage = "hello",
                EnableCaching = false
            });

            // Assert
            Assert.That(handler.LastRequestBody, Does.Not.Contain("cache_control"));
        }

        private class FakeHandler : HttpMessageHandler
        {
            private readonly string _responseJson;

            public FakeHandler(string responseJson)
            {
                _responseJson = responseJson;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(_responseJson, Encoding.UTF8, "application/json")
                };
                return Task.FromResult(response);
            }
        }

        private class FakeCapturingHandler : HttpMessageHandler
        {
            private readonly string _responseJson;

            public string LastRequestBody { get; private set; }

            public FakeCapturingHandler(string responseJson)
            {
                _responseJson = responseJson;
                LastRequestBody = string.Empty;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequestBody = request.Content == null
                    ? string.Empty
                    : await request.Content.ReadAsStringAsync().ConfigureAwait(false);

                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(_responseJson, Encoding.UTF8, "application/json")
                };
                return response;
            }
        }
    }
}
