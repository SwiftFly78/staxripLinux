Imports System.Collections.Concurrent
Imports System.Globalization
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks

Public Enum ThumbnailerRowMode
    Fixed
    TimeInterval
End Enum


Public Class Thumbnailer
    Public Shared Function Run(pProject As Project, ParamArray pSourcePaths As String()) As ConcurrentDictionary(Of String, Boolean)
        Dim cts = New CancellationTokenSource()
        Return Task.Run(Async Function() Await RunAsync(cts.Token, pProject, pSourcePaths)).Result
    End Function

    Public Shared Async Function RunAsync(pToken As CancellationToken, pProject As Project, ParamArray pSourcePaths As String()) As Task(Of ConcurrentDictionary(Of String, Boolean))
        Dim proceededSources = New ConcurrentDictionary(Of String, Boolean)

        If pSourcePaths Is Nothing OrElse pSourcePaths.Length = 0 Then Return proceededSources
        If Not Package.VapourSynth.VerifyOK(True) Then Return proceededSources

        Using semaSlim = New SemaphoreSlim(3)
            Dim taskList = New List(Of Task)()

            For Each path In pSourcePaths
                taskList.Add(New Task(Function()
                                          Dim succeeded = RunAsync(pProject, path, pToken).Result
                                          proceededSources(path) = succeeded
                                          semaSlim.Release()
                                          Return succeeded
                                      End Function)
                )
            Next

            Dim taskArray = taskList.ToArray()
            For Each t In taskArray
                Await semaSlim.WaitAsync(pToken)
                pToken.ThrowIfCancellationRequested()
                t.Start()
            Next

            Await Task.WhenAll(taskArray)
        End Using

        Return proceededSources
    End Function



    Private Shared Async Function RunAsync(pProject As Project, pSourcePath As String, pToken As CancellationToken) As Task(Of Boolean)
        If Not File.Exists(pSourcePath) Then Return False

        Dim proj As Project = pProject
        If proj Is Nothing Then
            proj = New Project()
            proj.Init()
            proj.SourceFile = pSourcePath
            proj.TargetFile = pSourcePath
        End If

        Dim defaultImageBackColor = New ColorHSL(0, 0, 0.05, 1)
        Dim dar = MediaInfo.GetVideo(pSourcePath, "DisplayAspectRatio").ToSingle()

        Dim headerInfoTask = GetHeaderInfosAsync(pSourcePath)

        Dim settings As ObjectStorage = proj.ThumbnailerSettings

        Dim thumbWidth = Mathf.Clamp(settings.GetInt("ThumbWidth", 600), 0, 1920)
        Dim thumbHeight = CInt(thumbWidth / dar)
        thumbWidth -= thumbWidth Mod 4
        thumbHeight -= thumbHeight Mod 4

        Dim scriptTask = PrepareVapoursynthVideoScriptAsync(pSourcePath, New Size(thumbWidth, thumbHeight), True, pToken)

        Dim interval = Mathf.Clamp(settings.GetInt("Interval", 60), 1, 1800)
        Dim columns = Mathf.Clamp(settings.GetInt("Columns", 4), 1, 50)
        Dim rows = Mathf.Clamp(settings.GetInt("Rows", 6), 1, 80)
        Dim rowMode = settings.GetInt("RowMode", ThumbnailerRowMode.Fixed)
        Dim spacerPercent = Mathf.Clamp(settings.GetInt("SpacerPercent", 20), 0, 1000)
        Dim spacer = CInt(thumbWidth / 10.0F * (spacerPercent / 100.0F))

        Dim timestampFontName = settings.GetString("TimestampFontName", FontManager.GetThumbnailFont().FamilyName)
        Dim timestampFontSizePercent = Mathf.Clamp(settings.GetInt("TimestampFontSizePercent", 100), 10, 1000)
        Dim timestampFontSize = (thumbWidth / 20.0F) * (timestampFontSizePercent / 100.0F)
        Dim timestampFontColorValue = settings.GetString("TimestampFontColor", New ColorHSL(0, 0, 0.9, 1).ToHTML())
        Dim timestampFontColor = timestampFontColorValue.ToColor(New ColorHSL(0, 0, 0.9, 1))
        Dim timestampOutlineColorValue = settings.GetString("TimestampOutlineColor", New ColorHSL(0, 0, 0.1, 1).ToHTML())
        Dim timestampOutlineColor = timestampOutlineColorValue.ToColor(New ColorHSL(0, 0, 0.1, 1))
        Dim timestampOutlineStrengthPercent = Mathf.Clamp(settings.GetInt("TimestampOutlineStrengthPercent", 100), 0, 1000)
        Dim timestampOutlineStrength = (timestampFontSize / 8.0F) * (timestampOutlineStrengthPercent / 100.0F)

        Dim timestampAlignmentValue = settings.GetString("TimestampAlignment")
        Dim timestampAlignment As ContentAlignment = Nothing
        If Not [Enum].TryParse(timestampAlignmentValue, timestampAlignment) Then timestampAlignment = ContentAlignment.BottomLeft
        Dim timestampAlignmentLeftFlags = {ContentAlignment.BottomLeft, ContentAlignment.MiddleLeft, ContentAlignment.TopLeft}
        Dim timestampAlignmentCenterFlags = {ContentAlignment.BottomCenter, ContentAlignment.MiddleCenter, ContentAlignment.TopCenter}
        Dim timestampAlignmentTopFlags = {ContentAlignment.TopLeft, ContentAlignment.TopCenter, ContentAlignment.TopRight}
        Dim timestampAlignmentMiddleFlags = {ContentAlignment.MiddleLeft, ContentAlignment.MiddleCenter, ContentAlignment.MiddleRight}
        Dim timestampAlignmentMargin = New Size(CInt(thumbWidth / 100.0F * dar), CInt(thumbHeight / 100.0F))

        Dim imageWidth = (spacer + thumbWidth) * columns + spacer
        Dim imageBackColorValue = settings.GetString("ImageBackColor", defaultImageBackColor.ToHTML())
        Dim imageBackColor = imageBackColorValue.ToColor(defaultImageBackColor)

        Dim header = settings.GetBool("Header", True)
        Dim headerBackColorValue = settings.GetString("HeaderBackColor", defaultImageBackColor.ToHTML())
        Dim headerBackColor = headerBackColorValue.ToColor(defaultImageBackColor)
        Dim headerFontName = settings.GetString("HeaderFontName", FontManager.GetThumbnailFont().FamilyName)
        Dim headerFontSizePercent = Mathf.Clamp(settings.GetInt("HeaderFontSizePercent", 100), 10, 1000)
        Dim headerFontSize = (thumbWidth / 25.0F) * (columns / 4.0F) * (headerFontSizePercent / 100.0F)
        Dim headerFontColorValue = settings.GetString("HeaderFontColor", New ColorHSL(0, 0, 0.8, 1).ToHTML())
        Dim headerFontColor = headerFontColorValue.ToColor(New ColorHSL(0, 0, 0.8, 1))

        Dim headerSeparatorHeightPercent = Mathf.Clamp(settings.GetInt("HeaderSeparatorHeightPercent", 0), 0, 1000)
        Dim headerSeparatorHeight = CInt(thumbWidth / 10.0F * (headerSeparatorHeightPercent / 100.0F))

        Dim imageFileFormat = settings.GetString("ImageFileFormat", "jpg").ToLowerEx()
        Dim imageQuality = Mathf.Clamp(settings.GetInt("ImageQuality", 70), 1, 100)
        Dim imageFormat As ImageFormat

        Select Case imageFileFormat
            Case "bmp" : imageFormat = ImageFormat.Bitmap
            Case "png" : imageFormat = ImageFormat.Png
            Case "tif" : imageFormat = ImageFormat.Tiff
            Case Else
                imageFileFormat = "jpg"
                imageFormat = ImageFormat.Jpeg
        End Select

        Dim imageFilePathWithoutExtension = settings.GetString("ImageFilePathWithoutExtension", "")
        Dim imageFilePath = Macro.Expand(
                                    If(String.IsNullOrWhiteSpace(imageFilePathWithoutExtension), $"%target_dir%{Path.DirectorySeparatorChar}%target_name%_Thumbnail", imageFilePathWithoutExtension),
                                    If(String.IsNullOrWhiteSpace(proj?.SourceFile) OrElse String.IsNullOrWhiteSpace(proj.TargetFile), New Project() With {.SourceFile = pSourcePath, .TargetFile = pSourcePath}, proj)
                            ) + "." + imageFileFormat

        Dim headerInfo = Await headerInfoTask
        Dim headerNearText = If(headerInfo?.NearSide, "")

        'fonts are measured in pixels, Eto font sizes are in points
        Dim headerFont As Font = Nothing
        Dim timestampFont As Font = Nothing
        Dim headerHeight = 0

        UIInvoke(Sub()
                     headerFont = FontManager.GetFont(FontCategory.Thumbnail, headerFontName, headerFontSize * 0.75F)
                     timestampFont = FontManager.GetFont(FontCategory.Thumbnail, timestampFontName, timestampFontSize * 0.75F)
                     headerHeight = CInt(Math.Ceiling(headerFont.MeasureString(headerNearText).Height))
                 End Sub)

        Dim frames = MediaInfo.GetVideo(pSourcePath, "FrameCount").ToInt()
        Dim calculatedRows = If(rowMode = ThumbnailerRowMode.TimeInterval AndAlso interval > 0, CInt((MediaInfo.GetGeneral(pSourcePath, "Duration").ToInt() / 1000 / interval) / columns), rows)
        calculatedRows = Math.Max(1, calculatedRows)
        Dim thumbCount = columns * calculatedRows
        Dim thumbVerticalOffset = spacer + headerHeight + headerSeparatorHeight + spacer
        If Not header Then thumbVerticalOffset = spacer

        Dim imageHeight = (spacer + thumbHeight) * calculatedRows + thumbVerticalOffset
        Dim headerRect = New RectangleF(spacer, spacer, imageWidth - spacer * 2, headerHeight)

        Dim script = Await scriptTask
        If script Is Nothing Then Return False

        pToken.ThrowIfCancellationRequested()

        'the frames are requested in the background, drawing happens on the UI thread
        Dim thumbs(thumbCount - 1) As FrameData
        Dim timestamps(thumbCount - 1) As String

        Using frameServer = FrameServerFactory.Create(script.Path)
            If frameServer Is Nothing Then Return False

            For x = 0 To thumbCount - 1
                pToken.ThrowIfCancellationRequested()
                Dim serverPos = CInt((frames / thumbCount) * (x + 1)) - CInt((frames / thumbCount) / 2)
                serverPos = Math.Max(0, Math.Min(serverPos, frameServer.Info.FrameCount - 1))
                thumbs(x) = FrameData.Create(frameServer, serverPos)
                timestamps(x) = g.GetTimeString(serverPos / frameServer.FrameRate)
            Next
        End Using

        pToken.ThrowIfCancellationRequested()

        UIInvoke(Sub()
                     Using image = New Bitmap(imageWidth, imageHeight, PixelFormat.Format32bppRgb)
                         Using imageGraphics = New Graphics(image)
                             imageGraphics.AntiAlias = True
                             imageGraphics.ImageInterpolation = ImageInterpolation.High
                             imageGraphics.Clear(imageBackColor)

                             If header Then
                                 If headerBackColor <> imageBackColor Then
                                     imageGraphics.FillRectangle(headerBackColor, headerRect.X, headerRect.Y, headerRect.Width, headerRect.Height + headerSeparatorHeight)
                                 End If

                                 imageGraphics.DrawText(headerFont, headerFontColor, headerRect.X, headerRect.Y, headerNearText)
                             End If

                             For x = 0 To thumbCount - 1
                                 Dim frame = thumbs(x)

                                 If frame Is Nothing Then
                                     Continue For
                                 End If

                                 Dim rowPos = x \ columns
                                 Dim columnPos = x Mod columns
                                 Dim left = spacer + columnPos * (thumbWidth + spacer)
                                 Dim top = thumbVerticalOffset + rowPos * (thumbHeight + spacer)

                                 Using thumb = FrameHelp.CreateBitmap(frame)
                                     imageGraphics.DrawImage(thumb, left, top, thumbWidth, thumbHeight)
                                 End Using

                                 Dim timestamp = timestamps(x)
                                 Dim timestampSize = timestampFont.MeasureString(timestamp)
                                 Dim tx = If(timestampAlignmentLeftFlags.Contains(timestampAlignment), timestampAlignmentMargin.Width,
                                     If(timestampAlignmentCenterFlags.Contains(timestampAlignment), (thumbWidth - timestampSize.Width) / 2, thumbWidth - timestampAlignmentMargin.Width - timestampSize.Width))
                                 Dim ty = If(timestampAlignmentTopFlags.Contains(timestampAlignment), timestampAlignmentMargin.Height,
                                     If(timestampAlignmentMiddleFlags.Contains(timestampAlignment), (thumbHeight - timestampSize.Height) / 2, thumbHeight - timestampAlignmentMargin.Height - timestampSize.Height))

                                 tx += left
                                 ty += top

                                 'outline drawn with offset copies of the text
                                 If timestampOutlineStrength > 0 Then
                                     Dim o = Math.Max(1.0F, timestampOutlineStrength)

                                     For Each offset In {New PointF(-o, 0), New PointF(o, 0), New PointF(0, -o), New PointF(0, o),
                                                         New PointF(-o, -o), New PointF(o, o), New PointF(-o, o), New PointF(o, -o)}
                                         imageGraphics.DrawText(timestampFont, timestampOutlineColor, CSng(tx) + offset.X, CSng(ty) + offset.Y, timestamp)
                                     Next
                                 End If

                                 imageGraphics.DrawText(timestampFont, timestampFontColor, CSng(tx), CSng(ty), timestamp)
                             Next
                         End Using

                         If imageFormat = ImageFormat.Jpeg Then
                             ImageHelp.SaveJpeg(image, imageFilePath, imageQuality)
                         Else
                             ImageHelp.Save(image, imageFilePath, imageFormat)
                         End If
                     End Using
                 End Sub)

        Return True
    End Function



    Private Shared Async Function PrepareVapoursynthVideoScriptAsync(filePath As String, size As Size, synchronize As Boolean, pToken As CancellationToken) As Task(Of VideoScript)
        Dim script As VideoScript = Nothing

        If Not File.Exists(filePath) Then Return script

        Await Task.Run(
            Sub()
                Dim scriptFilePath = Path.Combine(Folder.Temp, $"{filePath.Base()}_Thumbnails.vpy")
                Dim cacheFilePath = Path.Combine(Folder.Temp, $"{filePath.FileName}_Thumbnails.ffindex")

                script = New VideoScript() With {
                    .Engine = ScriptEngine.VapourSynth,
                    .Path = scriptFilePath
                }

                script.Filters.Add(New VideoFilter($"clip = core.ffms2.Source({VideoScript.ToPythonString(filePath)}, cachefile={VideoScript.ToPythonString(cacheFilePath)}, cache=False)"))

                If size.Width > 0 AndAlso size.Height > 0 Then
                    script.Filters.Add(New VideoFilter($"clip = core.resize.Spline64(clip, {size.Width},{size.Height})"))
                End If

                Dim fileBitDepth = MediaInfo.GetVideo(filePath, "BitDepth").ToInt
                Dim fileHdrFormat = MediaInfo.GetVideo(filePath, "HDR_Format_Commercial")

                If fileBitDepth > 8 AndAlso Not String.IsNullOrWhiteSpace(fileHdrFormat) AndAlso fileHdrFormat <> "SDR" Then
                    If Package.VSLibPlacebo.RequirementsFulfilled Then
                        script.Filters.Add(New VideoFilter("clip = core.fmtc.bitdepth(clip, bits=16)" + BR + "clip = core.placebo.Tonemap(clip, src_csp=1, dst_csp=0, dynamic_peak_detection=0, tone_mapping_function=7)" + BR + $"clip = clip.resize.Bicubic(format = vs.YUV420P8)"))
                    End If
                End If

                pToken.ThrowIfCancellationRequested()

                If synchronize Then script.Synchronize(True, True, True)

                Dim errorMsg = script.Error
                If errorMsg <> "" Then
                    script = Nothing
                End If
            End Sub, pToken
        )

        Return script
    End Function



    Private Shared Async Function GetHeaderInfosAsync(filePath As String) As Task(Of HeaderInfo)
        Dim info As HeaderInfo = Nothing

        If Not File.Exists(filePath) Then Return info

        Await Task.Run(
            Sub()
                Dim mig As Func(Of String, String) = Function(param) MediaInfo.GetGeneral(filePath, param)
                Dim print As Func(Of String, Boolean) = Function(x) Not String.IsNullOrWhiteSpace(x)
                Dim size As Func(Of Long, String) = Function(x) $"{x} Bytes ({If(x < 1000000000,
                                                                                $"{(x / 1000000).ToInvariantString("f2")} MB",
                                                                                $"{(x / 1000000000).ToInvariantString("f2")} GB")} ⇌ {If(x < (1024 ^ 3),
                                                                                                                                        $"{(x / (1024 ^ 2)).ToInvariantString("f2")} MiB",
                                                                                                                                        $"{(x / (1024 ^ 3)).ToInvariantString("f2")} GiB")})"

                Dim sb = New StringBuilder()
                sb.Append($"Filename:  {filePath.FileName}")

                sb.AppendLine()
                sb.Append($"General :  {size(mig("FileSize").ToLong())}")
                Dim duration = mig("Duration/String3")
                sb.Append($" · {If(duration.StartsWith("00:"), duration.Substring(3), duration)}")
                sb.Append($" · {mig("FrameCount")} frames")
                Dim overallBitRate = (mig("OverallBitRate").ToInt() / 1000000).ToInvariantString("f2")
                sb.Append($" · {overallBitRate} Mb/s")

                Dim videoStreamCount = MediaInfo.GetVideoCount(filePath)
                For i = 0 To videoStreamCount - 1
                    Dim streamNumber = i
                    Dim miv As Func(Of String, String) = Function(param) MediaInfo.GetVideo(filePath, streamNumber, param)

                    sb.AppendLine()
                    Dim streamPrint =
                    sb.Append($"Video{If(videoStreamCount > 1, $" #{streamNumber + 1}", "   ")}:")
                    sb.Append($"  {miv("Format").Replace("Video", "").TrimEx()}")
                    sb.Append($" · {miv("Width")}x{miv("Height")} px")
                    sb.Append($" · {miv("FrameRate")}")
                    Dim frameRateNum = miv("FrameRate_Num")
                    Dim frameRateOriginalNum = miv("FrameRate_Original_Num")
                    Dim frameRateDen = miv("FrameRate_Den")
                    Dim frameRateOriginalDen = miv("FrameRate_Original_Den")
                    If print(frameRateNum) Then sb.Append($" ({If(print(frameRateOriginalNum), frameRateOriginalNum, frameRateNum)}/{If(print(frameRateOriginalDen), frameRateOriginalDen, frameRateDen)})")
                    sb.Append($" fps")
                    sb.Append($" · {miv("BitRate").ToInt() \ 1000} kb/s")
                    Dim formatProfile = miv("Format_Profile")
                    If print(formatProfile) Then sb.Append($" · {formatProfile}")
                    Dim hdrFormat = miv("HDR_Format_Commercial")
                    sb.Append($" · {If(print(hdrFormat), hdrFormat, "SDR")}")
                    Dim bitDepth = miv("BitDepth")
                    If print(bitDepth) Then sb.Append($" ({bitDepth} bit)")
                    Dim colorSpace = miv("ColorSpace")
                    If print(colorSpace) Then sb.Append($" · {colorSpace} {miv("ChromaSubsampling/String")}")
                Next

                Dim audioStreamCount = MediaInfo.GetAudioCount(filePath)
                For i = 0 To audioStreamCount - 1
                    Dim streamNumber = i
                    Dim mia As Func(Of String, String) = Function(param) MediaInfo.GetAudio(filePath, streamNumber, param)

                    sb.AppendLine()
                    sb.Append($"Audio{If(audioStreamCount > 1, $" #{streamNumber + 1}", "   ")}:")
                    Dim lang = mia("Language/String3")
                    sb.Append($"  {If(print(lang), lang.ToTitleCase(), "Und")}")
                    sb.Append($" · {mia("Channel(s)")} Channel(s)")
                    Dim bitRate = mia("BitRate")
                    sb.Append($" · {If(print(bitRate), (bitRate.ToInt() / 1000).ToString("f0"), "-"),4} kb/s")
                    sb.Append($" · {mia("SamplingRate")} Hz")
                    sb.Append($" · {mia("Format/String")}")
                    Dim audioTitle = mia("Title")
                    If print(audioTitle) Then sb.Append($" · ""{audioTitle}""")
                Next

                Dim textStreamCount = MediaInfo.GetSubtitleCount(filePath)
                If textStreamCount > 0 Then
                    sb.AppendLine()
                    sb.Append($"Text    : ")

                    For i = 0 To textStreamCount - 1
                        Dim streamNumber = i
                        Dim mit As Func(Of String, String) = Function(param) MediaInfo.GetText(filePath, streamNumber, param)

                        If streamNumber > 0 Then sb.Append($" |")
                        sb.Append($" {mit("Language/String3").ToTitleCase()}")
                        sb.Append($"·{mit("Format").Replace("UTF-8", "SRT")}")
                        If mit("Default") = "Yes" Then sb.Append($"·[DEFAULT]")
                        If mit("Forced") = "Yes" Then sb.Append($"·[FORCED]")
                    Next
                End If

                info = New HeaderInfo With {
                    .NearSide = sb.ToString()
                }
            End Sub
        )

        Return info
    End Function


    Private Class HeaderInfo
        Property NearSide As String
        Property FarSide As String
    End Class
End Class

'positions like the WinForms ContentAlignment enum, stored as text in the thumbnailer settings
Public Enum ContentAlignment
    TopLeft = 1
    TopCenter = 2
    TopRight = 4
    MiddleLeft = 16
    MiddleCenter = 32
    MiddleRight = 64
    BottomLeft = 256
    BottomCenter = 512
    BottomRight = 1024
End Enum
