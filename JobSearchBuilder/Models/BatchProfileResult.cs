namespace JobSearchBuilder.Models
{
    public class BatchProfileResult
    {
        public string Description { get; set; }
        public QueryProfileResult Profile { get; set; }
        public bool IsError { get; set; }
        public string ErrorMessage { get; set; }

        public BatchProfileResult()
        {
            Description = string.Empty;
            ErrorMessage = string.Empty;
        }
    }
}
