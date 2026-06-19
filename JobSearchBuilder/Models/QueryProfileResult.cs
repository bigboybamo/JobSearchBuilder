using System.Collections.Generic;

namespace JobSearchBuilder.Models
{
    public class QueryProfileResult
    {
        public string Role { get; set; }
        public string Seniority { get; set; }
        public List<string> TechStack { get; set; }
        public List<string> Locations { get; set; }
        public List<string> VisaTerms { get; set; }
        public List<string> RemoteTerms { get; set; }
        public List<string> TimezoneTerms { get; set; }
        public List<string> ExcludeTerms { get; set; }

        public QueryProfileResult()
        {
            Role = string.Empty;
            Seniority = string.Empty;
            TechStack = new List<string>();
            Locations = new List<string>();
            VisaTerms = new List<string>();
            RemoteTerms = new List<string>();
            TimezoneTerms = new List<string>();
            ExcludeTerms = new List<string>();
        }
    }
}
