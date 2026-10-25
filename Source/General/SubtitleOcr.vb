Imports System.IO.Compression
Imports System.Text
Imports System.Text.RegularExpressions

Imports StaxRip.UI
Imports VB6 = Microsoft.VisualBasic

'Converts bitmap subtitles (PGS, VobSub) to SRT with the OCR engine Tesseract
Public Class SubtitleOcr
    Const DownloadURL = "https://github.com/tesseract-ocr/tessdata_fast/raw/main/"

    Shared ReadOnly Property Languages As New Dictionary(Of String, String) From {
        {"afr", "Afrikaans"}, {"amh", "Amharic"}, {"ara", "Arabic"}, {"asm", "Assamese"},
        {"aze", "Azerbaijani"}, {"aze_cyrl", "Azerbaijani (Cyrillic)"}, {"bel", "Belarusian"},
        {"ben", "Bengali"}, {"bod", "Tibetan"}, {"bos", "Bosnian"}, {"bre", "Breton"},
        {"bul", "Bulgarian"}, {"cat", "Catalan"}, {"ceb", "Cebuano"}, {"ces", "Czech"},
        {"chi_sim", "Chinese (Simplified)"}, {"chi_sim_vert", "Chinese (Simplified, vertical)"},
        {"chi_tra", "Chinese (Traditional)"}, {"chi_tra_vert", "Chinese (Traditional, vertical)"},
        {"chr", "Cherokee"}, {"cos", "Corsican"}, {"cym", "Welsh"}, {"dan", "Danish"},
        {"deu", "German"}, {"deu_latf", "German (Fraktur)"}, {"div", "Divehi"}, {"dzo", "Dzongkha"},
        {"ell", "Greek"}, {"eng", "English"}, {"enm", "English (Middle)"}, {"epo", "Esperanto"},
        {"est", "Estonian"}, {"eus", "Basque"}, {"fao", "Faroese"}, {"fas", "Persian"},
        {"fil", "Filipino"}, {"fin", "Finnish"}, {"fra", "French"}, {"frk", "Frankish (German Fraktur)"},
        {"frm", "French (Middle)"}, {"fry", "Western Frisian"}, {"gla", "Scottish Gaelic"},
        {"gle", "Irish"}, {"glg", "Galician"}, {"grc", "Greek (Ancient)"}, {"guj", "Gujarati"},
        {"hat", "Haitian Creole"}, {"heb", "Hebrew"}, {"hin", "Hindi"}, {"hrv", "Croatian"},
        {"hun", "Hungarian"}, {"hye", "Armenian"}, {"iku", "Inuktitut"}, {"ind", "Indonesian"},
        {"isl", "Icelandic"}, {"ita", "Italian"}, {"ita_old", "Italian (Old)"}, {"jav", "Javanese"},
        {"jpn", "Japanese"}, {"jpn_vert", "Japanese (vertical)"}, {"kan", "Kannada"},
        {"kat", "Georgian"}, {"kat_old", "Georgian (Old)"}, {"kaz", "Kazakh"}, {"khm", "Khmer"},
        {"kir", "Kyrgyz"}, {"kmr", "Kurdish (Kurmanji)"}, {"kor", "Korean"},
        {"kor_vert", "Korean (vertical)"}, {"lao", "Lao"}, {"lat", "Latin"}, {"lav", "Latvian"},
        {"lit", "Lithuanian"}, {"ltz", "Luxembourgish"}, {"mal", "Malayalam"}, {"mar", "Marathi"},
        {"mkd", "Macedonian"}, {"mlt", "Maltese"}, {"mon", "Mongolian"}, {"mri", "Maori"},
        {"msa", "Malay"}, {"mya", "Burmese"}, {"nep", "Nepali"}, {"nld", "Dutch"},
        {"nor", "Norwegian"}, {"oci", "Occitan"}, {"ori", "Odia"}, {"pan", "Punjabi"},
        {"pol", "Polish"}, {"por", "Portuguese"}, {"pus", "Pashto"}, {"que", "Quechua"},
        {"ron", "Romanian"}, {"rus", "Russian"}, {"san", "Sanskrit"}, {"sin", "Sinhala"},
        {"slk", "Slovak"}, {"slv", "Slovenian"}, {"snd", "Sindhi"}, {"spa", "Spanish"},
        {"spa_old", "Spanish (Old)"}, {"sqi", "Albanian"}, {"srp", "Serbian"},
        {"srp_latn", "Serbian (Latin)"}, {"sun", "Sundanese"}, {"swa", "Swahili"}, {"swe", "Swedish"},
        {"syr", "Syriac"}, {"tam", "Tamil"}, {"tat", "Tatar"}, {"tel", "Telugu"}, {"tgk", "Tajik"},
        {"tha", "Thai"}, {"tir", "Tigrinya"}, {"ton", "Tongan"}, {"tur", "Turkish"},
        {"uig", "Uyghur"}, {"ukr", "Ukrainian"}, {"urd", "Urdu"}, {"uzb", "Uzbek"},
        {"uzb_cyrl", "Uzbek (Cyrillic)"}, {"vie", "Vietnamese"}, {"yid", "Yiddish"}, {"yor", "Yoruba"}}

    'languages downloaded by the user, Tesseract finds the languages of the distribution itself
    Shared ReadOnly Property DataDir As String
        Get
            Return Path.Combine(Folder.Settings, "tessdata")
        End Get
    End Property

    Private Shared SystemLanguages As String()

    Shared Function GetSystemLanguages() As String()
        If SystemLanguages Is Nothing Then
            If Package.Tesseract.Path = "" Then
                Return {}
            End If

            'the first line names the folder, osd is used for the page orientation
            Dim output = ProcessHelp.GetConsoleOutput(Package.Tesseract.Path, {"--list-langs"})
            SystemLanguages = output.SplitLinesNoEmpty.Skip(1).Select(Function(i) i.Trim).Where(Function(i) i <> "osd").ToArray
        End If

        Return SystemLanguages
    End Function

    Shared Function GetDownloadPath(language As String) As String
        Return Path.Combine(DataDir, language + ".traineddata")
    End Function

    Shared Function IsInstalled(language As String) As Boolean
        Return File.Exists(GetDownloadPath(language)) OrElse GetSystemLanguages.Contains(language)
    End Function

    Shared Function GetInstalledLanguages() As List(Of String)
        Dim downloaded = If(Directory.Exists(DataDir), Directory.GetFiles(DataDir, "*.traineddata").Select(Function(i) i.Base), Enumerable.Empty(Of String))
        Return downloaded.Union(GetSystemLanguages).OrderBy(Function(i) GetLanguageName(i)).ToList
    End Function

    Shared Function GetLanguageName(language As String) As String
        Return If(Languages.ContainsKey(language), Languages(language) + " (" + language + ")", language)
    End Function

    'the Tesseract language of a subtitle language, Nothing if Tesseract doesn't support it
    Shared Function GetLanguage(lang As Language) As String
        If lang Is Nothing OrElse Not lang.IsDetermined Then
            Return Nothing
        End If

        Dim code = lang.CultureInfo.ThreeLetterISOLanguageName
        Dim name = lang.CultureInfo.Name

        Select Case code
            Case "zho"
                code = If(name.ContainsAny("Hant", "TW", "HK", "MO"), "chi_tra", "chi_sim")
            Case "nob", "nno"
                code = "nor"
            Case "srp"
                code = If(name.Contains("Latn"), "srp_latn", code)
            Case "aze", "uzb"
                code = If(name.Contains("Cyrl"), code + "_cyrl", code)
            Case "kur"
                code = "kmr"
            Case "tgl"
                code = "fil"
        End Select

        Return If(Languages.ContainsKey(code), code, Nothing)
    End Function

    'subtitles without a supported language use English or another installed language
    Shared Function GetDefaultLanguage(lang As Language) As String
        Dim ret = GetLanguage(lang)

        If ret Is Nothing Then
            Dim installed = GetInstalledLanguages()
            ret = If(installed.Contains("eng") OrElse installed.Count = 0, "eng", installed(0))
        End If

        Return ret
    End Function

    'downloads the language data, returns False if it was canceled or failed
    Shared Function Download(language As String) As Boolean
        Directory.CreateDirectory(DataDir)
        Dim target = GetDownloadPath(language)
        Dim partPath = target + ".part"

        Using form As New DownloadForm(DownloadURL + language + ".traineddata", partPath)
            If form.ShowDialog() = DialogResult.OK AndAlso File.Exists(partPath) Then
                FileHelp.Move(partPath, target)
                Return File.Exists(target)
            End If
        End Using

        FileHelp.Delete(partPath)
        Return False
    End Function

    Shared Sub ShowLanguages()
        Using form As New OcrLanguagesForm
            If form.ShowDialog() = DialogResult.OK Then
                For Each i In form.SelectedLanguages
                    If Not Download(i) Then
                        Exit For
                    End If
                Next
            End If
        End Using
    End Sub

    'asks for the language of a subtitle, languages that aren't installed are downloaded,
    'returns Nothing if it was canceled
    Shared Function SelectLanguage(lang As Language, fileName As String) As String
        Dim installed = GetInstalledLanguages()
        Dim language = GetDefaultLanguage(lang)

        Using td As New TaskDialog(Of DialogResult)
            td.Title = "OCR Language"
            td.Content = $"Language of the text in {fileName}." + BR2 +
                "Languages that aren't installed are downloaded once from the tessdata_fast repository of Tesseract on GitHub."
            td.Buttons = TaskButton.OkCancel

            For Each i In installed
                td.MenuItems.Add(New ListBag(Of String)(GetLanguageName(i), i))
            Next

            For Each i In Languages.Keys.Except(installed).OrderBy(Function(x) Languages(x))
                td.MenuItems.Add(New ListBag(Of String)(GetLanguageName(i) + " - download", i))
            Next

            td.MenuValue = td.MenuItems.OfType(Of ListBag(Of String)).FirstOrDefault(Function(i) i.Value = language)

            If td.Show() <> DialogResult.OK Then
                Return Nothing
            End If

            language = DirectCast(td.MenuValue, ListBag(Of String)).Value
        End Using

        Return If(IsInstalled(language) OrElse Download(language), language, Nothing)
    End Function

    Shared Function IsBitmap(subtitle As Subtitle) As Boolean
        Return subtitle.Path.Ext.EqualsAny("sup", "idx") AndAlso File.Exists(subtitle.Path)
    End Function

    'idx files can contain multiple subtitles, their SRT files are named after the stream and language
    Shared Function GetTextPath(subtitle As Subtitle) As String
        Dim dir = If(p.TempDir <> "" AndAlso Directory.Exists(p.TempDir), p.TempDir, subtitle.Path.Dir)
        Dim name = subtitle.Path.Base

        If subtitle.Path.Ext = "idx" AndAlso Regex.Matches(subtitle.Path.ReadAllText, "^id: ", RegexOptions.Multiline).Count > 1 Then
            name += $"_{subtitle.IndexIDX}_[{subtitle.Language.Name}]"
        End If

        Return Path.Combine(dir, name + ".srt")
    End Function

    'converts the subtitles that aren't converted yet, missing languages can be downloaded unless jobs are processed,
    'subtitles that can't be read are skipped, Tesseract errors abort opening the source
    Shared Sub Convert(subtitles As IEnumerable(Of Subtitle))
        Dim pending = subtitles.Where(Function(i) Not File.Exists(GetTextPath(i))).ToList

        If pending.Count = 0 Then
            Exit Sub
        End If

        If Package.Tesseract.Path = "" Then
            Log.Write("Subtitle OCR", "Skipped, Tesseract is not installed." + BR2)
            Exit Sub
        End If

        Dim missing = pending.Select(Function(i) GetDefaultLanguage(i.Language)).Distinct.Where(Function(i) Not IsInstalled(i)).ToList

        If missing.Count > 0 AndAlso Not g.IsJobProcessing Then
            Using td As New TaskDialog(Of String)
                td.Title = "Download OCR languages?"
                td.Content = "The OCR of the bitmap subtitles requires languages that aren't installed: " +
                    missing.Select(AddressOf GetLanguageName).Join(", ") + BR2 +
                    "They can be downloaded once from the tessdata_fast repository of Tesseract on GitHub. " +
                    "Subtitles whose language isn't installed aren't converted."
                td.AddButton("Download")
                td.AddButton("Skip")

                If td.Show() = "Download" Then
                    For Each i In missing
                        If Not Download(i) Then
                            Exit For
                        End If
                    Next
                End If
            End Using
        End If

        For Each subtitle In pending
            Dim language = GetDefaultLanguage(subtitle.Language)

            If IsInstalled(language) Then
                Try
                    Convert(subtitle, language, GetTextPath(subtitle))
                Catch ex As ErrorAbortException
                    g.ShowException(ex, Nothing, Nothing, If(g.IsJobProcessing, s.ErrorMessageTimeout, 0))
                    Throw New AbortException
                Catch ex As Exception When TypeOf ex IsNot AbortException
                    Log.Write("Subtitle OCR", $"Skipped {subtitle.Path.FileName}: {ex.Message}" + BR2)
                End Try
            Else
                Log.Write("Subtitle OCR", $"Skipped {subtitle.Path.FileName}, the language {GetLanguageName(language)} isn't installed." + BR2)
            End If
        Next
    End Sub

    'returns the number of written subtitles, no file is written if no text was recognized
    Shared Function Convert(subtitle As Subtitle, language As String, outputPath As String) As Integer
        Dim workDir = Path.Combine(Folder.Temp, "StaxRip " + Guid.NewGuid.ToString)
        Directory.CreateDirectory(workDir)

        Try
            Dim times As New List(Of (Start As TimeSpan, [End] As TimeSpan))
            Dim listPath = Path.Combine(workDir, "images.txt")

            Using writer As New StreamWriter(listPath)
                Dim bitmaps = If(subtitle.Path.Ext = "idx", VobSubReader.Read(subtitle.Path, subtitle.IndexIDX), PgsReader.Read(subtitle.Path))

                For Each bitmap In bitmaps
                    Dim image = GetImage(bitmap)

                    If image.Pixels IsNot Nothing Then
                        Dim imagePath = Path.Combine(workDir, times.Count & ".png")
                        WritePng(imagePath, image.Width, image.Height, image.Pixels)
                        writer.WriteLine(imagePath)
                        times.Add((bitmap.Start, bitmap.End))
                    End If
                Next
            End Using

            If times.Count = 0 Then
                Return 0
            End If

            Dim texts = Recognize(listPath, times.Count, language, subtitle.Path.FileName)
            Return WriteSrt(outputPath, times, texts)
        Finally
            FolderHelp.Delete(workDir)
        End Try
    End Function

    Shared Function Recognize(listPath As String, count As Integer, language As String, fileName As String) As String()
        'Tesseract appends .txt, which must not overwrite the list
        Dim outputBase = Path.Combine(listPath.Dir, "text")

        Using proc As New Proc
            proc.Header = "Subtitle OCR"
            proc.Package = Package.Tesseract
            proc.WriteLog(fileName + ", " + GetLanguageName(language) + BR2)
            proc.Arguments = listPath.Escape + " " + outputBase.Escape + " -l " + language + " --psm 6"

            If File.Exists(GetDownloadPath(language)) Then
                proc.Arguments += " --tessdata-dir " + DataDir.Escape
            End If

            'multiple threads use much more CPU time for little gain
            proc.Process.StartInfo.Environment("OMP_THREAD_LIMIT") = "1"

            proc.ProgressFunc = Function(value)
                                    Dim match = Regex.Match(value, "^Page (\d+) : ")
                                    Return If(match.Success, $"Progress: {(match.Groups(1).Value.ToInt + 1) * 100 \ count}%", Nothing)
                                End Function

            proc.Start()
        End Using

        'the text of the images is separated by form feeds
        Dim pages = File.ReadAllText(outputBase + ".txt").Split(VB6.ChrW(12))

        If pages.Length <> count Then
            Throw New ErrorAbortException("Subtitle OCR failed", $"Tesseract recognized {pages.Length} instead of {count} images.")
        End If

        Return pages.Select(AddressOf CleanText).ToArray
    End Function

    Shared Function CleanText(text As String) As String
        Dim lines = text.Replace("|", "I").Split({VB6.ChrW(10), VB6.ChrW(13)}, StringSplitOptions.RemoveEmptyEntries).
            Select(Function(i) Regex.Replace(i.Trim, " {2,}", " ")).Where(Function(i) i <> "")

        Return String.Join(BR, lines)
    End Function

    Shared Function WriteSrt(path As String, times As List(Of (Start As TimeSpan, [End] As TimeSpan)), texts As String()) As Integer
        Dim entries = times.Select(Function(t, i) (t.Start, t.End, Text:=texts(i))).Where(Function(i) i.Text <> "").OrderBy(Function(i) i.Start).ToList
        Dim merged As New List(Of (Start As TimeSpan, [End] As TimeSpan, Text As String))

        For Each entry In entries
            'fades and repeated images result in the same text in a row
            If merged.Count > 0 AndAlso merged.Last.Text = entry.Text AndAlso entry.Start <= merged.Last.End + TimeSpan.FromMilliseconds(100) Then
                Dim last = merged.Last
                merged(merged.Count - 1) = (last.Start, If(entry.End > last.End, entry.End, last.End), last.Text)
            Else
                merged.Add(entry)
            End If
        Next

        If merged.Count = 0 Then
            Return 0
        End If

        Dim sb As New StringBuilder

        For i = 0 To merged.Count - 1
            sb.Append(i + 1).Append(BR)
            sb.Append(FormatTime(merged(i).Start)).Append(" --> ").Append(FormatTime(merged(i).End)).Append(BR)
            sb.Append(merged(i).Text).Append(BR2)
        Next

        sb.ToString.WriteFileUTF8(path)
        Return merged.Count
    End Function

    Shared Function FormatTime(value As TimeSpan) As String
        If value < TimeSpan.Zero Then
            value = TimeSpan.Zero
        End If

        Return $"{CInt(Math.Floor(value.TotalHours)):00}:{value.Minutes:00}:{value.Seconds:00},{value.Milliseconds:000}"
    End Function

    'Tesseract expects dark text on a light background with a margin,
    'long gaps between lines are shortened, Nothing if there is no text
    Shared Function GetImage(bitmap As SubtitleBitmap) As (Width As Integer, Height As Integer, Pixels As Byte())
        Const threshold = 40, margin = 20, maxGap = 30
        Dim w = bitmap.Width
        Dim left = w
        Dim right = -1
        Dim maxInk = 0
        Dim inkRows As New List(Of Boolean)

        For y = 0 To bitmap.Height - 1
            Dim hasInk = False

            For x = 0 To w - 1
                Dim value As Integer = bitmap.Ink(y * w + x)

                If value > maxInk Then
                    maxInk = value
                End If

                If value > threshold Then
                    hasInk = True
                    left = Math.Min(left, x)
                    right = Math.Max(right, x)
                End If
            Next

            inkRows.Add(hasInk)
        Next

        If right < 0 Then
            Return Nothing
        End If

        Dim rows As New List(Of Integer)
        Dim gap = 0

        For y = inkRows.IndexOf(True) To inkRows.LastIndexOf(True)
            gap = If(inkRows(y), 0, gap + 1)

            If gap <= maxGap Then
                rows.Add(y)
            End If
        Next

        Dim width = right - left + 1 + 2 * margin
        Dim height = rows.Count + 2 * margin
        Dim pixels(width * height - 1) As Byte
        Array.Fill(pixels, CByte(255))

        For i = 0 To rows.Count - 1
            For x = left To right
                Dim value = bitmap.Ink(rows(i) * w + x) * 255 \ maxInk
                pixels((i + margin) * width + x - left + margin) = CByte(255 - value)
            Next
        Next

        Return (width, height, pixels)
    End Function

    Private Shared ReadOnly CrcTable As UInteger() = Enumerable.Range(0, 256).Select(
        Function(n)
            Dim c = CUInt(n)

            For k = 0 To 7
                c = If((c And 1UI) = 1UI, &HEDB88320UI Xor (c >> 1), c >> 1)
            Next

            Return c
        End Function).ToArray

    'writes an 8-bit grayscale PNG file
    Shared Sub WritePng(path As String, width As Integer, height As Integer, pixels As Byte())
        Using stream = File.Create(path)
            Dim signature = New Byte() {&H89, &H50, &H4E, &H47, &HD, &HA, &H1A, &HA}
            stream.Write(signature, 0, signature.Length)

            Dim header(12) As Byte
            WriteUInt32(header, 0, CUInt(width))
            WriteUInt32(header, 4, CUInt(height))
            header(8) = 8
            WriteChunk(stream, "IHDR", header)

            Using data As New MemoryStream
                Using zlib As New ZLibStream(data, CompressionLevel.Fastest, True)
                    For y = 0 To height - 1
                        zlib.WriteByte(0)
                        zlib.Write(pixels, y * width, width)
                    Next
                End Using

                WriteChunk(stream, "IDAT", data.ToArray)
            End Using

            WriteChunk(stream, "IEND", Array.Empty(Of Byte))
        End Using
    End Sub

    Shared Sub WriteChunk(stream As Stream, type As String, data As Byte())
        Dim chunk(data.Length + 11) As Byte
        WriteUInt32(chunk, 0, CUInt(data.Length))
        Encoding.ASCII.GetBytes(type, 0, 4, chunk, 4)
        Array.Copy(data, 0, chunk, 8, data.Length)

        Dim crc = &HFFFFFFFFUI

        For i = 4 To chunk.Length - 5
            crc = CrcTable(CInt((crc Xor chunk(i)) And &HFFUI)) Xor (crc >> 8)
        Next

        WriteUInt32(chunk, chunk.Length - 4, crc Xor &HFFFFFFFFUI)
        stream.Write(chunk, 0, chunk.Length)
    End Sub

    Shared Sub WriteUInt32(buffer As Byte(), offset As Integer, value As UInteger)
        buffer(offset) = CByte(value >> 24)
        buffer(offset + 1) = CByte((value >> 16) And &HFFUI)
        buffer(offset + 2) = CByte((value >> 8) And &HFFUI)
        buffer(offset + 3) = CByte(value And &HFFUI)
    End Sub

    Shared Function ReadUInt16(buffer As Byte(), offset As Integer) As Integer
        Return (CInt(buffer(offset)) << 8) Or buffer(offset + 1)
    End Function

    'returns False if the stream ends before
    Shared Function ReadBytes(stream As Stream, buffer As Byte(), offset As Integer, count As Integer) As Boolean
        While count > 0
            Dim read = stream.Read(buffer, offset, count)

            If read = 0 Then
                Return False
            End If

            offset += read
            count -= read
        End While

        Return True
    End Function
