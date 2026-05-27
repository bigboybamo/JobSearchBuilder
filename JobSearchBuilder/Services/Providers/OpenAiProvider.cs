using JobSearchBuilder.Interfaces;
using JobSearchBuilder.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace JobSearchBuilder.Services.Providers
{
    public class OpenAiProvider : ILlmProvider
    {
        private readonly AppSettings _settings;
        private readonly HttpClient _httpClient;

        public string ProviderName { get { return "OpenAI"; } }
        public string ModelId { get { return _settings.Ai.GetModelId("OpenAI", "Balanced"); } }

        public OpenAiProvider(AppSettings settings, HttpMessageHandler handler = null)
        {
            if (settings == null) throw new ArgumentNullException("settings");
            _settings = settings;
            _httpClient = handler == null ? new HttpClient() : new HttpClient(handler);
        }

        public async Task<LlmResponse> SendAsync(LlmRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");

            JArray messages = new JArray
            {
                new JObject { ["role"] = "system", ["content"] = request.SystemPrompt ?? string.Empty },
                new JObject { ["role"] = "user", ["content"] = request.UserMessage ?? string.Empty }
            };

            JObject payload = new JObject
            {
                ["model"] = _settings.Ai.GetModelId("OpenAI", request.ModelTier),
                ["messages"] = messages
            };

            if (request.Tools != null && request.Tools.Count > 0)
            {
                JArray tools = new JArray();
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
                        ["type"] = "function",
                        ["function"] = new JObject
                        {
                            ["name"] = tool.Name ?? string.Empty,
                            ["description"] = tool.Description ?? string.Empty,
                            ["parameters"] = schema
                        }
                    });
                }
                payload["tools"] = tools;
            }

            if (!string.IsNullOrWhiteSpace(request.ForceToolName))
            {
                payload["tool_choice"] = new JObject
                {
                    ["type"] = "function",
                    ["function"] = new JObject { ["name"] = request.ForceToolName }
                };
            }

            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
            httpRequest.Headers.Add("Authorization", "Bearer " + _settings.Ai.GetApiKey("OpenAI"));
            httpRequest.Content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.SendAsync(httpRequest).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            string responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            JObject root = JObject.Parse(responseJson);

            LlmResponse mapped = new LlmResponse();
            JObject message = (JObject)root.SelectToken("choices[0].message");
            if (message != null)
            {
                mapped.TextContent = (string)message["content"] ?? string.Empty;

                JObject function = (JObject)message.SelectToken("tool_calls[0].function");
                if (function != null)
                {
                    mapped.ToolCallName = (string)function["name"] ?? string.Empty;
                    mapped.ToolCallArguments = (string)function["arguments"] ?? string.Empty;
                }
            }

            JObject usage = (JObject)root["usage"];
            if (usage != null)
            {
                mapped.InputTokens = (int?)usage["prompt_tokens"] ?? 0;
                mapped.OutputTokens = (int?)usage["completion_tokens"] ?? 0;
            }

            mapped.CacheReadTokens = 0;
            mapped.CacheWriteTokens = 0;
            return mapped;
        }
    }
}
