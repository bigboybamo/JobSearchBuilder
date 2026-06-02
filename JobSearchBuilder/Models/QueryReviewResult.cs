using System.Collections.Generic;

namespace JobSearchBuilder.Models
{
    public class QueryReviewResult
    {
        public List<string> Issues { get; set; }
        public List<string> Suggestions { get; set; }

        public QueryReviewResult()
        {
            Issues = new List<string>();
            Suggestions = new List<string>();
        }
    }
}
