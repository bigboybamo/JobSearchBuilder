using JobSearchBuilder.Interfaces;
using JobSearchBuilder.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace JobSearchBuilder.Services.Providers
{
    public class AnthropicProvider : ILlmProvider
    {
        private readonly AppSettings _settings;
        private readonly HttpClient _httpClient;

        public string ProviderName { get { return "Anthropic"; } }
        public string ModelId { get { return _settings.Ai.GetModelId("Anthropic", "Balanced"); } }

        public AnthropicProvider(AppSettings settings, HttpMessageHandler handler = null)
        {
            if (settings == null) throw new ArgumentNullException("settings");
            _settings = settings;
            _httpClient = handler == null ? new HttpClient() : new HttpClient(handler);
        }

        public async Task<LlmResponse> SendAsync(LlmRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");

            JObject systemBlock = new JObject
            {
                ["type"] = "text",
                ["text"] = request.SystemPrompt ?? string.Empty
            };

            if (request.EnableCaching)
                systemBlock["cache_control"] = new JObject { ["type"] = "ephemeral" };

            JArray tools = new JArray();
            if (request.Tools != null)
            {
                foreach (LlmToolDefinition tool in request.Tools)
                {
                    JObject schema;
                    try
                    {
                        schema = JObject.Parse(tool.InputSchema ?? "{}");
                    }
                    catch
                    {
                        schema = new JObject();
                    }

                    tools.Add(new JObject
                    {
                        ["name"] = tool.Name ?? string.Empty,
                        ["description"] = tool.Description ?? string.Empty,
                        ["input_schema"] = schema
                    });
                }
            }

            JObject payload = new JObject
            {
                ["model"] = _settings.Ai.GetModelId("Anthropic", request.ModelTier),
                ["max_tokens"] = 2048,
                ["system"] = new JArray { systemBlock },
                ["messages"] = new JArray
                {
                    new JObject
                    {
                        ["role"] = "user",
                        ["content"] = request.UserMessage ?? string.Empty
                    }
                }
            };

            if (tools.Count > 0)
                payload["tools"] = tools;

            if (!string.IsNullOrWhiteSpace(request.ForceToolName))
            {
                payload["tool_choice"] = new JObject
                {
                    ["type"] = "tool",
                    ["name"] = request.ForceToolName
                };
            }

            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            httpRequest.Headers.Add("x-api-key", _settings.Ai.GetApiKey("Anthropic"));
            httpRequest.Headers.Add("anthropic-version", "2023-06-01");
            if (request.EnableCaching)
                httpRequest.Headers.Add("anthropic-beta", "prompt-caching-2024-07-31");
            httpRequest.Content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.SendAsync(httpRequest).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            string responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            JObject root = JObject.Parse(responseJson);

            LlmResponse mapped = new LlmResponse();
            JArray content = (JArray)root["content"];
            if (content != null)
            {
                foreach (JObject block in content)
                {
                    string type = (string)block["type"];
                    if (type == "text")
                        mapped.TextContent = (string)block["text"] ?? string.Empty;

                    if (type == "tool_use")
                    {
                        mapped.ToolCallName = (string)block["name"] ?? string.Empty;
                        JToken input = block["input"];
                        mapped.ToolCallArguments = input == null ? string.Empty : input.ToString(Formatting.None);
                    }
                }
            }

            JObject usage = (JObject)root["usage"];
            if (usage != null)
            {
                mapped.InputTokens = (int?)usage["input_tokens"] ?? 0;
                mapped.OutputTokens = (int?)usage["output_tokens"] ?? 0;
                mapped.CacheReadTokens = (int?)usage["cache_read_input_tokens"] ?? 0;
                mapped.CacheWriteTokens = (int?)usage["cache_creation_input_tokens"] ?? 0;
            }

            if (mapped.CacheReadTokens > 0)
                Debug.WriteLine("Anthropic cache hit tokens: " + mapped.CacheReadTokens);

            return mapped;
        }
    }
}
