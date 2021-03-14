using System;
using System.IO;
using System.Text;

namespace DotLauncher.Utils
{
    public enum LogLevel
    {
        Info,
        Debug,
        Warn,
        Error,
        Fatal
    }

    public static class Logger
    {
        private static string logFileName;
        private static readonly object LockObject = new object();

        public static void Init(string logsDirectory)
        {
            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
            }

            var dateTime = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            logFileName = $"{PathUtils.Combine(logsDirectory, dateTime)}.log";

            Info("Log created successfully");
        }

        public static void Info(string message, object data = null) => WriteEntry(LogLevel.Info, message, data);
        public static void Debug(string message, object data = null) => WriteEntry(LogLevel.Debug, message, data);
        public static void Warn(string message, object data = null) => WriteEntry(LogLevel.Warn, message, data);
        public static void Error(string message, object data = null) => WriteEntry(LogLevel.Error, message, data);
        public static void Fatal(string message, object data = null) => WriteEntry(LogLevel.Fatal, message, data);

        private static void WriteEntry(LogLevel logLevel, string message, object data)
        {
            var sb = new StringBuilder();

            sb.Append($"{DateTime.Now:yyyy-MM-dd HH-mm-ss}");
            sb.Append("\t");
            sb.Append($"[{logLevel.ToString().ToUpper()}]");
            sb.Append("\t");
            sb.Append(message);

            if (data != null)
            {
                sb.Append("\t");
                sb.Append(JsonUtils.Serialize(data, writeIndented: false));
            }

            sb.AppendLine();

            lock (LockObject)
            {
                File.AppendAllText(logFileName, sb.ToString());
            }
        }
    }
}