End Class

'a subtitle as brightness of its pixels on black, usually bright text with a dark outline
Public Class SubtitleBitmap
    Property Start As TimeSpan
    Property [End] As TimeSpan
    Property Width As Integer
    Property Height As Integer
    Property Ink As Byte()
End Class

'reads the subtitles of Blu-ray discs (.sup)
Public Class PgsReader
    Class PgsObject
        Property Width As Integer
        Property Height As Integer
        Property Data As New MemoryStream
    End Class

    Class CompositionObject
        Property ID As Integer
        Property X As Integer
        Property Y As Integer
        Property IsCropped As Boolean
        Property CropX As Integer
        Property CropY As Integer
        Property CropWidth As Integer
        Property CropHeight As Integer
    End Class

    'a subtitle is shown until the next composition
    Shared Iterator Function Read(path As String) As IEnumerable(Of SubtitleBitmap)
        Dim palettes As New Dictionary(Of Integer, Byte())
        Dim objects As New Dictionary(Of Integer, PgsObject)
        Dim composition As New List(Of CompositionObject)
        Dim compositionTime As TimeSpan
        Dim paletteID As Integer
        Dim shown As SubtitleBitmap = Nothing

        Using stream = File.OpenRead(path)
            Dim header(12) As Byte

            While SubtitleOcr.ReadBytes(stream, header, 0, header.Length)
                If header(0) <> &H50 OrElse header(1) <> &H47 Then
                    Throw New InvalidDataException($"Invalid PGS segment at position {stream.Position - header.Length} of {path}")
                End If

                Dim data(SubtitleOcr.ReadUInt16(header, 11) - 1) As Byte

                If Not SubtitleOcr.ReadBytes(stream, data, 0, data.Length) Then
                    Exit While
                End If

                Select Case header(10)
                    Case &H16 'presentation composition
                        If data.Length < 11 Then
                            Continue While
                        End If

                        Dim pts = (CLng(header(2)) << 24) Or (CLng(header(3)) << 16) Or (CLng(header(4)) << 8) Or header(5)
                        compositionTime = TimeSpan.FromMilliseconds(pts / 90)
                        paletteID = data(9)
                        composition = ReadComposition(data)

                        'epoch start
                        If (data(7) And &H80) <> 0 Then
                            objects.Clear()
                            palettes.Clear()
                        End If
                    Case &H14 'palette definition
                        If data.Length >= 2 Then
                            ReadPalette(data, palettes)
                        End If
                    Case &H15 'object definition
                        If data.Length >= 4 Then
                            ReadObject(data, objects)
                        End If
                    Case &H80 'end of display set
                        Dim bitmap = Render(composition, objects, If(palettes.ContainsKey(paletteID), palettes(paletteID), Nothing))

                        If shown IsNot Nothing AndAlso bitmap IsNot Nothing AndAlso shown.Width = bitmap.Width AndAlso
                            shown.Height = bitmap.Height AndAlso shown.Ink.SequenceEqual(bitmap.Ink) Then

                            Continue While
                        End If

                        If shown IsNot Nothing Then
                            shown.End = compositionTime
                            Yield shown
                        End If

                        shown = bitmap

                        If shown IsNot Nothing Then
                            shown.Start = compositionTime
                        End If
                End Select
            End While
        End Using

        If shown IsNot Nothing Then
            shown.End = shown.Start + TimeSpan.FromSeconds(5)
            Yield shown
        End If
    End Function

    Shared Function ReadComposition(data As Byte()) As List(Of CompositionObject)
        Dim ret As New List(Of CompositionObject)
        Dim pos = 11

        For i = 1 To data(10)
            If pos + 8 > data.Length Then
                Exit For
            End If

            Dim obj As New CompositionObject With {
                .ID = SubtitleOcr.ReadUInt16(data, pos),
                .X = SubtitleOcr.ReadUInt16(data, pos + 4),
                .Y = SubtitleOcr.ReadUInt16(data, pos + 6),
                .IsCropped = (data(pos + 3) And &H80) <> 0 AndAlso pos + 16 <= data.Length
            }

            If obj.IsCropped Then
                obj.CropX = SubtitleOcr.ReadUInt16(data, pos + 8)
                obj.CropY = SubtitleOcr.ReadUInt16(data, pos + 10)
                obj.CropWidth = SubtitleOcr.ReadUInt16(data, pos + 12)
                obj.CropHeight = SubtitleOcr.ReadUInt16(data, pos + 14)
                pos += 8
            End If

            ret.Add(obj)
            pos += 8
        Next

        Return ret
    End Function

    'the palette contains the brightness of the colors on black
    Shared Sub ReadPalette(data As Byte(), palettes As Dictionary(Of Integer, Byte()))
        If Not palettes.ContainsKey(data(0)) Then
            palettes(data(0)) = New Byte(255) {}
        End If

        Dim palette = palettes(data(0))

        For pos = 2 To data.Length - 5 Step 5
            Dim luma = Math.Clamp((CInt(data(pos + 1)) - 16) * 255 \ 219, 0, 255)
            palette(data(pos)) = CByte(luma * data(pos + 4) \ 255)
        Next
    End Sub

    'large objects are split into multiple segments
    Shared Sub ReadObject(data As Byte(), objects As Dictionary(Of Integer, PgsObject))
        Dim id = SubtitleOcr.ReadUInt16(data, 0)

        If (data(3) And &H80) <> 0 AndAlso data.Length >= 11 Then
            objects(id) = New PgsObject With {.Width = SubtitleOcr.ReadUInt16(data, 7), .Height = SubtitleOcr.ReadUInt16(data, 9)}
            objects(id).Data.Write(data, 11, data.Length - 11)
        ElseIf objects.ContainsKey(id) Then
            objects(id).Data.Write(data, 4, data.Length - 4)
        End If
    End Sub

    Shared Function Render(composition As List(Of CompositionObject), objects As Dictionary(Of Integer, PgsObject), palette As Byte()) As SubtitleBitmap
        Dim parts = composition.Where(Function(i) objects.ContainsKey(i.ID) AndAlso objects(i.ID).Width > 0 AndAlso objects(i.ID).Height > 0).ToList

        If parts.Count = 0 OrElse palette Is Nothing Then
            Return Nothing
        End If

        Dim getWidth = Function(i As CompositionObject) If(i.IsCropped, i.CropWidth, objects(i.ID).Width)
        Dim getHeight = Function(i As CompositionObject) If(i.IsCropped, i.CropHeight, objects(i.ID).Height)
        Dim left = parts.Min(Function(i) i.X)
        Dim top = parts.Min(Function(i) i.Y)

        Dim ret As New SubtitleBitmap With {
            .Width = parts.Max(Function(i) i.X + getWidth(i)) - left,
            .Height = parts.Max(Function(i) i.Y + getHeight(i)) - top
        }

        ret.Ink = New Byte(ret.Width * ret.Height - 1) {}

        For Each part In parts
            Dim obj = objects(part.ID)
            Dim indexes = DecodeRle(obj)
            Dim cropX = If(part.IsCropped, part.CropX, 0)
            Dim cropY = If(part.IsCropped, part.CropY, 0)

            For y = 0 To Math.Min(getHeight(part), obj.Height - cropY) - 1
                For x = 0 To Math.Min(getWidth(part), obj.Width - cropX) - 1
                    Dim value = palette(indexes((y + cropY) * obj.Width + x + cropX))
                    Dim target = (y + part.Y - top) * ret.Width + x + part.X - left

                    If value > ret.Ink(target) Then
                        ret.Ink(target) = value
                    End If
                Next
            Next
        Next

        Return ret
    End Function

    'returns the palette indexes of the pixels
    Shared Function DecodeRle(obj As PgsObject) As Byte()
        Dim ret(obj.Width * obj.Height - 1) As Byte
        Dim length = CInt(obj.Data.Length)

        'codes at the end are at most 4 bytes long
        Dim data(length + 3) As Byte
        Array.Copy(obj.Data.GetBuffer, data, length)

        Dim pos = 0
        Dim x = 0
        Dim y = 0

        While pos < length AndAlso y < obj.Height
            Dim color = data(pos)
            Dim count = 1
            pos += 1

            If color = 0 Then
                Dim flags = data(pos)
                pos += 1

                If flags = 0 Then
                    x = 0
                    y += 1
                    Continue While
                End If

                count = flags And &H3F

                If (flags And &H40) <> 0 Then
                    count = (count << 8) Or data(pos)
                    pos += 1
                End If

                If (flags And &H80) <> 0 Then
                    color = data(pos)
                    pos += 1
                End If
            End If

            If x < obj.Width Then
                Array.Fill(ret, color, y * obj.Width + x, Math.Min(count, obj.Width - x))
            End If

            x += count
        End While

        Return ret
    End Function
