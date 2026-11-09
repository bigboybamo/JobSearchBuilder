using JobSearchBuilder.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace JobSearchBuilder.Services
{
    /// <summary>
    /// Records the outcome of profile persistence calls around another store.
    /// Logs profile ids only, because names and keywords are user content.
    /// Exceptions are rethrown unchanged so existing UI handling still applies.
    /// </summary>
    public class LoggingProfileStore : IProfileStore
    {
        private const string Component = "ProfileStore";

        private readonly IProfileStore _inner;

        public LoggingProfileStore(IProfileStore inner)
        {
            if (inner == null) throw new ArgumentNullException("inner");
            _inner = inner;
        }

        public IReadOnlyList<SearchProfile> GetAll()
        {
            return Run("GetAll", string.Empty, false, () => _inner.GetAll());
        }

        public SearchProfile GetById(int id)
        {
            return Run("GetById", "profileId=" + id, false, () => _inner.GetById(id));
        }

        public SearchProfile Save(SearchProfile profile)
        {
            string context = profile == null ? string.Empty : "profileId=" + profile.Id;
            return Run("Save", context, true, () => _inner.Save(profile));
        }

        public void Delete(int id)
        {
            Run<object>("Delete", "profileId=" + id, true, () =>
            {
                _inner.Delete(id);
                return null;
            });
        }

        private static T Run<T>(string operation, string context, bool isWrite, Func<T> action)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                T result = action();
                string message = ("Completed in " + stopwatch.ElapsedMilliseconds + " ms. " + context).TrimEnd();
                if (isWrite)
                    AppLog.Info(Component, operation, message);
                else
                    AppLog.Debug(Component, operation, message);
                return result;
            }
            catch (Exception ex)
            {
                AppLog.Error(Component, operation, ("Failed after " + stopwatch.ElapsedMilliseconds + " ms. " + context).TrimEnd(), ex);
                throw;
            }
        }
    }
}
