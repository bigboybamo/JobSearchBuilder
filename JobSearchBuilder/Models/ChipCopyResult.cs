using System.Collections.Generic;
using System.Linq;

namespace JobSearchBuilder.Models
{
    /// <summary>
    /// Per-category counts of what a ProfileChipCopier.CopyChips call changed.
    /// Only single-flag categories are used as keys.
    /// </summary>
    public class ChipCopyResult
    {
        public Dictionary<ChipCategory, int> Added { get; private set; }
        public Dictionary<ChipCategory, int> Removed { get; private set; }

        public ChipCopyResult()
        {
            Added = new Dictionary<ChipCategory, int>();
            Removed = new Dictionary<ChipCategory, int>();
        }

        public bool HasChanges
        {
            get { return Added.Values.Any(v => v > 0) || Removed.Values.Any(v => v > 0); }
        }

        public int GetAdded(ChipCategory category)
        {
            int count;
            return Added.TryGetValue(category, out count) ? count : 0;
        }

        public int GetRemoved(ChipCategory category)
        {
            int count;
            return Removed.TryGetValue(category, out count) ? count : 0;
        }
    }
}
