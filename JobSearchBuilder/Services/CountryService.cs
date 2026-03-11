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
            return ParseApiResponse(json);
        }

        private void SaveToCache(List<string> countries)
        {
            File.WriteAllText(_cacheFile, new JArray(countries.ToArray()).ToString());
        }

        /// <summary>Extracted for unit testing — parses the REST Countries API JSON payload.</summary>
        public static List<string> ParseApiResponse(string json)
        {
            JArray arr = JArray.Parse(json);
            List<string> names = new List<string>();
            foreach (JObject country in arr)
            {
                string name = (string)country["name"]?["common"];
                if (!string.IsNullOrWhiteSpace(name))
                    names.Add(name);
            }
            names.Sort(StringComparer.OrdinalIgnoreCase);
            return names;
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
