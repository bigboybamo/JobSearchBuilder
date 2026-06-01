using JobSearchBuilder.Interfaces;
using JobSearchBuilder.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JobSearchBuilder.Services
{
    public class QuerySuggestionService
    {
        private const string ToolName = "suggest_keywords";

        private readonly ILlmProvider _provider;
        private readonly PromptLoader _promptLoader;

        public QuerySuggestionService(ILlmProvider provider, PromptLoader promptLoader)
        {
            if (provider == null) throw new ArgumentNullException("provider");
            if (promptLoader == null) throw new ArgumentNullException("promptLoader");

            _provider = provider;
            _promptLoader = promptLoader;
        }

        public async Task<List<string>> SuggestAsync(string category, List<string> existingKeywords, string partialInput)
        {
            if (string.IsNullOrWhiteSpace(category))
                throw new ArgumentException("Suggestion category is required.", "category");

            try
            {
                LlmRequest request = new LlmRequest
                {
                    SystemPrompt = _promptLoader.Load("query_suggestions", "v1"),
                    UserMessage = BuildUserMessage(category, existingKeywords, partialInput),
                    ForceToolName = ToolName,
                    ModelTier = "Balanced",
                    EnableCaching = true,
                    Tools = new List<LlmToolDefinition>
                    {
                        new LlmToolDefinition
                        {
                            Name = ToolName,
                            Description = "Suggests up to five concise keywords for a job search query category.",
                            InputSchema = GetToolSchema()
                        }
                    }
                };

                LlmResponse response = await _provider.SendAsync(request).ConfigureAwait(false);
                if (response == null ||
                    !string.Equals(response.ToolCallName, ToolName, StringComparison.OrdinalIgnoreCase))
                {
                    return new List<string>();
                }

                return ParseSuggestions(response.ToolCallArguments);
            }
            catch
            {
                return new List<string>();
            }
        }

        private static string BuildUserMessage(string category, List<string> existingKeywords, string partialInput)
        {
            string existing = existingKeywords == null || existingKeywords.Count == 0
                ? "(none)"
                : string.Join(", ", existingKeywords.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));

            return "Category: " + category.Trim() + "\n" +
                   "Already added: " + existing + "\n" +
                   "Partial input: " + ((partialInput ?? string.Empty).Trim());
        }

        private static List<string> ParseSuggestions(string argumentsJson)
        {
            List<string> suggestions = new List<string>();
            if (string.IsNullOrWhiteSpace(argumentsJson))
                return suggestions;

            JObject root = JObject.Parse(argumentsJson);
            JArray array = root["suggestions"] as JArray ?? root["Suggestions"] as JArray;
            if (array == null)
                return suggestions;

            foreach (JToken token in array)
            {
                string value = ((string)token ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(value) &&
                    !suggestions.Any(x => string.Equals(x, value, StringComparison.OrdinalIgnoreCase)))
                {
                    suggestions.Add(value);
                }
            }

            return suggestions.Take(5).ToList();
        }

        private static string GetToolSchema()
        {
            return @"{
  ""type"": ""object"",
  ""properties"": {
    ""suggestions"": {
      ""type"": ""array"",
      ""items"": { ""type"": ""string"" },
      ""maxItems"": 5
    }
  },
  ""required"": [""suggestions""],
  ""additionalProperties"": false
}";
        }
    }
}
