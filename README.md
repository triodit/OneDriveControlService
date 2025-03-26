# OneDrive Idle Control - Setup Guide (2025 Edition)

## What It Does

This tool:
- Stops OneDrive when you're actively using your computer
- Restarts OneDrive after 60 minutes of idle time
- Launches OneDrive with administrator rights (requires UAC or elevated task)
- Cleans up logs older than 48 hours
- Runs invisibly in the background

## Requirements

- Windows 10 or 11
- .NET 8.0 SDK or newer: https://dotnet.microsoft.com/en-us/download/dotnet/8.0
- C# support in Visual Studio Code (install "C#" by Microsoft)
- Run this command to add the missing dependency:

```bash
dotnet add package Microsoft.Extensions.Hosting
```

## Installation Steps

1. Clone or unzip the project.
2. Open PowerShell in the project folder.
3. Run:

```bash
dotnet restore
dotnet publish -c Release -o publish
```

4. The compiled executable will be inside the `publish` folder.

## Task Scheduler Setup (Optional for Startup)

1. Open Task Scheduler (Win + R → `taskschd.msc`).
2. Create a new Task:
   - Run only when user is logged on
   - Run with highest privileges (important for admin launch)
3. Add a trigger: "At log on"
4. Add an action: "Start a program" → point to your `OneDriveControlService.exe`

## Log Files

Logs are saved next to the .exe as:

```
onedrive_log_YYYYMMDD.log
```

Only events (start, kill, errors) are logged. Old logs are deleted automatically after 2 days.

## To Customize Idle Time

Edit the `Worker.cs` file:

```csharp
const int IdleThresholdMinutes = 60;
```

Then rebuild the project.

## To Update

Rebuild the project with:

```bash
dotnet publish -c Release -o publish
```

## To Uninstall

1. Delete the scheduled task (if created).
2. Delete the published files and source folder.

## Support

If OneDrive does not launch, make sure your scheduled task runs with highest privileges or run the app manually once to approve UAC.
