# [Documentation](../README.md) / [Usage](README.md) / Events

Events let StaxRipLinux run commands automatically, for example archive the log after every job, shut down after the
last one, or change a setting when a source with a certain resolution is opened. There are two ways to react to an
event:

- **Event commands**, created in `Tools > Advanced > Event Commands...`, run a [command](Commands.md) with
  parameters, optionally only when [criteria](#criteria) are met. See [Events](User-Interface/Events.md) in the user
  interface section for the dialogs.
- **PowerShell scripts** named after the event, like `AfterJobProcessed.ps1`, run when they're in the `Scripts` folder
  of the settings (`Tools > Folders > Scripts`), see [PowerShell Scripting](PowerShell.md).

## Events

| Event | When it's raised |
|---|---|
| After Project Loaded | A project or template was loaded, also when a job starts |
| After Source Opened | A source was opened and demuxed, before the project is processed further |
| After Source Loaded | Right after *After Source Opened*, when the source is completely loaded |
| After Project Or Source Loaded | After a project or a source was loaded |
| Before Muxing When Source Opening | While a source is opened, before the container options are initialized with the demuxed tracks |
| Before Job Adding | Before the current project is added to the job list |
| After Job Added | After the current project was added to the job list |
| Before Job Processed | When a job starts, before its source is checked |
| Before Processing | When the source of the job is ready and the processing begins |
| While Processing | During the encoding, at most once per minute, with the progress |
| After Video Encoded | After the video of a job was encoded |
| After Job Muxed | After the target file of a job was muxed |
| After Job Processed | After a job was finished successfully |
| After Job Failed | After a job failed |
| After Jobs Processed | After the last job of the job list |
| Application Exit | When StaxRipLinux exits |

The macros of the project, like `%source_file%` or `%target_file%`, can be used in the parameters. During
*While Processing*, the [While Processing Macros](Macros.md#while-processing-macros) contain the progress and the
command line of the running process.

## Criteria

Criteria compare a [macro](Macros.md) with a value. Numbers can be compared with *Is*, *Is Not*, *Is Higher Than* and
*Is Lower Than*, texts with *Is*, *Is Not*, *Contains* and *Contains Not*, true/false values with *Is* and *Is Not*.
With *Match All* the command runs only when all criteria are met, with *Match Any* when one of them is met.

## Examples

**Copy the target file to a NAS after each job:**

- Event: *After Job Processed*
- Command: `ExecuteCommandLine`
- Command Line: `cp "%target_file%" /mnt/nas/videos/`

**Load another template for UHD sources:**

- Event: *After Source Loaded*
- Criteria: `%source_height%` *Is Higher Than* `1080`
- Command: `LoadProfile` or `ExecutePowerShellCode` with code that changes the project

**Send a desktop notification when all jobs are done:**

- Event: *After Jobs Processed*
- Command: `ExecuteCommandLine`
- Command Line: `notify-send "StaxRipLinux" "All jobs are done"`

**Shut down after the jobs:** choose `Shutdown` under *When finished do* in the processing window, see
[Jobs](User-Interface/Jobs.md#processing). An event command with the command `Shutdown` does the same for every
run.

> [!TIP]
> *Write Event Commands to log file* in the [Settings](User-Interface/Settings.md#logs) logs every event command that
> runs, which helps to find out why a command doesn't run.
