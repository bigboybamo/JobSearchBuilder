using System.Collections.Generic;

namespace JobSearchBuilder.Models
{
    public class LlmRequest
    {
        public string SystemPrompt { get; set; }
        public string UserMessage { get; set; }
        public List<LlmToolDefinition> Tools { get; set; }
        public string ForceToolName { get; set; }
        public string ModelTier { get; set; }
        public bool EnableCaching { get; set; }

        public LlmRequest()
        {
            SystemPrompt = string.Empty;
            UserMessage = string.Empty;
            Tools = new List<LlmToolDefinition>();
            ForceToolName = string.Empty;
            ModelTier = "Balanced";
            EnableCaching = false;
        }
    }
}
