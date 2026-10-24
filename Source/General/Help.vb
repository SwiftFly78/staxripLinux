
Imports System.ComponentModel
Imports System.Globalization
Imports System.Management.Automation
Imports System.Reflection
Imports System.Runtime.Serialization.Formatters.Binary
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading
Imports Microsoft.VisualBasic


Public Class ObjectHelp
    Shared Function GetCompareString(obj As Object) As String
        Dim sb As New StringBuilder
        ParseCompareString(obj, Nothing, sb)
        Return sb.ToString
    End Function

    Shared Sub ParseCompareString(obj As Object, declaringObj As Object, sb As StringBuilder)
        If TypeOf obj Is ICollection Then
            For Each i In DirectCast(obj, ICollection)
                If IsGoodType(i) Then
                    If IsToString(i) Then
                        sb.Append(i.ToString)
                    Else
                        ParseCompareString(i, obj, sb)
                    End If
                End If
            Next
        Else
            If IsGoodType(obj) Then
                Dim t = obj.GetType

                While Not t Is Nothing
                    For Each i As FieldInfo In t.GetFields(BindingFlags.Public Or BindingFlags.NonPublic Or BindingFlags.Instance Or BindingFlags.DeclaredOnly)
                        If Not i.IsNotSerialized Then
                            Dim o = i.GetValue(obj)

                            If IsGoodType(o) Then
                                If IsToString(o) Then
                                    sb.Append(i.Name + "=" + o.ToString + BR)
                                Else
                                    If Not o Is declaringObj Then
                                        ParseCompareString(o, obj, sb)
                                    End If
                                End If
                            End If
                        End If
                    Next

                    t = t.BaseType
                End While
            End If
        End If
    End Sub

    Private Shared Function IsGoodType(o As Object) As Boolean
        If o Is Nothing Then Return False
        If TypeOf o Is Pointer Then Return False
        If TypeOf o Is PropertyChangedEventHandler Then Return False
        If Not o.GetType.IsSerializable Then Return False
        Return True
    End Function

    Private Shared Function IsToString(o As Object) As Boolean
        If Not o Is Nothing Then
            If o.GetType.IsPrimitive Then Return True
            If TypeOf o Is String Then Return True
            'some fields change here
            If TypeOf o Is CultureInfo Then Return True
            'some fields change here
            If TypeOf o Is StringBuilder Then Return True
        End If
    End Function

    <DebuggerHidden()>
    Shared Function GetCopy(Of T)(o As T) As T
        Using ms As New MemoryStream
            Dim bf As New BinaryFormatter
            bf.Serialize(ms, o)
            ms.Position = 0
            Return DirectCast(bf.Deserialize(ms), T)
        End Using
    End Function
End Class

Public Enum RecycleOption
    DeletePermanently
    SendToRecycleBin
End Enum

Public Class FolderHelp
    Shared Function HasFiles(path As String, Optional searchPattern As String = "*") As Boolean
        Return Directory.Exists(path) AndAlso Directory.GetFiles(path, searchPattern).Any()
    End Function

    Shared Sub Create(path As String)
        Directory.CreateDirectory(path)
    End Sub

    Shared Sub Delete(filepath As String, Optional recycleOption As RecycleOption = RecycleOption.DeletePermanently)
        If Directory.Exists(filepath) Then
            Try
                If recycleOption = RecycleOption.SendToRecycleBin AndAlso FileHelp.MoveToTrash(filepath) Then
                    Exit Sub
                End If

                Directory.Delete(filepath, True)
            Catch ex As Exception
                g.ShowException(ex)
            End Try
        End If
    End Sub

    Shared Sub Copy(source As String, target As String)
        Try
            CopyDirectory(source, target)
        Catch ex As Exception
            g.ShowException(ex)
        End Try
    End Sub

    Shared Sub CopyDirectory(source As String, target As String)
        Directory.CreateDirectory(target)

        For Each file In Directory.GetFiles(source)
            IO.File.Copy(file, Path.Combine(target, Path.GetFileName(file)), True)
        Next

        For Each dir As String In Directory.GetDirectories(source)
            CopyDirectory(dir, Path.Combine(target, Path.GetFileName(dir)))
        Next
    End Sub

    Shared Sub Move(source As String, target As String)
        Try
            If Directory.Exists(target) Then
                CopyDirectory(source, target)
                Directory.Delete(source, True)
            Else
                Directory.Move(source, target)
            End If
        Catch ex As Exception
            g.ShowException(ex)
        End Try
    End Sub

    Shared Function Compare(dir1 As String, dir2 As String) As Boolean
        Return dir1.FixDir = dir2.FixDir
    End Function
End Class

