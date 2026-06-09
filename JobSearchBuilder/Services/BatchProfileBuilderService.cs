using JobSearchBuilder.Interfaces;
using JobSearchBuilder.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace JobSearchBuilder.Services
{
    public class BatchProfileBuilderService
    {
        private const string ToolName = "build_query_profile";
        private const int MaxPollDurationMs = 600000;

        private static readonly HttpClient _sharedHttpClient = new HttpClient();

        private readonly ILlmProvider _provider;
        private readonly PromptLoader _promptLoader;
        private readonly AppSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly int _pollIntervalMs;

        public BatchProfileBuilderService(
            ILlmProvider provider,
            PromptLoader promptLoader,
            AppSettings settings,
            HttpMessageHandler handler = null,
            int pollIntervalMs = 5000)
        {
            if (provider == null) throw new ArgumentNullException("provider");
            if (promptLoader == null) throw new ArgumentNullException("promptLoader");
            if (settings == null) throw new ArgumentNullException("settings");

            _provider = provider;
            _promptLoader = promptLoader;
            _settings = settings;
            _httpClient = handler == null ? _sharedHttpClient : new HttpClient(handler);
            _pollIntervalMs = pollIntervalMs;
        }

        public Task<List<BatchProfileResult>> BuildBatchAsync(IList<string> descriptions)
        {
            if (descriptions == null) throw new ArgumentNullException("descriptions");
            if (descriptions.Count == 0)
                return Task.FromResult(new List<BatchProfileResult>());

            if (string.Equals(_provider.ProviderName, "Anthropic", StringComparison.OrdinalIgnoreCase))
                return BuildWithAnthropicBatchAsync(descriptions);

            return BuildInParallelAsync(descriptions);
        }

        private async Task<List<BatchProfileResult>> BuildInParallelAsync(IList<string> descriptions)
        {
            NlProfileBuilderService service = new NlProfileBuilderService(_provider, _promptLoader);
            List<Task<BatchProfileResult>> tasks = new List<Task<BatchProfileResult>>();

            foreach (string description in descriptions)
                tasks.Add(BuildSingleSafeAsync(service, description));

            BatchProfileResult[] results = await Task.WhenAll(tasks).ConfigureAwait(false);
            return new List<BatchProfileResult>(results);
        }

        private static async Task<BatchProfileResult> BuildSingleSafeAsync(NlProfileBuilderService service, string description)
        {
            try
            {
                QueryProfileResult profile = await service.BuildAsync(description).ConfigureAwait(false);
                return new BatchProfileResult
                {
                    Description = description,
                    Profile = profile,
                    IsError = false
                };
            }
            catch (Exception ex)
            {
                return new BatchProfileResult
                {
                    Description = description,
                    IsError = true,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<List<BatchProfileResult>> BuildWithAnthropicBatchAsync(IList<string> descriptions)
        {
            JObject body = BuildAnthropicBatchRequest(descriptions);
            string submitJson;
            using (HttpRequestMessage submitRequest = CreateAnthropicRequest(HttpMethod.Post, "https://api.anthropic.com/v1/messages/batches"))
            {
                submitRequest.Content = new StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json");
                using (HttpResponseMessage submitResponse = await _httpClient.SendAsync(submitRequest).ConfigureAwait(false))
                {
                    submitResponse.EnsureSuccessStatusCode();
                    submitJson = await submitResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }

            string batchId = (string)JObject.Parse(submitJson)["id"];
            if (string.IsNullOrWhiteSpace(batchId))
                throw new InvalidOperationException("Anthropic batch response did not include an id.");

            DateTime pollDeadline = DateTime.UtcNow.AddMilliseconds(MaxPollDurationMs);
            while (true)
            {
                if (DateTime.UtcNow > pollDeadline)
                    throw new TimeoutException("Anthropic batch did not complete within the allowed time.");

                string pollJson;
                using (HttpRequestMessage pollRequest = CreateAnthropicRequest(HttpMethod.Get, "https://api.anthropic.com/v1/messages/batches/" + batchId))
                using (HttpResponseMessage pollResponse = await _httpClient.SendAsync(pollRequest).ConfigureAwait(false))
                {
                    pollResponse.EnsureSuccessStatusCode();
                    pollJson = await pollResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                }

                string status = (string)JObject.Parse(pollJson)["processing_status"];
                if (string.Equals(status, "ended", StringComparison.OrdinalIgnoreCase))
                    break;

                if (_pollIntervalMs > 0)
                    await Task.Delay(_pollIntervalMs).ConfigureAwait(false);
            }

            string jsonlContent;
            using (HttpRequestMessage resultsRequest = CreateAnthropicRequest(HttpMethod.Get, "https://api.anthropic.com/v1/messages/batches/" + batchId + "/results"))
            using (HttpResponseMessage resultsResponse = await _httpClient.SendAsync(resultsRequest).ConfigureAwait(false))
            {
                resultsResponse.EnsureSuccessStatusCode();
                jsonlContent = await resultsResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            }

            return ParseBatchResults(descriptions, jsonlContent);
        }

        private JObject BuildAnthropicBatchRequest(IList<string> descriptions)
        {
            string prompt = _promptLoader.Load("nl_profile_builder", "v2");
            JObject schema = JObject.Parse(GetToolSchema());
            JArray requests = new JArray();

            for (int i = 0; i < descriptions.Count; i++)
            {
                requests.Add(new JObject
                {
                    ["custom_id"] = i.ToString(),
                    ["params"] = new JObject
                    {
                        ["model"] = _settings.Ai.GetModelId("Anthropic", "Balanced"),
                        ["max_tokens"] = 2048,
                        ["system"] = new JArray
                        {
                            new JObject
                            {
                                ["type"] = "text",
                                ["text"] = prompt,
                                ["cache_control"] = new JObject { ["type"] = "ephemeral" }
                            }
                        },
                        ["messages"] = new JArray
                        {
                            new JObject
                            {
                                ["role"] = "user",
                                ["content"] = descriptions[i]
                            }
                        },
                        ["tools"] = new JArray
                        {
                            new JObject
                            {
                                ["name"] = ToolName,
                                ["description"] = "Builds a structured job search profile from a plain English role description.",
                                ["input_schema"] = schema
                            }
                        },
                        ["tool_choice"] = new JObject
                        {
                            ["type"] = "tool",
                            ["name"] = ToolName
                        }
                    }
                });
            }

            return new JObject { ["requests"] = requests };
        }

        private HttpRequestMessage CreateAnthropicRequest(HttpMethod method, string url)
        {
            HttpRequestMessage request = new HttpRequestMessage(method, url);
            request.Headers.Add("x-api-key", _settings.Ai.GetApiKey("Anthropic"));
            request.Headers.Add("anthropic-version", "2023-06-01");
            request.Headers.Add("anthropic-beta", "message-batches-2024-09-24");
            return request;
        }

        private static List<BatchProfileResult> ParseBatchResults(IList<string> descriptions, string jsonlContent)
        {
            BatchProfileResult[] results = new BatchProfileResult[descriptions.Count];
            string[] lines = (jsonlContent ?? string.Empty).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                JObject root;
                try
                {
                    root = JObject.Parse(line);
                }
                catch
                {
                    continue;
                }

                int index;
                if (!int.TryParse((string)root["custom_id"], out index) || index < 0 || index >= descriptions.Count)
                    continue;

                JObject result = (JObject)root["result"];
                string type = (string)result?["type"];
                if (string.Equals(type, "succeeded", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        results[index] = ParseSucceededResult(descriptions[index], result);
                    }
                    catch (Exception ex)
                    {
                        results[index] = new BatchProfileResult
                        {
                            Description = descriptions[index],
                            IsError = true,
                            ErrorMessage = ex.Message
                        };
                    }
                }
                else if (string.Equals(type, "errored", StringComparison.OrdinalIgnoreCase))
                {
                    results[index] = new BatchProfileResult
                    {
                        Description = descriptions[index],
                        IsError = true,
                        ErrorMessage = (string)result.SelectToken("error.message") ?? "Batch request failed."
                    };
                }
            }

            List<BatchProfileResult> ordered = new List<BatchProfileResult>();
            for (int i = 0; i < descriptions.Count; i++)
            {
                ordered.Add(results[i] ?? new BatchProfileResult
                {
                    Description = descriptions[i],
                    IsError = true,
                    ErrorMessage = "No result returned."
                });
            }

            return ordered;
        }

        private static BatchProfileResult ParseSucceededResult(string description, JObject result)
        {
            JArray content = (JArray)result.SelectToken("message.content");
            if (content != null)
            {
                foreach (JToken token in content)
                {
                    JObject block = token as JObject;
                    if (block == null) continue;
                    if (string.Equals((string)block["type"], "tool_use", StringComparison.OrdinalIgnoreCase))
                    {
                        JToken input = block["input"];
                        return new BatchProfileResult
                        {
                            Description = description,
                            Profile = ParseProfile(input == null ? string.Empty : input.ToString(Formatting.None)),
                            IsError = false
                        };
                    }
                }
            }

            return new BatchProfileResult
            {
                Description = description,
                IsError = true,
                ErrorMessage = "No profile tool result returned."
            };
        }

        private static QueryProfileResult ParseProfile(string argumentsJson)
        {
            if (string.IsNullOrWhiteSpace(argumentsJson))
                throw new InvalidOperationException("The profile tool result was empty.");

            JObject root = JObject.Parse(argumentsJson);
            return new QueryProfileResult
            {
                Role = (string)root["role"] ?? (string)root["Role"] ?? string.Empty,
                Seniority = (string)root["seniority"] ?? (string)root["Seniority"] ?? string.Empty,
                TechStack = ReadStringList(root, "tech_stack", "TechStack"),
                RemoteTerms = ReadStringList(root, "remote_terms", "RemoteTerms"),
                TimezoneTerms = ReadStringList(root, "timezone_terms", "TimezoneTerms"),
                ExcludeTerms = ReadStringList(root, "exclude_terms", "ExcludeTerms")
            };
        }

        private static List<string> ReadStringList(JObject root, string snakeName, string pascalName)
        {
            List<string> values = new List<string>();
            JArray array = root[snakeName] as JArray ?? root[pascalName] as JArray;
            if (array == null)
                return values;

            foreach (JToken token in array)
            {
                string value = ((string)token ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(value))
                    values.Add(value);
            }

            return values;
        }

        private static string GetToolSchema()
        {
            return @"{
  ""type"": ""object"",
  ""properties"": {
    ""role"": { ""type"": ""string"" },
    ""seniority"": { ""type"": ""string"" },
    ""tech_stack"": { ""type"": ""array"", ""items"": { ""type"": ""string"" } },
    ""remote_terms"": { ""type"": ""array"", ""items"": { ""type"": ""string"" } },
    ""timezone_terms"": { ""type"": ""array"", ""items"": { ""type"": ""string"" } },
    ""exclude_terms"": { ""type"": ""array"", ""items"": { ""type"": ""string"" } }
  },
  ""required"": [""role"", ""seniority"", ""tech_stack"", ""remote_terms"", ""timezone_terms"", ""exclude_terms""],
  ""additionalProperties"": false
}";
        }
    }
}
