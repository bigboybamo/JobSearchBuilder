using JobSearchBuilder.Services;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace JobSearchBuilder.Tests
{
    [TestFixture]
    public class AppSettingsLoaderTests
    {
        private const string TestVar = "JSB_TEST_API_KEY";

        [TearDown]
        public void TearDown()
        {
            // Always clear the process-scoped variable so tests don't leak into each other.
            Environment.SetEnvironmentVariable(TestVar, null);
        }

        // -------------------------------------------------------------------
        // ResolveApiKey — env var is the documented source of truth,
        // .env is a fallback used only when the variable is unset.
        // -------------------------------------------------------------------

        [Test]
        public void ResolveApiKey_EnvironmentVariableSet_TakesPrecedenceOverDotEnv()
        {
            Environment.SetEnvironmentVariable(TestVar, "from-environment");
            var dotEnv = new Dictionary<string, string> { { TestVar, "from-dotenv" } };

            string result = AppSettingsLoader.ResolveApiKey(TestVar, dotEnv);

            Assert.That(result, Is.EqualTo("from-environment"));
        }

        [Test]
        public void ResolveApiKey_EnvironmentVariableUnset_FallsBackToDotEnv()
        {
            Environment.SetEnvironmentVariable(TestVar, null);
            var dotEnv = new Dictionary<string, string> { { TestVar, "from-dotenv" } };

            string result = AppSettingsLoader.ResolveApiKey(TestVar, dotEnv);

            Assert.That(result, Is.EqualTo("from-dotenv"));
        }

        [Test]
        public void ResolveApiKey_EnvironmentVariableWhitespace_FallsBackToDotEnv()
        {
            Environment.SetEnvironmentVariable(TestVar, "   ");
            var dotEnv = new Dictionary<string, string> { { TestVar, "from-dotenv" } };

            string result = AppSettingsLoader.ResolveApiKey(TestVar, dotEnv);

            Assert.That(result, Is.EqualTo("from-dotenv"));
        }

        [Test]
        public void ResolveApiKey_NeitherSource_ReturnsEmpty()
        {
            Environment.SetEnvironmentVariable(TestVar, null);

            string result = AppSettingsLoader.ResolveApiKey(TestVar, new Dictionary<string, string>());

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void ResolveApiKey_NullDotEnvAndNoVariable_ReturnsEmpty()
        {
            Environment.SetEnvironmentVariable(TestVar, null);

            string result = AppSettingsLoader.ResolveApiKey(TestVar, null);

            Assert.That(result, Is.Empty);
        }
    }
}
