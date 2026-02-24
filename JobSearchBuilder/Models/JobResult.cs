using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobSearchBuilder.Models
{
    public class JobResult
    {
        public int Id { get; set; }
        public int SearchQueryId { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
        public string Company { get; set; }
        public string Location { get; set; }
        public string RemoteNotes { get; set; }
        public DateTime? PostedDate { get; set; }
        public DateTime FirstSeenAt { get; set; }
        public DateTime LastSeenAt { get; set; }
        public double MatchScore { get; set; }
        public SearchQuery Query { get; set; }

        public JobResult()
        {
            Url = string.Empty;
            Title = string.Empty;
            Company = string.Empty;
            Location = string.Empty;
            RemoteNotes = string.Empty;
            FirstSeenAt = DateTime.UtcNow;
            LastSeenAt = DateTime.UtcNow;
        }
    }
}
