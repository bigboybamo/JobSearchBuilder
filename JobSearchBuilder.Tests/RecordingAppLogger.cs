using JobSearchBuilder.Interfaces;
using JobSearchBuilder.Models;
using System;
using System.Collections.Generic;

namespace JobSearchBuilder.Tests
{
    internal class RecordingAppLogger : IAppLogger
    {
        public class Entry
        {
            public LogLevel Level { get; set; }
            public string Component { get; set; }
            public string Operation { get; set; }
            public string Message { get; set; }
            public Exception Exception { get; set; }
        }

        public List<Entry> Entries { get; } = new List<Entry>();

        public void Log(LogLevel level, string component, string operation, string message, Exception exception)
        {
            Entries.Add(new Entry
            {
                Level = level,
                Component = component,
                Operation = operation,
                Message = message,
                Exception = exception
            });
        }
    }
}
