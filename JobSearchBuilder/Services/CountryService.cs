using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace JobSearchBuilder.Services
{
    public class CountryService
    {
        private static readonly string DefaultCachePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "countries.json");

        private const string ApiUrl = "https://restcountries.com/v3.1/all?fields=name";

        private readonly string _cacheFile;
        private readonly Func<string> _fetchJson;

        /// <summary>Production constructor — uses the default cache path and live HTTP.</summary>
        public CountryService()
            : this(DefaultCachePath, () =>
            {
                using (WebClient client = new WebClient())
                    return client.DownloadString(ApiUrl);
            })
        { }

        /// <summary>Test constructor — allows injecting a custom cache path and a fetch stub.</summary>
        public CountryService(string cacheFilePath, Func<string> fetchJson)
        {
            _cacheFile = cacheFilePath;
            _fetchJson = fetchJson;
        }

        /// <summary>
        /// Returns country common names, sorted alphabetically.
        /// Reads from the local cache file if it exists; otherwise fetches from the API and saves it.
        /// </summary>
        public List<string> GetCountries()
        {
            if (File.Exists(_cacheFile))
                return LoadFromCache();

            List<string> countries = FetchFromApi();
            SaveToCache(countries);
            return countries;
        }

        private List<string> LoadFromCache()
        {
            string json = File.ReadAllText(_cacheFile);
            return ParseCache(json);
        }

        private List<string> FetchFromApi()
        {
            string json = _fetchJson();

            JToken root;
            try
            {
                root = JToken.Parse(json);
            }
            catch
            {
                return GetFallbackCountries();
            }

            if (IsApiErrorResponse(root))
                return GetFallbackCountries();

            return ParseApiResponse(root);
        }

        private void SaveToCache(List<string> countries)
        {
            File.WriteAllText(_cacheFile, new JArray(countries.ToArray()).ToString());
        }

        /// <summary>Extracted for unit testing — parses the REST Countries API JSON payload.</summary>
        public static List<string> ParseApiResponse(string json)
        {
            return ParseApiResponse(JToken.Parse(json));
        }

        private static List<string> ParseApiResponse(JToken root)
        {
            JArray arr = root as JArray;
            if (arr == null)
            {
                JObject obj = root as JObject;
                if (obj != null)
                {
                    arr = obj["data"] as JArray
                          ?? obj["countries"] as JArray
                          ?? obj["results"] as JArray;
                }
            }

            List<string> names = new List<string>();
            if (arr == null)
                return names;

            foreach (JObject country in arr)
            {
                string name = (string)country["name"]?["common"]
                              ?? (string)country["names"]?["common"];
                if (!string.IsNullOrWhiteSpace(name))
                    names.Add(name);
            }
            names.Sort(StringComparer.OrdinalIgnoreCase);
            return names;
        }

        private static bool IsApiErrorResponse(JToken root)
        {
            JObject obj = root as JObject;
            if (obj == null)
                return false;

            JToken success = obj["success"];
            if (success != null && success.Type == JTokenType.Boolean && !(bool)success)
                return true;

            if (obj["errors"] != null || obj["error"] != null)
                return true;

            if (obj["message"] != null &&
                obj["data"] == null &&
                obj["countries"] == null &&
                obj["results"] == null)
            {
                return true;
            }

            return false;
        }

        private static List<string> GetFallbackCountries()
        {
            List<string> countries = new List<string>
            {
                "Argentina",
                "Australia",
                "Austria",
                "Belgium",
                "Brazil",
                "Canada",
                "Chile",
                "China",
                "Colombia",
                "Denmark",
                "Egypt",
                "Finland",
                "France",
                "Germany",
                "Ghana",
                "Greece",
                "India",
                "Indonesia",
                "Ireland",
                "Israel",
                "Italy",
                "Japan",
                "Kenya",
                "Mexico",
                "Netherlands",
                "New Zealand",
                "Nigeria",
                "Norway",
                "Poland",
                "Portugal",
                "Singapore",
                "South Africa",
                "Spain",
                "Sweden",
                "Switzerland",
                "United Arab Emirates",
                "United Kingdom",
                "United States"
            };
            countries.Sort(StringComparer.OrdinalIgnoreCase);
            return countries;
        }

        /// <summary>Extracted for unit testing — parses the local cache JSON file.</summary>
        public static List<string> ParseCache(string json)
        {
            JArray arr = JArray.Parse(json);
            List<string> result = new List<string>();
            foreach (JToken item in arr)
                result.Add((string)item);
            return result;
        }
    }
}
