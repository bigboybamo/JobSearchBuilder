using JobSearchBuilder.Models;
using JobSearchBuilder.Services;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace JobSearchBuilder.Tests
{
    [TestFixture]
    public class ProfileChipCopierTests
    {
        private SearchProfile _source;
        private SearchProfile _target;

        [SetUp]
        public void SetUp()
        {
            _source = new SearchProfile
            {
                Name = "Source",
                Seniority = "Senior",
                StackKeywords = new List<string> { "C#", ".NET", "Azure" },
                ExcludeKeywords = new List<string> { "clearance", "contract" },
                TimezoneFilters = new List<string> { "UTC+1" },
                SourceGroupIds = new List<int> { 1, 2, 2 }
            };

            _target = new SearchProfile
            {
                Id = 7,
                Name = "Target",
                Seniority = "Mid",
                StackKeywords = new List<string> { "C#", "React" },
                RoleKeywords = new List<string> { "Engineer" },
                ExcludeKeywords = new List<string> { "internship" },
                SourceGroupIds = new List<int> { 2, 3 }
            };
        }

        // -------------------------------------------------------------------
        // Merge mode
        // -------------------------------------------------------------------

        [Test]
        public void CopyChips_MergeMode_AddsMissingTermsOnly()
        {
            ChipCopyResult result = ProfileChipCopier.CopyChips(_source, _target, ChipCategory.Stack, false);

            Assert.That(_target.StackKeywords, Is.EqualTo(new[] { "C#", "React", ".NET", "Azure" }));
            Assert.That(result.GetAdded(ChipCategory.Stack), Is.EqualTo(2));
            Assert.That(result.GetRemoved(ChipCategory.Stack), Is.EqualTo(0));
        }

        [Test]
        public void CopyChips_MergeMode_IgnoresCaseWhenDeduplicating()
        {
            _source.StackKeywords = new List<string> { "c#", " REACT ", "Go" };

            ChipCopyResult result = ProfileChipCopier.CopyChips(_source, _target, ChipCategory.Stack, false);

            Assert.That(_target.StackKeywords, Is.EqualTo(new[] { "C#", "React", "Go" }));
            Assert.That(result.GetAdded(ChipCategory.Stack), Is.EqualTo(1));
        }

        [Test]
        public void CopyChips_MergeMode_NullTargetList_TreatedAsEmpty()
        {
            _target.TimezoneFilters = null;

            ProfileChipCopier.CopyChips(_source, _target, ChipCategory.Timezone, false);

            Assert.That(_target.TimezoneFilters, Is.EqualTo(new[] { "UTC+1" }));
        }

        // -------------------------------------------------------------------
        // Replace mode
        // -------------------------------------------------------------------

        [Test]
        public void CopyChips_ReplaceMode_ClearsSelectedCategoriesOnly()
        {
            ChipCopyResult result = ProfileChipCopier.CopyChips(_source, _target, ChipCategory.Exclude, true);

            Assert.That(_target.ExcludeKeywords, Is.EqualTo(new[] { "clearance", "contract" }));
            Assert.That(_target.StackKeywords, Is.EqualTo(new[] { "C#", "React" }));
            Assert.That(result.GetAdded(ChipCategory.Exclude), Is.EqualTo(2));
            Assert.That(result.GetRemoved(ChipCategory.Exclude), Is.EqualTo(1));
        }

        [Test]
        public void CopyChips_ReplaceMode_EmptySourceCategory_ClearsTarget()
        {
            ChipCopyResult result = ProfileChipCopier.CopyChips(_source, _target, ChipCategory.Role, true);

            Assert.That(_target.RoleKeywords, Is.Empty);
            Assert.That(result.GetRemoved(ChipCategory.Role), Is.EqualTo(1));
            Assert.That(result.HasChanges, Is.True);
        }

        // -------------------------------------------------------------------
        // Category selection
        // -------------------------------------------------------------------

        [Test]
        public void CopyChips_UnselectedCategories_AreUnchanged()
        {
            ProfileChipCopier.CopyChips(_source, _target, ChipCategory.Timezone, true);

            Assert.That(_target.StackKeywords, Is.EqualTo(new[] { "C#", "React" }));
            Assert.That(_target.RoleKeywords, Is.EqualTo(new[] { "Engineer" }));
            Assert.That(_target.ExcludeKeywords, Is.EqualTo(new[] { "internship" }));
            Assert.That(_target.SourceGroupIds, Is.EqualTo(new[] { 2, 3 }));
            Assert.That(_target.Seniority, Is.EqualTo("Mid"));
            Assert.That(_target.Name, Is.EqualTo("Target"));
            Assert.That(_target.Id, Is.EqualTo(7));
        }

        [Test]
        public void CopyChips_NoCategories_ReportsNoChanges()
        {
            ChipCopyResult result = ProfileChipCopier.CopyChips(_source, _target, ChipCategory.None, false);

            Assert.That(result.HasChanges, Is.False);
        }

        [Test]
        public void CopyChips_SourceGroupsSelected_CopiesDistinctIds()
        {
            ChipCopyResult result = ProfileChipCopier.CopyChips(_source, _target, ChipCategory.SourceGroups, false);

            Assert.That(_target.SourceGroupIds, Is.EqualTo(new[] { 2, 3, 1 }));
            Assert.That(result.GetAdded(ChipCategory.SourceGroups), Is.EqualTo(1));
        }

        [Test]
        public void CopyChips_SourceGroupsReplaceMode_UsesSourceIdsOnly()
        {
            ChipCopyResult result = ProfileChipCopier.CopyChips(_source, _target, ChipCategory.SourceGroups, true);

            Assert.That(_target.SourceGroupIds, Is.EqualTo(new[] { 1, 2 }));
            Assert.That(result.GetRemoved(ChipCategory.SourceGroups), Is.EqualTo(1));
        }

        // -------------------------------------------------------------------
        // Seniority
        // -------------------------------------------------------------------

        [TestCase(false)]
        [TestCase(true)]
        public void CopyChips_SenioritySelected_ReplacesInEitherMode(bool replace)
        {
            ChipCopyResult result = ProfileChipCopier.CopyChips(_source, _target, ChipCategory.Seniority, replace);

            Assert.That(_target.Seniority, Is.EqualTo("Senior"));
            Assert.That(result.GetAdded(ChipCategory.Seniority), Is.EqualTo(1));
        }

        [TestCase("Any")]
        [TestCase("")]
        [TestCase(null)]
        public void CopyChips_SourceSeniorityUnset_KeepsTargetSeniority(string sourceSeniority)
        {
            _source.Seniority = sourceSeniority;

            ChipCopyResult result = ProfileChipCopier.CopyChips(_source, _target, ChipCategory.Seniority, true);

            Assert.That(_target.Seniority, Is.EqualTo("Mid"));
            Assert.That(result.HasChanges, Is.False);
        }

        // -------------------------------------------------------------------
        // Argument validation
        // -------------------------------------------------------------------

        [Test]
        public void CopyChips_NullSource_ThrowsArgumentNullException()
        {
            Assert.That(() => ProfileChipCopier.CopyChips(null, _target, ChipCategory.All, false),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void CopyChips_NullTarget_ThrowsArgumentNullException()
        {
            Assert.That(() => ProfileChipCopier.CopyChips(_source, null, ChipCategory.All, false),
                Throws.TypeOf<ArgumentNullException>());
        }

        // -------------------------------------------------------------------
        // NormalizeTerms / ParsePastedTerms
        // -------------------------------------------------------------------

        [Test]
        public void NormalizeTerms_BlankAndDuplicateTerms_AreRemoved()
        {
            List<string> result = ProfileChipCopier.NormalizeTerms(new[] { " C# ", "", null, "c#", "Azure" });

            Assert.That(result, Is.EqualTo(new[] { "C#", "Azure" }));
        }

        [Test]
        public void ParsePastedTerms_MultiLineText_ReturnsOneTermPerLine()
        {
            List<string> result = ProfileChipCopier.ParsePastedTerms("C#\r\n\r\n  .NET  \nAzure\r\nc#");

            Assert.That(result, Is.EqualTo(new[] { "C#", ".NET", "Azure" }));
        }

        [Test]
        public void ParsePastedTerms_QuotedTerms_StripsWrappingQuotes()
        {
            List<string> result = ProfileChipCopier.ParsePastedTerms("\"visa sponsorship\"\n“remote first”\nsay \"hi\" there");

            Assert.That(result, Is.EqualTo(new[] { "visa sponsorship", "remote first", "say \"hi\" there" }));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("  \r\n ")]
        public void ParsePastedTerms_EmptyText_ReturnsEmptyList(string text)
        {
            Assert.That(ProfileChipCopier.ParsePastedTerms(text), Is.Empty);
        }
    }
}
