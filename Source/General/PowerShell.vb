
Imports System.Collections.ObjectModel
Imports System.Management.Automation
Imports System.Management.Automation.Runspaces
Imports System.Runtime.InteropServices

Public Class PowerShell
    Shared References As New List(Of Object)
    Shared Property InitCode As String

    Shared Function Invoke(
        code As String,
        Optional variableName As String = Nothing,
        Optional variableValue As Object = Nothing,
        Optional args As String() = Nothing) As Collection(Of PSObject)

        Dim runspace = RunspaceFactory.CreateRunspace()
        runspace.ApartmentState = Threading.ApartmentState.STA
        runspace.ThreadOptions = PSThreadOptions.UseCurrentThread
        runspace.Open()

        'execution policies don't exist on Linux
        Dim pipeline = runspace.CreatePipeline()

        If InitCode <> "" Then
            pipeline.Commands.AddScript(InitCode)
        End If

        pipeline.Commands.AddScript(code)

        If Not args.NothingOrEmpty Then
            Dim scriptCommand = pipeline.Commands(pipeline.Commands.Count - 1)

            For Each arg In args
                scriptCommand.Parameters.Add(Nothing, arg)
            Next
        End If

        If variableName <> "" Then
            runspace.SessionStateProxy.SetVariable(variableName, variableValue)
        End If

        If code.Contains("Register-ObjectEvent") Then
            References.Add(runspace)
            References.Add(pipeline)
            Return pipeline.Invoke()
        Else
            Dim ret = pipeline.Invoke()
            pipeline.Dispose()
            runspace.Dispose()
            Return ret
        End If
    End Function

    Shared Function InvokeAndConvert(code As String) As String
        Return InvokeAndConvert(code, Nothing, Nothing)
    End Function

    Shared Function InvokeAndConvert(code As String, varName As String, varValue As Object) As String
        Dim objects = Invoke(code, varName, varValue)

        If objects Is Nothing Then
            Return ""
        End If

        Dim lines As New List(Of String)

        For Each obj In objects
            If Not obj Is Nothing Then
                lines.Add(obj.ToString)
            End If
        Next

        Return String.Join(Environment.NewLine, lines)
    End Function
End Class

'the pwsh of the terminal when PowerShell isn't installed: StaxRipLinux started with --pwsh runs the console
'of the PowerShell SDK it runs the scripts with, with the PSReadLine of the full AppImage
Public Class PowerShellTerminal
    Const SwitchName = "--pwsh"
    Const PR_SET_NAME = 15

    <DllImport("libc")>
    Private Shared Function prctl(operation As Integer, name As String, arg3 As IntPtr, arg4 As IntPtr, arg5 As IntPtr) As Integer
    End Function

    Shared Function IsCommandLine(args As String()) As Boolean
        Return args.Length > 0 AndAlso args(0) = SwitchName
    End Function

    Shared Function Run(args As String()) As Integer
        'pwsh sends telemetry and looks for new versions
        For Each pair In {({"POWERSHELL_TELEMETRY_OPTOUT", "1"}), ({"POWERSHELL_UPDATECHECK", "Off"})}
            If Environment.GetEnvironmentVariable(pair(0)) Is Nothing Then
                Environment.SetEnvironmentVariable(pair(0), pair(1))
            End If
        Next

        'job processing counts the processes named StaxRip
        prctl(PR_SET_NAME, "pwsh", IntPtr.Zero, IntPtr.Zero, IntPtr.Zero)

        Dim modules = Path.Combine(Folder.Apps, "Support", "PowerShell")

        If Directory.Exists(modules) Then
            Dim value = Environment.GetEnvironmentVariable("PSModulePath")
            Environment.SetEnvironmentVariable("PSModulePath", If(value = "", modules, modules + Path.PathSeparator + value))
        End If

        Return Global.Microsoft.PowerShell.ConsoleShell.Start(InitialSessionState.CreateDefault(),
            "PowerShell " & PSVersionInfo.PSVersion.ToString, "", args.Skip(1).ToArray)
    End Function

    'pwsh when PowerShell is installed, otherwise StaxRipLinux, as AppImage with a mount of its own,
    'because the mount of this instance ends with it
    Shared Function GetCommand() As String
        If ShellHelp.Which("pwsh") <> "" Then
            Return "pwsh"
        End If

        Dim exe = If(StaxRipUpdate.AppImagePath <> "", StaxRipUpdate.AppImagePath, AppInfo.ExecutablePath)

        If Path.GetFileName(exe) = "dotnet" Then
            Return ShellHelp.Quote(exe) + " " + ShellHelp.Quote(Path.Combine(Folder.Startup, "StaxRip.dll")) + " " + SwitchName
        End If

        Return ShellHelp.Quote(exe) + " " + SwitchName
    End Function
End Class
