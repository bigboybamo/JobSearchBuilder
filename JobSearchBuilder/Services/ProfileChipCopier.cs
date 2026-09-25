using JobSearchBuilder.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JobSearchBuilder.Services
{
    /// <summary>
    /// Copies chips between SearchProfile instances. Pure logic — no UI or I/O —
    /// so the editor can apply it to the profile read from the UI and the
    /// copy dialog can run it on a throwaway profile to preview the result.
    /// </summary>
    public static class ProfileChipCopier
    {
        private static readonly char[] QuoteChars = { '"', '“', '”' };

        /// <summary>
        /// Copies the selected categories from <paramref name="source"/> into
        /// <paramref name="target"/>. Merge mode appends terms the target does not
        /// already have (case-insensitive); replace mode swaps each selected
        /// category for the source's values. Seniority is a single value, so it is
        /// always replaced — unless the source has none or has "Any".
        /// </summary>
        public static ChipCopyResult CopyChips(SearchProfile source, SearchProfile target,
                                               ChipCategory categories, bool replace)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (target == null) throw new ArgumentNullException("target");

            ChipCopyResult result = new ChipCopyResult();

            target.StackKeywords   = CopyTerms(ChipCategory.Stack,    source.StackKeywords,   target.StackKeywords,   categories, replace, result);
            target.RoleKeywords    = CopyTerms(ChipCategory.Role,     source.RoleKeywords,    target.RoleKeywords,    categories, replace, result);
            target.LocationFilters = CopyTerms(ChipCategory.Location, source.LocationFilters, target.LocationFilters, categories, replace, result);
            target.VisaFilters     = CopyTerms(ChipCategory.Visa,     source.VisaFilters,     target.VisaFilters,     categories, replace, result);
            target.RemoteFilters   = CopyTerms(ChipCategory.Remote,   source.RemoteFilters,   target.RemoteFilters,   categories, replace, result);
            target.TimezoneFilters = CopyTerms(ChipCategory.Timezone, source.TimezoneFilters, target.TimezoneFilters, categories, replace, result);
            target.ExcludeKeywords = CopyTerms(ChipCategory.Exclude,  source.ExcludeKeywords, target.ExcludeKeywords, categories, replace, result);

            if ((categories & ChipCategory.SourceGroups) != 0)
            {
                List<int> before = (target.SourceGroupIds ?? new List<int>()).Distinct().ToList();
                IEnumerable<int> incoming = source.SourceGroupIds ?? Enumerable.Empty<int>();
                List<int> after = (replace ? incoming : before.Concat(incoming)).Distinct().ToList();

                target.SourceGroupIds = after;
                result.Added[ChipCategory.SourceGroups] = after.Count(id => !before.Contains(id));
                result.Removed[ChipCategory.SourceGroups] = before.Count(id => !after.Contains(id));
            }

            if ((categories & ChipCategory.Seniority) != 0)
            {
                bool hasSeniority = !string.IsNullOrWhiteSpace(source.Seniority) &&
                                    !string.Equals(source.Seniority, "Any", StringComparison.OrdinalIgnoreCase);
                bool changed = hasSeniority &&
                               !string.Equals(source.Seniority, target.Seniority, StringComparison.OrdinalIgnoreCase);

                if (changed)
                    target.Seniority = source.Seniority;

                result.Added[ChipCategory.Seniority] = changed ? 1 : 0;
            }

            return result;
        }

        /// <summary>
        /// Trims terms, drops blanks and removes case-insensitive duplicates,
        /// keeping the first occurrence of each term.
        /// </summary>
        public static List<string> NormalizeTerms(IEnumerable<string> terms)
        {
            List<string> result = new List<string>();
            if (terms == null)
                return result;

            foreach (string term in terms)
            {
                string value = (term ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(value) && !ContainsTerm(result, value))
                    result.Add(value);
            }

            return result;
        }

        /// <summary>
        /// Splits pasted text into chip terms: one term per line, blank lines
        /// ignored, and quotes wrapping a whole term removed.
        /// </summary>
        public static List<string> ParsePastedTerms(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            IEnumerable<string> lines = text
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim().Trim(QuoteChars));

            return NormalizeTerms(lines);
        }

        private static List<string> CopyTerms(ChipCategory category, List<string> source, List<string> target,
                                              ChipCategory categories, bool replace, ChipCopyResult result)
        {
            if ((categories & category) == 0)
                return target;

            List<string> before = NormalizeTerms(target);
            IEnumerable<string> incoming = source ?? Enumerable.Empty<string>();
            List<string> after = NormalizeTerms(replace ? incoming : before.Concat(incoming));

            result.Added[category] = after.Count(t => !ContainsTerm(before, t));
            result.Removed[category] = before.Count(t => !ContainsTerm(after, t));
            return after;
        }

        private static bool ContainsTerm(IEnumerable<string> terms, string term)
        {
            return terms.Any(x => string.Equals(x, term, StringComparison.OrdinalIgnoreCase));
        }
    }
}
