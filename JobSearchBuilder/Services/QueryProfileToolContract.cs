using JobSearchBuilder.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace JobSearchBuilder.Services
{
    internal static class QueryProfileToolContract
    {
        internal const string ToolName = "build_query_profile";

        internal static QueryProfileResult ParseProfile(string argumentsJson)
        {
            if (string.IsNullOrWhiteSpace(argumentsJson))
                throw new InvalidOperationException("The profile tool result was empty.");

            JObject root = JObject.Parse(argumentsJson);
            return new QueryProfileResult
            {
                Roles = ReadRoles(root),
                Seniority = ReadString(root, "seniority", "Seniority"),
                TechStack = ReadStringList(root, "tech_stack", "TechStack"),
                Locations = ReadStringList(root, "locations", "Locations"),
                VisaTerms = ReadStringList(root, "visa_terms", "VisaTerms"),
                RemoteTerms = ReadStringList(root, "remote_terms", "RemoteTerms"),
                TimezoneTerms = ReadStringList(root, "timezone_terms", "TimezoneTerms"),
                ExcludeTerms = ReadStringList(root, "exclude_terms", "ExcludeTerms")
            };
        }

        internal static string GetToolSchema()
        {
            return @"{
  ""type"": ""object"",
  ""properties"": {
    ""roles"": { ""type"": ""array"", ""items"": { ""type"": ""string"" } },
    ""seniority"": { ""type"": ""string"" },
    ""tech_stack"": { ""type"": ""array"", ""items"": { ""type"": ""string"" } },
    ""locations"": { ""type"": ""array"", ""items"": { ""type"": ""string"" } },
    ""visa_terms"": { ""type"": ""array"", ""items"": { ""type"": ""string"" } },
    ""remote_terms"": { ""type"": ""array"", ""items"": { ""type"": ""string"" } },
    ""timezone_terms"": { ""type"": ""array"", ""items"": { ""type"": ""string"" } },
    ""exclude_terms"": { ""type"": ""array"", ""items"": { ""type"": ""string"" } }
  },
  ""required"": [""roles"", ""seniority"", ""tech_stack"", ""locations"", ""visa_terms"", ""remote_terms"", ""timezone_terms"", ""exclude_terms""],
  ""additionalProperties"": false
}";
        }

        private static List<string> ReadRoles(JObject root)
        {
            List<string> roles = ReadStringValues(root, "roles", "Roles", true);
            if (roles.Count > 0)
                return roles;

            return ReadStringValues(root, "role", "Role", true);
        }

        private static string ReadString(JObject root, string snakeName, string pascalName)
        {
            return (string)root[snakeName] ?? (string)root[pascalName] ?? string.Empty;
        }

        private static List<string> ReadStringList(JObject root, string snakeName, string pascalName)
        {
            return ReadStringValues(root, snakeName, pascalName, false);
        }

        private static List<string> ReadStringValues(
            JObject root,
            string snakeName,
            string pascalName,
            bool acceptScalar)
        {
            List<string> values = new List<string>();
            JToken token = root[snakeName] ?? root[pascalName];
            JArray array = token as JArray;
            if (array != null)
            {
                foreach (JToken item in array)
                    AddValue(values, (string)item);

                return values;
            }

            if (acceptScalar)
                AddValue(values, (string)token);

            return values;
        }

        private static void AddValue(List<string> values, string value)
        {
            string trimmed = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                return;

            if (!values.Exists(item => string.Equals(item, trimmed, StringComparison.OrdinalIgnoreCase)))
                values.Add(trimmed);
        }
    }
}
