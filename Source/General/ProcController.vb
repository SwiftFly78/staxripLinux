
Imports System.Globalization
Imports System.Text.RegularExpressions
Imports System.Threading
Imports System.Threading.Tasks

Imports StaxRip.UI
Imports Microsoft.VisualBasic

'Controls a running process and its output in the processing window,
'all UI work happens on the UI thread, output arrives on reader threads.
Public Class ProcController
    Private UseFirstExpression As Boolean = True
    Private ReadOnly _progressSeparator As String = " | "
    Private ReadOnly _progressSeparatorPattern As String = Regex.Escape(_progressSeparator)
    Private _progressHighlightingFailCounter As Integer = 0
    Private _progressReformattingFailCounter As Integer = 0
    Private _projectScriptFrameRate As Double = -1.0
    Private _lastHighlightedText As String = ""
    Private _lastProgressText As String = ""
    Private _lastProgressSet As Date = Date.Now
    Private _triggerWhileProcessing As Boolean = False
    Private _lastTriggerWhileProcessing As Date = Date.Now
    Private _ffmpegDuration As TimeSpan = TimeSpan.Zero
    Private _logDirty As Integer
    Private _pendingProgress As String
    Private Shared _blockActivation As Boolean = False
    Private Shared _lastActivation As Date = Date.UtcNow
    Private Shared _progressInterval As Integer = 200

    Property Proc As Proc
    Property LogTextBox As New RichTextBoxEx
    Property ProgressBar As New LabelProgressBar
    Property ProcForm As ProcessingForm
    Property Button As New ButtonEx
    Property LastProgress As Double

    ReadOnly Property ProjectScriptFrameRate As Double
        Get
            If _projectScriptFrameRate < 0 Then
                _projectScriptFrameRate = If(Proc?.Project?.Script Is Nothing, 0.0, Proc.Project.Script.GetFramerate())
            End If
            Return _projectScriptFrameRate
        End Get
    End Property

    Shared Property Procs As New List(Of ProcController)
    Shared Property Aborted As Boolean = False

    Shared Property BlockActivation As Boolean
        Get
            Return _blockActivation OrElse
                (s.PreventFocusStealUntil >= 0 AndAlso SecondsSinceLastActivation <= s.PreventFocusStealUntil) OrElse
                (s.PreventFocusStealAfter >= 0 AndAlso SecondsSinceLastActivation >= s.PreventFocusStealAfter)
        End Get
        Set(value As Boolean)
            _blockActivation = value
        End Set
    End Property

    Shared ReadOnly Property LastActivation As Date
        Get
            Return _lastActivation
        End Get
    End Property

    Shared ReadOnly Property SecondsSinceLastActivation As Double
        Get
            Return (Date.UtcNow - _lastActivation).TotalSeconds
        End Get
    End Property

    'must be called on the UI thread
    Sub New(proc As Proc)
        Me.Proc = proc
        ProcForm = g.ProcForm

        _triggerWhileProcessing = Not p.SkipVideoEncoding AndAlso
                                TypeOf p.VideoEncoder IsNot NullEncoder AndAlso
                                proc.Package IsNot Nothing AndAlso
                                proc.Package Is TryCast(p.VideoEncoder, BasicVideoEncoder)?.CommandLineParams.Package

        Button.Text = " " + proc.Title + " "
        Button.Font = FontManager.GetCodeFont(-1)
        AddHandler Button.Click, AddressOf Click

        ProgressBar.Font = FontManager.GetCodeFont(-1)

        LogTextBox.ReadOnly = True
        LogTextBox.WordWrap = True
        LogTextBox.Font = FontManager.GetCodeFont(-1)

        ProcForm.AddProcControls(Button)

        AddHandler proc.ProcDisposed, AddressOf ProcDisposed
        AddHandler proc.OutputDataReceived, AddressOf DataReceived
        AddHandler proc.ErrorDataReceived, AddressOf DataReceived

        ApplyTheme()
    End Sub

    Sub OnThemeChanged(theme As Theme)
        ApplyTheme(theme)
    End Sub

    Sub ApplyTheme()
        ApplyTheme(ThemeManager.CurrentTheme)
    End Sub

    Sub ApplyTheme(theme As Theme)
        Button.Font = FontManager.GetCodeFont(-1, If(Proc.IsSilent, FontStyle.None, FontStyle.Bold))
    End Sub

    Public Shared Sub SetLastActivation(Optional offsetSeconds As Integer = 0)
        _lastActivation = Date.UtcNow.AddSeconds(offsetSeconds)
    End Sub

    'called on reader threads
    Sub DataReceived(value As String)
        If value = "" Then Exit Sub

        Dim ret = Proc.ProcessData(value)
        If ret.Data = "" Then Exit Sub

        If ret.Skip Then
            If Proc.IntegerFrameOutput AndAlso Proc.FrameCount > 0 AndAlso ret.Data.IsInt Then
                ret.Data = "Progress: " + (ret.Data.ToInt / Proc.FrameCount * 100).ToString("0.00") + "%"
            End If

            If Proc.IntegerPercentOutput AndAlso ret.Data.IsInt Then
                ret.Data = "Progress: " + ret.Data + "%"
            End If

            If Interlocked.Exchange(_pendingProgress, ret.Data) Is Nothing Then
                UIBeginInvoke(Sub()
                                  Dim value2 = Interlocked.Exchange(_pendingProgress, Nothing)

                                  If value2 IsNot Nothing Then
                                      ProgressHandler(value2)
                                  End If
                              End Sub)
            End If
        Else
            If ret.Data.Trim <> "" Then
                Proc.Log.WriteLine(ret.Data)
            End If

            If Proc.Package Is Package.ffmpeg OrElse Proc.Package Is Package.DoViTool OrElse Proc.Package Is Package.HDR10PlusTool Then
                If Proc.FrameCount <= 0 Then
                    Dim match = Regex.Match(value, "NUMBER_OF_FRAMES(?:-\w+)?\s*:\s+(\d+)", RegexOptions.IgnoreCase)
                    If match.Success Then
                        Proc.FrameCount = match.Groups(1).Value.ToInt()
                    End If
                End If

                If _ffmpegDuration = TimeSpan.Zero Then
                    Dim match = Regex.Match(value, "DURATION(?:-\w+)?\s*:\s+((\d+):(\d+):(\d+))", RegexOptions.IgnoreCase)
                    If match.Success Then
                        Dim t As TimeSpan
                        If TimeSpan.TryParse(match.Groups(1).Value, t) Then
                            _ffmpegDuration = t
                        End If
                    End If
                End If
            End If

            'log updates are coalesced to limit the UI work for chatty tools
            If Interlocked.Exchange(_logDirty, 1) = 0 Then
                Task.Run(Sub()
                             Thread.Sleep(250)
                             Interlocked.Exchange(_logDirty, 0)
                             UIBeginInvoke(AddressOf LogHandler)
                         End Sub)
            End If
        End If
    End Sub

    Sub LogHandler()
        SetAndHighlightLog(Proc.Log.ToString(), ThemeManager.CurrentTheme)
    End Sub

    Public Sub SetAndHighlightLog(text As String, theme As Theme)
        If Proc.IsSilent Then Exit Sub

        If text Is Nothing Then text = LogTextBox.Text
        If theme Is Nothing Then theme = ThemeManager.CurrentTheme

        If text = "" OrElse Not s.OutputHighlighting OrElse text.Length > 300000 Then
            If LogTextBox.Text <> text?.ReplaceTabsWithSpaces() Then
                LogTextBox.Text = text?.ReplaceTabsWithSpaces()
                LogTextBox.ScrollToEnd()
            End If

            _lastHighlightedText = ""
            Exit Sub
        End If

        If _lastHighlightedText = text Then Exit Sub
        _lastHighlightedText = text

        Dim format = Sub(index As Integer, length As Integer, backColor As ColorHSL, foreColor As ColorHSL, fontStyles() As FontStyle)
                         LogTextBox.SetFormat(index, length, foreColor.ToColor, If(backColor.A > 0, backColor.ToColor, CType(Nothing, Color?)), fontStyles IsNot Nothing AndAlso fontStyles.Contains(FontStyle.Bold))
                     End Sub

        Try
            Dim oh = theme.ProcessingForm.OutputHighlighting
            Dim matches As MatchCollection
            Dim help As Integer
            Dim duplicate As String
            Dim pathsFormat = "(""/[^\a\b\e\f\n\r\t\v""]+\.({0})"")|((?<![""\w])/[^\s""]+\.({0})(?![""\w\.]))"
            Dim isX264 = Proc.Package Is Package.x264
            Dim isX265 = Proc.Package Is Package.x265
            Dim isSvtAv1 = {Package.SvtAv1EncApp, Package.SvtAv1EncAppEssential, Package.SvtAv1EncAppHdr, Package.SvtAv1EncAppPsyex}.Contains(Proc.Package)

            LogTextBox.Text = text?.ReplaceTabsWithSpaces()

            matches = Regex.Matches(LogTextBox.Text, "(?<=\n)----------.*----------(?=\n)", RegexOptions.IgnoreCase)
            For Each m As Match In matches
                format(m.Index, m.Length, oh.HeaderBackColor, oh.HeaderForeColor, oh.HeaderFontStyles)
            Next

            matches = Regex.Matches(LogTextBox.Text, "(?<=----------\n\n).*(?=\n\n.+)", RegexOptions.IgnoreCase)
            For Each m As Match In matches
                format(m.Index, m.Length, oh.EncoderTitleBackColor, oh.EncoderTitleForeColor, oh.EncoderTitleFontStyles)
            Next

            If isX265 OrElse isSvtAv1 Then
                help = 1
                matches = Regex.Matches(LogTextBox.Text, "((?<=\n)x265\s\[info\]|SVT\s\[config\]):\s(.+\s:|.+\sprofile|tools)\s.+", RegexOptions.IgnoreCase)
                For Each m As Match In matches
                    If help Mod 2 = 0 Then
                        format(m.Index, m.Length, oh.AlternateBackColor, oh.AlternateForeColor, oh.AlternateFontStyles)
                    End If
                    help += 1
                Next
            End If

            matches = Regex.Matches(LogTextBox.Text, "(?<=\n)(x264|x265|svt|avs2yuv|vspipe|y4m|vvenc|vvencFFapp)(?=\s*\[)", RegexOptions.IgnoreCase)
            For Each m As Match In matches
                format(m.Index, m.Length, oh.SourceBackColor.SetHue(205), oh.SourceForeColor, Nothing)
            Next

            matches = Regex.Matches(LogTextBox.Text, "(?<=\n)(avs\+|avs|vpy)(?=\s+\[)", RegexOptions.IgnoreCase)
            For Each m As Match In matches
                format(m.Index, m.Length, oh.SourceBackColor.SetHue(300), oh.SourceForeColor, Nothing)
            Next

            matches = Regex.Matches(LogTextBox.Text, "(?<=\n)(raw)(?=\s+\[)", RegexOptions.IgnoreCase)
            For Each m As Match In matches
                format(m.Index, m.Length, oh.SourceBackColor.SetHue(175), oh.SourceForeColor, Nothing)
            Next

            matches = Regex.Matches(LogTextBox.Text, "(?<=\n.*(?:\s|svt|pipe|mod|enc|app)\[)(warn(?:ing)?)\]:\s(.+)", RegexOptions.IgnoreCase)
            For Each m As Match In matches
                format(m.Groups(1).Index, m.Groups(1).Length, oh.WarningLabelBackColor, oh.WarningLabelForeColor, oh.WarningLabelFontStyles)
                format(m.Groups(2).Index, m.Groups(2).Length, oh.WarningTextBackColor, oh.WarningTextForeColor, oh.WarningTextFontStyles)
            Next

            matches = Regex.Matches(LogTextBox.Text, "(?<=\n.*(?:\s|svt|pipe|mod|enc|app)\[)info|verbose(?=\]:\s)", RegexOptions.IgnoreCase)
            For Each m As Match In matches
                format(m.Index, m.Length, oh.InfoLabelBackColor, oh.InfoLabelForeColor, Nothing)
            Next

            matches = Regex.Matches(LogTextBox.Text, "(?<=\s|^)(--\w[^\s=]*|-[^-\s]+(?=[\s=]))(?:[\s=]((?!--|-[\S]+\s)[^""\s]+|""[^""\n]*"")?)?", RegexOptions.IgnoreCase)
            For Each m As Match In matches
                format(m.Groups(1).Index, m.Groups(1).Length, oh.ParameterBackColor, oh.ParameterForeColor, oh.ParameterFontStyles)
                If m.Groups.Count > 2 Then
                    format(m.Groups(2).Index, m.Groups(2).Length, oh.ParameterValueBackColor, oh.ParameterValueForeColor, oh.ParameterValueFontStyles)
                End If
            Next

            matches = Regex.Matches(LogTextBox.Text, "(?<=\S) \| (?=\S)", RegexOptions.IgnoreCase)
            For Each m As Match In matches
                format(m.Index, m.Length, oh.PipeBackColor, oh.PipeForeColor, oh.PipeFontStyles)
            Next

            matches = Regex.Matches(LogTextBox.Text, "(?<=^|\n|\| )(""?/[^\s""]*/(?:x264|x265|ffmpeg|vspipe|SvtAv1EncApp|aomenc|rav1e|mkvmerge|mp4box|MP4Box|nvencc|qsvencc|vceencc|vvencFFapp|opusenc|fdkaac|flac|dovi_tool|hdr10plus_tool)""?)(?=\s)", RegexOptions.IgnoreCase)
            For Each m As Match In matches
                format(m.Index, m.Length, oh.ExeFileBackColor, oh.ExeFileForeColor, oh.ExeFileFontStyles)
            Next

            duplicate = "json|bin|rpu"
            matches = Regex.Matches(LogTextBox.Text, String.Format(pathsFormat, duplicate), RegexOptions.IgnoreCase)
            For Each m As Match In matches
                format(m.Index, m.Length, oh.MetadataFileBackColor, oh.MetadataFileForeColor, oh.MetadataFileFontStyles)
            Next

            duplicate = FileTypes.Video.Union(FileTypes.Audio).Union(FileTypes.SubtitleExludingContainers).Distinct().OrderByDescending(Function(x) x.Length).Select(Function(x) Regex.Escape(x)).Join("|")
            matches = Regex.Matches(LogTextBox.Text, String.Format(pathsFormat, duplicate), RegexOptions.IgnoreCase)
            For Each m As Match In matches
                format(m.Index, m.Length, oh.MediaFileBackColor, oh.MediaFileForeColor, oh.MediaFileFontStyles)
            Next

            duplicate = FileTypes.Scripts.Union(FileTypes.Indexes).Union({"so"}).Distinct().OrderByDescending(Function(x) x.Length).Select(Function(x) Regex.Escape(x)).Join("|")
            matches = Regex.Matches(LogTextBox.Text, String.Format(pathsFormat, duplicate), RegexOptions.IgnoreCase)
            For Each m As Match In matches
                format(m.Index, m.Length, oh.ScriptFileBackColor, oh.ScriptFileForeColor, oh.ScriptFileFontStyles)
            Next

            matches = Regex.Matches(LogTextBox.Text, "(?<=\s)frames\s(\d+)\s-\s(\d+)\sof\s(\d+)", RegexOptions.IgnoreCase)
            For Each m As Match In matches
                If m.Groups(1).Value.ToInt() > 0 OrElse m.Groups(2).Value.ToInt() + 1 < m.Groups(3).Value.ToInt() Then
                    format(m.Index, m.Length, oh.FramesCuttedBackColor, oh.FramesCuttedForeColor, oh.FramesCuttedFontStyles)
                    For i = 1 To m.Groups.Count - 1
                        format(m.Groups(i).Index, m.Groups(i).Length, oh.FramesCuttedNumberBackColor, oh.FramesCuttedNumberForeColor, oh.FramesCuttedNumberFontStyles)
                    Next
                Else
                    format(m.Index, m.Length, oh.FramesBackColor, oh.FramesForeColor, oh.FramesFontStyles)
                End If
            Next

            matches = Regex.Matches(LogTextBox.Text, "(?<=\]:\s)(avisynth|vapoursynth).*\d+.*", RegexOptions.IgnoreCase)
            For Each m As Match In matches
                format(m.Index, m.Length, oh.FrameServerBackColor, oh.FrameServerForeColor, oh.FrameServerFontStyles)
            Next

            matches = Regex.Matches(LogTextBox.Text, "(?<=\]\s*:\s)[^:]*encoder(?:[^\d\n]*?([^\d\s]*\d+\S*\d[^\d\s]*))(?:.*(djatom|patman|jpsdr))?.*(?=\n)", RegexOptions.IgnoreCase)
            For Each m As Match In matches
                format(m.Index, m.Length, oh.EncoderBackColor, oh.EncoderForeColor, oh.EncoderFontStyles)
                format(m.Groups(1).Index, m.Groups(1).Length, oh.EncoderBackColor, oh.EncoderForeColor.AddSaturation(0.1).AddLuminance(0.175), oh.EncoderFontStyles.Union({FontStyle.Bold}).ToArray)
                If m.Groups.Count > 2 Then
                    format(m.Groups(2).Index, m.Groups(2).Length, oh.EncoderBackColor, oh.EncoderForeColor.AddSaturation(0.1).AddLuminance(0.175), oh.EncoderFontStyles.Union({FontStyle.Bold}).ToArray)
                End If
            Next

        Catch ex As Exception
        Finally
            LogTextBox.ScrollToEnd()
        End Try
    End Sub

    Sub ProgressHandler(value As String)
        value = value.Trim()

        If _lastProgressText = value Then Exit Sub
        If _lastProgressSet >= Date.Now.AddMilliseconds(-_progressInterval) Then Exit Sub

        _lastProgressText = value
        _lastProgressSet = Date.Now

        Dim sw = Stopwatch.StartNew()

        SetProgressText(value, ThemeManager.CurrentTheme)
        SetProgress(value)

        sw.Stop()

        If _progressInterval < sw.ElapsedMilliseconds Then _progressInterval += 25
    End Sub

    Sub SetProgressText(value As String, theme As Theme)
        If Proc.IsSilent Then Exit Sub

        If theme Is Nothing Then theme = ThemeManager.CurrentTheme
        value = value.Trim()

        If s.ProgressReformatting AndAlso value <> "" Then
            If _progressReformattingFailCounter < 64 Then
                Dim pattern As String
                Dim match As Match

                If Proc.Package Is Package.x264 Then
                    If UseFirstExpression Then
                        'Mod by Patman
                        pattern = "\[(\d+)(\.?\d*)%\]\s+(\d+)/(\d+)\sframes\s@\s(\d+)(\.?\d*)\sfps\s\|\s(\d+)(\.?\d*)\s([a-z]{2}/s)\s\|\s(\d+:\d+:\d+)\s\[-(\d+:\d+:\d+)\]\s\|\s(\d+)(\.\d+)?\s([a-z]{1,2})\s\[(\d+)(\.\d+)?\s([a-z]{1,2})\]"
                        match = Regex.Match(value, pattern, RegexOptions.IgnoreCase)

                        If match.Success Then
                            Dim fps = 0.0
                            Dim fpsParse = Double.TryParse(match.Groups(5).Value + match.Groups(6).Value, NumberStyles.Float, CultureInfo.InvariantCulture, fps)
                            Dim speedString = ""

                            If fpsParse AndAlso ProjectScriptFrameRate > 0 Then
                                Dim speed = fps / ProjectScriptFrameRate
                                speedString = $" ({speed.ToString("0.00", CultureInfo.InvariantCulture)}x)"
                            End If

                            value = $"[{match.Groups(1).Value,2}{match.Groups(2).Value}%] {match.Groups(3).Value.PadLeft(match.Groups(4).Value.Length)}/{match.Groups(4).Value} frames @ {match.Groups(5).Value}{match.Groups(6).Value} fps{speedString}{_progressSeparator}{match.Groups(7).Value,4} {match.Groups(9).Value}{_progressSeparator}{match.Groups(12).Value}{match.Groups(13).Value} {match.Groups(14).Value} ({match.Groups(15).Value} {match.Groups(17).Value}){_progressSeparator}{match.Groups(10).Value} (-{match.Groups(11).Value})"
                        Else
                            UseFirstExpression = Not UseFirstExpression
                            _progressReformattingFailCounter += 1
                        End If
                    Else
                        'Mod by DJATOM since x264 161, using header
                        pattern = "\[\s*((\d+)\.?(\d*))%\]\s+((\d+)/(\d+))\s+((\d+)\.?(\d*))\s+((\d+)\.?(\d*))\s+(\d+:\d+:\d+)\s+(\d+:\d+:\d+)\s+((\d+)\.(\d+))\s([a-z]{1,2})\s+((\d+)\.(\d+))\s([a-z]{1,2})"
                        match = Regex.Match(value, pattern, RegexOptions.IgnoreCase)

                        If match.Success Then
                            Dim fps = 0.0
                            Dim fpsParse = Double.TryParse($"{match.Groups(8).Value}.{match.Groups(9).Value}", NumberStyles.Float, CultureInfo.InvariantCulture, fps)
                            Dim speedString = ""

                            If fpsParse AndAlso ProjectScriptFrameRate > 0 Then
                                Dim speed = fps / ProjectScriptFrameRate
                                speedString = $" ({speed.ToString("0.00", CultureInfo.InvariantCulture)}x)"
                            End If

                            value = $"[{match.Groups(2).Value,2}.{match.Groups(3).Value}%] {match.Groups(5).Value.PadLeft(match.Groups(6).Value.Length)}/{match.Groups(6).Value} frames @ {match.Groups(8).Value}.{match.Groups(9).Value} fps{speedString}{_progressSeparator}{match.Groups(11).Value,4} kb/s{_progressSeparator}{match.Groups(16).Value} {match.Groups(18).Value} ({match.Groups(20).Value}.{match.Groups(21).Value} {match.Groups(22).Value}){_progressSeparator}{match.Groups(13).Value} (-{match.Groups(14).Value})"
                        Else
                            UseFirstExpression = Not UseFirstExpression
                            _progressReformattingFailCounter += 1
                        End If
                    End If
                ElseIf Proc.Package Is Package.x265 Then
                    If UseFirstExpression Then
                        'Mod by Patman since x265 3.5-RC1
                        pattern = "\[(\d+)(\.?\d*)%\]\s+(\d+)/(\d+)\sframes\s@\s(\d+)(\.?\d*)\sfps\s\|\s(\d+)(\.?\d*)\s([a-z]{2}/s)\s\|\s(\d+:\d+:\d+)\s\[-(\d+:\d+:\d+)\]\s\|\s(\d+)(\.\d+)?\s([a-z]{1,2})\s\[(\d+)(\.\d+)?\s([a-z]{1,2})\]"
                        match = Regex.Match(value, pattern, RegexOptions.IgnoreCase)

                        If match.Success Then
                            Dim fps = 0.0
                            Dim fpsParse = Double.TryParse(match.Groups(5).Value + match.Groups(6).Value, NumberStyles.Float, CultureInfo.InvariantCulture, fps)
                            Dim speedString = ""

                            If fpsParse AndAlso ProjectScriptFrameRate > 0 Then
                                Dim speed = fps / ProjectScriptFrameRate
                                speedString = $" ({speed.ToString("0.00", CultureInfo.InvariantCulture)}x)"
                            End If

                            value = $"[{match.Groups(1).Value,2}{match.Groups(2).Value}%] {match.Groups(3).Value.PadLeft(match.Groups(4).Value.Length)}/{match.Groups(4).Value} frames @ {match.Groups(5).Value}{match.Groups(6).Value} fps{speedString}{_progressSeparator}{match.Groups(7).Value,4} {match.Groups(9).Value}{_progressSeparator}{match.Groups(12).Value}{match.Groups(13).Value} {match.Groups(14).Value} ({match.Groups(15).Value} {match.Groups(17).Value}){_progressSeparator}{match.Groups(10).Value} (-{match.Groups(11).Value})"
                        Else
                            UseFirstExpression = Not UseFirstExpression
                            _progressReformattingFailCounter += 1
                        End If
                    Else
                        'Mod by DJATOM since x265 3.4+65, including progress-frames
                        pattern = "\[((\d+)\.?(\d*))%\]\s+((\d+)(\(\d+\))?/(\d+)(\sframes)),\s((\d+)\.?(\d*)(\sfps)),\s((\d+)\.?(\d*)\s([a-z]{2}/s)),\selapsed:\s(\d+:\d+:\d+),\seta:\s(\d+:\d+:\d+),\ssize:\s(\d+)\.(\d+)\s([a-z]{1,2}),\sest\.\ssize:\s(\d+)\.(\d+)\s([a-z]{1,2})"
                        match = Regex.Match(value, pattern, RegexOptions.IgnoreCase)

                        If match.Success Then
                            Dim fps = 0.0
                            Dim fpsParse = Double.TryParse($"{match.Groups(10).Value}.{match.Groups(11).Value}", NumberStyles.Float, CultureInfo.InvariantCulture, fps)
                            Dim speedString = ""

                            If fpsParse AndAlso ProjectScriptFrameRate > 0 Then
                                Dim speed = fps / ProjectScriptFrameRate
                                speedString = $" ({speed.ToString("0.00", CultureInfo.InvariantCulture)}x)"
                            End If

                            value = $"[{match.Groups(2).Value,2}.{match.Groups(3).Value}%] {match.Groups(5).Value.PadLeft(match.Groups(7).Value.Length)}{match.Groups(6).Value}/{match.Groups(7).Value} frames @ {match.Groups(10).Value}.{match.Groups(11).Value} fps{speedString}{_progressSeparator}{match.Groups(14).Value,4} {match.Groups(16).Value}{_progressSeparator}{match.Groups(19).Value} {match.Groups(21).Value} ({match.Groups(22).Value} {match.Groups(24).Value}){_progressSeparator}{match.Groups(17).Value} (-{match.Groups(18).Value})"
                        Else
                            UseFirstExpression = Not UseFirstExpression
                            _progressReformattingFailCounter += 1
                        End If
                    End If
                ElseIf {Package.SvtAv1EncApp, Package.SvtAv1EncAppEssential, Package.SvtAv1EncAppHdr, Package.SvtAv1EncAppPsyex}.Contains(Proc.Package) Then
                    pattern = "^Encoding:\s+(\d+)/(\s*\d+)\sFrames\s@\s(\d+\.\d+)\s(fp[s|m])\s\|\s(\d+)\.\d+\skb[p/]s\s\|\sTime:\s(\d+:\d\d:\d\d)\s\[(-?\d+:\d\d:\d\d)\]\s\|\sSize:\s(-?\d+\.\d+)\s(.B)\s\[(-?\d+)\.\d+\s(.B)\]"
                    match = Regex.Match(value, pattern, RegexOptions.IgnoreCase)

                    If match.Success Then
                        Dim frame = 0.0F
                        Dim frameParse = Single.TryParse($"{match.Groups(1).Value}", NumberStyles.Float, CultureInfo.InvariantCulture, frame)
                        Dim frames = 0.0F
                        Dim framesParse = Single.TryParse($"{match.Groups(2).Value}", NumberStyles.Float, CultureInfo.InvariantCulture, frames)
                        Dim percentString = "Encoding:"
                        Dim fps = 0.0F
                        Dim fpsParse = Single.TryParse($"{match.Groups(3).Value}", NumberStyles.Float, CultureInfo.InvariantCulture, fps)
                        Dim speedString = ""

                        If frameParse AndAlso framesParse Then
                            Dim percent = frame / frames * 100
                            percentString = $"[{percent.ToString("0.0", CultureInfo.InvariantCulture),4}%]"
                        End If

                        If fpsParse AndAlso ProjectScriptFrameRate > 0 Then
                            Dim speed = fps / ProjectScriptFrameRate / If(match.Groups(4).Value = "fps", 1, 60)
                            speedString = $" ({speed.ToString("0.00", CultureInfo.InvariantCulture)}x)"
                        End If

                        value = $"{percentString} {match.Groups(1).Value.PadLeft(match.Groups(2).Value.Length)}/{match.Groups(2).Value.Trim()} frames @ {match.Groups(3).Value} {match.Groups(4).Value}{speedString}{_progressSeparator}{match.Groups(5).Value,4} kb/s{_progressSeparator}{match.Groups(8).Value,5} {match.Groups(9).Value} ({match.Groups(10).Value} {match.Groups(11).Value}){_progressSeparator}{match.Groups(6).Value} ({match.Groups(7).Value})"
                    Else
                        pattern = "^Encoding:\s+(\d+)/(\s*\d+)\sFrames\s@\s(\d+\.\d+)\s(fp[s|m])\s\|\s(\d+)\.\d+\skb[p/]s\s\|\sSize:\s(-?\d+\.\d+)\s(.B)\s\[(-?\d+)\.\d+\s(.B)\]\s\|\sTime:\s(\d+:\d\d:\d\d)\s\[(-?\d+:\d\d:\d\d)\]"
                        match = Regex.Match(value, pattern, RegexOptions.IgnoreCase)

                        If match.Success Then
                            Dim frame = 0.0F
                            Dim frameParse = Single.TryParse($"{match.Groups(1).Value}", NumberStyles.Float, CultureInfo.InvariantCulture, frame)
                            Dim frames = 0.0F
                            Dim framesParse = Single.TryParse($"{match.Groups(2).Value}", NumberStyles.Float, CultureInfo.InvariantCulture, frames)
                            Dim percentString = "Encoding:"
                            Dim fps = 0.0F
                            Dim fpsParse = Single.TryParse($"{match.Groups(3).Value}", NumberStyles.Float, CultureInfo.InvariantCulture, fps)
                            Dim speedString = ""

                            If frameParse AndAlso framesParse Then
                                Dim percent = frame / frames * 100
                                percentString = $"[{percent.ToString("0.0"),4}%]"
                            End If

                            If fpsParse AndAlso ProjectScriptFrameRate > 0 Then
                                Dim speed = fps / ProjectScriptFrameRate / If(match.Groups(4).Value = "fps", 1, 60)
                                speedString = $" ({speed.ToString("0.00")}x)"
                            End If

                            value = $"{percentString} {match.Groups(1).Value.PadLeft(match.Groups(2).Value.Length)}/{match.Groups(2).Value.Trim()} frames @ {match.Groups(3).Value} {match.Groups(4).Value}{speedString}{_progressSeparator}{match.Groups(5).Value,4} kb/s{_progressSeparator}{match.Groups(6).Value,5} {match.Groups(7).Value} ({match.Groups(8).Value} {match.Groups(9).Value}){_progressSeparator}{match.Groups(10).Value} ({match.Groups(11).Value})"
                        Else
                            _progressReformattingFailCounter += 1
                        End If
                    End If
                ElseIf Proc.Package Is Package.ffmpeg OrElse Proc.Package Is Package.DoViTool OrElse Proc.Package Is Package.HDR10PlusTool Then
                    pattern = "^frame=\s*(\d+)\s+fps=\s*(\d+)\s+.*size=\s*(\d+)(\w{3})\s+time=\s*(\d+:\d+:\d+(?:\.\d+)?)\s+bitrate=\s*(\d+(?:\.\d+))kbits/s\s+speed=\s*(\d+(?:\.\d+)?)x"
                    match = Regex.Match(value, pattern, RegexOptions.IgnoreCase)

                    If match.Success Then
                        Dim frame = 0
                        Dim frameParse = Integer.TryParse($"{match.Groups(1).Value}", NumberStyles.Integer, CultureInfo.InvariantCulture, frame)
                        Dim fps = 0.0F
                        Dim fpsParse = Single.TryParse($"{match.Groups(2).Value}", NumberStyles.Float, CultureInfo.InvariantCulture, fps)
                        Dim size = 0.0F
                        Dim sizeParse = Single.TryParse($"{match.Groups(3).Value}", NumberStyles.Float, CultureInfo.InvariantCulture, size)
                        Dim sizeUnit = match.Groups(4).Value
                        Dim time = TimeSpan.Zero
                        Dim timeParse = TimeSpan.TryParse($"{match.Groups(5).Value}", time)
                        Dim bitrate = 0.0F
                        Dim bitrateParse = Single.TryParse($"{match.Groups(6).Value}", NumberStyles.Float, CultureInfo.InvariantCulture, bitrate)
                        Dim speed = 0.0F
                        Dim speedParse = Single.TryParse($"{match.Groups(7).Value}", NumberStyles.Float, CultureInfo.InvariantCulture, speed)
                        Dim frames = Proc.FrameCount
                        Dim framesString = If(frames > 0, frames.ToString(), "?")
                        Dim percentString = ""

                        If frameParse AndAlso frames > 0 Then
                            Dim percent = frame / frames * 100
                            percentString = $"[{percent.ToString("0.0", CultureInfo.InvariantCulture),4}%] "
                        ElseIf timeParse AndAlso _ffmpegDuration <> TimeSpan.Zero Then
                            Dim percent = CSng(time.Ticks / _ffmpegDuration.Ticks * 100)
                            percentString = $"[{percent.ToString("0.0", CultureInfo.InvariantCulture),4}%] "
                        End If

                        value = $"{percentString}{match.Groups(1).Value.PadLeft(framesString.Length)}/{framesString} frames @ {fps.ToInvariantString()} fps ({speed.ToInvariantString("0.0")}x){_progressSeparator}{bitrate.ToInvariantString("0"),4} kb/s"
                    Else
                        _progressReformattingFailCounter += 1
                    End If
                End If
            End If
        Else
            _progressReformattingFailCounter = 0
        End If


        If s.ProgressHighlighting Then
            If _progressHighlightingFailCounter < 64 Then
                Dim noMatch = True

                Try
                    Dim baseHue = ThemeManager.ColorCategories.FirstOrDefault(Function(x) x.Item1 = s.ProgressHighlightingColorName)?.Item2
                    Dim tokens As New List(Of (Start As Integer, Length As Integer, Hue As Integer, Bold As Boolean))

                    Dim AddMatch = Sub(pattern As String, group As Integer, hueOffset As Integer, bold As Boolean, lastOnly As Boolean)
                                       Dim matches = Regex.Matches(value, pattern, RegexOptions.IgnoreCase)
                                       Dim selectedMatches = matches.Cast(Of Match).Where(Function(x) x.Success)
                                       If lastOnly Then selectedMatches = selectedMatches.Reverse().Take(1)

                                       For Each match As Match In selectedMatches
                                           noMatch = False
                                           Dim mg = match.Groups(group)
                                           tokens.Add((mg.Index, mg.Length, If(baseHue, 181) + hueOffset, bold))
                                       Next
                                   End Sub

                    AddMatch("(\d+(?:\.\d+)?%)", 1, 30, False, True)
                    AddMatch("(\d+/\d+\s*frames)", 1, 15, False, True)
                    AddMatch("@\s*(\d+(?:\.\d+)?\s*fps)", 1, -5, False, True)
                    AddMatch("(\d+(?:\.\d+)?\s*(kb/s|kbits/s|kbps|mb/s|mbits/s|mbps))", 1, -15, False, True)
                    AddMatch("(\d+(?:\.\d+)?\s*(kb|mb))(?:[^a-z/0-9]|$)", 1, 15, False, True)
                    AddMatch("(-?\d+:\d+:\d+)", 1, 0, False, True)
                    AddMatch(_progressSeparatorPattern, 0, 45, True, False)

                    tokens = tokens.OrderBy(Function(t) t.Start).ToList()

                    Dim segments As New List(Of (Text As String, Color As Color?, Bold As Boolean))
                    Dim pos = 0
                    Dim luminance = If(IsDarkMode, 0.65F, 0.4F)

                    For Each t In tokens
                        If t.Start < pos Then
                            Continue For
                        End If

                        If t.Start > pos Then
                            segments.Add((value.Substring(pos, t.Start - pos), Nothing, False))
                        End If

                        segments.Add((value.Substring(t.Start, t.Length), New ColorHSL(t.Hue, 0.55, luminance).ToColor, t.Bold))
                        pos = t.Start + t.Length
                    Next

                    If pos < value.Length Then
                        segments.Add((value.Substring(pos), Nothing, False))
                    End If

                    ProgressBar.SetSegments(segments)
                Catch ex As Exception
                    ProgressBar.Text = value
                Finally
                    If noMatch Then _progressHighlightingFailCounter += 1
                End Try
            Else
                ProgressBar.Text = value
            End If
        Else
            _progressHighlightingFailCounter = 0
            ProgressBar.Text = value
        End If
    End Sub

    Sub SetProgress(value As String)
        If Proc.IsSilent Then Return

        Dim match As Match
        Dim frame As Integer = 0
        Dim frames As Integer = Proc.FrameCount
        Dim progress As Single = -1

        match = Regex.Match(value, "(?:\s|^)\[?\s*(\d+(?:[.,]\d+)?)%\]?\s?", RegexOptions.IgnoreCase)
        If match.Success Then
            progress = match.Groups(1).Value.ToSingle()
        Else
            match = Regex.Match(value, "(\d+(?:[.,]\d+))\/100", RegexOptions.IgnoreCase)
            If match.Success Then
                progress = match.Groups(1).Value.ToSingle()
            Else
                match = Regex.Match(value, "frame(?:(?:=\s*)|\s+)(\d+)(?:\s|\/)", RegexOptions.IgnoreCase)
                If match.Success AndAlso frames > 0 Then
                    frame = match.Groups(1).Value.ToInt()
                Else
                    match = Regex.Match(value, "\s(\d+)(?:\s?\/\s?(\d+))?\sframes", RegexOptions.IgnoreCase)
                    If match.Success Then
                        frame = match.Groups(1).Value.ToInt()
                        frames = match.Groups(2).Value.ToInt()
                    Else
                        match = Regex.Match(value, "time=((\d+):(\d+):(\d+))", RegexOptions.IgnoreCase)
                        If match.Success Then
                            Dim t As TimeSpan
                            If TimeSpan.TryParse(match.Groups(1).Value, t) Then
                                If _ffmpegDuration <> TimeSpan.Zero Then
                                    progress = CSng(t.Ticks / _ffmpegDuration.Ticks * 100)
                                End If
                            End If
                        Else

                        End If
                    End If
                End If
            End If
        End If

        If frame < frames AndAlso progress < 0 Then
            progress = CSng(frame / frames * 100)
        End If

        If progress >= 0 AndAlso LastProgress <> progress Then
            ProcForm.SetProgressTitle(progress)
            ProgressBar.Value = progress

            If _triggerWhileProcessing Then
                Dim eventProgress = Fix(progress * 10) / 10
                Dim eventLastProgress = Fix(LastProgress * 10) / 10
                Dim now = Date.Now
                If eventProgress > eventLastProgress AndAlso eventProgress < 100.0F AndAlso _lastTriggerWhileProcessing.AddSeconds(s.EventWhileProcessingCooldown) < now Then
                    _lastTriggerWhileProcessing = Date.Now
                    Task.Run(Sub() g.RaiseAppEvent(ApplicationEvent.WhileProcessing, Proc.CommandLine, eventProgress, value))
                End If
            End If

            LastProgress = progress
        End If

        If progress < 0 Then
            ProcForm.SetProgressTitle(-1)
            LastProgress = 0
        End If
    End Sub

    Sub Click(sender As Object, e As EventArgs)
        SyncLock Procs
            For Each i In Procs
                If i.Button Is sender Then
                    i.Activate()
                Else
                    i.Deactivate()
                End If
            Next
        End SyncLock
    End Sub

    Sub ProcDisposed()
        UIBeginInvoke(Sub() Cleanup())
    End Sub

    Shared Sub SetOutputHighlighting(value As Boolean, theme As Theme)
        If value <> s.OutputHighlighting Then
            s.OutputHighlighting = value

            For Each pc In Procs
                pc.SetAndHighlightLog(Nothing, theme)
            Next
        End If
    End Sub

    Shared Sub Abort()
        Aborted = True
        g.ShutdownMode = ShutdownMode.Nothing

        For Each i In Procs.ToArray
            i.Proc.Abort = True
            i.Proc.Kill()
        Next
    End Sub

    Shared Sub Skip()
        For Each i In Procs.ToArray
            i.Proc.Skip = True
            i.Proc.Kill()
        Next
    End Sub

    Shared Sub Suspend()
        For Each process In GetProcesses()
            ProcessHelp.SendSignal(process.Id, "STOP")
        Next
    End Sub

    Shared Sub ResumeProcs()
        For Each process In GetProcesses()
            ProcessHelp.SendSignal(process.Id, "CONT")
        Next
    End Sub

    'the processes including all processes of shell pipelines
    Shared Function GetProcesses() As List(Of Process)
        Dim ret As New List(Of Process)

        For Each pc In Procs.ToArray
            Try
                If pc.Proc.Process.HasExited Then
                    Continue For
                End If

                ret.Add(pc.Proc.Process)

                If pc.Proc.IsShell Then
                    ret.AddRange(ProcessHelp.GetDescendants(pc.Proc.Process))
                End If
            Catch
            End Try
        Next

        Return ret
    End Function

    Shared Function GetProcessPriority() As ProcessPriorityClass?
        SyncLock Procs
            For Each pc In Procs
                If Not pc.Proc.IsSilent Then
                    'processes that aren't started yet or have exited have no priority
                    Try
                        Return pc.Proc.Process?.PriorityClass
                    Catch ex As InvalidOperationException
                        Return Nothing
                    End Try
                End If
            Next
        End SyncLock
        Return Nothing
    End Function

    Shared Sub SetProcessPriority(priority As ProcessPriorityClass)
        For Each process In GetProcesses()
            Try
                process.PriorityClass = priority
            Catch
            End Try
        Next
    End Sub

    Sub Cleanup()
        BlockActivation = True

        SyncLock Procs
            Procs.Remove(Me)

            RemoveHandler Proc.ProcDisposed, AddressOf ProcDisposed
            RemoveHandler Proc.OutputDataReceived, AddressOf DataReceived
            RemoveHandler Proc.ErrorDataReceived, AddressOf DataReceived

            ProcForm.RemoveProcControls(Me)

            For Each i In Procs
                i.Deactivate()
            Next

            If Procs.Count > 0 Then
                Procs(0).Activate()
            End If
        End SyncLock

        If Not Proc.Succeeded And Not Proc.Skip Then
            Abort()
        End If

        Task.Run(Sub()
                     Thread.Sleep(500)

                     SyncLock Procs
                         If Procs.Count = 0 AndAlso Not g.IsJobProcessing Then
                             Finished()
                         End If
                     End SyncLock
                 End Sub)
    End Sub

    Shared Sub Finished()
        UIBeginInvoke(Sub()
                          g.ProcForm?.HideForm()

                          If g.MainForm Is Nothing OrElse g.MainForm.IsDisposed Then
                              Exit Sub
                          End If

                          BlockActivation = False

                          If Not g.IsJobProcessing AndAlso Not g.MainForm.Visible Then
                              g.MainForm.Show()
                          End If

                          Aborted = False
                      End Sub)
    End Sub

    Sub Activate()
        Proc.IsSilent = False
        ProcForm.ShowProc(Me)
        _lastHighlightedText = ""
        LogHandler()
        ApplyTheme()
    End Sub

    Sub Deactivate()
        Proc.IsSilent = True
        ApplyTheme()
    End Sub

    Shared Sub AddProc(proc As Proc)
        SyncLock Procs
            Dim pc As New ProcController(proc)
            Procs.Add(pc)

            If Procs.Count = 1 Then
                pc.Activate()
            Else
                pc.Deactivate()
            End If
        End SyncLock
    End Sub

    'can be called from any thread
    Shared Sub Start(proc As Proc)
        If Aborted Then
            Throw New AbortException
        End If

        UIInvoke(Sub()
                     If g.MainForm.Visible Then
                         g.MainForm.Hide()
                     End If

                     If g.ProcForm Is Nothing OrElse g.ProcForm.IsDisposed Then
                         g.ProcForm = New ProcessingForm
                         _progressInterval = 200
                     End If

                     If g.ProcForm.WindowState <> WindowState.Minimized OrElse Not BlockActivation Then
                         g.ProcForm.ShowForm()
                     End If

                     AddProc(proc)
                     g.ProcForm.UpdateControls()
                 End Sub)
    End Sub
End Class
