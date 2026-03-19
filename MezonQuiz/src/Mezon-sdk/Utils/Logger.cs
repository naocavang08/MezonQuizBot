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
            bool useColors = true)
        {
            var logger = GetLogger(name);
            logger.Level = logLevel;
            logger.UseColors = useColors;
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

        private void Log(LogLevel level, string message)
        {
            if (IsDisabled || level < Level) return;

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var colorString = "";
            var resetString = "";

            if (UseColors)
            {
                colorString = GetColorCode(level);
                resetString = "\x1b[0m";
            }

            var formattedMessage = $"[{timestamp}] [{Name}] {colorString}[{level}]{resetString} {message}";
            
            if (level >= LogLevel.ERROR)
            {
                Console.Error.WriteLine(formattedMessage);
            }
            else
            {
                Console.WriteLine(formattedMessage);
            }
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