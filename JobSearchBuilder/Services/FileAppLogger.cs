using JobSearchBuilder.Interfaces;
using JobSearchBuilder.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace JobSearchBuilder.Services
{
    /// <summary>
    /// Writes redacted JSON entries, each followed by a separator line for
    /// readability, to a file per local day, named
    /// applog_dd_MM_yy.log. Disk use stays bounded by the per-day size limit, which
    /// rolls a full file to applog_dd_MM_yy.1.log, and by removing older days.
    /// </summary>
    public class FileAppLogger : IAppLogger
    {
        private const string DateFormat = "dd_MM_yy";
        private const int MaxFieldLength = 16000;
        private const string EntrySeparator = "___________________________________";

        private static readonly Regex DatedFileName = new Regex(
            @"^applog_(\d{2}_\d{2}_\d{2})(?:\.1)?\.log$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private readonly object _sync = new object();
        private readonly string _directory;
        private readonly Func<DateTime> _localNow;
        private readonly LogLevel _minimumLevel;
        private readonly long _maxFileSizeBytes;
        private readonly int _retainedFileCount;
        private DateTime _lastPrunedDate;

        public FileAppLogger(string directory, LogLevel minimumLevel, long maxFileSizeBytes, int retainedFileCount,
                             Func<DateTime> localNow = null)
        {
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("Log directory is required.", "directory");
            if (maxFileSizeBytes <= 0) throw new ArgumentOutOfRangeException("maxFileSizeBytes");
            if (retainedFileCount < 0) throw new ArgumentOutOfRangeException("retainedFileCount");

            _directory = directory;
            _localNow = localNow ?? (() => DateTime.Now);
            _minimumLevel = minimumLevel;
            _maxFileSizeBytes = maxFileSizeBytes;
            _retainedFileCount = retainedFileCount;
        }

        public void Log(LogLevel level, string component, string operation, string message, Exception exception)
        {
            if (level < _minimumLevel)
                return;

            string line = FormatEntry(level, component, operation, message, exception);
            System.Diagnostics.Debug.WriteLine(line);
            byte[] bytes = new UTF8Encoding(false).GetBytes(line + Environment.NewLine + EntrySeparator + Environment.NewLine);

            lock (_sync)
            {
                try
                {
                    Directory.CreateDirectory(_directory);

                    DateTime today = _localNow().Date;
                    PruneOldDaysOnce(today);

                    string path = CurrentPath(today, false);
                    RollIfFull(path, CurrentPath(today, true), bytes.Length);

                    // Shared so a second running instance can still append.
                    using (FileStream stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete))
                        stream.Write(bytes, 0, bytes.Length);
                }
                catch (Exception ex)
                {
                    // Losing a log line is better than failing the operation being logged.
                    System.Diagnostics.Debug.WriteLine("FileAppLogger: write failed: " + ex.GetType().Name);
                }
            }
        }

        private static string FormatEntry(LogLevel level, string component, string operation, string message, Exception exception)
        {
            JObject entry = new JObject
            {
                ["timestamp"] = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                ["level"] = level.ToString(),
                ["component"] = Sanitize(component),
                ["operation"] = Sanitize(operation),
                ["message"] = Sanitize(message)
            };

            if (exception != null)
            {
                entry["exceptionType"] = exception.GetType().FullName;
                entry["exception"] = Sanitize(exception.ToString());
            }

            return entry.ToString(Formatting.None);
        }

        private static string Sanitize(string value)
        {
            string redacted = LogRedactor.Redact(value ?? string.Empty);
            return redacted.Length > MaxFieldLength
                ? redacted.Substring(0, MaxFieldLength) + "...[truncated]"
                : redacted;
        }

        private void RollIfFull(string path, string overflowPath, int incomingBytes)
        {
            FileInfo current = new FileInfo(path);
            if (!current.Exists || current.Length == 0 || current.Length + incomingBytes <= _maxFileSizeBytes)
                return;

            File.Delete(overflowPath);
            File.Move(path, overflowPath);
        }

        private void PruneOldDaysOnce(DateTime today)
        {
            if (_lastPrunedDate == today)
                return;

            _lastPrunedDate = today;

            var previousDays = Directory.GetFiles(_directory, "applog_*.log")
                .Select(file => new { File = file, Date = ParseFileDate(file) })
                .Where(x => x.Date.HasValue && x.Date.Value != today)
                .GroupBy(x => x.Date.Value)
                .OrderByDescending(g => g.Key)
                .Skip(_retainedFileCount);

            foreach (var day in previousDays)
                foreach (var entry in day)
                    File.Delete(entry.File);
        }

        // Parsed rather than sorted by name: dd_MM_yy file names do not sort by date.
        private static DateTime? ParseFileDate(string file)
        {
            Match match = DatedFileName.Match(Path.GetFileName(file));
            if (!match.Success)
                return null;

            DateTime date;
            return DateTime.TryParseExact(match.Groups[1].Value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
                ? date
                : (DateTime?)null;
        }

        private string CurrentPath(DateTime day, bool overflow)
        {
            string name = "applog_" + day.ToString(DateFormat, CultureInfo.InvariantCulture) + (overflow ? ".1" : string.Empty) + ".log";
            return Path.Combine(_directory, name);
        }
    }
}
