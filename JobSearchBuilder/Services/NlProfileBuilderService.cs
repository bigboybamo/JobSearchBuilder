using JobSearchBuilder.Interfaces;
using JobSearchBuilder.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JobSearchBuilder.Services
{
    public class NlProfileBuilderService
    {
        private const string ToolName = "build_query_profile";

        private readonly ILlmProvider _provider;
        private readonly PromptLoader _promptLoader;

        public NlProfileBuilderService(ILlmProvider provider, PromptLoader promptLoader)
        {
            if (provider == null) throw new ArgumentNullException("provider");
            if (promptLoader == null) throw new ArgumentNullException("promptLoader");

            _provider = provider;
            _promptLoader = promptLoader;
        }

        public async Task<QueryProfileResult> BuildAsync(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Role description is required.", "description");

            LlmRequest request = new LlmRequest
            {
                SystemPrompt = _promptLoader.Load("nl_profile_builder", "v2"),
                UserMessage = description.Trim(),
                ForceToolName = ToolName,
                ModelTier = "Balanced",
                EnableCaching = true,
                Tools = new List<LlmToolDefinition>
                {
                    new LlmToolDefinition
                    {
                        Name = ToolName,
                        Description = "Builds a structured job search profile from a plain English role description.",
                        InputSchema = GetToolSchema()
                    }
                }
            };

            LlmResponse response = await _provider.SendAsync(request).ConfigureAwait(false);
            if (response == null)
                throw new InvalidOperationException("The LLM provider returned no response.");

            if (!string.Equals(response.ToolCallName, ToolName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The LLM provider did not return the expected profile tool result.");

            return ParseResult(response.ToolCallArguments);
        }

        private static QueryProfileResult ParseResult(string argumentsJson)
        {
            if (string.IsNullOrWhiteSpace(argumentsJson))
                throw new InvalidOperationException("The profile tool result was empty.");

            JObject root = JObject.Parse(argumentsJson);
            QueryProfileResult result = new QueryProfileResult
            {
                Role = ReadString(root, "role", "Role"),
                Seniority = ReadString(root, "seniority", "Seniority"),
                TechStack = ReadStringList(root, "tech_stack", "TechStack"),
                RemoteTerms = ReadStringList(root, "remote_terms", "RemoteTerms"),
                TimezoneTerms = ReadStringList(root, "timezone_terms", "TimezoneTerms"),
                ExcludeTerms = ReadStringList(root, "exclude_terms", "ExcludeTerms")
            };

            return result;
        }

        private static string ReadString(JObject root, string snakeName, string pascalName)
        {
            return (string)root[snakeName] ?? (string)root[pascalName] ?? string.Empty;
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
