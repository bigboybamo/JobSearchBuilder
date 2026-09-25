using System;

namespace JobSearchBuilder.Models
{
    /// <summary>
    /// The parts of a SearchProfile that can be copied from one profile to another.
    /// Values are powers of two so several categories can be combined into one mask.
    /// </summary>
    [Flags]
    public enum ChipCategory
    {
        None         = 0,
        Stack        = 1,
        Role         = 2,
        Location     = 4,
        Visa         = 8,
        Remote       = 16,
        Timezone     = 32,
        Exclude      = 64,
        SourceGroups = 128,
        Seniority    = 256,

        AllKeywords = Stack | Role | Location | Visa | Remote | Timezone | Exclude,
        All         = AllKeywords | SourceGroups | Seniority
    }
}
