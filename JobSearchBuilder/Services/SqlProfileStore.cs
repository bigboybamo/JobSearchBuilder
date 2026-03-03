using JobSearchBuilder.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace JobSearchBuilder.Services
{
    public class SqlProfileStore : IProfileStore
    {
        private readonly IDbConnectionFactory _factory;

        public SqlProfileStore(IDbConnectionFactory factory)
        {
            _factory = factory;
        }

        public IReadOnlyList<SearchProfile> GetAll() => LoadProfiles(null);

        public SearchProfile GetById(int id) => LoadProfiles(id).FirstOrDefault();

        public SearchProfile Save(SearchProfile profile)
        {
            if (profile.Id == 0)
                Insert(profile);
            else
                Update(profile);
            return profile;
        }

        public void Delete(int id)
        {
            using (IDbConnection conn = _factory.CreateOpenConnection())
            using (IDbCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM SearchProfiles WHERE Id = @Id";
                AddParam(cmd, "@Id", id);
                cmd.ExecuteNonQuery();
            }
        }

        // -------------------------------------------------------------------
        // Private helpers
        // -------------------------------------------------------------------

        private List<SearchProfile> LoadProfiles(int? filterId)
        {
            List<SearchProfile> profiles = new List<SearchProfile>();

            using (IDbConnection conn = _factory.CreateOpenConnection())
            {
                // 1. Load core profile rows
                using (IDbCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = filterId.HasValue
                        ? "SELECT Id, Name, Seniority, CreatedAt, UpdatedAt FROM SearchProfiles WHERE Id = @Id"
                        : "SELECT Id, Name, Seniority, CreatedAt, UpdatedAt FROM SearchProfiles ORDER BY Id";

                    if (filterId.HasValue)
                        AddParam(cmd, "@Id", filterId.Value);

                    using (IDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            profiles.Add(new SearchProfile
                            {
                                Id        = reader.GetInt32(0),
                                Name      = reader.GetString(1),
                                Seniority = reader.GetString(2),
                                CreatedAt = reader.GetDateTime(3),
                                UpdatedAt = reader.GetDateTime(4)
                            });
                        }
                    }
                }

                if (profiles.Count == 0)
                    return profiles;

                // idList is built from integer PKs we just read — no injection risk.
                string idList = string.Join(",", profiles.Select(p => p.Id));

                // 2. Load keywords
                using (IDbCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT ProfileId, Category, Keyword FROM ProfileKeywords WHERE ProfileId IN (" + idList + ")";
                    using (IDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int profileId   = reader.GetInt32(0);
                            string category = reader.GetString(1);
                            string keyword  = reader.GetString(2);
                            SearchProfile p = profiles.First(x => x.Id == profileId);

                            switch (category)
                            {
                                case "Stack":    p.StackKeywords.Add(keyword);   break;
                                case "Role":     p.RoleKeywords.Add(keyword);    break;
                                case "Location": p.LocationFilters.Add(keyword); break;
                                case "Visa":     p.VisaFilters.Add(keyword);     break;
                                case "Remote":   p.RemoteFilters.Add(keyword);   break;
                            }
                        }
                    }
                }

                // 3. Load source group IDs
                using (IDbCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT ProfileId, SourceGroupId FROM ProfileSourceGroups WHERE ProfileId IN (" + idList + ")";
                    using (IDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int profileId     = reader.GetInt32(0);
                            int sourceGroupId = reader.GetInt32(1);
                            profiles.First(x => x.Id == profileId).SourceGroupIds.Add(sourceGroupId);
                        }
                    }
                }
            }

            return profiles;
        }

        private void Insert(SearchProfile profile)
        {
            profile.CreatedAt = DateTime.UtcNow;
            profile.UpdatedAt = DateTime.UtcNow;

            using (IDbConnection conn = _factory.CreateOpenConnection())
            using (IDbTransaction tx = conn.BeginTransaction())
            {
                try
                {
                    using (IDbCommand cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = "INSERT INTO SearchProfiles (Name, Seniority, CreatedAt, UpdatedAt) VALUES (@Name, @Seniority, @CreatedAt, @UpdatedAt)";
                        AddParam(cmd, "@Name",      profile.Name);
                        AddParam(cmd, "@Seniority", profile.Seniority);
                        AddParam(cmd, "@CreatedAt", profile.CreatedAt);
                        AddParam(cmd, "@UpdatedAt", profile.UpdatedAt);
                        cmd.ExecuteNonQuery();
                    }

                    using (IDbCommand cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = _factory.LastInsertIdSql;
                        profile.Id = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    InsertKeywordsAndGroups(conn, tx, profile);
                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        private void Update(SearchProfile profile)
        {
            profile.UpdatedAt = DateTime.UtcNow;

            using (IDbConnection conn = _factory.CreateOpenConnection())
            using (IDbTransaction tx = conn.BeginTransaction())
            {
                try
                {
                    using (IDbCommand cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = "UPDATE SearchProfiles SET Name = @Name, Seniority = @Seniority, UpdatedAt = @UpdatedAt WHERE Id = @Id";
                        AddParam(cmd, "@Name",      profile.Name);
                        AddParam(cmd, "@Seniority", profile.Seniority);
                        AddParam(cmd, "@UpdatedAt", profile.UpdatedAt);
                        AddParam(cmd, "@Id",        profile.Id);
                        cmd.ExecuteNonQuery();
                    }

                    using (IDbCommand cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = "DELETE FROM ProfileKeywords WHERE ProfileId = @ProfileId";
                        AddParam(cmd, "@ProfileId", profile.Id);
                        cmd.ExecuteNonQuery();
                    }

                    using (IDbCommand cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText = "DELETE FROM ProfileSourceGroups WHERE ProfileId = @ProfileId";
                        AddParam(cmd, "@ProfileId", profile.Id);
                        cmd.ExecuteNonQuery();
                    }

                    InsertKeywordsAndGroups(conn, tx, profile);
                    tx.Commit();
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        private static void InsertKeywordsAndGroups(IDbConnection conn, IDbTransaction tx, SearchProfile profile)
        {
            Action<string, IEnumerable<string>> insertCategory = (category, keywords) =>
            {
                if (keywords == null) return;

                foreach (string kw in keywords)
                {
                    if (string.IsNullOrWhiteSpace(kw))
                        continue;

                    using (IDbCommand cmd = conn.CreateCommand())
                    {
                        cmd.Transaction = tx;
                        cmd.CommandText =
                            "INSERT INTO ProfileKeywords (ProfileId, Category, Keyword) VALUES (@ProfileId, @Category, @Keyword)";

                        AddParam(cmd, "@ProfileId", profile.Id);
                        AddNullableParam(cmd, "@Category", category);
                        AddParam(cmd, "@Keyword", kw);

                        cmd.ExecuteNonQuery();
                    }
                }
            };

            insertCategory("Stack",    profile.StackKeywords);
            insertCategory("Role",     profile.RoleKeywords);
            insertCategory("Location", profile.LocationFilters);
            insertCategory("Visa",     profile.VisaFilters);
            insertCategory("Remote",   profile.RemoteFilters);

            foreach (int sgId in profile.SourceGroupIds ?? Enumerable.Empty<int>())
            {
                using (IDbCommand cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = "INSERT INTO ProfileSourceGroups (ProfileId, SourceGroupId) VALUES (@ProfileId, @SourceGroupId)";
                    AddParam(cmd, "@ProfileId",     profile.Id);
                    AddParam(cmd, "@SourceGroupId", sgId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static void AddParam(IDbCommand cmd, string name, object value)
        {
            IDbDataParameter p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            cmd.Parameters.Add(p);
        }

        private static void AddNullableParam(IDbCommand cmd, string name, object value)
        {
            IDbDataParameter p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }
    }
}
