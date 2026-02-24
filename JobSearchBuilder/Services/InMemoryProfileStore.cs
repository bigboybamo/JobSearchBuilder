using JobSearchBuilder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobSearchBuilder.Services
{
    public class InMemoryProfileStore : IProfileStore
    {
        private readonly List<SearchProfile> _profiles = new List<SearchProfile>();
        private int _nextId = 1;

        public IReadOnlyList<SearchProfile> GetAll()
        {
            return _profiles.AsReadOnly();
        }

        public SearchProfile GetById(int id)
        {
            return _profiles.FirstOrDefault(p => p.Id == id);
        }

        public SearchProfile Save(SearchProfile profile)
        {
            if (profile.Id == 0)
            {
                profile.Id = _nextId++;
                profile.CreatedAt = DateTime.UtcNow;
                profile.UpdatedAt = DateTime.UtcNow;
                _profiles.Add(profile);
            }
            else
            {
                SearchProfile existing = _profiles.FirstOrDefault(p => p.Id == profile.Id);
                if (existing == null)
                    throw new InvalidOperationException("Profile " + profile.Id + " not found.");

                profile.UpdatedAt = DateTime.UtcNow;
                _profiles.Remove(existing);
                _profiles.Add(profile);
            }
            return profile;
        }

        public void Delete(int id)
        {
            SearchProfile p = _profiles.FirstOrDefault(x => x.Id == id);
            if (p != null) _profiles.Remove(p);
        }

        public void SeedDefaults()
        {
            if (_profiles.Count > 0) return;

            Save(new SearchProfile
            {
                Name = "UK Sponsorship .NET",
                Seniority = "Senior",
                StackKeywords = new List<string> { "C#", ".NET", "ASP.NET Core" },
                RoleKeywords = new List<string> { "Developer", "Engineer" },
                LocationFilters = new List<string> { "United Kingdom", "UK", "London", "Manchester", "Leeds", "Birmingham" },
                VisaFilters = new List<string> { "visa sponsorship", "sponsor visa" },
                RemoteFilters = new List<string> { "remote", "hybrid" },
                SourceGroupIds = new List<int> { 1, 2 }
            });
        }
    }
}
