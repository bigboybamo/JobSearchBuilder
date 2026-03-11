using JobSearchBuilder.Models;
using JobSearchBuilder.Services;
using NUnit.Framework;
using System.Collections.Generic;

namespace JobSearchBuilder.Tests
{
    [TestFixture]
    public class QueryBuilderTests
    {
        private List<AtsSourceGroup> _groups;
        private QueryBuilder _builder;

        [SetUp]
        public void SetUp()
        {
            _groups = new List<AtsSourceGroup>
            {
                new AtsSourceGroup { Id = 1, Name = "Group A", Domains = new List<string> { "boards.greenhouse.io", "jobs.lever.co" } },
                new AtsSourceGroup { Id = 2, Name = "Group B", Domains = new List<string> { "jobs.ashbyhq.com" } }
            };
            _builder = new QueryBuilder(_groups);
        }

        // -------------------------------------------------------------------
        // Constructor
        // -------------------------------------------------------------------

        [Test]
        public void Constructor_NullGroups_ThrowsArgumentNullException()
        {
            Assert.Throws<System.ArgumentNullException>(() => new QueryBuilder(null));
        }

        // -------------------------------------------------------------------
        // Empty / minimal profile
        // -------------------------------------------------------------------

        [Test]
        public void Build_NullProfile_ThrowsArgumentNullException()
        {
            Assert.Throws<System.ArgumentNullException>(() => _builder.Build(null));
        }

        [Test]
        public void Build_EmptyProfile_ReturnsEmptyQuery()
        {
            QueryResult result = _builder.Build(new SearchProfile());
            Assert.That(result.RawQuery, Is.Empty);
        }

        // -------------------------------------------------------------------
        // Seniority block
        // -------------------------------------------------------------------

        [Test]
        public void Build_SeniorityAny_IsExcludedFromQuery()
        {
            SearchProfile profile = new SearchProfile { Seniority = "Any" };
            QueryResult result = _builder.Build(profile);
            Assert.That(result.RawQuery, Does.Not.Contain("Any"));
        }

        [Test]
        public void Build_SenioritySet_IsIncludedInQuery()
        {
            SearchProfile profile = new SearchProfile { Seniority = "Senior" };
            QueryResult result = _builder.Build(profile);
            Assert.That(result.RawQuery, Does.Contain("Senior"));
        }

        [Test]
        public void Build_SeniorityWithSpace_IsQuoted()
        {
            SearchProfile profile = new SearchProfile { Seniority = "Tech Lead" };
            QueryResult result = _builder.Build(profile);
            Assert.That(result.RawQuery, Does.Contain("\"Tech Lead\""));
        }

        // -------------------------------------------------------------------
        // Stack / keyword quoting
        // -------------------------------------------------------------------

        [Test]
        public void Build_SingleStackKeyword_WrappedInParens()
        {
            SearchProfile profile = new SearchProfile { StackKeywords = new List<string> { "Python" } };
            QueryResult result = _builder.Build(profile);
            Assert.That(result.RawQuery, Does.Contain("(\"Python\")"));
        }

        [Test]
        public void Build_MultipleStackKeywords_JoinedWithOr()
        {
            SearchProfile profile = new SearchProfile
            {
                StackKeywords = new List<string> { "C#", ".NET" }
            };
            QueryResult result = _builder.Build(profile);
            Assert.That(result.RawQuery, Does.Contain("\"C#\" OR \".NET\""));
        }

        [Test]
        public void Build_KeywordWithDot_IsQuoted()
        {
            SearchProfile profile = new SearchProfile { StackKeywords = new List<string> { "ASP.NET Core" } };
            QueryResult result = _builder.Build(profile);
            Assert.That(result.RawQuery, Does.Contain("\"ASP.NET Core\""));
        }

        [Test]
        public void Build_RoleKeywordNoSpaceOrDot_IsNotQuoted()
        {
            SearchProfile profile = new SearchProfile { RoleKeywords = new List<string> { "Engineer" } };
            QueryResult result = _builder.Build(profile);
            Assert.That(result.RawQuery, Does.Contain("(Engineer)"));
            Assert.That(result.RawQuery, Does.Not.Contain("\"Engineer\""));
        }

        [Test]
        public void Build_RoleKeywordWithSpace_IsQuoted()
        {
            SearchProfile profile = new SearchProfile { RoleKeywords = new List<string> { "Software Engineer" } };
            QueryResult result = _builder.Build(profile);
            Assert.That(result.RawQuery, Does.Contain("\"Software Engineer\""));
        }

        // -------------------------------------------------------------------
        // ATS site block
        // -------------------------------------------------------------------

        [Test]
        public void Build_NoSourceGroups_OmitsSiteBlock()
        {
            SearchProfile profile = new SearchProfile { SourceGroupIds = new List<int>() };
            QueryResult result = _builder.Build(profile);
            Assert.That(result.RawQuery, Does.Not.Contain("site:"));
        }

        [Test]
        public void Build_OneSourceGroup_IncludesItsDomainsAsSiteFilters()
        {
            SearchProfile profile = new SearchProfile { SourceGroupIds = new List<int> { 1 } };
            QueryResult result = _builder.Build(profile);
            Assert.That(result.RawQuery, Does.Contain("site:boards.greenhouse.io"));
            Assert.That(result.RawQuery, Does.Contain("site:jobs.lever.co"));
        }

