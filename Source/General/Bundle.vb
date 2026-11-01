
Imports System.Text
Imports System.Text.RegularExpressions

'tools the full AppImage ships in the Apps folder, see Bundle/README.md
Public Class Bundle
    Shared ReadOnly Property PythonDir As String
        Get
            Return Path.Combine(Folder.Apps, "FrameServer", "Python")
        End Get
    End Property

    Shared ReadOnly Property AviSynthDir As String
        Get
            Return Path.Combine(Folder.Apps, "FrameServer", "AviSynth")
        End Get
    End Property

    Private Shared Versions As Dictionary(Of String, String)

    'Apps/versions.txt has lines of "<package ID> = <version>"
    Shared Function GetVersion(pack As Package) As String
        If Versions Is Nothing Then
            Dim dic As New Dictionary(Of String, String)
            Dim versionsFile = Path.Combine(Folder.Apps, "versions.txt")

            If File.Exists(versionsFile) Then
                For Each line In File.ReadAllLines(versionsFile)
                    If line.Contains(" = ") Then
                        dic(line.Left(" = ")) = line.Right(" = ")
                    End If
                Next
            End If

            Versions = dic
        End If

        Dim ret As String = Nothing
        Return If(Versions.TryGetValue(pack.ID, ret), ret, "")
    End Function

    Private Shared VapourSynthDirValue As String

    'the VapourSynth Python module of the bundled Python, it contains vspipe and the libraries
    Shared ReadOnly Property VapourSynthDir As String
        Get
            If VapourSynthDirValue Is Nothing Then
                Dim libDir = Path.Combine(PythonDir, "lib")
                VapourSynthDirValue = ""

                If Directory.Exists(libDir) Then
                    VapourSynthDirValue = If(Directory.GetDirectories(libDir, "python3*").
                        Select(Function(x) Path.Combine(x, "site-packages", "vapoursynth")).
                        FirstOrDefault(Function(x) File.Exists(Path.Combine(x, "libvsscript.so"))), "")
                End If
            End If

            Return VapourSynthDirValue
        End Get
    End Property

    Private Shared SystemVSScriptPath As String

    'distributions set VSSCRIPT_PATH to their VSScript, which the bundled vspipe would load instead of its own
    Shared Function GetVSScriptPath() As String
        If Package.VapourSynth.IsBundled Then
            Return NativeLibraries.GetRealPath(Path.Combine(VapourSynthDir, "libvsscript.so"))
        End If

        Return SystemVSScriptPath
    End Function

    'VSScript reads the Python it loads from vapoursynth.toml, keyed by its own real path,
    'which changes with every mount of the AppImage, like `vapoursynth config` the entry is written
    'and the entries of former mounts and extractions are removed
    Shared Sub ConfigureVapourSynth()
        Try
            If VapourSynthDir = "" Then
                Exit Sub
            End If

            SystemVSScriptPath = Environment.GetEnvironmentVariable("VSSCRIPT_PATH")
            Dim vsscriptPath = GetVSScriptPath()

            If vsscriptPath <> "" Then
                NativeLibraries.SetNativeEnvironmentVariable("VSSCRIPT_PATH", vsscriptPath)
            End If

            Dim exe = Directory.GetFiles(Path.Combine(PythonDir, "bin")).FirstOrDefault(Function(x) Regex.IsMatch(x.FileName, "^python3\.\d+$"))
            Dim pythonLib = Directory.GetFiles(Path.Combine(PythonDir, "lib"), "libpython3.*.so.1.0").FirstOrDefault()

            If exe Is Nothing OrElse pythonLib Is Nothing Then
                Exit Sub
            End If

            exe = NativeLibraries.GetRealPath(exe)
            pythonLib = NativeLibraries.GetRealPath(pythonLib)
            Dim key = NativeLibraries.GetRealPath(Path.Combine(VapourSynthDir, "libvsscript.so")).Replace("/lib64/", "/lib/")
            Dim mtime = New DateTimeOffset(File.GetLastWriteTimeUtc(exe)).ToUnixTimeSeconds()
            Dim configFile = Path.Combine(Folder.XdgConfigHome, "vapoursynth", "vapoursynth.toml")
            Dim lines = If(File.Exists(configFile), File.ReadAllLines(configFile).ToList, New List(Of String))
            Dim entry = $"{Quote(key)} = [{Quote(exe)},{Quote(pythonLib)},{Quote(mtime.ToString)}]"

            lines.RemoveAll(Function(line)
                                Dim lineKey = GetKey(line)
                                Return lineKey = key OrElse ((lineKey.Contains("/.mount_") OrElse
                                    lineKey.Contains("/appimage_extracted_")) AndAlso Not File.Exists(lineKey))
                            End Function)

            lines.Add(entry)
            Directory.CreateDirectory(configFile.Dir)
            File.WriteAllLines(configFile, lines, New UTF8Encoding(False))
        Catch ex As Exception
            Debug.WriteLine(ex)
        End Try
    End Sub

    Shared Function Quote(value As String) As String
        Return """" + value.Replace("\", "\\").Replace("""", "\""") + """"
    End Function

    Shared Function GetKey(line As String) As String
        If Not line.StartsWith("""") Then
            Return ""
        End If

        Dim endIndex = line.IndexOf(""" =", StringComparison.Ordinal)
        Return If(endIndex > 0, line.Substring(1, endIndex - 1).Replace("\""", """").Replace("\\", "\"), "")
    End Function
End Class