Public Class FileHelp
    Shared Sub Move(src As String, dest As String)
        If src.PathEquals(dest) OrElse Not File.Exists(src) Then
            Exit Sub
        End If

        If File.Exists(dest) Then
            Delete(dest)
        End If

        Try
            File.Move(src, dest)
        Catch ex As Exception
            g.ShowException(ex)
        End Try
    End Sub

    Shared Sub Copy(src As String, dest As String)
        If File.Exists(src) Then
            Try
                File.Copy(src, dest, True)
            Catch ex As Exception
                g.ShowException(ex)
            End Try
        End If
    End Sub

    'moves a file or folder to the trash using gio, the freedesktop.org trash is used as fallback
    Shared Function MoveToTrash(path As String) As Boolean
        Try
            Using proc As New Process
                proc.StartInfo.FileName = "gio"
                proc.StartInfo.ArgumentList.Add("trash")
                proc.StartInfo.ArgumentList.Add(path)
                proc.StartInfo.UseShellExecute = False
                proc.StartInfo.RedirectStandardError = True
                proc.StartInfo.RedirectStandardOutput = True
                proc.Start()
                proc.WaitForExit()

                If proc.ExitCode = 0 Then
                    Return True
                End If
            End Using
        Catch
        End Try

        Try
            Dim trashDir = IO.Path.Combine(Folder.XdgDataHome, "Trash")
            Dim filesDir = IO.Path.Combine(trashDir, "files")
            Dim infoDir = IO.Path.Combine(trashDir, "info")
            Directory.CreateDirectory(filesDir)
            Directory.CreateDirectory(infoDir)

            Dim name = IO.Path.GetFileName(path.TrimEnd("/"c))
            Dim target = IO.Path.Combine(filesDir, name)
            Dim counter = 1

            While File.Exists(target) OrElse Directory.Exists(target)
                counter += 1
                target = IO.Path.Combine(filesDir, name + "." & counter)
            End While

            File.WriteAllText(IO.Path.Combine(infoDir, IO.Path.GetFileName(target) + ".trashinfo"),
                "[Trash Info]" + vbLf + "Path=" + Uri.EscapeDataString(IO.Path.GetFullPath(path)).Replace("%2F", "/") + vbLf +
                "DeletionDate=" + Date.Now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture) + vbLf)

            If Directory.Exists(path) Then
                Directory.Move(path, target)
            Else
                File.Move(path, target)
            End If

            Return True
        Catch
        End Try

        Return False
    End Function

    Shared Sub Delete(path As String, Optional recycleOption As RecycleOption = RecycleOption.DeletePermanently)
        Try
            If File.Exists(path) Then
                If recycleOption = RecycleOption.SendToRecycleBin AndAlso MoveToTrash(path) Then
                    Exit Sub
                End If

                File.Delete(path)
            End If
        Catch
        End Try
    End Sub

    Shared Sub Delete(paths As IEnumerable(Of String), Optional recycleOption As RecycleOption = RecycleOption.DeletePermanently)
        If paths Is Nothing OrElse Not paths.Any() Then Return

        For Each path In paths.ToArray
            Delete(path, recycleOption)
        Next
    End Sub

    Shared Function ReadAllText(path As String) As String
        If path.FileExists Then
            Return File.ReadAllText(path)
        End If

        Return ""
    End Function
End Class

