using JobSearchBuilder.Models;
using System;

namespace JobSearchBuilder.Interfaces
{
    public interface IAppLogger
    {
        void Log(LogLevel level, string component, string operation, string message, Exception exception);
    }
}
