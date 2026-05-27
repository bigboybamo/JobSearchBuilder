namespace JobSearchBuilder.Models
{
    public class LlmResponse
    {
        public string TextContent { get; set; }
        public string ToolCallName { get; set; }
        public string ToolCallArguments { get; set; }
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public int CacheReadTokens { get; set; }
        public int CacheWriteTokens { get; set; }

        public LlmResponse()
        {
            TextContent = string.Empty;
            ToolCallName = string.Empty;
            ToolCallArguments = string.Empty;
        }
    }
}