End Class

'reads the subtitles of DVDs (.idx/.sub)
Public Class VobSubReader
    Shared Iterator Function Read(idxPath As String, streamIndex As Integer) As IEnumerable(Of SubtitleBitmap)
        Dim luma(15) As Integer
        Dim hasPalette = False
        Dim entries As New List(Of (Time As TimeSpan, Position As Long))
        Dim index = -1
        Dim delay = TimeSpan.Zero

        For Each line In File.ReadAllLines(idxPath)
            If line.StartsWith("palette:") Then
                Dim colors = line.Substring(8).Split(","c)

                For i = 0 To Math.Min(colors.Length, 16) - 1
                    Dim rgb As Integer

                    If Integer.TryParse(colors(i).Trim, Globalization.NumberStyles.HexNumber, Nothing, rgb) Then
                        luma(i) = (((rgb >> 16) And &HFF) * 299 + ((rgb >> 8) And &HFF) * 587 + (rgb And &HFF) * 114) \ 1000
                        hasPalette = True
                    End If
                Next
            ElseIf line.StartsWith("id:") Then
                Dim match = Regex.Match(line, "index: *(\d+)")
                index = If(match.Success, match.Groups(1).Value.ToInt, -1)
                delay = TimeSpan.Zero
            ElseIf line.StartsWith("delay:") Then
                delay = ParseTime(line.Substring(6))
            ElseIf line.StartsWith("timestamp:") AndAlso index = streamIndex Then
                Dim match = Regex.Match(line, "timestamp: *(-?\d+:\d+:\d+:\d+), *filepos: *([0-9a-fA-F]+)")

                If match.Success Then
                    entries.Add((ParseTime(match.Groups(1).Value) + delay, Convert.ToInt64(match.Groups(2).Value, 16)))
                End If
            End If
        Next

        Using stream = File.OpenRead(idxPath.ChangeExt("sub"))
            For i = 0 To entries.Count - 1
                Dim packet = ReadPacket(stream, entries(i).Position, streamIndex)

                If packet Is Nothing Then
                    Continue For
                End If

                Dim decoded = Decode(packet, luma, hasPalette)

                If decoded.Bitmap Is Nothing Then
                    Continue For
                End If

                Dim bitmap = decoded.Bitmap
                bitmap.Start = entries(i).Time + decoded.StartDelay

                If decoded.StopDelay.HasValue Then
                    bitmap.End = entries(i).Time + decoded.StopDelay.Value
                ElseIf i < entries.Count - 1 AndAlso entries(i + 1).Time > bitmap.Start Then
                    bitmap.End = entries(i + 1).Time
                Else
                    bitmap.End = bitmap.Start + TimeSpan.FromSeconds(5)
                End If

                Yield bitmap
            Next
        End Using
    End Function

    'times in idx files look like 01:02:03:456
    Shared Function ParseTime(value As String) As TimeSpan
        Dim match = Regex.Match(value, "(-?)(\d+):(\d+):(\d+):(\d+)")

        If Not match.Success Then
            Return TimeSpan.Zero
        End If

        Dim ret = New TimeSpan(0, match.Groups(2).Value.ToInt, match.Groups(3).Value.ToInt, match.Groups(4).Value.ToInt, match.Groups(5).Value.ToInt)
        Return If(match.Groups(1).Value = "-", -ret, ret)
    End Function

    'the subpicture unit at the position, it can be split into multiple PES packets
    Shared Function ReadPacket(stream As FileStream, position As Long, streamIndex As Integer) As Byte()
        Dim spu As Byte() = Nothing
        Dim filled = 0
        Dim header(5) As Byte
        stream.Position = position

        For packets = 1 To 1000
            If Not SubtitleOcr.ReadBytes(stream, header, 0, 4) OrElse header(0) <> 0 OrElse header(1) <> 0 OrElse header(2) <> 1 Then
                Return Nothing
            End If

            Select Case header(3)
                Case &HBA 'pack header, MPEG-2 has stuffing bytes, MPEG-1 is 2 bytes shorter
                    Dim pack(9) As Byte

                    If Not SubtitleOcr.ReadBytes(stream, pack, 0, pack.Length) Then
                        Return Nothing
                    End If

                    stream.Seek(If((pack(0) And &HC0) = &H40, pack(9) And 7, -2), SeekOrigin.Current)
                Case &HB9 'program end
                    Return Nothing
                Case Else
                    If Not SubtitleOcr.ReadBytes(stream, header, 4, 2) Then
                        Return Nothing
                    End If

                    Dim length = SubtitleOcr.ReadUInt16(header, 4)
                    Dim data(length - 1) As Byte

                    If Not SubtitleOcr.ReadBytes(stream, data, 0, length) Then
                        Return Nothing
                    End If

                    'private stream 1 with the subtitle stream ID after the PES header
                    If header(3) = &HBD AndAlso length > 3 Then
                        Dim offset = 3 + data(2)

                        If offset < length - 1 AndAlso data(offset) = &H20 + streamIndex Then
                            If spu Is Nothing Then
                                If offset + 3 > length Then
                                    Return Nothing
                                End If

                                spu = New Byte(SubtitleOcr.ReadUInt16(data, offset + 1) - 1) {}
                            End If

                            Dim count = Math.Min(length - offset - 1, spu.Length - filled)
                            Array.Copy(data, offset + 1, spu, filled, count)
                            filled += count

                            If filled >= spu.Length Then
                                Return spu
                            End If
                        End If
                    End If
            End Select
        Next

        Return Nothing
    End Function

    Shared Function Decode(spu As Byte(), luma As Integer(), hasPalette As Boolean) As (Bitmap As SubtitleBitmap, StartDelay As TimeSpan, StopDelay As TimeSpan?)
        Dim length = spu.Length

        If length < 4 Then
            Return Nothing
        End If

        'commands at the end can read past the data
        ReDim Preserve spu(length + 7)

        'palette indexes and alpha of the 4 pixel values
        Dim colors = {0, 1, 2, 3}
        Dim alpha = {0, 15, 15, 15}
        Dim x1, x2, y1, y2, offset1, offset2 As Integer
        Dim startDelay = TimeSpan.Zero
        Dim stopDelay As TimeSpan? = Nothing
        Dim position = SubtitleOcr.ReadUInt16(spu, 2)

        'control sequences, the last one points to itself
        For sequence = 1 To 64
            If position + 4 > length Then
                Exit For
            End If

            Dim delay = TimeSpan.FromMilliseconds(SubtitleOcr.ReadUInt16(spu, position) * 1024 / 90)
            Dim nextPosition = SubtitleOcr.ReadUInt16(spu, position + 2)
            Dim i = position + 4

            While i < length
                Dim command = spu(i)
                i += 1

                Select Case command
                    Case 0, 1 'start display, forced or normal
                        startDelay = delay
                    Case 2
                        stopDelay = delay
                    Case 3, 4 'the last pixel value comes first
                        Dim target = If(command = 3, colors, alpha)
                        target(3) = spu(i) >> 4
                        target(2) = spu(i) And 15
                        target(1) = spu(i + 1) >> 4
                        target(0) = spu(i + 1) And 15
                        i += 2
                    Case 5 'display area
                        x1 = (CInt(spu(i)) << 4) Or (spu(i + 1) >> 4)
                        x2 = (CInt(spu(i + 1) And 15) << 8) Or spu(i + 2)
                        y1 = (CInt(spu(i + 3)) << 4) Or (spu(i + 4) >> 4)
                        y2 = (CInt(spu(i + 4) And 15) << 8) Or spu(i + 5)
                        i += 6
                    Case 6 'offsets of the top and bottom field
                        offset1 = SubtitleOcr.ReadUInt16(spu, i)
                        offset2 = SubtitleOcr.ReadUInt16(spu, i + 2)
                        i += 4
                    Case 7 'color and contrast changes aren't needed
                        i += SubtitleOcr.ReadUInt16(spu, i)
                    Case Else 'end of the sequence
                        Exit While
                End Select
            End While

            If nextPosition <= position Then
                Exit For
            End If

            position = nextPosition
        Next

        Dim width = x2 - x1 + 1
        Dim height = y2 - y1 + 1

        If width <= 0 OrElse height <= 0 OrElse offset1 = 0 Then
            Return Nothing
        End If

        'without palette the pattern pixels are assumed to be the text
        Dim ink(3) As Byte

        For v = 0 To 3
            Dim brightness = If(hasPalette, luma(colors(v)), If(v = 1, 255, If(v = 3, 128, 0)))
            ink(v) = CByte(brightness * alpha(v) \ 15)
        Next

        Dim pixels(width * height - 1) As Byte
        DecodeField(spu, offset1, pixels, width, height, 0, ink)
        DecodeField(spu, offset2, pixels, width, height, 1, ink)

        Return (New SubtitleBitmap With {.Width = width, .Height = height, .Ink = pixels}, startDelay, stopDelay)
    End Function

    'interlaced lines with 2 bit run-length codes of 1 to 4 nibbles
    Shared Sub DecodeField(spu As Byte(), offset As Integer, pixels As Byte(), width As Integer, height As Integer, firstLine As Integer, ink As Byte())
        Dim nibble = offset * 2

        Dim read = Function() As Integer
                       If nibble >= spu.Length * 2 Then
                           Return 0
                       End If

                       Dim value = spu(nibble \ 2)
                       nibble += 1
                       Return If(nibble Mod 2 = 1, value >> 4, value And 15)
                   End Function

        For y = firstLine To height - 1 Step 2
            Dim x = 0

            While x < width
                Dim code = read()

                If code < 4 Then
                    code = (code << 4) Or read()

                    If code < &H10 Then
                        code = (code << 4) Or read()

                        If code < &H40 Then
                            code = (code << 4) Or read()
                        End If
                    End If
                End If

                'count 0 fills the rest of the line
                Dim count = If(code >> 2 = 0, width - x, Math.Min(code >> 2, width - x))
                Array.Fill(pixels, ink(code And 3), y * width + x, count)
                x += count
            End While

            nibble += nibble Mod 2
        Next
    End Sub
End Class
