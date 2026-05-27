namespace JobSearchBuilder.Models
{
    public class LlmToolDefinition
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string InputSchema { get; set; }

        public LlmToolDefinition()
        {
            Name = string.Empty;
            Description = string.Empty;
            InputSchema = "{}";
        }
    }
}