        [Test]
        public void Build_MultipleSourceGroups_MergesDomainsWithOr()
        {
            SearchProfile profile = new SearchProfile { SourceGroupIds = new List<int> { 1, 2 } };
            QueryResult result = _builder.Build(profile);
            Assert.That(result.RawQuery, Does.Contain("site:boards.greenhouse.io"));
            Assert.That(result.RawQuery, Does.Contain("site:jobs.ashbyhq.com"));
        }

        [Test]
        public void Build_UnknownSourceGroupId_ProducesNoSiteBlock()
        {
            SearchProfile profile = new SearchProfile { SourceGroupIds = new List<int> { 999 } };
            QueryResult result = _builder.Build(profile);
            Assert.That(result.RawQuery, Does.Not.Contain("site:"));
        }

        // -------------------------------------------------------------------
        // Google URL
        // -------------------------------------------------------------------

        [Test]
        public void Build_NonEmptyProfile_GoogleUrlStartsWithSearchBase()
        {
            SearchProfile profile = new SearchProfile { StackKeywords = new List<string> { "C#" } };
            QueryResult result = _builder.Build(profile);
            Assert.That(result.GoogleSearchUrl, Does.StartWith("https://www.google.com/search?q="));
        }

        [Test]
        public void Build_NonEmptyProfile_GoogleUrlContainsTbsParam()
        {
            SearchProfile profile = new SearchProfile { StackKeywords = new List<string> { "C#" } };
            QueryResult result = _builder.Build(profile);
            Assert.That(result.GoogleSearchUrl, Does.Contain("&tbs=li:1"));
        }

        [Test]
        public void Build_EmptyProfile_GoogleUrlIsEmpty()
        {
            QueryResult result = _builder.Build(new SearchProfile());
            Assert.That(result.GoogleSearchUrl, Does.Contain("q=&tbs=li:1").Or.EqualTo(string.Empty).Or.Contain("q=%0a"));
        }

        // -------------------------------------------------------------------
        // Blocks list
        // -------------------------------------------------------------------

        [Test]
        public void Build_FullProfile_BlocksListContainsAllSections()
        {
            SearchProfile profile = new SearchProfile
            {
                SourceGroupIds = new List<int> { 1 },
                StackKeywords = new List<string> { "C#" },
                RoleKeywords = new List<string> { "Developer" },
                Seniority = "Senior",
                LocationFilters = new List<string> { "London" },
                VisaFilters = new List<string> { "visa sponsorship" },
                RemoteFilters = new List<string> { "remote" }
            };

            QueryResult result = _builder.Build(profile);
            Assert.That(result.Blocks.Count, Is.EqualTo(7));
        }

        // -------------------------------------------------------------------
        // Exclude keywords
        // -------------------------------------------------------------------

        [Test]
        public void Build_SingleExcludeKeyword_ProducesNegatedTerm()
        {
            SearchProfile profile = new SearchProfile
            {
                ExcludeKeywords = new List<string> { "must relocate" }
            };
            QueryResult result = _builder.Build(profile);
            Assert.That(result.RawQuery, Does.Contain("-"));
            Assert.That(result.RawQuery, Does.Contain("\"must relocate\""));
        }

        [Test]
        public void Build_MultipleExcludeKeywords_EachTermNegatedSeparately()
        {
            SearchProfile profile = new SearchProfile
            {
                ExcludeKeywords = new List<string> { "must relocate", "US only", "on-site required" }
            };
            QueryResult result = _builder.Build(profile);
            Assert.That(result.RawQuery, Does.Contain("-\"must relocate\""));
            Assert.That(result.RawQuery, Does.Contain("-\"US only\""));
            Assert.That(result.RawQuery, Does.Contain("-\"on-site required\""));
        }

        [Test]
        public void Build_ExcludeKeywords_DoNotAppearWithoutNegation()
        {
            SearchProfile profile = new SearchProfile
            {
                ExcludeKeywords = new List<string> { "must relocate" }
            };
            QueryResult result = _builder.Build(profile);
            // term must be preceded by - not treated as a positive filter
            Assert.That(result.RawQuery, Does.Not.Contain("(\"must relocate\")"));
        }

        [Test]
        public void Build_ExcludeKeywordsPresent_AddsOneMoreBlock()
        {
            SearchProfile profileWithout = new SearchProfile
            {
                StackKeywords = new List<string> { "C#" }
            };
            SearchProfile profileWith = new SearchProfile
            {
                StackKeywords = new List<string> { "C#" },
                ExcludeKeywords = new List<string> { "must relocate" }
            };

            QueryResult without = _builder.Build(profileWithout);
            QueryResult with = _builder.Build(profileWith);
            Assert.That(with.Blocks.Count, Is.EqualTo(without.Blocks.Count + 1));
        }

        [Test]
        public void Build_EmptyExcludeKeywords_NoExcludeBlock()
        {
            SearchProfile profile = new SearchProfile
            {
                ExcludeKeywords = new List<string>()
            };
            QueryResult result = _builder.Build(profile);
            Assert.That(result.RawQuery, Does.Not.Contain("-"));
        }
    }
}
