using JobSearchBuilder.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace JobSearchBuilder.Services
{
    public static class AppSettingsLoader
    {
        private static string SettingsPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

        private static string LocalSettingsPath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.local.json");

        public static AppSettings Load()
        {
            if (!File.Exists(SettingsPath))
                throw new FileNotFoundException("appsettings.json not found.", SettingsPath);

            string json = File.ReadAllText(SettingsPath);
            JObject root = JObject.Parse(json);

            // appsettings.local.json is gitignored and can override ConnectionString for local dev.
            if (File.Exists(LocalSettingsPath))
            {
                JObject local = JObject.Parse(File.ReadAllText(LocalSettingsPath));
                if (local["ConnectionString"] != null)
                    root["ConnectionString"] = local["ConnectionString"];
            }

            AppSettings settings = new AppSettings();

            // ATS source groups
            JArray groups = (JArray)root["AtsSourceGroups"];
            if (groups != null)
            {
                foreach (JObject g in groups)
                {
                    AtsSourceGroup group = new AtsSourceGroup
                    {
                        Id = (int)g["Id"],
                        Name = (string)g["Name"]
                    };
                    JArray domains = (JArray)g["Domains"];
                    if (domains != null)
                        foreach (string d in domains)
                            group.Domains.Add(d);

                    settings.AtsSourceGroups.Add(group);
                }
            }

            // Defaults — simple string lists
            JObject defaults = (JObject)root["Defaults"];
            if (defaults != null)
            {
                settings.SeniorityLevels = ReadStringList(defaults, "SeniorityLevels");
                settings.CommonRoles = ReadStringList(defaults, "CommonRoles");
                settings.CommonVisaTerms = ReadStringList(defaults, "CommonVisaTerms");
                settings.CommonRemoteTerms = ReadStringList(defaults, "CommonRemoteTerms");
                settings.CommonLocations = ReadStringList(defaults, "CommonLocations");
                settings.CommonExcludeTerms = ReadStringList(defaults, "CommonExcludeTerms");
                settings.CommonTimezoneTerms = ReadStringList(defaults, "CommonTimezoneTerms");
            }

            settings.ConnectionString = (string)root["ConnectionString"];
            settings.Ai = ReadAiSettings(root);

            return settings;
        }

        private static AiSettings ReadAiSettings(JObject root)
        {
            AiSettings ai = new AiSettings();
            JObject aiNode = (JObject)root["Ai"];
            if (aiNode == null)
                return ai;

            ai.Provider = (string)aiNode["Provider"] ?? string.Empty;

            JObject modelsNode = (JObject)aiNode["Models"];
            if (modelsNode != null)
            {
                foreach (JProperty providerProp in modelsNode.Properties())
                {
                    Dictionary<string, string> tiers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    JObject tiersNode = providerProp.Value as JObject;
                    if (tiersNode != null)
                    {
                        foreach (JProperty tierProp in tiersNode.Properties())
                            tiers[tierProp.Name] = (string)tierProp.Value ?? string.Empty;
                    }
                    ai.Models[providerProp.Name] = tiers;
                }
            }

            Dictionary<string, string> dotEnv = LoadDotEnv();
            ai.ApiKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Anthropic", ResolveApiKey("ANTHROPIC_API_KEY", dotEnv) },
                { "OpenAI",    ResolveApiKey("OPENAI_API_KEY", dotEnv) },
                { "Gemini",    ResolveApiKey("GEMINI_API_KEY", dotEnv) }
            };
            return ai;
        }

        /// <summary>
        /// Resolves an API key. Environment variables are the documented source of truth;
        /// a gitignored <c>.env</c> file is a local-dev fallback used only when the
        /// variable is unset. Extracted for unit testing.
        /// </summary>
        public static string ResolveApiKey(string envVarName, Dictionary<string, string> dotEnv)
        {
            string fromEnvironment = Environment.GetEnvironmentVariable(envVarName);
            if (!string.IsNullOrWhiteSpace(fromEnvironment))
                return fromEnvironment;

            return GetDotEnvValue(dotEnv, envVarName);
        }

        private static Dictionary<string, string> LoadDotEnv()
        {
            Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Walk up from the exe directory to find .env at the repo root
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            string envPath = null;
            for (int i = 0; i < 6; i++)
            {
                string candidate = Path.Combine(dir, ".env");
                if (File.Exists(candidate))
                {
                    envPath = candidate;
                    break;
                }
                string parent = Path.GetDirectoryName(dir);
                if (parent == null || parent == dir) break;
                dir = parent;
            }

            if (envPath == null)
                return values;

            foreach (string line in File.ReadAllLines(envPath))
            {
                string trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith("#"))
                    continue;

                int eq = trimmed.IndexOf('=');
                if (eq <= 0) continue;

                string key = trimmed.Substring(0, eq).Trim();
                string value = trimmed.Substring(eq + 1).Trim();
                values[key] = value;
            }

            return values;
        }

        private static string GetDotEnvValue(Dictionary<string, string> dotEnv, string key)
        {
            if (dotEnv == null)
                return string.Empty;

            string value;
            return dotEnv.TryGetValue(key, out value) ? value : string.Empty;
        }

        private static List<string> ReadStringList(JObject parent, string key)
        {
            List<string> result = new List<string>();
            JArray arr = (JArray)parent[key];
            if (arr != null)
                foreach (string s in arr)
                    result.Add(s);
            return result;
        }

    }

    /// <summary>
    /// Plain data bag — everything the app needs from appsettings.json.
    /// </summary>
    public class AppSettings
    {
        public string ConnectionString { get; set; }
        public AiSettings Ai { get; set; }
        public List<AtsSourceGroup> AtsSourceGroups { get; set; }
        public List<string> SeniorityLevels { get; set; }
        public List<string> CommonRoles { get; set; }
        public List<string> CommonVisaTerms { get; set; }
        public List<string> CommonRemoteTerms { get; set; }
        public List<string> CommonLocations { get; set; }
        public List<string> CommonExcludeTerms { get; set; }
        public List<string> CommonTimezoneTerms { get; set; }

        public AppSettings()
        {
            AtsSourceGroups = new List<AtsSourceGroup>();
            SeniorityLevels = new List<string>();
            CommonRoles = new List<string>();
            CommonVisaTerms = new List<string>();
            CommonRemoteTerms = new List<string>();
            CommonLocations = new List<string>();
            CommonExcludeTerms = new List<string>();
            CommonTimezoneTerms = new List<string>();
            Ai = new AiSettings();
        }
    }

    public class AiSettings
    {
        public string Provider { get; set; }
        public Dictionary<string, Dictionary<string, string>> Models { get; set; }
        public Dictionary<string, string> ApiKeys { get; set; }

        public AiSettings()
        {
            Provider = string.Empty;
            Models = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            ApiKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public string GetModelId(string providerName, string tier)
        {
            if (string.IsNullOrWhiteSpace(providerName) || string.IsNullOrWhiteSpace(tier))
                return string.Empty;

            Dictionary<string, string> tiers;
            if (Models == null || !Models.TryGetValue(providerName, out tiers))
                return string.Empty;

            string modelId;
            return tiers.TryGetValue(tier, out modelId) ? modelId : string.Empty;
        }

        public string GetApiKey(string provider)
        {
            if (string.IsNullOrWhiteSpace(provider))
                return string.Empty;

            string apiKey;
            return ApiKeys != null && ApiKeys.TryGetValue(provider, out apiKey) ? apiKey : string.Empty;
        }
    }
}
