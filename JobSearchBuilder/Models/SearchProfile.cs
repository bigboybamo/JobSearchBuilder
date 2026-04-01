using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobSearchBuilder.Models
{
    public class SearchProfile
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Seniority { get; set; }
        public List<string> StackKeywords { get; set; }
        public List<string> RoleKeywords { get; set; }
        public List<string> LocationFilters { get; set; }
        public List<string> VisaFilters { get; set; }
        public List<string> RemoteFilters { get; set; }
        public List<string> TimezoneFilters { get; set; }
        public List<string> ExcludeKeywords { get; set; }
        public List<int> SourceGroupIds { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public SearchProfile()
        {
            Name = string.Empty;
            Seniority = "Any";
            StackKeywords = new List<string>();
            RoleKeywords = new List<string>();
            LocationFilters = new List<string>();
            VisaFilters = new List<string>();
            RemoteFilters = new List<string>();
            TimezoneFilters = new List<string>();
            ExcludeKeywords = new List<string>();
            SourceGroupIds = new List<int>();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public override string ToString() { return Name; }
    }
}
