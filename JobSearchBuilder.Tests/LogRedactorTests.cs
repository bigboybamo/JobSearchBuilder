using JobSearchBuilder.Services;
using NUnit.Framework;

namespace JobSearchBuilder.Tests
{
    [TestFixture]
    public class LogRedactorTests
    {
        [TestCase("failed with sk-ant-api03-abcdefghijklmnop1234", "sk-ant-api03")]
        [TestCase("failed with sk-proj1234567890abcdefgh", "sk-proj1234567890")]
        [TestCase("key AIzaSyA1234567890abcdefghijklmnopqrs", "AIzaSyA1234567890")]
        [TestCase("x-api-key: plainsecretvalue", "plainsecretvalue")]
        [TestCase("{\"x-goog-api-key\":\"plainsecretvalue\"}", "plainsecretvalue")]
        [TestCase("ANTHROPIC_API_KEY=plainsecretvalue", "plainsecretvalue")]
        [TestCase("Authorization: Bearer abcdef123456", "abcdef123456")]
        [TestCase("Authorization: plainsecretvalue", "plainsecretvalue")]
        [TestCase("sent Bearer abcdef123456 upstream", "abcdef123456")]
        [TestCase("https://example.test/v1:generate?key=plainsecretvalue&alt=json", "plainsecretvalue")]
        [TestCase("Server=.\\SQLEXPRESS;Database=MyDb;", "SQLEXPRESS")]
        [TestCase("Server=.\\SQLEXPRESS;Database=MyDb;", "MyDb")]
        [TestCase("User Id=sa;Password=p@ss word;", "p@ss word")]
        [TestCase("Data Source=host1;Pwd=hunter2", "hunter2")]
        public void Redact_SensitiveValue_RemovesValue(string input, string secret)
        {
            string result = LogRedactor.Redact(input);

            Assert.That(result, Does.Not.Contain(secret));
            Assert.That(result, Does.Contain("[REDACTED]"));
        }

        [TestCase("Completed in 120 ms. profileId=4")]
        [TestCase("Active provider: Anthropic / claude-sonnet-5-5")]
        public void Redact_OrdinaryText_IsUnchanged(string input)
        {
            Assert.That(LogRedactor.Redact(input), Is.EqualTo(input));
        }

        [Test]
        public void Redact_Null_ReturnsNull()
        {
            Assert.That(LogRedactor.Redact(null), Is.Null);
        }
    }
}
