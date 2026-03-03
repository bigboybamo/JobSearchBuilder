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

            // appsettings.local.json is gitignored and can override any value in appsettings.json.
            // Only ConnectionString is merged here; extend as needed.
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
            }

            settings.ConnectionString = (string)root["ConnectionString"];

            return settings;
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
        public List<AtsSourceGroup> AtsSourceGroups { get; set; }
        public List<string> SeniorityLevels { get; set; }
        public List<string> CommonRoles { get; set; }
        public List<string> CommonVisaTerms { get; set; }
        public List<string> CommonRemoteTerms { get; set; }
        public List<string> CommonLocations { get; set; }

        public AppSettings()
        {
            AtsSourceGroups = new List<AtsSourceGroup>();
            SeniorityLevels = new List<string>();
            CommonRoles = new List<string>();
            CommonVisaTerms = new List<string>();
            CommonRemoteTerms = new List<string>();
            CommonLocations = new List<string>();
        }
    }
}
