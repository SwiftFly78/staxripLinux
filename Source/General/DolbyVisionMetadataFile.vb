Imports System.Text
Imports System.Text.RegularExpressions

Imports VB6 = Microsoft.VisualBasic


<Serializable>
Public Class RpuPreset
    Public ReadOnly Property Id As Integer = 0
    Public ReadOnly Property Offset As New CropPadding(0)

    Public Sub New(id As Integer, offset As CropPadding)
        Me.Id = id
        Me.Offset = offset
    End Sub
End Class

<Serializable>
Public Class RpuEdit
    Public ReadOnly Property Id As Integer = 0
    Public ReadOnly Property StartFrame As Integer
    Public ReadOnly Property EndFrame As Integer

    Public Sub New(id As Integer, startFrame As Integer, endFrame As Integer)
        Me.Id = id
        Me.StartFrame = startFrame
        Me.EndFrame = endFrame
    End Sub
End Class

<Serializable>
Public Class DolbyVisionMetadataFile
    <NonSerialized> Private Shared ReadOnly _autoCropThresholdBegin As Integer = 1000
    <NonSerialized> Private Shared ReadOnly _autoCropThresholdEnd As Integer = 1000
    Private _crop As CropPadding? = Nothing
    Private _lastWriteTime As DateTime = Nothing
    Private _lastWriteTimeL5 As DateTime = Nothing
    Private _trimRanges As List(Of Range) = Nothing

    Public ReadOnly Property Path As String = Nothing
    Public ReadOnly Property Edits As New List(Of RpuEdit)
    Public ReadOnly Property Presets As New List(Of RpuPreset)

    Public ReadOnly Property LastWriteTime As DateTime
        Get
            Return _lastWriteTime
        End Get
    End Property

    Public ReadOnly Property LastWriteTimeL5 As DateTime
        Get
            Return _lastWriteTimeL5
        End Get
    End Property


    Public ReadOnly Property Level5JsonFilePath As String
        Get
            Return If(String.IsNullOrWhiteSpace(Path), "", $"{Path.DirAndBase()}_L5.json")
        End Get
    End Property

    Public ReadOnly Property EditorConfigFilePath As String
        Get
            Return If(String.IsNullOrWhiteSpace(Path), "", $"{Path.DirAndBase()}_Config.json")
        End Get
    End Property

    Public ReadOnly Property ModifiedRpuFilePath As String
        Get
            Return If(String.IsNullOrWhiteSpace(Path), "", $"{Path.DirAndBase()}_Modified.rpu")
        End Get
    End Property

    Public ReadOnly Property TrimmedRpuFilePath As String
        Get
            Return If(String.IsNullOrWhiteSpace(Path), "", $"{Path.DirAndBase()}_Trimmed.rpu")
        End Get
    End Property

    'the _Config suffix excludes it from the HDR10+ metadata search
    Public ReadOnly Property TrimConfigFilePath As String
        Get
            Return If(String.IsNullOrWhiteSpace(Path), "", $"{Path.DirAndBase()}_Trim_Config.json")
        End Get
    End Property

    Public ReadOnly Property HasPathChanged As Boolean
        Get
            Return Path.FileExists() AndAlso _lastWriteTime <> File.GetLastWriteTimeUtc(Path)
        End Get
    End Property

    Public ReadOnly Property HasLevel5Changed As Boolean
        Get
            Return Level5JsonFilePath.FileExists() AndAlso _lastWriteTimeL5 <> File.GetLastWriteTimeUtc(Level5JsonFilePath)
        End Get
    End Property

    Public ReadOnly Property HasToBeTrimmed As Boolean
        Get
            If p.Ranges Is Nothing OrElse Not p.Ranges.Any() Then Return False
            If _trimRanges Is Nothing OrElse Not _trimRanges.Any() Then Return True
            If p.Ranges.Count <> _trimRanges.Count Then Return True

            p.Ranges.Sort()
            _trimRanges.Sort()

            For i = 0 To p.Ranges.Count - 1
                If p.Ranges(i).CompareTo(_trimRanges(i)) <> 0 Then Return True
            Next

            Return False
        End Get
    End Property

    Public ReadOnly Property Crop As CropPadding
        Get
            If _crop Is Nothing Then
                Select Case p.AutoCropDolbyVisionMode
                    Case AutoCropDolbyVisionMode.Automatic
                        _crop = GetCrop(_autoCropThresholdBegin, _autoCropThresholdEnd)
                    Case AutoCropDolbyVisionMode.ManualThreshold
                        _crop = GetCrop(p.AutoCropDolbyVisionThresholdBegin, p.AutoCropDolbyVisionThresholdEnd)
                    Case Else
                        Throw New NotImplementedException(NameOf(AutoCropDolbyVisionMode))
                End Select
            End If

            Return _crop.Value
        End Get
    End Property


    Private Sub New()
    End Sub

    Public Sub New(filePath As String)
        Me.Path = filePath
        Me._lastWriteTime = File.GetLastWriteTimeUtc(filePath)

        WriteLevel5Export(False, False)
        ReadLevel5Export()
    End Sub

    Private Function GetCrop(thresholdBegin As Integer, thresholdEnd As Integer) As CropPadding
        Dim newCrop As New CropPadding(0)

        If Not Edits?.Any() Then Return newCrop
        If Not Presets?.Any() Then Return newCrop

        Dim max = Edits.Max(Function(x) x.EndFrame)
        thresholdBegin = Math.Min(max, thresholdBegin)
        thresholdBegin = Math.Max(0, thresholdBegin)
        thresholdEnd = Math.Min(max, thresholdEnd)
        thresholdEnd = Math.Max(0, thresholdEnd)

        Dim entries = Edits.Where(Function(x) x.StartFrame < (max - thresholdEnd) AndAlso x.EndFrame > thresholdBegin) _
                            .Join(Presets, Function(edit) edit.Id, Function(preset) preset.Id, Function(edit, preset) New With {edit.StartFrame, edit.EndFrame, edit.Id, preset.Offset}) _
                            .OrderBy(Function(x) x.StartFrame).ToList()

        If entries.Count < 1 Then Return newCrop

        newCrop.Left = entries.Min(Function(x) x.Offset.Left)
        newCrop.Top = entries.Min(Function(x) x.Offset.Top)
        newCrop.Right = entries.Min(Function(x) x.Offset.Right)
        newCrop.Bottom = entries.Min(Function(x) x.Offset.Bottom)

        Return newCrop
    End Function

    Public Sub RefreshLevel5Data()
        If Not HasLevel5Changed Then Exit Sub
        If Not (p.AutoCropMode = AutoCropMode.DolbyVisionOnly OrElse p.AutoCropMode = AutoCropMode.Always) Then Exit Sub

        ReadLevel5Export()
    End Sub

    Public Sub ReadLevel5Export(Optional logContent As Boolean = True)
        If Not Path?.FileExists() Then Return
        If Not Level5JsonFilePath.FileExists() Then Return

        Edits.Clear()
        Presets.Clear()

        Try
            Dim jsonContent = Level5JsonFilePath.ReadAllText()
            Dim strippedJsonContent = jsonContent.Right("presets").Left("edits")
            strippedJsonContent = Regex.Replace(strippedJsonContent, "\s", "")
            Dim presetMatches = Regex.Matches(strippedJsonContent, "\{.+?:(?<id>\d+),.+?:(?<left>\d+),.+?:(?<right>\d+),.+?:(?<top>\d+),.+?:(?<bottom>\d+)\}")

            If presetMatches.Count = 0 Then Return

            For Each match As Match In presetMatches
                Dim offset = New CropPadding(match.Groups("left").Value.ToInt(), match.Groups("top").Value.ToInt(), match.Groups("right").Value.ToInt(), match.Groups("bottom").Value.ToInt())
                Presets.Add(New RpuPreset(match.Groups("id").Value.ToInt(), offset))
            Next

            strippedJsonContent = jsonContent.Right("edits")
            strippedJsonContent = Regex.Replace(strippedJsonContent, "\s", "")
            Dim editMatches = Regex.Matches(strippedJsonContent, """(?<start>\d+)-(?<end>\d+)"":(?<id>\d+),?")

            For Each match As Match In editMatches
                Edits.Add(New RpuEdit(match.Groups("id").Value.ToInt(), match.Groups("start").Value.ToInt(), match.Groups("end").Value.ToInt()))
            Next

            _lastWriteTimeL5 = File.GetLastWriteTimeUtc(Level5JsonFilePath)
        Catch ex As AbortException
            Throw
        Catch ex As Exception
            g.ShowException(ex)
            Throw New AbortException
        Finally
            If logContent Then
                Dim exists = File.Exists(Level5JsonFilePath)
                Dim content = If(exists, File.ReadAllText(Level5JsonFilePath), "No file found!")

                Log.WriteHeader($"Content of {IO.Path.GetFileName(Level5JsonFilePath)}")
                Log.WriteLine(content)
            End If

            Log.Save()
        End Try
    End Sub

    Public Sub WriteLevel5Export(Optional logContent As Boolean = False, Optional overwrite As Boolean = False)
        If Not Path?.FileExists() Then Return
        If Not overwrite AndAlso Level5JsonFilePath.FileExists() Then Return

        Try
            Dim pd = Path.Dir
            Dim l5 = Level5JsonFilePath.Replace(pd, ".")
            Dim arguments = "export --data " + ("level5=" + l5).Escape + " -i " + Path.Escape
            Using proc As New Proc
                proc.Package = Package.DoViTool
                proc.Project = p
                proc.Header = "Extract Level5 data from RPU metadata file"
                proc.Encoding = Encoding.UTF8
                proc.Arguments = arguments
                proc.WorkingDirectory = pd
                proc.AllowedExitCodes = {0}
                proc.OutputFiles = {Level5JsonFilePath}
                proc.Start()
            End Using
        Catch ex As AbortException
            Throw
        Catch ex As Exception
            g.ShowException(ex)
            Throw New AbortException
        Finally
            If logContent Then
                Dim exists = File.Exists(Level5JsonFilePath)
                Dim content = If(exists, File.ReadAllText(Level5JsonFilePath), "No file created!")

                Log.WriteHeader($"Content of {IO.Path.GetFileName(Level5JsonFilePath)}")
                Log.WriteLine(content)
            End If

            Log.Save()
        End Try
    End Sub

    Public Function ReadProfileFromRpu() As Integer
        If Not Path?.FileExists() Then Return -1

        Dim output = ProcessHelp.GetConsoleOutput(Package.DoViTool.Path, $"info -i {Path.Escape} -s", False).Split(New String() {VB6.vbCrLf, VB6.vbLf, VB6.vbCr}, StringSplitOptions.RemoveEmptyEntries)

        For i = 0 To output.Length - 1
            Dim line = output(i).Trim()
            Dim match = Regex.Match(line, "Profile: (\d+)(.*)$")
            If match.Success Then
                Dim profile = match.Groups(1).Value
                Return profile.ToInt()
            End If
        Next

        Return -1
    End Function

    Public Sub WriteEditorConfigFile(offset As CropPadding, mode As DoviMode, Optional logContent As Boolean = True, Optional overwrite As Boolean = True)
        If Not Level5JsonFilePath?.FileExists() Then Return
        If Not overwrite AndAlso EditorConfigFilePath?.FileExists() Then Return

        Try
            Dim p = ""
            For Each entry In Presets.OrderBy(Function(x) x.Id)
                Dim left = Math.Max(0, entry.Offset.Left - offset.Left)
                Dim top = Math.Max(0, entry.Offset.Top - offset.Top)
                Dim right = Math.Max(0, entry.Offset.Right - offset.Right)
                Dim bottom = Math.Max(0, entry.Offset.Bottom - offset.Bottom)

                p += $"{{"
                p += $"""id"":{entry.Id},"
                p += $"""left"":{left},"
                p += $"""right"":{right},"
                p += $"""top"":{top},"
                p += $"""bottom"":{bottom}"
                p += $"}},"
            Next
            p = p.TrimEnd(","c)
            p = $"""presets"":[{p}]"

            Dim e = ""
            For Each entry In Edits.OrderBy(Function(x) x.StartFrame)
                e += $"""{entry.StartFrame}-{entry.EndFrame}"":{entry.Id},"
            Next
            e = e.TrimEnd(","c)
            e = $"""edits"":{{{e}}}"

            Dim result = $"{{""mode"":{mode + 0},""active_area"":{{""crop"":false,{p},{e}}}}}"
            result.WriteFileUTF8(EditorConfigFilePath)
        Catch ex As AbortException
            Throw
        Catch ex As Exception
            g.ShowException(ex)
            Throw New AbortException
        Finally
            If logContent Then
                Dim exists = File.Exists(EditorConfigFilePath)
                Dim content = If(exists, File.ReadAllText(EditorConfigFilePath), "No file created!")

                Log.WriteHeader($"Content of {IO.Path.GetFileName(EditorConfigFilePath)}")
                Log.WriteLine(content)
            End If

            Log.Save()
        End Try
    End Sub

    Public Function WriteModifiedRpu(Optional overwrite As Boolean = True) As String
        If Not Path?.FileExists() Then Return Nothing
        If Not EditorConfigFilePath?.FileExists() Then Return Nothing
        If Not overwrite AndAlso ModifiedRpuFilePath.FileExists() Then Return Nothing

        Try
            Dim arguments = "editor -i " + Path.Escape + " -j " + EditorConfigFilePath.Escape + " -o " + ModifiedRpuFilePath.Escape
            Using proc As New Proc
                proc.Package = Package.DoViTool
                proc.Project = p
                proc.Header = "Creating new RPU metadata file"
                proc.Encoding = Encoding.UTF8
                proc.Arguments = arguments
                proc.AllowedExitCodes = {0}
                proc.OutputFiles = {ModifiedRpuFilePath}
                proc.Start()
            End Using
        Catch ex As AbortException
            Throw
        Catch ex As Exception
            g.ShowException(ex)
            Throw New AbortException
        Finally
            Log.Save()
        End Try

        Return ModifiedRpuFilePath
    End Function

    Public Function TrimRpu(Optional overwrite As Boolean = True) As String
        If Not Path?.FileExists() Then Return Nothing
        If Not HasToBeTrimmed Then Return Nothing
        If Not overwrite AndAlso TrimmedRpuFilePath.FileExists() Then Return Nothing

        Dim inputFilePath = If(ModifiedRpuFilePath.FileExists(), ModifiedRpuFilePath, Path)
        _trimRanges = New List(Of Range)(p.Ranges)

        'DoViSplit only exists for Windows, the editor of dovi_tool removes the frames outside of the ranges
        Try
            Dim frames = Edits.Max(Function(x) x.EndFrame) + 1
            Dim remove As New List(Of String)
            Dim nextFrame = 0

            For Each range In _trimRanges.OrderBy(Function(x) x.Start)
                If range.Start > nextFrame Then remove.Add($"""{nextFrame}-{range.Start - 1}""")
                nextFrame = Math.Max(nextFrame, range.End + 1)
            Next

            If nextFrame < frames Then remove.Add($"""{nextFrame}-{frames - 1}""")

            Dim trimConfig = "{""remove"":[" + String.Join(",", remove) + "]}"
            trimConfig.WriteFileUTF8(TrimConfigFilePath)
            Dim arguments = "editor -i " + inputFilePath.Escape + " -j " + TrimConfigFilePath.Escape + " -o " + TrimmedRpuFilePath.Escape

            Using proc As New Proc
                proc.Package = Package.DoViTool
                proc.Project = p
                proc.Header = "Trimming RPU metadata file"
                proc.Encoding = Encoding.UTF8
                proc.Arguments = arguments
                proc.AllowedExitCodes = {0}
                proc.OutputFiles = {TrimmedRpuFilePath}
                proc.Start()
            End Using
        Catch ex As AbortException
            _trimRanges = Nothing
            Throw
        Catch ex As Exception
            _trimRanges = Nothing
            g.ShowException(ex)
            Throw New AbortException
        Finally
            Log.Save()
        End Try

        Return TrimmedRpuFilePath
    End Function
End Class

'serializable crop values, Eto.Drawing.Padding isn't serializable
<Serializable>
Public Structure CropPadding
    Public Left As Integer
    Public Top As Integer
    Public Right As Integer
    Public Bottom As Integer

    Sub New(all As Integer)
        Left = all
        Top = all
        Right = all
        Bottom = all
    End Sub

    Sub New(left As Integer, top As Integer, right As Integer, bottom As Integer)
        Me.Left = left
        Me.Top = top
        Me.Right = right
        Me.Bottom = bottom
    End Sub

    Shared ReadOnly Property Empty As CropPadding
        Get
            Return New CropPadding(0)
        End Get
    End Property

    Shared Operator =(a As CropPadding, b As CropPadding) As Boolean
        Return a.Left = b.Left AndAlso a.Top = b.Top AndAlso a.Right = b.Right AndAlso a.Bottom = b.Bottom
    End Operator

    Shared Operator <>(a As CropPadding, b As CropPadding) As Boolean
        Return Not a = b
    End Operator

    Overrides Function Equals(obj As Object) As Boolean
        Return TypeOf obj Is CropPadding AndAlso DirectCast(obj, CropPadding) = Me
    End Function

    Overrides Function GetHashCode() As Integer
        Return HashCode.Combine(Left, Top, Right, Bottom)
    End Function

    Overrides Function ToString() As String
        Return $"{Left}, {Top}, {Right}, {Bottom}"
    End Function
End Structure
