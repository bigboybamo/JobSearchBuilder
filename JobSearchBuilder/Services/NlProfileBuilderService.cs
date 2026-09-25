using JobSearchBuilder.Interfaces;
using JobSearchBuilder.Models;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JobSearchBuilder.Services
{
    public class NlProfileBuilderService
    {
        private const string ToolName = QueryProfileToolContract.ToolName;

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
                SystemPrompt = _promptLoader.Load("nl_profile_builder", "v4"),
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
                        InputSchema = QueryProfileToolContract.GetToolSchema()
                    }
                }
            };

            LlmResponse response = await _provider.SendAsync(request).ConfigureAwait(false);
            if (response == null)
                throw new InvalidOperationException("The LLM provider returned no response.");

            if (!string.Equals(response.ToolCallName, ToolName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The LLM provider did not return the expected profile tool result.");

            return QueryProfileToolContract.ParseProfile(response.ToolCallArguments);
        }


    }
}
