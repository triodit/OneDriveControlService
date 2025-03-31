📄 OneDrive Idle Control Service – Setup Guide (Formatted for PDF/DOC)
________________________________________
🕵️ What Is This?
OneDrive Idle Control is a silent background tool that:
•	❌ Stops OneDrive while you’re active
•	✅ Starts OneDrive again after 60 minutes of inactivity
•	🧼 Cleans up logs automatically
•	👻 Runs silently at login (no windows, no interaction)
Ideal for streamers, gamers, or anyone tired of OneDrive hogging CPU.
________________________________________
🧰 Requirements
•	Windows 10 or 11
•	.NET 6.0 SDK or newer
👉 Download from: https://dotnet.microsoft.com/en-us/download/dotnet/6.0
To check if it’s already installed:
1.	Open PowerShell
2.	Type:
css
CopyEdit
dotnet --list-sdks
3.	You should see something like 6.0.x or higher.
________________________________________
🛠️ Build the App
1.	Download or clone the project.
2.	Open PowerShell
3.	Navigate to the folder with OneDriveControlService.csproj
4.	Run:
r
CopyEdit
dotnet publish -c Release -o publish
5.	You’ll now see a folder called publish with:
CopyEdit
OneDriveControlService.exe
This .exe is fully silent and ready to run in the background.
________________________________________
⚙️ Set It to Run Automatically (Task Scheduler)
1.	Press Win + R, type taskschd.msc, press Enter.
2.	Click Create Task
3.	General Tab:
o	Name: OneDrive Idle Control
o	✅ Check “Run only when user is logged on”
o	✅ Check “Run with highest privileges”
4.	Triggers Tab:
o	Click New
o	Begin the task: At log on
o	✅ Enabled
5.	Actions Tab:
o	Click New
o	Action: Start a program
o	Program/script:
(Browse to your published .exe file)
e.g.
C:\Users\sarah\Documents\OneDriveControlService\publish\OneDriveControlService.exe
6.	Click OK to save.
The app will now launch silently every time you log in.
________________________________________
📁 Where Are the Logs?
Logs are written to the same folder as the .exe, like:
lua
CopyEdit
onedrive_log_20250324.log
•	Logs are created only when something happens
•	They are automatically deleted after 48 hours
•	You won’t see idle logs unless OneDrive is started or stopped
________________________________________
⚙️ Change Idle Time
Want to change the delay before OneDrive restarts?
1.	Open Worker.cs
2.	Look for:
csharp
CopyEdit
const int IdleThresholdMinutes = 60;
3.	Change 60 to however many minutes you want
4.	Rebuild:
r
CopyEdit
dotnet publish -c Release -o publish
________________________________________
🔁 Updating the App
1.	End the current task or stop it via Task Manager
2.	Make any changes to the code
3.	Rebuild with:
r
CopyEdit
dotnet publish -c Release -o publish
4.	Replace the .exe — Task Scheduler will use the new version automatically
________________________________________
🧼 To Uninstall
•	Open Task Scheduler
•	Find OneDrive Idle Control under “Task Scheduler Library”
•	Right-click → Delete
•	Delete the project folder if no longer needed
Done. Clean removal, no registry edits, no services.
________________________________________
❓ FAQ
Will this delete my OneDrive files?
🛑 No. It just closes the app while you’re using the PC.
Will I see a window?
No — the app is built to run as a background process (WinExe type).
Can I use this with other apps like Dropbox?
Yes! You can adapt the logic in Worker.cs to monitor and control any background app by name.

