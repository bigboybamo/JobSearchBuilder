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
            // Arrange
            List<AtsSourceGroup> groups = null;

            // Act
            TestDelegate action = () => new QueryBuilder(groups);

            // Assert
            Assert.Throws<System.ArgumentNullException>(action);
        }

        // -------------------------------------------------------------------
        // Empty / minimal profile
        // -------------------------------------------------------------------

        [Test]
        public void Build_NullProfile_ThrowsArgumentNullException()
        {
            // Arrange
            SearchProfile profile = null;

            // Act
            TestDelegate action = () => _builder.Build(profile);

            // Assert
            Assert.Throws<System.ArgumentNullException>(action);
        }

        [Test]
        public void Build_EmptyProfile_ReturnsEmptyQuery()
        {
            // Arrange
            SearchProfile profile = new SearchProfile();

            // Act
            QueryResult result = _builder.Build(profile);

            // Assert
            Assert.That(result.RawQuery, Is.Empty);
        }

        // -------------------------------------------------------------------
        // Seniority block
        // -------------------------------------------------------------------

        [Test]
        public void Build_SeniorityAny_IsExcludedFromQuery()
        {
            // Arrange
            SearchProfile profile = new SearchProfile { Seniority = "Any" };

            // Act
            QueryResult result = _builder.Build(profile);

            // Assert
            Assert.That(result.RawQuery, Does.Not.Contain("Any"));
        }

        [Test]
        public void Build_SenioritySet_IsIncludedInQuery()
        {
            // Arrange
            SearchProfile profile = new SearchProfile { Seniority = "Senior" };

            // Act
            QueryResult result = _builder.Build(profile);

            // Assert
            Assert.That(result.RawQuery, Does.Contain("Senior"));
        }

        [Test]
        public void Build_SeniorityWithSpace_IsQuoted()
        {
            // Arrange
            SearchProfile profile = new SearchProfile { Seniority = "Tech Lead" };

            // Act
            QueryResult result = _builder.Build(profile);

            // Assert
            Assert.That(result.RawQuery, Does.Contain("\"Tech Lead\""));
        }

        // -------------------------------------------------------------------
        // Stack / keyword quoting
        // -------------------------------------------------------------------

        [Test]
        public void Build_SingleStackKeyword_WrappedInParens()
        {
            // Arrange
            SearchProfile profile = new SearchProfile { StackKeywords = new List<string> { "Python" } };

            // Act
            QueryResult result = _builder.Build(profile);

            // Assert
            Assert.That(result.RawQuery, Does.Contain("(\"Python\")"));
        }

        [Test]
        public void Build_MultipleStackKeywords_JoinedWithOr()
        {
            // Arrange
            SearchProfile profile = new SearchProfile
            {
                StackKeywords = new List<string> { "C#", ".NET" }
            };

            // Act
            QueryResult result = _builder.Build(profile);

            // Assert
            Assert.That(result.RawQuery, Does.Contain("\"C#\" OR \".NET\""));
        }

        [Test]
        public void Build_KeywordWithDot_IsQuoted()
        {
            // Arrange
            SearchProfile profile = new SearchProfile { StackKeywords = new List<string> { "ASP.NET Core" } };

            // Act
            QueryResult result = _builder.Build(profile);

            // Assert
            Assert.That(result.RawQuery, Does.Contain("\"ASP.NET Core\""));
        }

        [Test]
        public void Build_RoleKeywordNoSpaceOrDot_IsNotQuoted()
        {
            // Arrange
            SearchProfile profile = new SearchProfile { RoleKeywords = new List<string> { "Engineer" } };

            // Act
            QueryResult result = _builder.Build(profile);

            // Assert
            Assert.That(result.RawQuery, Does.Contain("(Engineer)"));
            Assert.That(result.RawQuery, Does.Not.Contain("\"Engineer\""));
        }

        [Test]
        public void Build_RoleKeywordWithSpace_IsQuoted()
        {
            // Arrange
            SearchProfile profile = new SearchProfile { RoleKeywords = new List<string> { "Software Engineer" } };

            // Act
            QueryResult result = _builder.Build(profile);

            // Assert
            Assert.That(result.RawQuery, Does.Contain("\"Software Engineer\""));
        }

        [TestCase("C++")]
        [TestCase("full-stack")]
        [TestCase("OR")]
        [TestCase("Developer(C#)")]
        [TestCase("Node.js")]
        [TestCase("A OR B")]
        public void Build_RoleKeywordContainingGoogleSyntax_IsQuoted(string term)
        {
            // Arrange
            SearchProfile profile = new SearchProfile { RoleKeywords = new List<string> { term } };

            // Act
            QueryResult result = _builder.Build(profile);

            // Assert
            Assert.That(result.RawQuery, Does.Contain("(\"" + term + "\")"));
        }

        [TestCase("\"Engineer")]
        [TestCase("Engineer\"")]
        public void Build_RoleKeywordWithStrayQuote_IsNormalized(string term)
        {
            // Arrange
            SearchProfile profile = new SearchProfile { RoleKeywords = new List<string> { term } };

            // Act
            QueryResult result = _builder.Build(profile);

            // Assert
            Assert.That(result.RawQuery, Does.Contain("(\"Engineer\")"));
        }

        [Test]
        public void Build_StackKeywordWithEmbeddedQuotes_EscapesQuotes()
        {
            // Arrange
            SearchProfile profile = new SearchProfile
            {
                StackKeywords = new List<string> { "Senior \"C#\" Developer" }
            };

            // Act
            QueryResult result = _builder.Build(profile);

            // Assert
            Assert.That(result.RawQuery, Does.Contain("(\"Senior \\\"C#\\\" Developer\")"));
        }

        // -------------------------------------------------------------------
        // ATS site block
        // -------------------------------------------------------------------

        [Test]
        public void Build_NoSourceGroups_OmitsSiteBlock()
        {
            // Arrange
            SearchProfile profile = new SearchProfile { SourceGroupIds = new List<int>() };

            // Act
            QueryResult result = _builder.Build(profile);

            // Assert
            Assert.That(result.RawQuery, Does.Not.Contain("site:"));
        }

        [Test]
        public void Build_OneSourceGroup_IncludesItsDomainsAsSiteFilters()
        {
            // Arrange
            SearchProfile profile = new SearchProfile { SourceGroupIds = new List<int> { 1 } };

            // Act
            QueryResult result = _builder.Build(profile);

            // Assert
            Assert.That(result.RawQuery, Does.Contain("site:boards.greenhouse.io"));
            Assert.That(result.RawQuery, Does.Contain("site:jobs.lever.co"));
        }

        [Test]
        public void Build_MultipleSourceGroups_MergesDomainsWithOr()
        {
            // Arrange
            SearchProfile profile = new SearchProfile { SourceGroupIds = new List<int> { 1, 2 } };

            // Act
            QueryResult result = _builder.Build(profile);

            // Assert
            Assert.That(result.RawQuery, Does.Contain("site:boards.greenhouse.io"));
            Assert.That(result.RawQuery, Does.Contain("site:jobs.ashbyhq.com"));
        }

        [Test]
        public void Build_UnknownSourceGroupId_ProducesNoSiteBlock()
        {
            // Arrange
            SearchProfile profile = new SearchProfile { SourceGroupIds = new List<int> { 999 } };

            // Act
            QueryResult result = _builder.Build(profile);

            // Assert
            Assert.That(result.RawQuery, Does.Not.Contain("site:"));
        }

        // -------------------------------------------------------------------
        // Google URL
        // -------------------------------------------------------------------

        [Test]
        public void Build_NonEmptyProfile_GoogleUrlStartsWithSearchBase()
        {
            // Arrange
            SearchProfile profile = new SearchProfile { StackKeywords = new List<string> { "C#" } };

            // Act
            QueryResult result = _builder.Build(profile);

            // Assert
            Assert.That(result.GoogleSearchUrl, Does.StartWith("https://www.google.com/search?q="));
        }

        [Test]
        public void Build_NonEmptyProfile_GoogleUrlContainsTbsParam()
        {
            // Arrange
            SearchProfile profile = new SearchProfile { StackKeywords = new List<string> { "C#" } };

            // Act
            QueryResult result = _builder.Build(profile);

            // Assert
            Assert.That(result.GoogleSearchUrl, Does.Contain("&tbs=li:1"));
        }

        [Test]
        public void Build_EmptyProfile_GoogleUrlIsEmpty()
        {
            // Arrange
            SearchProfile profile = new SearchProfile();

            // Act
            QueryResult result = _builder.Build(profile);

            // Assert
            Assert.That(result.GoogleSearchUrl, Does.Contain("q=&tbs=li:1").Or.EqualTo(string.Empty).Or.Contain("q=%0a"));
        }

        // -------------------------------------------------------------------
        // Blocks list
        // -------------------------------------------------------------------

        [Test]
        public void Build_FullProfile_BlocksListContainsAllSections()
        {
            // Arrange
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

            // Act
            QueryResult result = _builder.Build(profile);

            // Assert
            Assert.That(result.Blocks.Count, Is.EqualTo(7));
        }

        // -------------------------------------------------------------------
        // Exclude keywords
        // -------------------------------------------------------------------

        [Test]
        public void Build_SingleExcludeKeyword_ProducesNegatedTerm()
        {
            // Arrange
            SearchProfile profile = new SearchProfile
            {
                ExcludeKeywords = new List<string> { "must relocate" }
            };

            // Act
            QueryResult result = _builder.Build(profile);

            // Assert
            Assert.That(result.RawQuery, Does.Contain("-"));
            Assert.That(result.RawQuery, Does.Contain("\"must relocate\""));
        }

        [Test]
        public void Build_MultipleExcludeKeywords_EachTermNegatedSeparately()
        {
            // Arrange
            SearchProfile profile = new SearchProfile
            {
                ExcludeKeywords = new List<string> { "must relocate", "US only", "on-site required" }
            };

            // Act
            QueryResult result = _builder.Build(profile);

            // Assert
            Assert.That(result.RawQuery, Does.Contain("-\"must relocate\""));
            Assert.That(result.RawQuery, Does.Contain("-\"US only\""));
            Assert.That(result.RawQuery, Does.Contain("-\"on-site required\""));
        }

        [Test]
        public void Build_ExcludeKeywords_DoNotAppearWithoutNegation()
        {
            // Arrange
            SearchProfile profile = new SearchProfile
            {
                ExcludeKeywords = new List<string> { "must relocate" }
            };

            // Act
            QueryResult result = _builder.Build(profile);

            // Assert
            // term must be preceded by - not treated as a positive filter
            Assert.That(result.RawQuery, Does.Not.Contain("(\"must relocate\")"));
        }

        [Test]
        public void Build_ExcludeKeywordsPresent_AddsOneMoreBlock()
        {
            // Arrange
            SearchProfile profileWithout = new SearchProfile
            {
                StackKeywords = new List<string> { "C#" }
            };
            SearchProfile profileWith = new SearchProfile
            {
                StackKeywords = new List<string> { "C#" },
                ExcludeKeywords = new List<string> { "must relocate" }
            };

            // Act
            QueryResult without = _builder.Build(profileWithout);
            QueryResult with = _builder.Build(profileWith);

            // Assert
            Assert.That(with.Blocks.Count, Is.EqualTo(without.Blocks.Count + 1));
        }

        [Test]
        public void Build_EmptyExcludeKeywords_NoExcludeBlock()
        {
            // Arrange
            SearchProfile profile = new SearchProfile
            {
                ExcludeKeywords = new List<string>()
            };

            // Act
            QueryResult result = _builder.Build(profile);

            // Assert
            Assert.That(result.RawQuery, Does.Not.Contain("-"));
        }
    }
}
