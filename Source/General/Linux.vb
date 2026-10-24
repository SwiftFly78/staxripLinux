
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Text
Imports System.Threading

'POSIX shell helpers
Public Class ShellHelp
    'quotes a value for /bin/sh using single quotes
    Shared Function Quote(value As String) As String
        If value Is Nothing Then
            Return "''"
        End If

        Return "'" + value.Replace("'", "'\''") + "'"
    End Function

    Shared ReadOnly Property Shell As String
        Get
            Return "/bin/sh"
        End Get
    End Property

    'starts a command line with /bin/sh -c, used for pipes like vspipe | x265
    Shared Sub SetShellCommand(psi As ProcessStartInfo, commandLine As String)
        psi.FileName = Shell
        psi.Arguments = ""
        psi.ArgumentList.Clear()
        psi.ArgumentList.Add("-c")
        psi.ArgumentList.Add(commandLine)
    End Sub

    'sets arguments split by SplitArguments, .NET would split them with the rules of Windows
    Shared Sub SetArguments(psi As ProcessStartInfo, arguments As String)
        psi.Arguments = ""
        psi.ArgumentList.Clear()

        For Each i In SplitArguments(arguments)
            psi.ArgumentList.Add(i)
        Next
    End Sub

    'splits arguments with the quoting rules of /bin/sh but without expansions, so command lines
    'work the same when executed directly or by the shell, backslashes outside of quotes
    'only escape quotes, so that arguments like ffmpeg filters keep their backslashes
    Shared Function SplitArguments(value As String) As List(Of String)
        Dim ret As New List(Of String)

        If value Is Nothing Then
            Return ret
        End If

        Dim sb As New StringBuilder
        Dim hasArg = False
        Dim i = 0

        While i < value.Length
            Dim c = value(i)

            If Char.IsWhiteSpace(c) Then
                If hasArg Then
                    ret.Add(sb.ToString)
                    sb.Clear()
                    hasArg = False
                End If
            ElseIf c = "'"c Then
                hasArg = True
                Dim quoteEnd = value.IndexOf("'"c, i + 1)

                If quoteEnd < 0 Then
                    quoteEnd = value.Length
                End If

                sb.Append(value, i + 1, quoteEnd - i - 1)
                i = quoteEnd
            ElseIf c = """"c Then
                hasArg = True
                i += 1

                While i < value.Length AndAlso value(i) <> """"c
                    If value(i) = "\"c AndAlso i + 1 < value.Length AndAlso "\""$`".Contains(value(i + 1)) Then
                        i += 1
                    End If

                    sb.Append(value(i))
                    i += 1
                End While
            ElseIf c = "\"c AndAlso i + 1 < value.Length AndAlso (value(i + 1) = """"c OrElse value(i + 1) = "'"c) Then
                hasArg = True
                i += 1
                sb.Append(value(i))
            Else
                hasArg = True
                sb.Append(c)
            End If

            i += 1
        End While

        If hasArg Then
            ret.Add(sb.ToString)
        End If

        Return ret
    End Function

    'searches an executable in PATH
    Shared Function Which(name As String) As String
        If name = "" Then
            Return ""
        End If

        If name.Contains("/"c) Then
            Return If(File.Exists(name), name, "")
        End If

        Dim paths = If(Environment.GetEnvironmentVariable("PATH"), "").Split(Path.PathSeparator)

        For Each dir As String In paths
            If dir = "" Then
                Continue For
            End If

            Dim fp = Path.Combine(dir, name)

            If File.Exists(fp) Then
                Return fp
            End If
        Next

        Return ""
    End Function
End Class

'tools that only exist for Windows like eac3to run with Wine, which maps the drive Z: to the root folder
Public Class WineHelp
    'eac3to reads arguments that start with / as options
    Shared Function ToWinePath(path As String) As String
        If path Is Nothing OrElse Not path.StartsWith("/") Then Return path
        Return "Z:" + path.Replace("/"c, "\"c)
    End Function
End Class

'resolves the Windows DLL names used in DllImport declarations to Linux shared libraries
Public Class NativeLibraries
    Private Shared Initialized As Boolean

    Private Shared ReadOnly Map As New Dictionary(Of String, String()) From {
        {"MediaInfo.dll", {"libmediainfo.so.0", "libmediainfo.so"}},
        {"MediaInfo", {"libmediainfo.so.0", "libmediainfo.so"}},
        {"vsscript.dll", {"libvapoursynth-script.so.0", "libvapoursynth-script.so"}},
        {"vsscript", {"libvapoursynth-script.so.0", "libvapoursynth-script.so"}},
        {"AviSynth.dll", {"libavisynth.so", "libavisynth.so.11", "libavisynth.so.10", "libavisynth.so.9"}},
        {"avisynth", {"libavisynth.so", "libavisynth.so.11", "libavisynth.so.10", "libavisynth.so.9"}},
        {"vulkan-1.dll", {"libvulkan.so.1", "libvulkan.so"}},
        {"libc", {"libc.so.6", "libc.musl-x86_64.so.1", "libc.musl-aarch64.so.1", "libc.so"}}
    }

    Shared Sub Init()
        If Initialized Then
            Exit Sub
        End If

        Initialized = True
        NativeLibrary.SetDllImportResolver(GetType(NativeLibraries).Assembly, AddressOf Resolve)
    End Sub

    Shared Function Resolve(name As String, assembly As Assembly, searchPath As DllImportSearchPath?) As IntPtr
        Dim candidates As String() = Nothing

        If Not Map.TryGetValue(name, candidates) Then
            Return IntPtr.Zero
        End If

        For Each candidate In GetCandidates(name)
            Dim handle As IntPtr

            If NativeLibrary.TryLoad(candidate, handle) Then
                Return handle
            End If
        Next

        Return IntPtr.Zero
    End Function

    Shared Function GetCandidates(name As String) As IEnumerable(Of String)
        Dim ret = GetCustomPaths(name).Concat(If(Map.ContainsKey(name), Map(name), {name}))

        'VSScript finds its config by its own file path, loading it via symlink breaks it
        If name = "vsscript.dll" OrElse name = "vsscript" Then
            Dim moduleDir = VapourSynthInfo.GetModuleDir()
            Dim pipLibrary = If(moduleDir <> "", Path.Combine(moduleDir, "libvsscript.so"), "")

            ret = {Environment.GetEnvironmentVariable("VSSCRIPT_PATH")}.Concat(ret).Concat({pipLibrary}).
                Where(Function(x) x <> "").
                Select(Function(x) GetRealPath(x)).
                ToArray
        End If

        Return ret
    End Function

    'canonical path of a library file, bare library names are searched in the library dirs
    Shared Function GetRealPath(library As String) As String
        Dim fp = If(library.StartsWith("/"), library, LinuxNames.FindLibrary(library))

        If fp = "" Then
            Return library
        End If

        Dim ptr = realpath(fp, IntPtr.Zero)

        If ptr = IntPtr.Zero Then
            Return library
        End If

        Try
            Return Marshal.PtrToStringUTF8(ptr)
        Finally
            free(ptr)
        End Try
    End Function

    'Environment.SetEnvironmentVariable doesn't change the environment native libraries see
    Shared Sub SetNativeEnvironmentVariable(name As String, value As String)
        setenv(name, value, 1)
        Environment.SetEnvironmentVariable(name, value)
    End Sub

    <DllImport("libc")>
    Private Shared Function setenv(name As String, value As String, overwrite As Integer) As Integer
    End Function

    <DllImport("libc")>
    Private Shared Function realpath(path As String, resolved As IntPtr) As IntPtr
    End Function

    <DllImport("libc")>
    Private Shared Sub free(ptr As IntPtr)
    End Sub

    'a custom library path can be configured in the apps dialog
    Shared Function GetCustomPaths(name As String) As IEnumerable(Of String)
        Dim ret As New List(Of String)

        Try
            Select Case name
                Case "MediaInfo.dll", "MediaInfo"
                    If Package.MediaInfo.Path.FileExists Then ret.Add(Package.MediaInfo.Path)
                Case "vsscript.dll", "vsscript"
                    If Package.VapourSynth.Path.FileExists Then
                        ret.Add(Path.Combine(Package.VapourSynth.Directory, "libvapoursynth-script.so.0"))
                        ret.Add(Path.Combine(Package.VapourSynth.Directory, "libvapoursynth-script.so"))
                    End If
                Case "AviSynth.dll", "avisynth"
                    If Package.AviSynth.Path.FileExists Then ret.Add(Package.AviSynth.Path)
            End Select
        Catch
        End Try

        Return ret.Where(Function(x) File.Exists(x))
    End Function

    Shared Function IsAvailable(name As String) As Boolean
        Dim handle As IntPtr

        For Each candidate In GetCandidates(name)
            If NativeLibrary.TryLoad(candidate, handle) Then
                Return True
            End If
        Next

        Return False
    End Function
End Class

'integration with the Linux desktop
Public Class DesktopHelp
    'xdg-utils isn't installed everywhere, the tools of GLib, KDE and Xfce work as well
    Shared Sub Open(fileOrUrl As String)
        Try
            Dim opener = {"xdg-open", "gio", "kde-open", "kde-open5", "exo-open"}.FirstOrDefault(Function(x) ShellHelp.Which(x) <> "")

            If opener = "" Then
                MsgError("No application found to open:", fileOrUrl + BR2 + "Install xdg-utils.")
                Exit Sub
            End If

            Dim psi As New ProcessStartInfo(opener) With {.UseShellExecute = False}

            If opener = "gio" Then
                psi.ArgumentList.Add("open")
            End If

            psi.ArgumentList.Add(fileOrUrl)
            Process.Start(psi)?.Dispose()
        Catch ex As Exception
            g.ShowException(ex, "Failed to open", fileOrUrl)
        End Try
    End Sub

    'returns True if the command exits successfully within a few seconds
    Shared Function TryRun(file As String, ParamArray args As String()) As Boolean
        If ShellHelp.Which(file) = "" Then
            Return False
        End If

        Try
            Using proc As New Process
                proc.StartInfo.FileName = file
                proc.StartInfo.UseShellExecute = False
                proc.StartInfo.RedirectStandardError = True
                proc.StartInfo.RedirectStandardOutput = True

                For Each arg In args
                    proc.StartInfo.ArgumentList.Add(arg)
                Next

                proc.Start()
                proc.WaitForExit(5000)
                Return proc.HasExited AndAlso proc.ExitCode = 0
            End Using
        Catch
        End Try
    End Function

    'shows a file selected in the file manager, uses the freedesktop FileManager1 D-Bus interface
    Shared Sub SelectFile(filePath As String)
        Try
            If Not File.Exists(filePath) AndAlso Not Directory.Exists(filePath) Then
                Open(filePath.Dir)
                Exit Sub
            End If

            Dim uri = New Uri(Path.GetFullPath(filePath)).AbsoluteUri

            If TryRun("dbus-send", "--session", "--print-reply", "--dest=org.freedesktop.FileManager1", "--type=method_call",
                "/org/freedesktop/FileManager1", "org.freedesktop.FileManager1.ShowItems", "array:string:" + uri, "string:") Then

                Exit Sub
            End If

            If TryRun("gdbus", "call", "--session", "--dest", "org.freedesktop.FileManager1", "--object-path",
                "/org/freedesktop/FileManager1", "--method", "org.freedesktop.FileManager1.ShowItems", "['" + uri + "']", "") Then

                Exit Sub
            End If
        Catch
        End Try

        Open(If(Directory.Exists(filePath), filePath, filePath.Dir))
    End Sub

    Shared ReadOnly Property TerminalEmulators As String()
        Get
            Return {"xdg-terminal-exec", "x-terminal-emulator", "konsole", "ptyxis", "gnome-terminal", "kgx",
                "xfce4-terminal", "mate-terminal", "lxterminal", "qterminal", "terminator", "tilix", "alacritty",
                "kitty", "foot", "wezterm", "urxvt", "st", "xterm"}
        End Get
    End Property

    Shared Function GetTerminal() As String
        If s.TerminalEmulator <> "" AndAlso ShellHelp.Which(s.TerminalEmulator.Split(" "c)(0)) <> "" Then
            Return s.TerminalEmulator
        End If

        'used by some desktops and window managers to define the preferred terminal
        Dim envTerminal = Environment.GetEnvironmentVariable("TERMINAL")

        If envTerminal <> "" AndAlso ShellHelp.Which(envTerminal.Split(" "c)(0)) <> "" Then
            Return envTerminal
        End If

        For Each i In TerminalEmulators
            If ShellHelp.Which(i) <> "" Then
                Return i
            End If
        Next

        Return ""
    End Function

    'opens a terminal emulator with the shell or a command
    Shared Sub OpenTerminal(Optional command As String = Nothing, Optional workingDirectory As String = Nothing)
        If command <> "" Then
            Dim exe = command.Split(" "c)(0)

            If ShellHelp.Which(exe) = "" Then
                MsgError(exe + " wasn't found.", "Install it with your package manager.")
                Exit Sub
            End If
        End If

        Dim shell = Environment.GetEnvironmentVariable("SHELL")
        RunInTerminal(If(command <> "", command, If(shell <> "", shell, ShellHelp.Shell)), workingDirectory, False)
    End Sub

    'runs a shell command line in a terminal emulator, the terminal stays open
    Shared Sub RunInTerminal(commandLine As String, Optional workingDirectory As String = Nothing, Optional waitForEnter As Boolean = True)
        Dim terminal = GetTerminal()

        If terminal = "" Then
            MsgError("No terminal emulator found.", "Install a terminal emulator like xterm or konsole or define one in the settings.")
            Exit Sub
        End If

        Dim script = If(waitForEnter, commandLine + "; echo; echo 'Press Enter to close...'; read dummy", commandLine)
        Dim psi As New ProcessStartInfo With {.UseShellExecute = False}
        Dim parts = terminal.Split({" "c}, StringSplitOptions.RemoveEmptyEntries)
        psi.FileName = parts(0)

        For Each i In parts.Skip(1)
            psi.ArgumentList.Add(i)
        Next

        'how the command to execute is passed differs between the terminal emulators
        Select Case Path.GetFileName(parts(0))
            Case "gnome-terminal", "kgx", "tilix", "ptyxis"
                psi.ArgumentList.Add("--")
            Case "xfce4-terminal", "mate-terminal", "terminator"
                psi.ArgumentList.Add("-x")
            Case "wezterm"
                psi.ArgumentList.Add("start")
                psi.ArgumentList.Add("--")
            Case "xdg-terminal-exec", "kitty", "foot"
            Case Else
                psi.ArgumentList.Add("-e")
        End Select

        psi.ArgumentList.Add(ShellHelp.Shell)
        psi.ArgumentList.Add("-c")
        psi.ArgumentList.Add(script)

        If workingDirectory <> "" AndAlso Directory.Exists(workingDirectory) Then
            psi.WorkingDirectory = workingDirectory
        Else
            psi.WorkingDirectory = Folder.Home
        End If

        Try
            Proc.SetEnvironmentVariables(psi)
            Process.Start(psi)?.Dispose()
        Catch ex As Exception
            g.ShowException(ex, "Failed to start terminal", terminal)
        End Try
    End Sub
End Class

'prevents standby while processing and executes the shutdown modes via systemd
Public Class PowerRequest
    Private Shared InhibitProcess As Process

    Shared Sub SuppressStandby()
        If InhibitProcess IsNot Nothing AndAlso Not InhibitProcess.HasExited Then
            Exit Sub
        End If

        'elogind provides the same tool on distributions without systemd
        Dim inhibit = {"systemd-inhibit", "elogind-inhibit"}.FirstOrDefault(Function(x) ShellHelp.Which(x) <> "")

        If inhibit = "" Then
            Exit Sub
        End If

        Try
            Dim psi As New ProcessStartInfo(inhibit) With {.UseShellExecute = False, .RedirectStandardInput = True,
                .RedirectStandardError = True, .RedirectStandardOutput = True}

            'cat ends when the input pipe closes, which also happens when StaxRip crashes or is killed
            For Each arg In {"--what=sleep:idle", "--who=" + AppInfo.ProductName, "--why=Encoding", "--mode=block", "cat"}
                psi.ArgumentList.Add(arg)
            Next

            InhibitProcess = Process.Start(psi)
        Catch
        End Try
    End Sub

    Shared Sub EnableStandby()
        Try
            If InhibitProcess IsNot Nothing AndAlso Not InhibitProcess.HasExited Then
                InhibitProcess.Kill(True)
            End If
        Catch
        End Try

        InhibitProcess = Nothing
    End Sub
End Class

Public Class Shutdown
    Shared Sub Commit(mode As ShutdownMode)
        Dim args As String = Nothing

        Select Case mode
            Case ShutdownMode.Standby
                args = "suspend"
            Case ShutdownMode.Hibernate
                args = "hibernate"
            Case ShutdownMode.Hybrid
                args = "hybrid-sleep"
            Case ShutdownMode.Shutdown
                args = "poweroff"
        End Select

        If args Is Nothing Then
            Exit Sub
        End If

        Try
            If mode = ShutdownMode.Shutdown AndAlso s.ShutdownTimeout > 0 AndAlso Not s.ShutdownForce Then
                Thread.Sleep(s.ShutdownTimeout * 1000)
            End If

            'loginctl of elogind supports the same commands on distributions without systemd
            Dim tool = {"systemctl", "loginctl"}.FirstOrDefault(Function(x) ShellHelp.Which(x) <> "")

            If tool = "" Then
                MsgError("Failed to execute shutdown mode " + mode.ToString, "Neither systemctl nor loginctl was found.")
                Exit Sub
            End If

            Dim psi As New ProcessStartInfo(tool) With {.UseShellExecute = False}
            psi.ArgumentList.Add(args)
            Process.Start(psi)?.Dispose()
        Catch ex As Exception
            g.ShowException(ex, "Failed to execute shutdown mode " + mode.ToString)
        End Try
    End Sub
End Class

Public Enum ShutdownMode
    [Nothing]
    Close
    Standby
    Hibernate
    Hybrid
    Shutdown
End Enum

'replacement for the application info of WinForms
Public Class AppInfo
    Shared ReadOnly Property ProductName As String
        Get
            Return "StaxRipLinux"
        End Get
    End Property

    Shared ReadOnly Property ProductVersion As String
        Get
            Return GetType(AppInfo).Assembly.GetName().Version.ToString(3)
        End Get
    End Property

    Shared ReadOnly Property StartupPath As String
        Get
            Return Folder.Startup
        End Get
    End Property

    'the command to start StaxRip again, the apphost when available
    Shared ReadOnly Property ExecutablePath As String
        Get
            Dim apphost = Path.Combine(Folder.Startup, "StaxRip")

            If File.Exists(apphost) Then
                Return apphost
            End If

            Return Environment.ProcessPath
        End Get
    End Property
End Class

'WinForms enum stored in the settings
Public Enum FormBorderStyle
    None
    FixedSingle
    Fixed3D
    FixedDialog
    Sizable
    FixedToolWindow
    SizableToolWindow
End Enum
