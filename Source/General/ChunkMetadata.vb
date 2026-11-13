Imports System.Text
Imports System.Text.Json
Imports System.Text.RegularExpressions

Imports StaxRip.VideoEncoderCommandLine

'encoders apply HDR metadata from the first entry on, so every chunk gets the entries of its frames
Public Class ChunkMetadata
    Shared Function GetPath(metadataPath As String, chunkName As String) As String
        Return Path.Combine(p.TempDir, metadataPath.Base + chunkName + metadataPath.ExtFull)
    End Function

    Shared Function IsChunkFile(filePath As String) As Boolean
        Return Regex.IsMatch(filePath.Base, "_chunk\d+$")
    End Function

    Shared Sub Write(params As IEnumerable(Of StringParam), startFrame As Integer, endFrame As Integer, chunkName As String)
        For Each param In params.Where(Function(i) i.GetArgs <> "")
            Write(Macro.Expand(param.Value), startFrame, endFrame, chunkName)
        Next
    End Sub

    Shared Sub Write(metadataPath As String, startFrame As Integer, endFrame As Integer, chunkName As String)
        Dim target = GetPath(metadataPath, chunkName)
        FileHelp.Delete(target)

        If Not metadataPath.FileExists() Then Exit Sub

        Dim isJson = metadataPath.Ext = "json"
        Dim frames = If(isJson, GetJsonFrameCount(metadataPath), GetRpuFrameCount(metadataPath))

        If startFrame >= frames Then
            Log.WriteLine($"{metadataPath.FileName} has no metadata for the frames {startFrame}-{endFrame}." + BR)
            Exit Sub
        End If

        'hdr10plus_tool removes the ranges one after another, so the end goes first
        Dim remove As New List(Of String)
        If endFrame < frames - 1 Then remove.Add($"""{endFrame + 1}-{frames - 1}""")
        If startFrame > 0 Then remove.Add($"""0-{startFrame - 1}""")

        If remove.Count = 0 Then
            File.Copy(metadataPath, target, True)
            Exit Sub
        End If

        Dim configPath = target.DirAndBase + "_Config.json"
        Dim config = "{""remove"":[" + String.Join(",", remove) + "]}"
        config.WriteFileUTF8(configPath)

        Using proc As New Proc
            proc.Package = If(isJson, Package.HDR10PlusTool, Package.DoViTool)
            proc.Project = p
            proc.Header = $"{If(isJson, "HDR10+", "Dolby Vision")} metadata of the frames {startFrame}-{endFrame}"
            proc.Encoding = Encoding.UTF8
            proc.Arguments = $"editor -i {metadataPath.Escape} -j {configPath.Escape} -o {target.Escape}"
            proc.AllowedExitCodes = {0}
            proc.OutputFiles = {target}
            proc.Start()
        End Using
    End Sub

    Shared Function GetRpuFrameCount(rpuPath As String) As Integer
        Dim output = ProcessHelp.GetConsoleOutput(Package.DoViTool.Path, $"info -i {rpuPath.Escape} -s", False)
        Dim match = Regex.Match(output, "Frames: (\d+)")
        Return If(match.Success, match.Groups(1).Value.ToInt(), 0)
    End Function

    Shared Function GetJsonFrameCount(jsonPath As String) As Integer
        Try
            Using stream = File.OpenRead(jsonPath), doc = JsonDocument.Parse(stream)
                Return doc.RootElement.GetProperty("SceneInfo").GetArrayLength()
            End Using
        Catch ex As Exception
            Log.WriteLine($"{jsonPath.FileName}: {ex.Message}" + BR)
            Return 0
        End Try
    End Function

    'chunk encodes take the chunk files of the metadata params instead of the full files
    Shared Function GetArgs(params As IEnumerable(Of StringParam), chunkName As String) As String
        Dim ret = ""

        For Each param In params.Where(Function(i) i.GetArgs <> "")
            Dim chunkPath = GetPath(Macro.Expand(param.Value), chunkName)

            If chunkPath.FileExists() Then
                ret += $" {param.Switch} {chunkPath.Escape}"
            End If
        Next

        Return ret
    End Function
End Class
