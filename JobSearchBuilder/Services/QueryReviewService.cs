using JobSearchBuilder.Interfaces;
using JobSearchBuilder.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JobSearchBuilder.Services
{
    public class QueryReviewService
    {
        private readonly ILlmProvider _provider;
        private readonly PromptLoader _promptLoader;

        public QueryReviewService(ILlmProvider provider, PromptLoader promptLoader)
        {
            if (provider == null) throw new ArgumentNullException("provider");
            if (promptLoader == null) throw new ArgumentNullException("promptLoader");

            _provider = provider;
            _promptLoader = promptLoader;
        }

        public async Task<QueryReviewResult> ReviewAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("Query is required.", "query");

            try
            {
                LlmRequest request = new LlmRequest
                {
                    SystemPrompt = _promptLoader.Load("query_review", "v1"),
                    UserMessage = query.Trim(),
                    ModelTier = "Balanced",
                    EnableCaching = true
                };

                LlmResponse response = await _provider.SendAsync(request).ConfigureAwait(false);
                if (response == null)
                    return CreateFailedResult("The AI provider returned no response.");

                return ParseResult(response.TextContent);
            }
            catch (Exception ex)
            {
                return CreateFailedResult(ex.Message);
            }
        }

        private static QueryReviewResult ParseResult(string textContent)
        {
            string json = StripMarkdownFence(textContent);
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidOperationException("The AI provider returned an empty review response.");

            JObject root = JObject.Parse(json);
            return new QueryReviewResult
            {
                Issues = ReadStringList(root, "issues", "Issues"),
                Suggestions = ReadStringList(root, "suggestions", "Suggestions")
            };
        }

        private static QueryReviewResult CreateFailedResult(string errorMessage)
        {
            return new QueryReviewResult
            {
                Failed = true,
                ErrorMessage = string.IsNullOrWhiteSpace(errorMessage)
                    ? "The query review could not be completed."
                    : errorMessage
            };
        }

        private static List<string> ReadStringList(JObject root, string camelName, string pascalName)
        {
            List<string> values = new List<string>();
            JArray array = root[camelName] as JArray ?? root[pascalName] as JArray;
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

        private static string StripMarkdownFence(string text)
        {
            string trimmed = (text ?? string.Empty).Trim();
            if (!trimmed.StartsWith("```"))
                return trimmed;

            int newline = trimmed.IndexOf('\n');
            if (newline >= 0)
                trimmed = trimmed.Substring(newline + 1).TrimStart('\r');

            if (trimmed.EndsWith("```"))
                trimmed = trimmed.Substring(0, trimmed.Length - 3).TrimEnd();

            return trimmed;
        }
    }
}
