
Imports System.Globalization
Imports System.Text
Imports System.Text.RegularExpressions
Imports Microsoft.VisualBasic
Imports StaxRip.UI
Imports StaxRip.VideoEncoderCommandLine

<Serializable()>
Public Class Macro
    Implements IComparable(Of Macro)

    Sub New()
        MyClass.New("", "", GetType(String), "")
    End Sub

    Sub New(name As String,
            friendlyName As String,
            type As Type,
            description As String)

        If name.StartsWith("$") Then
            Me.Name = name
        Else
            Me.Name = "%" + name + "%"
        End If

        Me.FriendlyName = friendlyName
        Me.Type = type
        Me.Description = description
    End Sub

    Private NameValue As String

    Property Name() As String
        Get
            If NameValue Is Nothing Then
                NameValue = ""
            End If

            Return NameValue
        End Get
        Set(Value As String)
            NameValue = Value
        End Set
    End Property

    Private FriendlyNameValue As String

    Property FriendlyName() As String
        Get
            If FriendlyNameValue = "" AndAlso NameValue <> "" Then
                FriendlyNameValue = NameValue.Replace("_", " ").Replace("%", " ").Trim(" "c).ToTitleCase
            End If

            If FriendlyNameValue Is Nothing Then FriendlyNameValue = ""

            Return FriendlyNameValue
        End Get
        Set(Value As String)
            FriendlyNameValue = Value
        End Set
    End Property

    Private TypeValue As Type

    Property Type As Type
        Get
            If TypeValue Is Nothing Then TypeValue = GetType(String)

            Return TypeValue
        End Get
        Set(Value As Type)
            TypeValue = Value
        End Set
    End Property

    Private DescriptionValue As String

    Property Description() As String
        Get
            If DescriptionValue Is Nothing Then
                DescriptionValue = ""
            End If

            Return DescriptionValue
        End Get
        Set(Value As String)
            DescriptionValue = Value
        End Set
    End Property

    Shared Function GetTips(includeGlobals As Boolean, includeInteractive As Boolean, includeParam As Boolean, includeWhileProcessing As Boolean, includeAudioSpecific As Boolean) As StringPairList
        Dim ret As New StringPairList

        For Each macro In GetMacros(includeGlobals, includeInteractive, includeParam, includeWhileProcessing, includeAudioSpecific)
            ret.Add(macro.Name, macro.Description)
        Next

        Return ret
    End Function

    Shared Function GetTipsFriendly(convertHTMLChars As Boolean) As StringPairList
        Dim ret As New StringPairList

        For Each mac As Macro In GetMacros(True, False, False, True, False)
            If convertHTMLChars Then
                ret.Add(HelpDocument.ConvertChars(mac.FriendlyName), mac.Description)
            Else
                ret.Add(mac.FriendlyName, mac.Description)
            End If
        Next

        Return ret
    End Function

    Overrides Function ToString() As String
        Return Name
    End Function

    Function CompareTo(other As Macro) As Integer Implements System.IComparable(Of Macro).CompareTo
        Return Name.CompareTo(other.Name)
    End Function

    Shared Function GetMacros(includeGlobals As Boolean, includeInteractive As Boolean, includeParam As Boolean, includeWhileProcessing As Boolean, includeAudioSpecific As Boolean) As List(Of Macro)
        Dim ret As New List(Of Macro)

        If includeGlobals Then
            ret.Add(New Macro("audio_bitrate", "Audio Bitrate", GetType(Integer), "Overall audio bitrate."))
            ret.Add(New Macro("compressibility", "Compressibility", GetType(Integer), "Compressibility value."))
            ret.Add(New Macro("crop_bottom", "Crop Bottom", GetType(Integer), "Bottom crop value."))
            ret.Add(New Macro("crop_height", "Crop Height", GetType(Integer), "Crop height."))
            ret.Add(New Macro("crop_left", "Crop Left", GetType(Integer), "Left crop value."))
            ret.Add(New Macro("crop_right", "Crop Right", GetType(Integer), "Right crop value."))
            ret.Add(New Macro("crop_top", "Crop Top", GetType(Integer), "Top crop value."))
            ret.Add(New Macro("crop_width", "Crop Width", GetType(Integer), "Crop width."))
            ret.Add(New Macro("current_date", "Current Date", GetType(String), "Returns the current date."))
            ret.Add(New Macro("current_time", "Current Time (12h)", GetType(String), "Returns the current time (12h)."))
            ret.Add(New Macro("current_time24", "Current Time (24h)", GetType(String), "Returns the current time (24h)."))
            ret.Add(New Macro("dpi", "Main Dialog DPI", GetType(Integer), "DPI value of the main dialog."))
            ret.Add(New Macro("empty", "Empty/Nothing", GetType(String), "Empty character for special use like removing transferred values."))
            ret.Add(New Macro("encoder", "Encoder", GetType(String), "Name of the active video encoder."))
            ret.Add(New Macro("encoder_codec", "Encoder Codec", GetType(String), "Codec that is used by the active video encoder."))
            ret.Add(New Macro("encoder_ext", "Encoder File Extension", GetType(String), "File extension of the format the encoder of the active project outputs."))
            ret.Add(New Macro("encoder_out_file", "Encoder Output File", GetType(String), "Output file of the video encoder."))
            ret.Add(New Macro("encoder_profile", "Encoder Profile", GetType(String), "Name of the selected video encoder profile name."))
            ret.Add(New Macro("encoder_settings", "Encoder Settings", GetType(String), "Settings of the active video encoder."))
            ret.Add(New Macro("muxer_ext", "Muxer Extension", GetType(String), "Output extension of the active muxer."))
            ret.Add(New Macro("jobs", "Jobs", GetType(Integer), "Number of all jobs in Jobs List."))
            ret.Add(New Macro("jobs_active", "Jobs Active", GetType(Integer), "Number of active jobs in Jobs List."))
            ret.Add(New Macro("player", "Player", GetType(String), "Path of the media player."))
            ret.Add(New Macro("plugin_dir", "Plugin Directory", GetType(String), "AviSynth/VapourSynth plugin auto load directory."))
            ret.Add(New Macro("pos_frame", "Position In Frames", GetType(Integer), "Current preview position in frames."))
            ret.Add(New Macro("pos_ms", "Position In Milliseconds", GetType(Integer), "Current preview position in milliseconds."))
            ret.Add(New Macro("processing", "Processing", GetType(String), "Returns 'True' if a job is currently processing otherwise 'False'."))
            ret.Add(New Macro("programs_dir", "Programs Directory", GetType(String), "Programs system directory."))
            ret.Add(New Macro("script_dir", "Script Directory", GetType(String), "Users PowerShell scripts directory."))
            ret.Add(New Macro("script_ext", "Script File Extension", GetType(String), "File extension of the AviSynth/VapourSynth script so either avs or vpy."))
            ret.Add(New Macro("script_file", "Script Path", GetType(String), "Path of the AviSynth/VapourSynth script."))
            ret.Add(New Macro("sel_end", "Selection End", GetType(Integer), "End position of the first selection in the preview."))
            ret.Add(New Macro("sel_start", "Selection Start", GetType(Integer), "Start position of the first selection in the preview."))
            ret.Add(New Macro("settings_dir", "Settings Directory", GetType(String), "Path of the settings direcory."))
            ret.Add(New Macro("source_dar", "Source Display Aspect Ratio", GetType(String), "Source display aspect ratio."))
            ret.Add(New Macro("source_dir", "Source Directory", GetType(String), "Directory of the source file."))
            ret.Add(New Macro("source_dir_name", "Source Directory Name", GetType(String), "Name of the source file directory."))
            ret.Add(New Macro("source_dir_parent", "Source Directory Parent", GetType(String), "Parent directory of the source file directory."))
            ret.Add(New Macro("source_ext", "Source File Extension", GetType(String), "File extension of the source file."))
            ret.Add(New Macro("source_file", "Source File Path", GetType(String), "File path of the source video."))
            ret.Add(New Macro("source_files", "Source Files Blank", GetType(String), "Source files in quotes separated by a blank."))
            ret.Add(New Macro("source_files_comma", "Source Files Comma", GetType(String), "Source files in quotes separated by comma."))
            ret.Add(New Macro("source_framerate", "Source Framerate", GetType(Integer), "Frame rate returned by the Source filter section with up to 6 decimal places, depending on digits, with no trailing zeros."))
            ret.Add(New Macro("source_framerate6", "Source Framerate (6 fixed decimal places)", GetType(Integer), "Frame rate returned by the Source filter section with 6 fixed decimal places."))
            ret.Add(New Macro("source_frames", "Source Frames", GetType(Integer), "Length in frames of the source video."))
            ret.Add(New Macro("source_height", "Source Image Height", GetType(Integer), "Image height of the source video."))
            ret.Add(New Macro("source_name", "Source Filename Without Extension", GetType(String), "The name of the source file without file extension."))
            ret.Add(New Macro("source_par_x", "Source Pixel Aspect Ratio X", GetType(String), "Source pixel/sample aspect ratio."))
            ret.Add(New Macro("source_par_y", "Source Pixel Aspect Ratio Y", GetType(String), "Source pixel/sample aspect ratio."))
            ret.Add(New Macro("source_seconds", "Source Seconds", GetType(Integer), "Length in seconds of the source video."))
            ret.Add(New Macro("source_temp_file", "Source Temp File", GetType(String), "File located in the temp directory using the same name as the source file."))
            ret.Add(New Macro("source_width", "Source Image Width", GetType(Integer), "Image width of the source video."))
            ret.Add(New Macro("source_mi_v:Format", "Source Video Codec", GetType(String), "Video codec of the source file."))
            ret.Add(New Macro("source_mi_vc", "Source Video Track Count", GetType(Integer), "Video track count of the source video."))
            ret.Add(New Macro("source_mi_ac", "Source Audio Track Count", GetType(Integer), "Audio track count of the source video."))
            ret.Add(New Macro("source_mi_tc", "Source Text Track Count", GetType(Integer), "Text track count of the source video."))
            ret.Add(New Macro("startup_dir", "Startup Directory", GetType(String), "Directory of the application."))
            ret.Add(New Macro("system_dir", "System Directory", GetType(String), "System directory."))
            ret.Add(New Macro("target_dar", "Target Display Aspect Ratio", GetType(String), "Target display aspect ratio."))
            ret.Add(New Macro("target_dir", "Target Directory", GetType(String), "Directory of the target file."))
            ret.Add(New Macro("target_file", "Target File Path", GetType(String), "File path of the target file."))
            ret.Add(New Macro("target_framerate", "Target Framerate", GetType(Integer), "Frame rate of the target video with up to 6 decimal places, depending on digits, with no trailing zeros."))
            ret.Add(New Macro("target_framerate6", "Target Framerate (6 fixed decimal places)", GetType(Integer), "Frame rate of the target video with 6 fixed decimal places."))
            ret.Add(New Macro("target_frames", "Target Frames", GetType(Integer), "Length in frames of the target video."))
            ret.Add(New Macro("target_height", "Target Image Height", GetType(Integer), "Image height of the target video."))
            ret.Add(New Macro("target_name", "Target Filename Without Extension", GetType(String), "Name of the target file without file extension."))
            ret.Add(New Macro("target_par_x", "Target Pixel Aspect Ratio X", GetType(String), "Target pixel/sample aspect ratio."))
            ret.Add(New Macro("target_par_y", "Target Pixel Aspect Ratio Y", GetType(String), "Target pixel/sample aspect ratio."))
            ret.Add(New Macro("target_seconds", "Target Seconds", GetType(Integer), "Length in seconds of the target video."))
            ret.Add(New Macro("target_size", "Target Size", GetType(Integer), "Size of the target video in kilo bytes."))
            ret.Add(New Macro("target_temp_file", "Target Temp File", GetType(String), "File located in the temp directory using the same name as the target file."))
            ret.Add(New Macro("target_width", "Target Image Width", GetType(Integer), "Image width of the target video."))
            ret.Add(New Macro("temp_dir", "Temp Directory", GetType(String), "Directory of the source file or the temp directory if enabled."))
            ret.Add(New Macro("temp_file", "Temp File", GetType(String), "File located in the temp directory using the same name as the source file."))
            ret.Add(New Macro("template_name", "Template Name", GetType(String), "Name of the template the active project is based on."))
            ret.Add(New Macro("text_editor", "Text Editor", GetType(String), "Path of the application currently associated with TXT files."))
            ret.Add(New Macro("version", "Version", GetType(String), "StaxRip version."))
            ret.Add(New Macro("video_bitrate", "Video Bitrate", GetType(Integer), "Video bitrate in Kbps."))
            ret.Add(New Macro("working_dir", "Working Directory", GetType(String), "Directory of the source file or the temp directory if enabled."))
        End If

        If includeInteractive Then
            ret.Add(New Macro("$browse_file$", "Browse For File", GetType(String), "Filepath returned from a file browser."))
            ret.Add(New Macro("$enter_text$", "Enter Text", GetType(String), "Text entered in an input box."))
            ret.Add(New Macro("$enter_text:prompt$", "Enter Text (Params)", GetType(String), "Text entered in an input box."))
            ret.Add(New Macro("$select:param1;param2;...$", "Select", GetType(String), "String selected from dropdown, to show an optional message the first parameter has to start with msg: and to give the items optional captions use caption|value. Example: $select:msg:hello;caption1|value1;caption2|value2$"))
        End If

        If includeParam Then
            ret.Add(New Macro("audio_bitrate:X", "Audio Bitrate X", GetType(Integer), "Audio bitrate of the X'th audio track."))
            ret.Add(New Macro("audio_channels:X", "Audio Channels X", GetType(Integer), "Audio channels of the X'th audio track."))
            ret.Add(New Macro("audio_codec:X", "Audio Codec X", GetType(String), "Audio codec of the X'th audio track."))
            ret.Add(New Macro("audio_delay:X", "Audio Delay X", GetType(Integer), "Audio delay of the X'th audio track."))
            ret.Add(New Macro("audio_file:X", "X'th Audio File", GetType(String), "File path of the X'th audio file."))
            ret.Add(New Macro("app_dir:name", "Application Directory", GetType(String), "Returns the directory of a given tool, it can be any type of tool found in the Apps dialog. Example: %app_dir:x265%"))
            ret.Add(New Macro("app_path:name", "Application File Path", GetType(String), "Returns the path of a given tool, it can be any type of tool found in the Apps dialog. Example: %app_path:x265%"))
            ret.Add(New Macro("app_version:name", "Application Version", GetType(String), "Returns the version of a given tool, it can be any type of tool found in the Apps dialog. Example: %app_version:x265%"))
            ret.Add(New Macro("eval:expression", "Eval Math Expression", GetType(String), "Evaluates a PowerShell expression which may contain macros."))
            ret.Add(New Macro("filter:name", "Filter", GetType(String), "Returns the script code of a filter of the active project that matches the specified name."))
            ret.Add(New Macro("isfilteractive:name", "Active Filter Check", GetType(String), "Returns `1` if the filter is active, otherwise `0`."))
            ret.Add(New Macro("random:digits", "Random Number", GetType(Integer), "Returns a 'digits' long random number, whereas 'digits' is clamped between `1` and `10`."))
            ret.Add(New Macro("source_mi_g:property", "MediaInfo General Property", GetType(String), "Returns the given MediaInfo property from the General section for the source file."))
            ret.Add(New Macro("source_mi_v:property", "MediaInfo Video Property", GetType(String), "Returns the given MediaInfo property from the Video section for the source file. Before `:` you can add a zero-based index for the track number of that section."))
            ret.Add(New Macro("source_mi_a:property", "MediaInfo Audio Property", GetType(String), "Returns the given MediaInfo property from the Audio section for the source file. Before `:` you can add a zero-based index for the track number of that section."))
            ret.Add(New Macro("source_mi_t:property", "MediaInfo Text Property", GetType(String), "Returns the given MediaInfo property from the Text section for the source file. Before `:` you can add a zero-based index for the track number of that section."))
        End If

        If includeWhileProcessing Then
            ret.Add(New Macro("commandline", "Command Line", GetType(String), "Returns the command line used for the running app."))
            ret.Add(New Macro("progress", "Progress", GetType(Integer), "Returns the current progress as Integer value."))
            ret.Add(New Macro("progressline", "Progress Line", GetType(String), "Returns the progress line received from the running app."))
        End If

        If includeAudioSpecific Then
            ret.Add(New Macro("input", "Input", GetType(String), "Audio source file."))
            ret.Add(New Macro("output", "Output", GetType(String), "Audio target file."))
            ret.Add(New Macro("bitrate", "Bitrate", GetType(String), "Audio bitrate."))
            ret.Add(New Macro("channels", "Channels", GetType(String), "Audio channels count."))
            ret.Add(New Macro("delay", "Delay", GetType(String), "Audio delay."))
            ret.Add(New Macro("language_native", "Language Native", GetType(String), "Native language name."))
            ret.Add(New Macro("language_english", "Language English", GetType(String), "English language name."))
            ret.Add(New Macro("streamid0", "0-based Stream ID", GetType(String), "ID of the stream (starts with 0)."))
            ret.Add(New Macro("streamid1", "1-based Stream ID", GetType(String), "ID of the stream (starts with 1)."))
        End If

        ret.Sort()

        Return ret
    End Function

    Shared Function ExpandGUI(value As String, Optional throwIfCancel As Boolean = False) As (Value As String, Caption As String, Cancel As Boolean)
        Dim ret As (Value As String, Caption As String, Cancel As Boolean) = (value, "", False)
        If ret.Value = "" Then Return ret

        If ret.Value.Contains("$browse_file$") Then
            Using dialog As New OpenFileDialog
                ret.Cancel = dialog.ShowDialog <> DialogResult.OK

                'the path is inserted into scripts and command lines as it is
                If Not ret.Cancel AndAlso dialog.FileName.Any(Function(c) """\%$`".Contains(c) OrElse Char.IsControl(c)) Then
                    MsgError("The file path contains unsupported characters.", "Paths with "" \ % $ ` or line breaks can't be used here." + BR2 + dialog.FileName)
                    ret.Cancel = True
                End If

                If ret.Cancel Then
                    If throwIfCancel Then Throw New AbortException
                    Return ret
                Else
                    ret.Value = ret.Value.Replace("$browse_file$", dialog.FileName)
                End If
            End Using
        End If

        If ret.Value.Contains("$enter_text$") Then
            Dim inputText = InputBox.Show("Please enter text/value")

            If inputText = "" Then
                ret.Cancel = True
                Return ret
            Else
                ret.Value = ret.Value.Replace("$enter_text$", inputText)
            End If
        End If

        If ret.Value.Contains("$enter_text:") Then
            Dim matches = Regex.Matches(ret.Value, "\$enter_text:(.+?)\$")

            For Each iMatch As Match In matches
                Dim inputText = InputBox.Show(iMatch.Groups(1).Value)

                If inputText = "" Then
                    ret.Cancel = True
                    Return ret
                Else
                    ret.Value = ret.Value.Replace(iMatch.Value, inputText)
                End If
            Next
        End If

        If ret.Value.Contains("$select:") Then
            Dim matches = Regex.Matches(ret.Value, "\$select:(.+?)\$")

            For Each iMatch As Match In matches
                Dim items = iMatch.Groups(1).Value.SplitNoEmpty(";").ToList

                If items.Count > 0 Then
                    Using td As New TaskDialog(Of String)
                        If items?(0)?.StartsWith("msg:") Then
                            td.Title = items(0).Substring(4)
                            items.RemoveAt(0)
                        Else
                            td.Title = "Please select one of the options."
                        End If

                        For Each iItem As String In items
                            If iItem.Contains("|") Then
                                td.AddCommand(iItem.Left("|"), iItem.Right("|"))
                            Else
                                td.AddCommand(iItem, iItem)
                            End If
                        Next

                        ret.Cancel = td.Show = ""

                        If ret.Cancel Then
                            Return ret
                        Else
                            ret.Caption = td.SelectedText
                            ret.Value = ret.Value.Replace(iMatch.Value, td.SelectedValue)
                        End If
                    End Using
                End If
            Next
        End If

        Return ret
    End Function


    Shared Function Expand(value As String) As String
        Return Expand(value, p)
    End Function

    Shared Function Expand(value As String, proj As Project) As String
        Return ExpandValues(value, proj, False).Replace(PercentPlaceholder, "%")
    End Function

    'expands a command line that runs in /bin/sh, values are quoted for their position
    Shared Function ExpandShell(value As String, Optional proj As Project = Nothing) As String
        Return QuoteShellValues(ExpandValues(value, If(proj, p), True))
    End Function

    'file names on Linux and metadata can contain characters like % and :, so
    'values must not form new macros like %eval:, NUL never occurs in them
    Shared ReadOnly PercentPlaceholder As String = ChrW(0) + "p"
    Shared ReadOnly ValueStart As String = ChrW(0) + "("
    Shared ReadOnly ValueEnd As String = ChrW(0) + ")"

    Shared Function Protect(value As String, shell As Boolean) As String
        If value = "" Then Return ""

        Dim needsQuotes = shell AndAlso value.Any(Function(c) Not Char.IsLetterOrDigit(c) AndAlso Not "_./:@%+=,-".Contains(c))
        value = value.Replace(ChrW(0), "").Replace("%", PercentPlaceholder)
        Return If(needsQuotes, ValueStart + value + ValueEnd, value)
    End Function

    Shared Function Unprotect(value As String) As String
        Return value.Replace(ValueStart, "").Replace(ValueEnd, "").Replace(PercentPlaceholder, "%")
    End Function

    Shared Function QuoteShellValues(value As String) As String
        Dim sb As New StringBuilder
        Dim quote As Char = Nothing
        Dim i = 0

        While i < value.Length
            If String.CompareOrdinal(value, i, ValueStart, 0, ValueStart.Length) = 0 Then
                Dim endIndex = value.IndexOf(ValueEnd, i, StringComparison.Ordinal)
                Dim text = Unprotect(value.Substring(i + ValueStart.Length, endIndex - i - ValueStart.Length))

                Select Case quote
                    Case "'"c
                        sb.Append(text.Replace("'", "'\''"))
                    Case """"c
                        For Each c In text
                            If "\""$`".Contains(c) Then sb.Append("\"c)
                            sb.Append(c)
                        Next
                    Case Else
                        sb.Append(ShellHelp.Quote(text))
                End Select

                i = endIndex + ValueEnd.Length
                Continue While
            End If

            If String.CompareOrdinal(value, i, PercentPlaceholder, 0, PercentPlaceholder.Length) = 0 Then
                sb.Append("%"c)
                i += PercentPlaceholder.Length
                Continue While
            End If

            Dim ch = value(i)

            If quote = "'"c Then
                If ch = "'"c Then quote = Nothing
            ElseIf ch = "\"c AndAlso i + 1 < value.Length AndAlso value(i + 1) <> ChrW(0) Then
                sb.Append(ch)
                i += 1
                ch = value(i)
            ElseIf quote = """"c Then
                If ch = """"c Then quote = Nothing
            ElseIf ch = "'"c OrElse ch = """"c Then
                quote = ch
            End If

            sb.Append(ch)
            i += 1
        End While

        Return sb.ToString
    End Function

    'file names on Linux and the metadata of media files can contain eval macros
    Shared Function IsUntrustedEval(macro As String, proj As Project) As Boolean
        Dim paths As New List(Of String) From {
            proj.SourceFile, proj.TargetFile, proj.TempDir, proj.FirstOriginalSourceFile,
            proj.LastOriginalSourceFile, proj.Hdr10PlusMetadataFile, proj.HdrDolbyVisionMetadataFile?.Path}

        Dim names As New List(Of String)

        If proj.SourceFiles IsNot Nothing Then paths.AddRange(proj.SourceFiles)

        If proj.AudioTracks IsNot Nothing Then
            For Each track In proj.AudioTracks
                paths.Add(track?.AudioProfile?.File)
                names.Add(track?.AudioProfile?.StreamName)
                names.Add(track?.AudioProfile?.Stream?.Title)
            Next
        End If

        Dim muxer = proj.VideoEncoder?.Muxer

        If muxer IsNot Nothing Then
            paths.AddRange({muxer.ChapterFile, muxer.CoverFile, muxer.TagFile, muxer.TimestampsFile})
            If muxer.Attachments IsNot Nothing Then paths.AddRange(muxer.Attachments)

            If muxer.Subtitles IsNot Nothing Then
                For Each subtitle In muxer.Subtitles
                    paths.Add(subtitle.Path)
                    names.Add(subtitle.Title)
                Next
            End If

            If proj.TakeOverTitle Then names.Add(TryCast(muxer, MkvMuxer)?.Title)
        End If

        Return paths.Any(Function(i) i <> "" AndAlso i.Contains("%eval:")) OrElse
            names.Any(Function(i) i <> "" AndAlso i.Contains(macro))
    End Function

    Private Shared Function ExpandValues(value As String, proj As Project, shell As Boolean) As String
        If value = "" Then Return ""
        If proj Is Nothing Then Return ""

        Dim matches As MatchCollection = Nothing

        If value.Contains("%") Then
            If value.Contains("%empty%") Then value = value.Replace("%empty%", "")
            If value.Contains("%current_date%") Then value = value.Replace("%current_date%", Protect(Date.Now.ToString("yyyy-MM-dd"), shell))
            If value.Contains("%current_time%") Then value = value.Replace("%current_time%", Protect(Date.Now.ToString("hh-mm-ss"), shell))
            If value.Contains("%current_time24%") Then value = value.Replace("%current_time24%", Protect(Date.Now.ToString("HH-mm-ss"), shell))
            If value.Contains("%source_file%") Then value = value.Replace("%source_file%", Protect(proj.SourceFile, shell))
            If value.Contains("%working_dir%") Then value = value.Replace("%working_dir%", Protect(proj.TempDir, shell))
            If value.Contains("%temp_dir%") Then value = value.Replace("%temp_dir%", Protect(proj.TempDir, shell))
            If value.Contains("%temp_file%") Then value = value.Replace("%temp_file%", Protect(Path.Combine(proj.TempDir, proj.SourceFile.Base), shell))
            If value.Contains("%source_temp_file%") Then value = value.Replace("%source_temp_file%", Protect(Path.Combine(proj.TempDir, g.GetSourceBase), shell))
            If value.Contains("%target_temp_file%") Then value = value.Replace("%target_temp_file%", Protect(Path.Combine(proj.TempDir, proj.TargetFile.Base), shell))
            If value.Contains("%source_name%") Then value = value.Replace("%source_name%", Protect(proj.SourceFile.Base, shell))
            If value.Contains("%source_ext%") Then value = value.Replace("%source_ext%", Protect(proj.FirstOriginalSourceFile.Ext, shell))
            If value.Contains("%version%") Then value = value.Replace("%version%", Protect(AppInfo.ProductVersion, shell))
            If value.Contains("%source_width%") Then value = value.Replace("%source_width%", Protect(proj.SourceWidth.ToString, shell))
            If value.Contains("%source_height%") Then value = value.Replace("%source_height%", Protect(proj.SourceHeight.ToString, shell))
            If value.Contains("%source_seconds%") Then value = value.Replace("%source_seconds%", Protect(proj.SourceSeconds.ToString, shell))
            If value.Contains("%source_frames%") Then value = value.Replace("%source_frames%", Protect(proj.SourceFrames.ToString, shell))
            If value.Contains("%source_framerate%") Then value = value.Replace("%source_framerate%", Protect(proj.SourceFrameRate.ToString("0.######", CultureInfo.InvariantCulture), shell))
            If value.Contains("%source_framerate6%") Then value = value.Replace("%source_framerate6%", Protect(proj.SourceFrameRate.ToString("f6", CultureInfo.InvariantCulture), shell))
            If value.Contains("%source_dir%") Then value = value.Replace("%source_dir%", Protect(proj.SourceFile.Dir, shell))
            If value.Contains("%source_dir_parent%") Then value = value.Replace("%source_dir_parent%", Protect(proj.SourceFile.Dir.Parent, shell))
            If value.Contains("%source_dir_name%") Then value = value.Replace("%source_dir_name%", Protect(proj.SourceFile.Dir.DirName, shell))

            If value.Contains("%source_mi_") Then
                Dim miFile = If(proj.FirstOriginalSourceFile.FileExists(), proj.FirstOriginalSourceFile, proj.SourceFile)

                If value.Contains("%source_mi_v:Format%") Then value = value.Replace("%source_mi_v:Format%", Protect(MediaInfo.GetVideo(miFile, "Format"), shell))

                If value.Contains("%source_mi_vc%") Then value = value.Replace("%source_mi_vc%", Protect(If(miFile.FileExists(), MediaInfo.GetVideoCount(miFile).ToInvariantString(), ""), shell))
                If value.Contains("%source_mi_ac%") Then value = value.Replace("%source_mi_ac%", Protect(If(miFile.FileExists(), MediaInfo.GetAudioCount(miFile).ToInvariantString(), ""), shell))
                If value.Contains("%source_mi_tc%") Then value = value.Replace("%source_mi_tc%", Protect(If(miFile.FileExists(), MediaInfo.GetSubtitleCount(miFile).ToInvariantString(), ""), shell))

                If value.Contains("%source_mi_") Then
                    For Each match As Match In Regex.Matches(value, "%source_mi_g:(.+?)%")
                        value = value.Replace(match.Value, Protect(MediaInfo.GetGeneral(miFile, match.Groups(1).Value), shell))
                    Next
                    For Each match As Match In Regex.Matches(value, "%source_mi_v(\d*):(.+?)%")
                        value = value.Replace(match.Value, Protect(MediaInfo.GetVideo(miFile, If(match.Groups(1).Success, match.Groups(1).Value.ToInt(), 0), match.Groups(2).Value), shell))
                    Next
                    For Each match As Match In Regex.Matches(value, "%source_mi_a(\d*):(.+?)%")
                        value = value.Replace(match.Value, Protect(MediaInfo.GetAudio(miFile, If(match.Groups(1).Success, match.Groups(1).Value.ToInt(), 0), match.Groups(2).Value), shell))
                    Next
                    For Each match As Match In Regex.Matches(value, "%source_mi_t(\d*):(.+?)%")
                        value = value.Replace(match.Value, Protect(MediaInfo.GetText(miFile, If(match.Groups(1).Success, match.Groups(1).Value.ToInt(), 0), match.Groups(2).Value), shell))
                    Next
                End If
            End If

            If value.Contains("%target_width%") Then value = value.Replace("%target_width%", Protect(proj.TargetWidth.ToString, shell))
            If value.Contains("%target_height%") Then value = value.Replace("%target_height%", Protect(proj.TargetHeight.ToString, shell))
            If value.Contains("%target_seconds%") Then value = value.Replace("%target_seconds%", Protect(proj.TargetSeconds.ToString, shell))
            If value.Contains("%target_bitdepth%") Then value = value.Replace("%target_bitdepth%", Protect(proj.Script.Info.BitDepth.ToString, shell))
            If value.Contains("%target_frames%") Then value = value.Replace("%target_frames%", Protect(proj.Script.Info.FrameCount.ToString, shell))
            If value.Contains("%target_framerate%") Then value = value.Replace("%target_framerate%", Protect(proj.Script.GetCachedFrameRate.ToString("0.######", CultureInfo.InvariantCulture), shell))
            If value.Contains("%target_framerate6%") Then value = value.Replace("%target_framerate6%", Protect(proj.Script.GetCachedFrameRate.ToString("f6", CultureInfo.InvariantCulture), shell))
            If value.Contains("%target_size%") Then value = value.Replace("%target_size%", Protect((proj.TargetSize * SizePrefix.Base).ToString, shell))
            If value.Contains("%target_file%") Then value = value.Replace("%target_file%", Protect(proj.TargetFile, shell))
            If value.Contains("%target_dir%") Then value = value.Replace("%target_dir%", Protect(proj.TargetFile.Dir, shell))
            If value.Contains("%target_name%") Then value = value.Replace("%target_name%", Protect(proj.TargetFile.Base, shell))
            If value.Contains("%crop_width%") Then value = value.Replace("%crop_width%", Protect((proj.SourceWidth - proj.CropLeft - proj.CropRight).ToString, shell))
            If value.Contains("%crop_height%") Then value = value.Replace("%crop_height%", Protect((proj.SourceHeight - proj.CropTop - proj.CropBottom).ToString, shell))
            If value.Contains("%crop_left%") Then value = value.Replace("%crop_left%", Protect(proj.CropLeft.ToString, shell))
            If value.Contains("%crop_top%") Then value = value.Replace("%crop_top%", Protect(proj.CropTop.ToString, shell))
            If value.Contains("%crop_right%") Then value = value.Replace("%crop_right%", Protect(proj.CropRight.ToString, shell))
            If value.Contains("%crop_bottom%") Then value = value.Replace("%crop_bottom%", Protect(proj.CropBottom.ToString, shell))
            If value.Contains("%video_bitrate%") Then value = value.Replace("%video_bitrate%", Protect(proj.VideoBitrate.ToString, shell))
            If value.Contains("%audio_bitrate%") Then value = value.Replace("%audio_bitrate%", Protect(CInt(proj.AudioTracks.Sum(Function(x) x.AudioProfile.Bitrate)).ToString, shell))

            matches = Regex.Matches(value, "%audio_bitrate:(\d+)?%")
            For Each match As Match In matches
                If Not match.Groups(1).Success Then
                    value = value.Replace(match.Value, Protect(CInt(proj.AudioTracks.Sum(Function(x) x.AudioProfile.Bitrate)).ToString, shell))
                Else
                    Dim track = match.Groups(1).Value.ToInt() - 1
                    If track >= 0 AndAlso track < proj.AudioTracks.Count Then
                        value = value.Replace(match.Value, Protect(CInt(proj.AudioTracks(track).AudioProfile.Bitrate).ToString, shell))
                    End If
                End If
            Next

            matches = Regex.Matches(value, "%audio_channels:(\d+)%")
            For Each match As Match In matches
                Dim track = match.Groups(1).Value.ToInt() - 1
                If track >= 0 AndAlso track < proj.AudioTracks.Count Then
                    value = value.Replace(match.Value, Protect(proj.AudioTracks(track).AudioProfile.Channels.ToString, shell))
                End If
            Next

            matches = Regex.Matches(value, "%audio_codec:(\d+)%")
            For Each match As Match In matches
                Dim track = match.Groups(1).Value.ToInt() - 1
                If track >= 0 AndAlso track < proj.AudioTracks.Count Then
                    value = value.Replace(match.Value, Protect(proj.AudioTracks(track).AudioProfile.AudioCodec.ToString, shell))
                End If
            Next

            matches = Regex.Matches(value, "%audio_delay:(\d+)%")
            For Each match As Match In matches
                Dim track = match.Groups(1).Value.ToInt() - 1
                If track >= 0 AndAlso track < proj.AudioTracks.Count Then
                    value = value.Replace(match.Value, Protect(proj.AudioTracks(track).AudioProfile.Delay.ToString, shell))
                End If
            Next

            matches = Regex.Matches(value, "%audio_file:(\d+)%")
            For Each match As Match In matches
                Dim track = match.Groups(1).Value.ToInt() - 1
                If track >= 0 AndAlso track < proj.AudioTracks.Count Then
                    value = value.Replace(match.Value, Protect(proj.AudioTracks(track).AudioProfile.File, shell))
                End If
            Next

            If value.Contains("%startup_dir%") Then value = value.Replace("%startup_dir%", Protect(Folder.Startup, shell))
            If value.Contains("%system_dir%") Then value = value.Replace("%system_dir%", Protect(Folder.System, shell))
            If value.Contains("%script_dir%") Then value = value.Replace("%script_dir%", Protect(Folder.Scripts, shell))
            If value.Contains("%programs_dir%") Then value = value.Replace("%programs_dir%", Protect(Folder.Programs, shell))
            If value.Contains("%plugin_dir%") Then value = value.Replace("%plugin_dir%", Protect(Folder.Plugins, shell))
            If value.Contains("%source_files_comma%") Then value = value.Replace("%source_files_comma%", Protect("""" + String.Join(""",""", proj.SourceFiles.ToArray) + """", shell))
            If value.Contains("%source_files%") Then
                If shell Then
                    value = value.Replace("%source_files%", Protect(String.Join(" ", proj.SourceFiles.Select(Function(i) i.Escape)), False))
                Else
                    value = value.Replace("%source_files%", Protect("""" + String.Join(""" """, proj.SourceFiles.ToArray) + """", shell))
                End If
            End If
            If value.Contains("%compressibility%") Then value = value.Replace("%compressibility%", Protect(Math.Round(proj.Compressibility, 3).ToString.Replace(",", "."), shell))
            If value.Contains("%encoder%") Then value = value.Replace("%encoder%", Protect(TryCast(proj.VideoEncoder, BasicVideoEncoder)?.CommandLineParams.Package.Name, shell))
            If value.Contains("%encoder_profile%") Then value = value.Replace("%encoder_profile%", Protect(proj.VideoEncoder.Name, shell))
            If value.Contains("%encoder_settings%") Then value = value.Replace("%encoder_settings%", Protect(TryCast(proj.VideoEncoder, BasicVideoEncoder)?.GetCommandLine(False, True).Replace("--", ""), shell))
            If value.Contains("%encoder_out_file%") Then value = value.Replace("%encoder_out_file%", Protect(proj.VideoEncoder.OutputPath, shell))
            If value.Contains("%encoder_ext%") Then value = value.Replace("%encoder_ext%", Protect(proj.VideoEncoder.OutputExt, shell))
            If value.Contains("%encoder_codec%") Then value = value.Replace("%encoder_codec%", Protect(proj.VideoEncoder.Codec, shell))
            If value.Contains("%muxer_ext%") Then value = value.Replace("%muxer_ext%", Protect(proj.VideoEncoder.Muxer.OutputExt, shell))
            If value.Contains("%script_ext%") Then value = value.Replace("%script_ext%", Protect(proj.Script.FileType, shell))
            If value.Contains("%pos_frame%") Then value = value.Replace("%pos_frame%", Protect(s.LastPosition.ToString, shell))
            If value.Contains("%template_name%") Then value = value.Replace("%template_name%", Protect(proj.TemplateName, shell))
            If value.Contains("%settings_dir%") Then value = value.Replace("%settings_dir%", Protect(Folder.Settings, shell))
            If value.Contains("%player%") Then value = value.Replace("%player%", Protect(Package.mpvnet.Path, shell))
            If value.Contains("%text_editor%") Then value = value.Replace("%text_editor%", Protect(g.GetTextEditorPath, shell))
            If value.Contains("%processing%") Then value = value.Replace("%processing%", Protect(g.IsJobProcessing.ToString, shell))
            If value.Contains("%dpi%") Then value = value.Replace("%dpi%", Protect(g.DPI.ToString(), shell))
            If value.Contains("%script_file%") Then value = value.Replace("%script_file%", Protect(proj.Script.Path, shell))
            If value.Contains("%pos_ms%") Then value = value.Replace("%pos_ms%", Protect(g.GetPreviewPosMS.ToString, shell))
            If value.Contains("%hdr10plus_path%") Then value = value.Replace("%hdr10plus_path%", Protect(p.Hdr10PlusMetadataFile, shell))
            If value.Contains("%hdrdv_path%") Then value = value.Replace("%hdrdv_path%", Protect(p.HdrDolbyVisionMetadataFile?.Path, shell))

            Dim jobs As List(Of Job)
            Dim getJobs = Function() As List(Of Job)
                              If jobs Is Nothing Then jobs = JobManager.GetJobs()
                              Return jobs
                          End Function
            If value.Contains("%jobs%") Then value = value.Replace("%jobs%", Protect(getJobs().Count.ToString(), shell))
            If value.Contains("%jobs_active%") Then value = value.Replace("%jobs_active%", Protect(getJobs().AsEnumerable().Count(Function(x) x.Active).ToString(), shell))

            Dim sourcePar As System.Drawing.Point
            Dim getSourcePar = Function() As System.Drawing.Point
                                   If sourcePar = System.Drawing.Point.Empty Then sourcePar = Calc.GetSourcePAR()
                                   Return sourcePar
                               End Function
            If value.Contains("%source_par_x%") Then value = value.Replace("%source_par_x%", Protect(getSourcePar().X.ToString, shell))
            If value.Contains("%source_par_y%") Then value = value.Replace("%source_par_y%", Protect(getSourcePar().Y.ToString, shell))

            Dim targetPar As System.Drawing.Point
            Dim getTargetPar = Function() As System.Drawing.Point
                                   If targetPar = System.Drawing.Point.Empty Then targetPar = Calc.GetTargetPAR()
                                   Return targetPar
                               End Function
            If value.Contains("%target_par_x%") Then value = value.Replace("%target_par_x%", Protect(getTargetPar().X.ToString, shell))
            If value.Contains("%target_par_y%") Then value = value.Replace("%target_par_y%", Protect(getTargetPar().Y.ToString, shell))

            If value.Contains("%source_dar%") Then value = value.Replace("%source_dar%", Protect(Calc.GetSourceDAR.ToString("f9", CultureInfo.InvariantCulture), shell))
            If value.Contains("%target_dar%") Then value = value.Replace("%target_dar%", Protect(Calc.GetTargetDAR.ToString("f9", CultureInfo.InvariantCulture), shell))

            If value.Contains("%sel_") Then
                If proj.Ranges.Count > 0 Then
                    If value.Contains("%sel_start%") Then
                        value = value.Replace("%sel_start%", Protect(proj.Ranges(0).Start.ToString, shell))
                    End If

                    If value.Contains("%sel_end%") Then
                        value = value.Replace("%sel_end%", Protect(proj.Ranges(0).End.ToString, shell))
                    End If
                Else
                    If value.Contains("%sel_start%") Then
                        value = value.Replace("%sel_start%", "0")
                    End If

                    If value.Contains("%sel_end%") Then
                        value = value.Replace("%sel_end%", "0")
                    End If
                End If
            End If

            If value.Contains("%app:") Then
                Dim mc = Regex.Matches(value, "%app:(.+?)%")

                For Each match As Match In mc
                    Dim pack = Package.Items.Values.FirstOrDefault(Function(pack2) pack2.Name.ToLowerInvariant = match.Groups(1).Value.ToLowerInvariant)
                    Dim path = pack?.Path

                    If path <> "" Then
                        value = value.Replace(match.Value, Protect(path, shell))

                        If Not value.Contains("%") Then Exit For
                    End If
                Next
            End If

            If value.Contains("%app_path:") Then
                Dim mc = Regex.Matches(value, "%app_path:(.+?)%")

                For Each match As Match In mc
                    Dim pack = Package.Items.Values.FirstOrDefault(Function(pack2) pack2.Name.ToLowerInvariant = match.Groups(1).Value.ToLowerInvariant)
                    Dim path = pack?.Path

                    If path <> "" Then
                        value = value.Replace(match.Value, Protect(path, shell))

                        If Not value.Contains("%") Then Exit For
                    End If
                Next
            End If

            If value.Contains("%app_dir:") Then
                For Each match As Match In Regex.Matches(value, "%app_dir:(.+?)%")
                    Dim pack = Package.Items.Values.FirstOrDefault(Function(pack2) pack2.Name.ToLowerInvariant = match.Groups(1).Value.ToLowerInvariant)
                    Dim path = pack?.Path

                    If path <> "" Then
                        value = value.Replace(match.Value, Protect(path.Dir, shell))

                        If Not value.Contains("%") Then Exit For
                    End If
                Next
            End If

            If value.Contains("%app_version:") Then
                For Each match As Match In Regex.Matches(value, "%app_version:(.+?)%")
                    Dim pack = Package.Items.Values.FirstOrDefault(
                        Function(pack2) pack2.Name.ToLowerInvariant = match.Groups(1).Value.ToLowerInvariant)

                    Dim version = pack?.Version

                    If version <> "" Then
                        value = value.Replace(match.Value, Protect(version, shell))

                        If Not value.Contains("%") Then Exit For
                    End If
                Next
            End If

            If value.Contains("%random:") Then
                For Each i As Match In Regex.Matches(value, "%random(?::(\d+))?%")
                    Dim digits = 6
                    If Integer.TryParse(i.Groups(1).Value, digits) Then
                        digits = If(digits < 1, 1, digits)
                        digits = If(digits > 10, 10, digits)
                    End If
                    Dim random = New Random()
                    Dim randomInt = random.Next(0, Enumerable.Repeat(10, digits).Aggregate(1, Function(a, b) a * b))
                    value = value.Replace(i.Value, Protect(randomInt.ToString().PadLeft(digits, "0"c), shell))

                    If Not value.Contains("%") Then Exit For
                Next
            End If

            If value.Contains("%filter:") Then
                Dim mc = Regex.Matches(value, "%filter:(.+?)%")

                For Each i As Match In mc
                    For Each i2 In proj.Script.Filters
                        If i2.Active AndAlso i2.Path.ToUpperInvariant = i.Groups(1).Value.ToUpperInvariant Then
                            value = value.Replace(i.Value, Protect(i2.Script, shell))
                            Exit For
                        End If
                    Next

                    value = value.Replace(i.Value, "")

                    If Not value.Contains("%") Then Exit For
                Next
            End If

            If value.Contains("%isfilteractive:") Then
                Dim mc = Regex.Matches(value, "%isfilteractive:(.+?)%")

                For Each i As Match In mc
                    For Each i2 In proj.Script.Filters
                        If i2.Active AndAlso i2.Path.ToUpperInvariant = i.Groups(1).Value.ToUpperInvariant Then
                            value = value.Replace(i.Value, "1")
                            Exit For
                        End If
                    Next

                    value = value.Replace(i.Value, "0")

                    If Not value.Contains("%") Then Exit For
                Next
            End If

            If value.Contains("%eval:") Then
                If Not value.Contains("%eval:<expression>%") AndAlso Not value.Contains("%eval:expression%") Then
                    matches = Regex.Matches(value, "%eval:(.+?)%")

                    For Each ma As Match In matches
                        If IsUntrustedEval(Unprotect(ma.Value), proj) Then Continue For

                        Try
                            value = value.Replace(ma.Value, Protect(PowerShell.InvokeAndConvert(Unprotect(ma.Groups(1).Value)), shell))
                        Catch ex As Exception
                            value = value.Replace(ma.Value, Protect(ex.ToString, shell))
                        End Try

                        If Not value.Contains("%") Then Exit For
                    Next
                End If
            End If

            If value.Contains("%") Then
                For Each var In OS.EnvVars
                    If value = "" OrElse var = "" Then Continue For

                    If value.ToLowerInvariant.Contains("%" + var.ToLowerInvariant + "%") Then
                        value = Environment.ExpandEnvironmentVariables(value)
                    End If

                    If Not value.Contains("%") Then Exit For
                Next
            End If
        End If

        'nested functions of any type are expanded from the inside out
        For pass = 1 To 100
            Dim before = value

            If value.Contains("<lower(") Then
                Const pattern = "<lower\(((?:(?!<\w+?\().)+?)\)>"

                While Regex.IsMatch(value, pattern)
                    matches = Regex.Matches(value, pattern)

                    For i = matches.Count - 1 To 0 Step -1
                        Dim match = matches(i)
                        Dim replaced = match.Groups(1).Value.ToLower()
                        value = value.Replace(match.Value, replaced)
                    Next
                End While
            End If

            If value.Contains("<title(") Then
                Const pattern = "<title\(((?:(?!<\w+?\().)+?)\)>"

                While Regex.IsMatch(value, pattern)
                    matches = Regex.Matches(value, pattern)

                    For i = matches.Count - 1 To 0 Step -1
                        Dim match = matches(i)
                        Dim replaced = match.Groups(1).Value.ToTitleCase()
                        value = value.Replace(match.Value, replaced)
                    Next
                End While
            End If

            If value.Contains("<upper(") Then
                Const pattern = "<upper\(((?:(?!<\w+?\().)+?)\)>"

                While Regex.IsMatch(value, pattern)
                    matches = Regex.Matches(value, pattern)

                    For i = matches.Count - 1 To 0 Step -1
                        Dim match = matches(i)
                        Dim replaced = match.Groups(1).Value.ToUpper()
                        value = value.Replace(match.Value, replaced)
                    Next
                End While
            End If

            If value.Contains("<trim(") Then
                Const pattern = "<trim\(((?:(?!<\w+?\().)+?)\)>"

                While Regex.IsMatch(value, pattern)
                    matches = Regex.Matches(value, pattern)

                    For i = matches.Count - 1 To 0 Step -1
                        Dim match = matches(i)
                        Dim replaced = match.Groups(1).Value.Trim()
                        value = value.Replace(match.Value, replaced)
                    Next
                End While
            End If

            If value.Contains("<if(") Then
                Const pattern = "<if\(((?:(?!<\w+?\().)+?);((?:(?!<\w+?\().)+?);((?:(?!<\w+?\().)+?)\)>"

                While Regex.IsMatch(value, pattern)
                    matches = Regex.Matches(value, pattern)

                    For i = matches.Count - 1 To 0 Step -1
                        Dim match = matches(i)
                        Dim condition = match.Groups(1).Value.Trim()
                        Dim trueValue = New String(match.Groups(2).Value.SkipWhile(Function(x, y) y = 0 AndAlso Char.IsWhiteSpace(x)).ToArray())
                        Dim falseValue = New String(match.Groups(3).Value.SkipWhile(Function(x, y) y = 0 AndAlso Char.IsWhiteSpace(x)).ToArray())
                        Dim newValue = "INVALID"

                        Try
                            Dim result = Convert.ToBoolean(New System.Data.DataTable().Compute(condition, Nothing))
                            newValue = If(result, trueValue, falseValue)
                        Catch ex As Exception
                            newValue = $"##{ex.Message}##"
                        End Try

                        newValue = newValue.Replace("''", "")
                        value = value.Substring(0, match.Index) & newValue & value.Substring(match.Index + match.Length)
                    Next
                End While
            End If

            If value.Contains("<replace(") Then
                Const pattern = "<replace\(((?:(?!<\w+?\().)+?);((?:(?!<\w+?\().)+?);((?:(?!<\w+?\().)+?)\)>"

                While Regex.IsMatch(value, pattern)
                    matches = Regex.Matches(value, pattern)

                    For i = matches.Count - 1 To 0 Step -1
                        Dim match = matches(i)
                        Dim input = match.Groups(1).Value.Trim()
                        Dim oldValue = New String(match.Groups(2).Value.SkipWhile(Function(x, y) y = 0 AndAlso Char.IsWhiteSpace(x)).ToArray())
                        Dim newValue = New String(match.Groups(3).Value.SkipWhile(Function(x, y) y = 0 AndAlso Char.IsWhiteSpace(x)).ToArray())
                        Dim output = input

                        If Not String.IsNullOrEmpty(input) AndAlso Not String.IsNullOrEmpty(oldValue) Then
                            output = input.Replace(oldValue, newValue)
                        End If

                        'the match is always replaced, otherwise the loop doesn't end
                        value = value.Replace(match.Value, output)
                    Next
                End While
            End If

            If value = before Then Exit For
        Next

        Return value
    End Function

    Shared Function ExpandWhileProcessing(value As String, proj As Project, commandline As String, progress As Single, progressline As String) As String
        If value = "" Then Return ""
        If Not value.Contains("%") Then Return value
        If proj Is Nothing Then proj = p

        If value.Contains("%commandline%") Then value = value.Replace("%commandline%", Protect(commandline.Trim().Escape(), False))
        If value.Contains("%progress%") Then value = value.Replace("%progress%", progress.ToInvariantString("0.0"))
        If value.Contains("%progressline%") Then value = value.Replace("%progressline%", Protect(progressline.Trim(), False))

        value = Expand(value, proj)
        Return value
    End Function

    Shared Function ExpandParamValues(value As String, params As List(Of CommandLineParam), Optional proj As Project = Nothing) As String
        If value = "" Then Return ""
        If params Is Nothing Then Return value
        If Not params.Any() Then Return value
        If proj Is Nothing Then proj = p

        If value.ContainsAny("%") Then
            For Each param In params.Where(Function(x) x.Path <> "")
                Dim switches = param.GetSwitches().Select(Function(x) $"{x.ToLowerEx()}").ToArray()

                For Each sw In switches
                    If value.Contains(sw) Then
                        If TypeOf param Is BoolParam Then
                            Dim castedParam = DirectCast(param, BoolParam)
                            Dim v = ""
                            If castedParam.ArgsFunc Is Nothing Then
                                v = castedParam.Value.ToInvariantString()
                            Else
                                v = castedParam.ArgsFunc.Invoke()
                                If v = "" Then
                                    v = castedParam.Value.ToString()
                                End If
                                v = v.ReplaceAll(switches, "").Replace(" ", "")
                            End If
                            If value.Contains($"%{sw}%") Then value = value.Replace($"%{sw}%", v)
                            If value.Contains($"%{sw}_D%") Then value = value.Replace($"%{sw}_D%", castedParam.IsDefaultValue.ToInvariantString())
                            If value.Contains($"%{sw}_L%") Then value = value.Replace($"%{sw}_L%", v.ToLowerInvariant())
                            If value.Contains($"%{sw}_T%") Then value = value.Replace($"%{sw}_T%", v.ToTitleCase())
                            If value.Contains($"%{sw}_U%") Then value = value.Replace($"%{sw}_U%", v.ToUpperInvariant())
                            If value.Contains($"%{sw}_V%") Then value = value.Replace($"%{sw}_V%", If(castedParam.Value, "1", "0"))
                            If value.Contains($"%{sw}_Z%") Then value = value.Replace($"%{sw}_Z%", If(castedParam.Visible, "1", "0"))
                        ElseIf TypeOf param Is NumParam Then
                            Dim castedParam = DirectCast(param, NumParam)
                            Dim v = ""
                            If castedParam.ArgsFunc Is Nothing Then
                                v = castedParam.Value.ToInvariantString()
                            Else
                                v = castedParam.ArgsFunc.Invoke()
                                If v = "" Then
                                    v = castedParam.Value.ToString()
                                End If
                                v = v.ReplaceAll(switches, "").Replace(" ", "")
                            End If
                            If value.Contains($"%{sw}%") Then value = value.Replace($"%{sw}%", v)
                            If value.Contains($"%{sw}_D%") Then value = value.Replace($"%{sw}_D%", castedParam.IsDefaultValue.ToInvariantString())
                            If value.Contains($"%{sw}_L%") Then value = value.Replace($"%{sw}_L%", v.ToLowerInvariant())
                            If value.Contains($"%{sw}_T%") Then value = value.Replace($"%{sw}_T%", v.ToTitleCase())
                            If value.Contains($"%{sw}_U%") Then value = value.Replace($"%{sw}_U%", v.ToUpperInvariant())
                            If value.Contains($"%{sw}_Z%") Then value = value.Replace($"%{sw}_Z%", If(castedParam.Visible, "1", "0"))
                        ElseIf TypeOf param Is OptionParam Then
                            Dim castedParam = DirectCast(param, OptionParam)
                            Dim v = ""
                            If castedParam.ArgsFunc Is Nothing Then
                                If castedParam.Values.NothingOrEmpty() Then
                                    If castedParam.IntegerValue Then
                                        v = castedParam.Value.ToInvariantString()
                                    Else
                                        v = castedParam.OptionText.Replace(" ", "")
                                    End If
                                Else
                                    v = castedParam.ValueText
                                End If
                            Else
                                v = castedParam.ArgsFunc.Invoke()
                                If v = "" Then
                                    v = castedParam.Value.ToString()
                                End If
                                v = v.ReplaceAll(switches, "").Replace(" ", "")
                            End If
                            If value.Contains($"%{sw}%") Then value = value.Replace($"%{sw}%", v.ToInvariantString())
                            If value.Contains($"%{sw}_D%") Then value = value.Replace($"%{sw}_D%", castedParam.IsDefaultValue.ToInvariantString())
                            If value.Contains($"%{sw}_L%") Then value = value.Replace($"%{sw}_L%", v.ToInvariantString().ToLowerInvariant())
                            If value.Contains($"%{sw}_T%") Then value = value.Replace($"%{sw}_T%", v.ToInvariantString().ToTitleCase())
                            If value.Contains($"%{sw}_U%") Then value = value.Replace($"%{sw}_U%", v.ToInvariantString().ToUpperInvariant())
                            If value.Contains($"%{sw}_V%") Then value = value.Replace($"%{sw}_V%", castedParam.Value.ToInvariantString())
                            If value.Contains($"%{sw}_Z%") Then value = value.Replace($"%{sw}_Z%", If(castedParam.Visible, "1", "0"))
                        ElseIf TypeOf param Is StringParam Then
                            Dim castedParam = DirectCast(param, StringParam)
                            Dim v = ""
                            If castedParam.ArgsFunc Is Nothing Then
                                v = castedParam.Value.ToInvariantString()
                            Else
                                v = castedParam.ArgsFunc.Invoke()
                                If v = "" Then
                                    v = castedParam.Value.ToInvariantString()
                                End If
                                v = v.ReplaceAll(switches, "").Replace(" ", "")
                            End If
                            If value.Contains($"%{sw}%") Then value = value.Replace($"%{sw}%", v)
                            If value.Contains($"%{sw}_D%") Then value = value.Replace($"%{sw}_D%", castedParam.IsDefaultValue.ToInvariantString())
                            If value.Contains($"%{sw}_L%") Then value = value.Replace($"%{sw}_L%", v.ToLowerInvariant())
                            If value.Contains($"%{sw}_T%") Then value = value.Replace($"%{sw}_T%", v.ToTitleCase())
                            If value.Contains($"%{sw}_U%") Then value = value.Replace($"%{sw}_U%", v.ToUpperInvariant())
                            If value.Contains($"%{sw}_Z%") Then value = value.Replace($"%{sw}_Z%", If(castedParam.Visible, "1", "0"))
                        End If
                        Exit For
                    End If
                Next

                If Not value.ContainsAny("%") Then Exit For
            Next
        End If

        value = Expand(value, proj)
        value = value.ReplaceInvalidFileSystemName("-"c)
        Return value
    End Function
End Class
