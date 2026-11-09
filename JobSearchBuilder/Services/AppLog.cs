using JobSearchBuilder.Interfaces;
using JobSearchBuilder.Models;
using System;

namespace JobSearchBuilder.Services
{
    /// <summary>
    /// Application-wide logging entry point. Static so that unhandled-exception
    /// handlers and services can log without changing their constructors.
    /// Does nothing until <see cref="Configure"/> is called.
    /// </summary>
    public static class AppLog
    {
        private static volatile IAppLogger _logger;

        public static void Configure(IAppLogger logger)
        {
            _logger = logger;
        }

        public static void Debug(string component, string operation, string message)
        {
            Write(LogLevel.Debug, component, operation, message, null);
        }

        public static void Info(string component, string operation, string message)
        {
            Write(LogLevel.Information, component, operation, message, null);
        }

        public static void Warning(string component, string operation, string message, Exception exception = null)
        {
            Write(LogLevel.Warning, component, operation, message, exception);
        }

        public static void Error(string component, string operation, string message, Exception exception = null)
        {
            Write(LogLevel.Error, component, operation, message, exception);
        }

        public static void Critical(string component, string operation, string message, Exception exception = null)
        {
            Write(LogLevel.Critical, component, operation, message, exception);
        }

        private static void Write(LogLevel level, string component, string operation, string message, Exception exception)
        {
            IAppLogger logger = _logger;
            if (logger == null)
                return;

            try
            {
                logger.Log(level, component, operation, message, exception);
            }
            catch
            {
                // A logging failure must never change application behaviour.
            }
        }
    }
}
