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
    public class GeminiProvider : ILlmProvider
    {
        private readonly AppSettings _settings;
        private readonly HttpClient _httpClient;

        public string ProviderName { get { return "Gemini"; } }
        public string ModelId { get { return _settings.Ai.GetModelId("Gemini", "Balanced"); } }

        public GeminiProvider(AppSettings settings, HttpMessageHandler handler = null)
        {
            if (settings == null) throw new ArgumentNullException("settings");
            _settings = settings;
            _httpClient = handler == null ? new HttpClient() : new HttpClient(handler);
        }

        public async Task<LlmResponse> SendAsync(LlmRequest request)
        {
            if (request == null) throw new ArgumentNullException("request");

            JObject payload = new JObject
            {
                ["contents"] = new JArray
                {
                    new JObject
                    {
                        ["parts"] = new JArray
                        {
                            new JObject { ["text"] = request.UserMessage ?? string.Empty }
                        }
                    }
                }
            };

            if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            {
                payload["system_instruction"] = new JObject
                {
                    ["parts"] = new JArray { new JObject { ["text"] = request.SystemPrompt } }
                };
            }

            if (request.Tools != null && request.Tools.Count > 0)
            {
                JArray declarations = new JArray();
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

                    declarations.Add(new JObject
                    {
                        ["name"] = tool.Name ?? string.Empty,
                        ["description"] = tool.Description ?? string.Empty,
                        ["parameters"] = schema
                    });
                }

                payload["tools"] = new JArray
                {
                    new JObject { ["functionDeclarations"] = declarations }
                };
            }

            if (!string.IsNullOrWhiteSpace(request.ForceToolName))
            {
                payload["tool_config"] = new JObject
                {
                    ["function_calling_config"] = new JObject
                    {
                        ["mode"] = "ANY",
                        ["allowed_function_names"] = new JArray { request.ForceToolName }
                    }
                };
            }

            string model = _settings.Ai.GetModelId("Gemini", request.ModelTier);
            string apiKey = _settings.Ai.GetApiKey("Gemini");
            string endpoint = "https://generativelanguage.googleapis.com/v1beta/models/" + model + ":generateContent?key=" + Uri.EscapeDataString(apiKey ?? string.Empty);

            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
            httpRequest.Content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.SendAsync(httpRequest).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            string responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            JObject root = JObject.Parse(responseJson);

            LlmResponse mapped = new LlmResponse();
            JArray parts = (JArray)root.SelectToken("candidates[0].content.parts");
            if (parts != null)
            {
                foreach (JObject part in parts)
                {
                    if (part["text"] != null)
                        mapped.TextContent = (string)part["text"] ?? string.Empty;

                    JObject functionCall = (JObject)part["functionCall"];
                    if (functionCall != null)
                    {
                        mapped.ToolCallName = (string)functionCall["name"] ?? string.Empty;
                        JToken args = functionCall["args"];
                        mapped.ToolCallArguments = args == null ? string.Empty : args.ToString(Formatting.None);
                    }
                }
            }

            JObject usage = (JObject)root["usageMetadata"];
            if (usage != null)
            {
                mapped.InputTokens = (int?)usage["promptTokenCount"] ?? 0;
                mapped.OutputTokens = (int?)usage["candidatesTokenCount"] ?? 0;
            }

            mapped.CacheReadTokens = 0;
            mapped.CacheWriteTokens = 0;
            return mapped;
        }
    }
}
