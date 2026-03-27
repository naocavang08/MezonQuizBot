namespace Mezon_sdk.Utils
{
    using System;
    using System.Collections.Concurrent;

    public enum LogLevel
    {
        DEBUG = 10,
        INFO = 20,
        WARNING = 30,
        ERROR = 40,
        CRITICAL = 50
    }

    public class Logger
    {
        private static readonly ConcurrentDictionary<string, Logger> _loggers = new ConcurrentDictionary<string, Logger>();
        
        public string Name { get; }
        public LogLevel Level { get; set; } = LogLevel.INFO;
        public bool IsDisabled { get; set; } = false;
        public bool UseColors { get; set; } = true;

        public string LogFormat { get; set; } = "[{0}] [{1}] [{2}] {3}";
        public string DateFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";

        private bool _isConfigured = false;

        public Logger(string name)
        {
            Name = name;
        }

        public static Logger GetLogger(string name)
        {
            return _loggers.GetOrAdd(name, n => new Logger(n));
        }

        public static Logger SetupLogger(
            string name = "mezon",
            LogLevel logLevel = LogLevel.INFO,
            string? logFormat = null,
            string? dateFormat = null,
            bool useColors = true)
        {
            var logger = GetLogger(name);

            if (!logger._isConfigured)
            {
                logger.Level = logLevel;
                logger.UseColors = useColors && logger.SupportsColor();

                if (logFormat != null) logger.LogFormat = logFormat;
                if (dateFormat != null) logger.DateFormat = dateFormat;

                logger._isConfigured = true;
            }

            return logger;
        }

        public static void DisableLogging(string name = "mezon")
        {
            var logger = GetLogger(name);
            logger.IsDisabled = true;
        }

        public static void EnableLogging(string name = "mezon", LogLevel logLevel = LogLevel.INFO)
        {
            var logger = GetLogger(name);
            logger.Level = logLevel;
            logger.IsDisabled = false;
        }

        public void Debug(string message) => Log(LogLevel.DEBUG, message);
        public void Info(string message) => Log(LogLevel.INFO, message);
        public void Warning(string message) => Log(LogLevel.WARNING, message);
        public void Error(string message) => Log(LogLevel.ERROR, message);
        public void Critical(string message) => Log(LogLevel.CRITICAL, message);

        private bool SupportsColor()
        {
            return !Console.IsOutputRedirected;
        }
        
        private void Log(LogLevel level, string message)
        {
            if (IsDisabled || level < Level) return;

            var timestamp = DateTime.Now.ToString(DateFormat);

            var color = UseColors ? GetColorCode(level) : "";
            var reset = UseColors ? "\x1b[0m" : "";
            var bold = UseColors ? "\x1b[1m" : "";

            var levelText = UseColors
                ? $"{color}{bold}[{level}]{reset}"
                : $"[{level}]";

            var formatted = string.Format(LogFormat, timestamp, Name, levelText, message);

            if (level >= LogLevel.ERROR)
                Console.Error.WriteLine(formatted);
            else
                Console.WriteLine(formatted);
        }

        private string GetColorCode(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.DEBUG:    return "\x1b[36m"; // Cyan
                case LogLevel.INFO:     return "\x1b[32m"; // Green
                case LogLevel.WARNING:  return "\x1b[33m"; // Yellow
                case LogLevel.ERROR:    return "\x1b[31m"; // Red
                case LogLevel.CRITICAL: return "\x1b[35m"; // Magenta
                default:                return "";
            }
        }
    }
}