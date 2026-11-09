using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using JobSearchBuilder.Services;

namespace JobSearchBuilder
{
    internal static class Program
    {
        private const string LogComponent = "Application";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ConfigureLogging();

            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            AppLog.Info(LogComponent, "Startup", "Application starting. Version " + typeof(Program).Assembly.GetName().Version);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());

            AppLog.Info(LogComponent, "Shutdown", "Application exited normally.");
        }

        private static void ConfigureLogging()
        {
            LoggingSettings settings = AppSettingsLoader.LoadLoggingSettings();
            AppLog.Configure(new FileAppLogger(
                settings.ResolveDirectory(),
                settings.MinimumLevel,
                settings.MaxFileSizeKb * 1024L,
                settings.RetainedFileCount));
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            AppLog.Critical(LogComponent, "UnhandledUiException", "Unhandled exception on the UI thread.", e.Exception);

            // Subscribing replaces the WinForms default, so show the same dialog it would have.
            using (ThreadExceptionDialog dialog = new ThreadExceptionDialog(e.Exception))
            {
                if (dialog.ShowDialog() == DialogResult.Abort)
                    Application.Exit();
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            AppLog.Critical(LogComponent, "UnhandledException",
                "Unhandled exception. Terminating: " + e.IsTerminating, e.ExceptionObject as Exception);
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            AppLog.Error(LogComponent, "UnobservedTaskException", "Unobserved background task exception.", e.Exception);
        }
    }
}
