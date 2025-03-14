using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MSPaintEx.Services
{
    public static class LogService
    {
        private static readonly string LogFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MSPaintEx",
            "logs.txt"
        );

        private static readonly object _lockObj = new object();
        private static readonly bool _isDebug;

        static LogService()
        {
            // Check if we're running in debug mode
            _isDebug = Debugger.IsAttached;

            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(LogFilePath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Create or append to log file with header
                if (!File.Exists(LogFilePath))
                {
                    File.WriteAllText(LogFilePath, $"=== MSPaintEx Log Started at {DateTime.Now} ===\n\n");
                }
                
                WriteToDebug("INFO", "LogService", "Logging system initialized");
            }
            catch (Exception ex)
            {
                WriteToDebug("ERROR", "LogService", $"Failed to initialize logging: {ex.Message}");
            }
        }

        private static void WriteToDebug(string level, string source, string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var logMessage = $"[{timestamp}] [{level}] [{source}] {message}";

            // Write to Debug output (visible in VS Output window and attached debuggers)
            Debug.WriteLine(logMessage);

            // Also write to trace for PowerShell console
            Trace.WriteLine(logMessage);
        }

        public static void LogError(string source, string message, Exception? ex = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[ERROR] [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}]");
            sb.AppendLine($"Message: {message}");
            
            if (ex != null)
            {
                sb.AppendLine($"Exception: {ex.GetType().Name}");
                sb.AppendLine($"Message: {ex.Message}");
                sb.AppendLine($"Stack Trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    sb.AppendLine($"Inner Exception: {ex.InnerException.Message}");
                    sb.AppendLine($"Inner Stack Trace: {ex.InnerException.StackTrace}");
                }
            }
            
            sb.AppendLine();

            try
            {
                File.AppendAllText(LogFilePath, sb.ToString());
                WriteToDebug("ERROR", source, $"{message}" + (ex != null ? $"\nException: {ex.Message}" : ""));
            }
            catch (Exception logEx)
            {
                WriteToDebug("ERROR", "LogService", $"Failed to write to log file: {logEx.Message}");
            }
        }

        public static void LogInfo(string source, string message)
        {
            var logEntry = $"[INFO] [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}] {message}\n";
            
            try
            {
                File.AppendAllText(LogFilePath, logEntry);
                WriteToDebug("INFO", source, message);
            }
            catch (Exception ex)
            {
                WriteToDebug("ERROR", "LogService", $"Failed to write to log file: {ex.Message}");
            }
        }

        public static void LogWarning(string source, string message)
        {
            var logEntry = $"[WARNING] [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}] {message}\n";
            
            try
            {
                File.AppendAllText(LogFilePath, logEntry);
                WriteToDebug("WARNING", source, message);
            }
            catch (Exception ex)
            {
                WriteToDebug("ERROR", "LogService", $"Failed to write to log file: {ex.Message}");
            }
        }
    }
} 