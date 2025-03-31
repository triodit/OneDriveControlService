using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using System.Globalization;

public class Worker : BackgroundService
{
    const int IdleThresholdMinutes = 60;
    readonly string overrideFile;

    public Worker()
    {
        overrideFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OneDriveControl", "override.txt");
        var directoryPath = Path.GetDirectoryName(overrideFile);
        if (directoryPath != null)
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

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
            try
            {
                int idleMinutes = GetIdleTimeMinutes();
                bool oneDriveIsRunning = Process.GetProcessesByName("OneDrive").Any();

                if (IsOverrideActive(out TimeSpan remaining))
                {
                    Log($"[~] Override active: {remaining.TotalMinutes:N1} min remaining");
                    if (!oneDriveIsRunning)
                    {
                        StartOneDriveUnElevated();
                    }
                }
                else
                {
                    if (idleMinutes >= IdleThresholdMinutes)
                    {
                        if (!oneDriveIsRunning)
                        {
                            StartOneDriveUnElevated();
                        }
                    }
                    else
                    {
                        if (oneDriveIsRunning)
                        {
                            KillOneDrive();
                        }
                    }
                }
            }
            catch (Exception) { }

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

    private bool IsOverrideActive(out TimeSpan remaining)
    {
        remaining = TimeSpan.Zero;

        try
        {
            if (File.Exists(overrideFile))
            {
                string text = File.ReadAllText(overrideFile).Trim();

                if (DateTime.TryParseExact(text, "yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime untilUtc))
                {
                    if (DateTime.UtcNow < untilUtc)
                    {
                        remaining = untilUtc - DateTime.UtcNow;
                        return true;
                    }
                    else
                    {
                        File.Delete(overrideFile);
                    }
                }
            }
        }
        catch { }

        return false;
    }

    private void StartOneDriveUnElevated()
    {
        Log("[~] Attempting to start OneDrive...");

        string[] commonPaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\\OneDrive\\OneDrive.exe"),
            @"C:\\Program Files\\Microsoft OneDrive\\OneDrive.exe",
            @"C:\\Program Files (x86)\\Microsoft OneDrive\\OneDrive.exe"
        };

        foreach (string path in commonPaths)
        {
            if (File.Exists(path))
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    Process.Start(psi);
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

            foreach (var file in Directory.GetFiles(logDir, "onedrive_log_*.log"))
            {
                string datePart = Path.GetFileName(file).Substring("onedrive_log_".Length, 8);
                if (DateTime.TryParseExact(datePart, "yyyyMMdd", null, DateTimeStyles.None, out DateTime logDate))
                {
                    if ((DateTime.Now - logDate).TotalDays > 2)
                    {
                        File.Delete(file);
                    }
                }
            }

            File.AppendAllText(todayFile, $"{DateTime.Now}: {message}{Environment.NewLine}");
        }
        catch { }
    }
}
