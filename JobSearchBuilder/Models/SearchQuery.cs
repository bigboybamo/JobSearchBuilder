using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobSearchBuilder.Models
{
    public class SearchQuery
    {
        public int Id { get; set; }
        public int SearchProfileId { get; set; }
        public string QueryText { get; set; }
        public string SourceGroupName { get; set; }
        public DateTime CreatedAt { get; set; }
        public SearchProfile Profile { get; set; }
        public List<JobResult> Results { get; set; }

        public SearchQuery()
        {
            QueryText = string.Empty;
            SourceGroupName = string.Empty;
            CreatedAt = DateTime.UtcNow;
            Results = new List<JobResult>();
        }
    }
}
