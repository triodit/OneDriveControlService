using System;
using System.Text;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;

class TrayApp : Form
{
    private NotifyIcon trayIcon;
    private ContextMenuStrip trayMenu;
    private string overrideFile;

    public TrayApp()
    {
        overrideFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OneDriveControl", "override.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(overrideFile));

        trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Override: 15 minutes", null, (s, e) => SetOverride(TimeSpan.FromMinutes(15)));
        trayMenu.Items.Add("Override: 1 hour", null, (s, e) => SetOverride(TimeSpan.FromHours(1)));
        trayMenu.Items.Add("Override: 4 hours", null, (s, e) => SetOverride(TimeSpan.FromHours(4)));
        trayMenu.Items.Add("Override: 24 hours", null, (s, e) => SetOverride(TimeSpan.FromHours(24)));
        trayMenu.Items.Add("Override: Until reboot", null, (s, e) => SetOverride(TimeSpan.FromDays(7)));
        trayMenu.Items.Add("Override: Indefinite", null, (s, e) => SetOverride(TimeSpan.FromDays(3650)));
        trayMenu.Items.Add("Cancel override", null, (s, e) => ClearOverride());
        trayMenu.Items.Add("Exit Tray", null, (s, e) => Application.Exit());

        trayIcon = new NotifyIcon()
        {
            Text = "OneDrive Control",
            Icon = new Icon(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tray_icon.ico")),
            ContextMenuStrip = trayMenu,
            Visible = true
        };

        this.WindowState = FormWindowState.Minimized;
        this.ShowInTaskbar = false;

        TryAddToStartup();
    }

    private void SetOverride(TimeSpan duration)
    {
        DateTime until = DateTime.UtcNow.Add(duration);
        File.WriteAllText(overrideFile, until.ToString("o"));
        trayIcon.BalloonTipTitle = "OneDrive Control";
        trayIcon.BalloonTipText = $"Override set until {until.ToLocalTime()}";
        trayIcon.ShowBalloonTip(3000);
    }

    private void ClearOverride()
    {
        if (File.Exists(overrideFile))
            File.Delete(overrideFile);
        trayIcon.BalloonTipTitle = "OneDrive Control";
        trayIcon.BalloonTipText = "Override canceled. Auto mode resumed.";
        trayIcon.ShowBalloonTip(3000);
    }

    private void TryAddToStartup()
    {
        try
        {
            string startupDir = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string exePath = Application.ExecutablePath;
            string shortcutPath = Path.Combine(startupDir, "OneDriveTray.lnk");

            if (!File.Exists(shortcutPath))
            {
                ShellLink.CreateShortcut(shortcutPath, exePath);
            }
        }
        catch { }
    }

    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new TrayApp());
    }
}

static class ShellLink
{
    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    internal class ShellLinkCoClass { }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    internal interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, ref IntPtr pfd, uint fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
        void Resolve(IntPtr hwnd, uint fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("0000010B-0000-0000-C000-000000000046")]
    internal interface IPersistFile
    {
        void GetClassID(out Guid pClassID);
        void IsDirty();
        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
        void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, bool fRemember);
        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
        void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
    }

    public static void CreateShortcut(string shortcutPath, string targetPath)
    {
        var link = (IShellLinkW)new ShellLinkCoClass();
        link.SetPath(targetPath);

        ((IPersistFile)link).Save(shortcutPath, false);
    }
}
