using Avalonia;
using System;
using System.Diagnostics;
using System.IO;

namespace MSPaintEx;

class Program
{
    // Custom trace listener that writes directly to console
    private class PowerShellTraceListener : TraceListener
    {
        public override void Write(string? message)
        {
            if (message != null)
            {
                // Write to standard output for normal visibility
                Console.Write(message);
                Console.Out.Flush();
                
                // Also write to a debug.log file in the application directory
                File.AppendAllText("debug.log", message);
            }
        }

        public override void WriteLine(string? message)
        {
            if (message != null)
            {
                // Write to standard output for normal visibility
                Console.WriteLine(message);
                Console.Out.Flush();
                
                // Also write to a debug.log file in the application directory
                File.AppendAllText("debug.log", message + Environment.NewLine);
            }
        }
    }

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Force console output mode
        Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });

        // Clear previous debug.log and create fresh one in a known location
        var debugLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log");
        if (File.Exists(debugLogPath))
        {
            File.Delete(debugLogPath);
        }

        // Write initial marker to both console and file
        Console.WriteLine("=== MSPaintEx Debug Output Started ===");
        File.WriteAllText(debugLogPath, "=== MSPaintEx Debug Output Started ===\n");
        
        // Add our custom listener
        Trace.Listeners.Clear();
        Trace.Listeners.Add(new PowerShellTraceListener());
        Trace.AutoFlush = true;
        
        try
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Starting application...");
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Fatal error: {ex}";
            Console.WriteLine(errorMsg);
            File.AppendAllText(debugLogPath, errorMsg + Environment.NewLine);
            throw;
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
