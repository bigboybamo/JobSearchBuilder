using JobSearchBuilder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace JobSearchBuilder.Services
{
    /// <summary>
    /// Builds a Google search query string from a SearchProfile.
    ///
    /// Output format (each block only emitted if non-empty):
    ///   (site:boards.greenhouse.io OR site:jobs.lever.co OR ...)
    ///   ("C#" OR ".NET" OR "ASP.NET Core")
    ///   (Developer OR Engineer)
    ///   (Senior OR Lead)
    ///   ("United Kingdom" OR UK OR London)
    ///   ("visa sponsorship" OR "tier 2")
    ///   (remote OR hybrid)
    ///   ("UTC+0" OR "CET" OR "GMT")
    /// </summary>
    public class QueryBuilder
    {
        private readonly List<AtsSourceGroup> _allGroups;

        public QueryBuilder(List<AtsSourceGroup> allGroups)
        {
            if (allGroups == null) throw new ArgumentNullException("allGroups");
            _allGroups = allGroups;
        }

        public QueryResult Build(SearchProfile profile)
        {
            if (profile == null) throw new ArgumentNullException("profile");

            var blocks = new List<string>();

            string siteBlock = BuildSiteBlock(profile.SourceGroupIds);
            if (!string.IsNullOrWhiteSpace(siteBlock))
                blocks.Add(siteBlock);

            string stackBlock = BuildTermBlock(profile.StackKeywords, true);
            if (!string.IsNullOrWhiteSpace(stackBlock))
                blocks.Add(stackBlock);

            string roleBlock = BuildTermBlock(profile.RoleKeywords, false);
            if (!string.IsNullOrWhiteSpace(roleBlock))
                blocks.Add(roleBlock);

            if (!string.IsNullOrWhiteSpace(profile.Seniority)
                && !string.Equals(profile.Seniority, "Any", StringComparison.OrdinalIgnoreCase))
            {
                blocks.Add("(" + QuoteIfNeeded(profile.Seniority) + ")");
            }

            string locationBlock = BuildTermBlock(profile.LocationFilters, true);
            if (!string.IsNullOrWhiteSpace(locationBlock))
                blocks.Add(locationBlock);

            string visaBlock = BuildTermBlock(profile.VisaFilters, true);
            if (!string.IsNullOrWhiteSpace(visaBlock))
                blocks.Add(visaBlock);

            string remoteBlock = BuildTermBlock(profile.RemoteFilters, false);
            if (!string.IsNullOrWhiteSpace(remoteBlock))
                blocks.Add(remoteBlock);

            string timezoneBlock = BuildTermBlock(profile.TimezoneFilters, true);
            if (!string.IsNullOrWhiteSpace(timezoneBlock))
                blocks.Add(timezoneBlock);

            string excludeBlock = BuildExcludeBlock(profile.ExcludeKeywords);
            if (!string.IsNullOrWhiteSpace(excludeBlock))
                blocks.Add(excludeBlock);

            string rawQuery = string.Join("\n", blocks);
            string inlineQuery = string.Join(" ", blocks.Select(b => b.Replace("\n", " ")));
            string googleUrl = BuildGoogleUrl(inlineQuery);

            return new QueryResult
            {
                RawQuery = rawQuery,
                InlineQuery = inlineQuery,
                GoogleSearchUrl = googleUrl,
                Blocks = blocks
            };
        }

        private string BuildSiteBlock(List<int> groupIds)
        {
            if (groupIds == null || groupIds.Count == 0)
                return string.Empty;

            List<string> domains = _allGroups
                .Where(g => groupIds.Contains(g.Id))
                .SelectMany(g => g.Domains)
                .Distinct()
                .ToList();

            if (domains.Count == 0) return string.Empty;

            IEnumerable<string> parts = domains.Select(d => "site:" + d);
            return "(\n  " + string.Join("\n  OR ", parts) + "\n)";
        }

        private static string BuildExcludeBlock(List<string> terms)
        {
            if (terms == null || terms.Count == 0) return string.Empty;

            List<string> nonEmpty = terms.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
            if (nonEmpty.Count == 0) return string.Empty;

            // Google requires each excluded phrase as its own -"term" entry.
            // The -(A OR B) group syntax is not reliably respected.
            return string.Join("\n", nonEmpty.Select(t => "-" + Quote(t)));
        }

        private static string BuildTermBlock(List<string> terms, bool quote)
        {
            if (terms == null || terms.Count == 0) return string.Empty;

            List<string> nonEmpty = terms.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
            if (nonEmpty.Count == 0) return string.Empty;

            if (nonEmpty.Count == 1)
            {
                string single = quote ? Quote(nonEmpty[0]) : QuoteIfNeeded(nonEmpty[0]);
                return "(" + single + ")";
            }

            IEnumerable<string> parts = nonEmpty.Select(t => quote ? Quote(t) : QuoteIfNeeded(t));
            return "(" + string.Join(" OR ", parts) + ")";
        }

        private static string Quote(string term)
        {
            return term.StartsWith("\"") ? term : "\"" + term + "\"";
        }

        private static string QuoteIfNeeded(string term)
        {
            return (term.Contains(' ') || term.Contains('.')) ? Quote(term) : term;
        }

        private static string BuildGoogleUrl(string query)
        {
            string encoded = HttpUtility.UrlEncode(query);
            return "https://www.google.com/search?q=" + encoded + "&tbs=li:1";
        }
    }

    public class QueryResult
    {
        public string RawQuery { get; set; }
        public string InlineQuery { get; set; }
        public string GoogleSearchUrl { get; set; }
        public List<string> Blocks { get; set; }

        public QueryResult()
        {
            RawQuery = string.Empty;
            InlineQuery = string.Empty;
            GoogleSearchUrl = string.Empty;
            Blocks = new List<string>();
        }
    }
}