Public Class ProcessHelp
    Shared Function GetConsoleOutput(file As String, arguments As String, Optional stderr As Boolean = False) As String
        Return GetConsoleOutput(file, arguments, Nothing, stderr)
    End Function

    'the arguments are passed unchanged, unlike the arguments string they don't need quoting
    Shared Function GetConsoleOutput(file As String, argumentList As IEnumerable(Of String), Optional stderr As Boolean = False) As String
        Return GetConsoleOutput(file, Nothing, argumentList, stderr)
    End Function

    Private Shared Function GetConsoleOutput(file As String, arguments As String, argumentList As IEnumerable(Of String), stderr As Boolean) As String
        Dim ret As String = ""

        Try
            Using proc As New Process
                proc.StartInfo.UseShellExecute = False
                proc.StartInfo.CreateNoWindow = True
                proc.StartInfo.FileName = file

                If file.Dir <> "" AndAlso Directory.Exists(file.Dir) Then
                    proc.StartInfo.WorkingDirectory = file.Dir
                End If

                If argumentList IsNot Nothing Then
                    For Each i In argumentList
                        proc.StartInfo.ArgumentList.Add(i)
                    Next
                Else
                    ShellHelp.SetArguments(proc.StartInfo, arguments)
                End If
                proc.StartInfo.RedirectStandardError = True
                proc.StartInfo.RedirectStandardOutput = True
                proc.Start()

                Dim outTask = proc.StandardOutput.ReadToEndAsync()
                Dim errTask = proc.StandardError.ReadToEndAsync()
                proc.WaitForExit()
                ret = If(stderr, errTask.Result, outTask.Result)
            End Using
        Catch
        End Try

        Return If(ret, "")
    End Function

    'returns the output of a shell command line
    Shared Function GetShellOutput(commandLine As String) As String
        Return GetConsoleOutput(ShellHelp.Shell, {"-c", commandLine})
    End Function

    Shared Function GetChildIDs(pid As Integer) As List(Of Integer)
        Dim ret As New List(Of Integer)

        Try
            For Each dir As String In Directory.GetDirectories("/proc")
                Dim id As Integer

                If Not Integer.TryParse(Path.GetFileName(dir), id) Then
                    Continue For
                End If

                Try
                    Dim stat = File.ReadAllText(Path.Combine(dir, "stat"))
                    Dim afterName = stat.Substring(stat.LastIndexOf(")"c) + 2)
                    Dim parentID = afterName.Split(" "c)(1).ToInt

                    If parentID = pid Then
                        ret.Add(id)
                    End If
                Catch
                End Try
            Next
        Catch
        End Try

        Return ret
    End Function

    Shared Sub KillProcessAndChildren(pid As Integer)
        For Each child In GetChildIDs(pid)
            KillProcessAndChildren(child)
        Next

        Try
            Process.GetProcessById(pid).Kill()
        Catch
        End Try
    End Sub

    Shared Function GetChilds(inputProcess As Process) As List(Of Process)
        Dim ret As New List(Of Process)

        For Each id In GetChildIDs(inputProcess.Id)
            Try
                ret.Add(Process.GetProcessById(id))
            Catch
            End Try
        Next

        Return ret
    End Function

    'all descendants of a process, used to suspend/resume or kill shell pipelines
    Shared Function GetDescendants(inputProcess As Process) As List(Of Process)
        Dim ret As New List(Of Process)
        AddDescendants(inputProcess.Id, ret)
        Return ret
    End Function

    Private Shared Sub AddDescendants(pid As Integer, list As List(Of Process))
        For Each id In GetChildIDs(pid)
            Try
                list.Add(Process.GetProcessById(id))
            Catch
            End Try

            AddDescendants(id, list)
        Next
    End Sub

    Shared Sub SendSignal(pid As Integer, signal As String)
        Try
            Using proc As New Process
                proc.StartInfo.FileName = "kill"
                proc.StartInfo.ArgumentList.Add("-" + signal)
                proc.StartInfo.ArgumentList.Add(pid.ToString)
                proc.StartInfo.UseShellExecute = False
                proc.StartInfo.CreateNoWindow = True
                proc.Start()
                proc.WaitForExit()
            End Using
        Catch
        End Try
    End Sub
End Class

Public Class CommandLineHelp
    Shared Function ConvertText(val As String) As String
        If val = "" Then Return ""
        Return Macro.Expand(val).Replace("""", "'").Trim
    End Function
End Class

Public Class IsoHelp
    Private Shared LoopDevices As New Dictionary(Of String, String)

    'mounts an ISO image using udisks and returns the mount point
    Shared Function Mount(isoPath As String) As String
        If String.IsNullOrWhiteSpace(isoPath) Then Return Nothing

        Try
            Dim output = ProcessHelp.GetConsoleOutput("udisksctl", "loop-setup --no-user-interaction -r -f " + isoPath.Escape)
            Dim match = Regex.Match(output, "(/dev/loop\d+)")

            If Not match.Success Then
                Throw New Exception("Failed to set up a loop device for:" + BR2 + isoPath + BR2 + output)
            End If

            Dim device = match.Groups(1).Value
            LoopDevices(isoPath) = device

            'udisks auto mounts loop devices on most desktops
            For x = 1 To 30
                Dim mountPoint = GetMountPoint(device)

                If mountPoint <> "" Then
                    Return mountPoint
                End If

                If x = 5 Then
                    ProcessHelp.GetConsoleOutput("udisksctl", "mount --no-user-interaction -b " + device)
                End If

                Thread.Sleep(200)
            Next

            Throw New Exception("Failed to mount " + device)
        Catch ex As Exception
            g.ShowException(ex)
        End Try
    End Function

    Shared Function GetMountPoint(device As String) As String
        Try
            For Each line In File.ReadAllLines("/proc/mounts")
                Dim parts = line.Split(" "c)

                If parts.Length > 1 AndAlso parts(0) = device Then
                    Return parts(1).Replace("\040", " ")
                End If
            Next
        Catch
        End Try

        Return ""
    End Function

    Shared Sub Dismount(isoPath As String)
        If String.IsNullOrWhiteSpace(isoPath) OrElse Not LoopDevices.ContainsKey(isoPath) Then Return

        Try
            Dim device = LoopDevices(isoPath)
            ProcessHelp.GetConsoleOutput("udisksctl", "unmount --no-user-interaction -b " + device)
            ProcessHelp.GetConsoleOutput("udisksctl", "loop-delete --no-user-interaction -b " + device)
            LoopDevices.Remove(isoPath)
        Catch ex As Exception
            g.ShowException(ex)
        End Try
    End Sub
End Class
