using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

public class Worker : BackgroundService
{
    const int IdleThresholdMinutes = 60;

    [DllImport("User32.dll")]
    static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [StructLayout(LayoutKind.Sequential)]
    struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log("Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            int idleMinutes = GetIdleTimeMinutes();
            bool oneDriveIsRunning = Process.GetProcessesByName("OneDrive").Any();

            if (idleMinutes >= IdleThresholdMinutes)
            {
                if (!oneDriveIsRunning)
                {
                    StartOneDrive();
                }
            }
            else
            {
                if (oneDriveIsRunning)
                {
                    KillOneDrive();
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private int GetIdleTimeMinutes()
    {
        LASTINPUTINFO lastInput = new LASTINPUTINFO();
        lastInput.cbSize = (uint)Marshal.SizeOf(lastInput);
        GetLastInputInfo(ref lastInput);
        uint idleTime = (uint)Environment.TickCount - lastInput.dwTime;
        return (int)(idleTime / 60000);
    }

    private void StartOneDrive()
    {
        Log("[~] Attempting to start OneDrive...");

        string[] commonPaths = new[]
        {
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft\\OneDrive\\OneDrive.exe"),
            @"C:\Program Files\Microsoft OneDrive\OneDrive.exe",
            @"C:\Program Files (x86)\Microsoft OneDrive\OneDrive.exe"
        };

        foreach (string path in commonPaths)
        {
            if (File.Exists(path))
            {
                try
                {
                    Process.Start(path);
                    Log($"[+] Started OneDrive from: {path}");
                    return;
                }
                catch (Exception ex)
                {
                    Log($"[!] Failed to start OneDrive from {path}: {ex.Message}");
                }
            }
        }

        Log("[!] OneDrive executable not found in common paths.");
    }

    private void KillOneDrive()
    {
        Log("[~] Attempting to kill OneDrive...");

        try
        {
            var processes = Process.GetProcessesByName("OneDrive");
            if (processes.Length == 0)
            {
                Log("[~] No OneDrive processes found to kill");
            }

            foreach (var proc in processes)
            {
                try
                {
                    Log($"[~] Killing OneDrive: Id={proc.Id}, SessionId={proc.SessionId}, StartTime={proc.StartTime}");
                    proc.Kill();
                    proc.WaitForExit();
                    Log("[-] Successfully killed OneDrive process");
                }
                catch (Exception ex)
                {
                    Log("[!] Error killing OneDrive: " + ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            Log("[!] Unexpected error during KillOneDrive: " + ex.Message);
        }
    }

    private void Log(string message)
    {
        try
        {
            string logDir = AppDomain.CurrentDomain.BaseDirectory;
            string todayFile = Path.Combine(logDir, $"onedrive_log_{DateTime.Now:yyyyMMdd}.log");

            // Delete logs older than 2 days
            foreach (var file in Directory.GetFiles(logDir, "onedrive_log_*.log"))
            {
                string datePart = Path.GetFileName(file).Substring("onedrive_log_".Length, 8);
                if (DateTime.TryParseExact(datePart, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime logDate))
                {
                    if ((DateTime.Now - logDate).TotalDays > 2)
                    {
                        File.Delete(file);
                    }
                }
            }

            File.AppendAllText(todayFile, $"{DateTime.Now}: {message}{Environment.NewLine}");
        }
        catch
        {
            // Silently ignore logging errors
        }
    }
}
