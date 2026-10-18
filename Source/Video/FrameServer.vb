Imports System.Runtime.InteropServices
Imports System.Text
Imports StaxRip.UI

Public Interface IFrameServer
    Inherits IDisposable

    Property Info As ServerInfo
    ReadOnly Property [Error] As String
    ReadOnly Property FrameRate As Decimal
    Function GetFrame(position As Integer, ByRef data As IntPtr, ByRef pitch As Integer) As Integer
End Interface

Public Structure ServerInfo
    Public Width As Integer
    Public Height As Integer
    Public FrameRateNum As Integer
    Public FrameRateDen As Integer
    Public FrameCount As Integer
    Public ColorSpace As ColorSpace
    Public BitsPerPixel As Integer

    ReadOnly Property FrameRate As Decimal
        Get
            Return If(FrameRateDen <> 0, Decimal.Divide(FrameRateNum, FrameRateDen), 0)
        End Get
    End Property

    ReadOnly Property BitDepth As Integer
        Get
            Select Case ColorSpace
                Case ColorSpace.BGR32, ColorSpace.Y32, ColorSpace.YUV420PS, ColorSpace.YUV422PS, ColorSpace.YUV444PS
                    Return 32
                Case ColorSpace.BGR24
                    Return 24
                Case ColorSpace.RGBP16, ColorSpace.Y16, ColorSpace.YUV420P16, ColorSpace.YUV422P16, ColorSpace.YUV444P16
                    Return 16
                Case ColorSpace.RGBP14, ColorSpace.Y14, ColorSpace.YUV420P14, ColorSpace.YUV422P14, ColorSpace.YUV444P14
                    Return 14
                Case ColorSpace.RGBP12, ColorSpace.Y12, ColorSpace.YUV420P12, ColorSpace.YUV422P12, ColorSpace.YUV444P12
                    Return 12
                Case ColorSpace.RGBP10, ColorSpace.Y10, ColorSpace.YUV420P10, ColorSpace.YUV422P10, ColorSpace.YUV444P10
                    Return 10
                Case ColorSpace.RGBP8, ColorSpace.Y8, ColorSpace.YUV410P8, ColorSpace.YUV411P8, ColorSpace.YUV420P8, ColorSpace.YUV420P8_, ColorSpace.YUV422P8, ColorSpace.YUV444P8, ColorSpace.YUY2
                    Return 8
                Case Else
                    Return 0
            End Select
        End Get
    End Property

    Function GetInfoText(position As Integer) As String
        If FrameRateDen = 0 Then Return ""

        Dim lengthtDate = Date.Today.AddSeconds(FrameCount / FrameRate)
        Dim dateFormat = If(lengthtDate.Hour = 0, "mm:ss.fff", "HH:mm:ss.fff")
        Dim frames = FrameCount.ToString
        Dim len = lengthtDate.ToString(dateFormat)

        If position > -1 Then
            frames = position & " of " & FrameCount
            Dim currentDate = Date.Today.AddSeconds(position / FrameRate)
            len = currentDate.ToString(dateFormat) + " of " + lengthtDate.ToString(dateFormat)
        End If

        Return "Width     : " & Width & BR &
               "Height    : " & Height & BR &
               "Frames    : " + frames + BR +
               "Time      : " + len + BR +
               "Framerate : " + FrameRate.ToInvariantString.Shorten(9) + " (" & FrameRateNum & "/" & FrameRateDen & ")" + BR +
               "Format    : " + ColorSpace.ToString.Replace("_", "")
    End Function
End Structure

Public Enum ColorSpace
    Unknown = 0
    BGR24 = 1342177281
    BGR32 = 1342177282
    RGBP8 = -1879048191
    RGBP10 = -1878720511
    RGBP12 = -1878654975
    RGBP14 = -1878589439
    RGBP16 = -1878982655
    Y8 = -536870912
    Y10 = -536543232
    Y12 = -536477696
    Y14 = -536412160
    Y16 = -536805376
    Y32 = -536739840
    YUV410P8 = -1610612471
    YUV411P8 = -1610611959
    YUV420P8 = -1610612720
    YUV420P8_ = -1610612728
    YUV420P10 = -1610285048
    YUV420P12 = -1610219512
    YUV420P14 = -1610153976
    YUV420P16 = -1610547192
    YUV420PS = -1610481656
    YUV422P8 = -1610611960
    YUV422P10 = -1610284280
    YUV422P12 = -1610218744
    YUV422P14 = -1610153208
    YUV422P16 = -1610546424
    YUV422PS = -1610480888
    YUV444P8 = -1610611957
    YUV444P10 = -1610284277
    YUV444P12 = -1610218741
    YUV444P14 = -1610153205
    YUV444P16 = -1610546421
    YUV444PS = -1610480885
    YUY2 = 1610612740
End Enum

Public Class FrameServerFactory
    Shared Function Create(path As String) As IFrameServer
        Try
            If path.Ext = "avs" Then
                Return New AviSynthServer(path)
            Else
                Return New VapourSynthServer(path)
            End If
        Catch ex As AbortException
            Throw
        Catch ex As Exception
            g.ShowException(ex)
            Return Nothing
        End Try
    End Function
End Class

'Frame server for VapourSynth scripts using the VSScript API 4 of libvapoursynth-script,
'frames are returned as packed BGRA, RGB24 planar output is packed, other formats return plane 0.
Public Class VapourSynthServer
    Implements IFrameServer

    Property Info As ServerInfo Implements IFrameServer.Info
    ReadOnly Property [Error] As String Implements IFrameServer.Error

    Private Shared ReadOnly LockObject As New Object

    Private ScriptAPI As IntPtr
    Private API As IntPtr
    Private Script As IntPtr
    Private Node As IntPtr
    Private Buffer As IntPtr
    Private BufferSize As Integer

    Private vsCreateScript As CreateScriptDelegate
    Private vsEvaluateFile As EvaluateFileDelegate
    Private vsGetError As GetErrorDelegate
    Private vsGetOutputNode As GetOutputNodeDelegate
    Private vsFreeScript As FreeScriptDelegate
    Private vsEvalSetWorkingDir As EvalSetWorkingDirDelegate
    Private vsGetVSAPI As GetVSAPIDelegate

    Private vsFreeNode As FreeNodeDelegate
    Private vsGetNodeType As GetNodeTypeDelegate
    Private vsGetVideoInfo As GetVideoInfoDelegate
    Private vsFreeFrame As FreeFrameDelegate
    Private vsGetStride As GetStrideDelegate
    Private vsGetReadPtr As GetReadPtrDelegate
    Private vsGetVideoFrameFormat As GetVideoFrameFormatDelegate
    Private vsGetFrameWidth As GetFrameSizeDelegate
    Private vsGetFrameHeight As GetFrameSizeDelegate
    Private vsGetFrame As GetFrameDelegate

    Const VSSCRIPT_API_VERSION As Integer = (4 << 16) Or 1
    Const VAPOURSYNTH_API_VERSION As Integer = (4 << 16) Or 0

    Sub New(path As String)
        SyncLock LockObject
            Open(path)
        End SyncLock
    End Sub

    ReadOnly Property FrameRate As Decimal Implements IFrameServer.FrameRate
        Get
            Return Info.FrameRate
        End Get
    End Property

    Shared Function GetDelegate(Of T As Class)(table As IntPtr, index As Integer) As T
        Dim ptr = Marshal.ReadIntPtr(table, index * IntPtr.Size)

        If ptr = IntPtr.Zero Then
            Throw New Exception("VapourSynth API function missing at index " & index)
        End If

        Return Marshal.GetDelegateForFunctionPointer(Of T)(ptr)
    End Function

    Sub Open(path As String)
        Try
            ScriptAPI = getVSScriptAPI(VSSCRIPT_API_VERSION)
        Catch ex As DllNotFoundException
            _Error = "VapourSynth isn't installed, libvapoursynth-script.so could not be loaded." + BR2 + ex.Message
            Exit Sub
        Catch ex As EntryPointNotFoundException
            _Error = "VapourSynth is too old, R55 or newer is required."
            Exit Sub
        End Try

        If ScriptAPI = IntPtr.Zero Then
            _Error = "Failed to initialize VSScript, VapourSynth R55 or newer and a matching Python version are required."
            Dim lastError = GetLastScriptError()

            If lastError <> "" Then
                _Error += BR2 + lastError
            End If

            Exit Sub
        End If

        vsGetVSAPI = GetDelegate(Of GetVSAPIDelegate)(ScriptAPI, 1)
        vsCreateScript = GetDelegate(Of CreateScriptDelegate)(ScriptAPI, 2)
        vsEvaluateFile = GetDelegate(Of EvaluateFileDelegate)(ScriptAPI, 5)
        vsGetError = GetDelegate(Of GetErrorDelegate)(ScriptAPI, 6)
        vsGetOutputNode = GetDelegate(Of GetOutputNodeDelegate)(ScriptAPI, 10)
        vsFreeScript = GetDelegate(Of FreeScriptDelegate)(ScriptAPI, 13)
        vsEvalSetWorkingDir = GetDelegate(Of EvalSetWorkingDirDelegate)(ScriptAPI, 14)

        API = vsGetVSAPI(VAPOURSYNTH_API_VERSION)

        If API = IntPtr.Zero Then
            _Error = "Failed to get the VapourSynth API."
            Exit Sub
        End If

        vsFreeNode = GetDelegate(Of FreeNodeDelegate)(API, 7)
        vsGetNodeType = GetDelegate(Of GetNodeTypeDelegate)(API, 9)
        vsGetVideoInfo = GetDelegate(Of GetVideoInfoDelegate)(API, 10)
        vsFreeFrame = GetDelegate(Of FreeFrameDelegate)(API, 16)
        vsGetStride = GetDelegate(Of GetStrideDelegate)(API, 21)
        vsGetReadPtr = GetDelegate(Of GetReadPtrDelegate)(API, 22)
        vsGetVideoFrameFormat = GetDelegate(Of GetVideoFrameFormatDelegate)(API, 24)
        vsGetFrameWidth = GetDelegate(Of GetFrameSizeDelegate)(API, 27)
        vsGetFrameHeight = GetDelegate(Of GetFrameSizeDelegate)(API, 28)
        vsGetFrame = GetDelegate(Of GetFrameDelegate)(API, 36)

        Script = vsCreateScript(IntPtr.Zero)

        If Script = IntPtr.Zero Then
            _Error = "Failed to create the VapourSynth script environment."
            Exit Sub
        End If

        vsEvalSetWorkingDir(Script, 1)

        If vsEvaluateFile(Script, path) <> 0 Then
            _Error = PtrToString(vsGetError(Script))

            If _Error = "" Then
                _Error = "Failed to evaluate the VapourSynth script."
            End If

            Free()
            Exit Sub
        End If

        Node = vsGetOutputNode(Script, 0)

        If Node = IntPtr.Zero OrElse vsGetNodeType(Node) <> 1 Then
            _Error = "Failed to find a video output at VapourSynth output index 0."
            Free()
            Exit Sub
        End If

        Dim vi = vsGetVideoInfo(Node)

        If vi = IntPtr.Zero Then
            _Error = "Failed to get the VapourSynth video info."
            Free()
            Exit Sub
        End If

        Dim format = ReadFormat(vi)
        Dim fpsNum = Marshal.ReadInt64(vi, 32)
        Dim fpsDen = Marshal.ReadInt64(vi, 40)

        'large frame rate values are reduced to fit into 32 bit
        While fpsNum > Integer.MaxValue OrElse fpsDen > Integer.MaxValue
            fpsNum \= 2
            fpsDen \= 2
        End While

        Dim info As New ServerInfo With {
            .Width = Marshal.ReadInt32(vi, 48),
            .Height = Marshal.ReadInt32(vi, 52),
            .FrameCount = Marshal.ReadInt32(vi, 56),
            .FrameRateNum = CInt(fpsNum),
            .FrameRateDen = CInt(fpsDen),
            .ColorSpace = GetColorSpace(format)
        }

        Me.Info = info
    End Sub

    Structure VideoFormat
        Public ColorFamily As Integer
        Public SampleType As Integer
        Public BitsPerSample As Integer
        Public BytesPerSample As Integer
        Public SubSamplingW As Integer
        Public SubSamplingH As Integer
        Public NumPlanes As Integer
    End Structure

    Shared Function ReadFormat(ptr As IntPtr) As VideoFormat
        Return New VideoFormat With {
            .ColorFamily = Marshal.ReadInt32(ptr, 0),
            .SampleType = Marshal.ReadInt32(ptr, 4),
            .BitsPerSample = Marshal.ReadInt32(ptr, 8),
            .BytesPerSample = Marshal.ReadInt32(ptr, 12),
            .SubSamplingW = Marshal.ReadInt32(ptr, 16),
            .SubSamplingH = Marshal.ReadInt32(ptr, 20),
            .NumPlanes = Marshal.ReadInt32(ptr, 24)
        }
    End Function

    Shared Function GetColorSpace(f As VideoFormat) As ColorSpace
        Dim isFloat = f.SampleType = 1

        Select Case f.ColorFamily
            Case 1 'gray
                If isFloat Then Return ColorSpace.Y32

                Select Case f.BitsPerSample
                    Case 8 : Return ColorSpace.Y8
                    Case 10 : Return ColorSpace.Y10
                    Case 12 : Return ColorSpace.Y12
                    Case 14 : Return ColorSpace.Y14
                    Case 16 : Return ColorSpace.Y16
                End Select
            Case 2 'RGB
                Select Case f.BitsPerSample
                    Case 8 : Return ColorSpace.RGBP8
                    Case 10 : Return ColorSpace.RGBP10
                    Case 12 : Return ColorSpace.RGBP12
                    Case 14 : Return ColorSpace.RGBP14
                    Case 16 : Return ColorSpace.RGBP16
                End Select
            Case 3 'YUV
                Dim ss = f.SubSamplingW & ":" & f.SubSamplingH

                If isFloat Then
                    Select Case ss
                        Case "0:0" : Return ColorSpace.YUV444PS
                        Case "1:0" : Return ColorSpace.YUV422PS
                        Case "1:1" : Return ColorSpace.YUV420PS
                    End Select
                End If

                Select Case ss & ":" & f.BitsPerSample
                    Case "0:0:8" : Return ColorSpace.YUV444P8
                    Case "1:0:8" : Return ColorSpace.YUV422P8
                    Case "1:1:8" : Return ColorSpace.YUV420P8
                    Case "2:2:8" : Return ColorSpace.YUV410P8
                    Case "2:0:8" : Return ColorSpace.YUV411P8
                    Case "0:0:10" : Return ColorSpace.YUV444P10
                    Case "1:0:10" : Return ColorSpace.YUV422P10
                    Case "1:1:10" : Return ColorSpace.YUV420P10
                    Case "0:0:12" : Return ColorSpace.YUV444P12
                    Case "1:0:12" : Return ColorSpace.YUV422P12
                    Case "1:1:12" : Return ColorSpace.YUV420P12
                    Case "0:0:14" : Return ColorSpace.YUV444P14
                    Case "1:0:14" : Return ColorSpace.YUV422P14
                    Case "1:1:14" : Return ColorSpace.YUV420P14
                    Case "0:0:16" : Return ColorSpace.YUV444P16
                    Case "1:0:16" : Return ColorSpace.YUV422P16
                    Case "1:1:16" : Return ColorSpace.YUV420P16
                End Select
        End Select

        Return ColorSpace.Unknown
    End Function

    Function GetFrame(position As Integer, ByRef data As IntPtr, ByRef pitch As Integer) As Integer Implements IFrameServer.GetFrame
        SyncLock LockObject
            If Node = IntPtr.Zero Then
                Return E_FAIL
            End If

            Dim errorBuffer = Marshal.AllocHGlobal(1024)

            Try
                Marshal.WriteByte(errorBuffer, 0)
                Dim frame = vsGetFrame(position, Node, errorBuffer, 1024)

                If frame = IntPtr.Zero Then
                    _Error = PtrToString(errorBuffer)
                    Return E_FAIL
                End If

                Try
                    Dim format = ReadFormat(vsGetVideoFrameFormat(frame))
                    Dim width = vsGetFrameWidth(frame, 0)
                    Dim height = vsGetFrameHeight(frame, 0)

                    If format.ColorFamily = 2 AndAlso format.BytesPerSample = 1 AndAlso format.SampleType = 0 Then
                        PackRGB(frame, width, height)
                        data = Buffer
                        pitch = width * 4
                    Else
                        'other formats provide the first plane, like packed BGRA in gray32
                        Dim stride = CInt(vsGetStride(frame, 0))
                        Dim size = stride * height
                        EnsureBuffer(size)
                        Dim bytes(size - 1) As Byte
                        Marshal.Copy(vsGetReadPtr(frame, 0), bytes, 0, size)
                        Marshal.Copy(bytes, 0, Buffer, size)
                        data = Buffer
                        pitch = stride
                    End If
                Finally
                    vsFreeFrame(frame)
                End Try

                Return 0
            Finally
                Marshal.FreeHGlobal(errorBuffer)
            End Try
        End SyncLock
    End Function

    Private Sub EnsureBuffer(size As Integer)
        If BufferSize < size Then
            If Buffer <> IntPtr.Zero Then
                Marshal.FreeHGlobal(Buffer)
            End If

            Buffer = Marshal.AllocHGlobal(size)
            BufferSize = size
        End If
    End Sub

    'packs the planes of RGB24 to BGRA
    Private Sub PackRGB(frame As IntPtr, width As Integer, height As Integer)
        Dim outPitch = width * 4
        EnsureBuffer(outPitch * height)

        Dim rPtr = vsGetReadPtr(frame, 0)
        Dim gPtr = vsGetReadPtr(frame, 1)
        Dim bPtr = vsGetReadPtr(frame, 2)
        Dim rStride = CInt(vsGetStride(frame, 0))
        Dim gStride = CInt(vsGetStride(frame, 1))
        Dim bStride = CInt(vsGetStride(frame, 2))

        Dim r(width - 1) As Byte
        Dim gr(width - 1) As Byte
        Dim b(width - 1) As Byte
        Dim line(outPitch - 1) As Byte

        For y = 0 To height - 1
            Marshal.Copy(rPtr + y * rStride, r, 0, width)
            Marshal.Copy(gPtr + y * gStride, gr, 0, width)
            Marshal.Copy(bPtr + y * bStride, b, 0, width)

            Dim i = 0

            For x = 0 To width - 1
                line(i) = b(x)
                line(i + 1) = gr(x)
                line(i + 2) = r(x)
                line(i + 3) = 255
                i += 4
            Next

            Marshal.Copy(line, 0, Buffer + y * outPitch, outPitch)
        Next
    End Sub

    'only available with VSScript API 4.3 and newer
    Shared Function GetLastScriptError() As String
        Try
            Return PtrToString(getVSScriptAPILastError())
        Catch ex As EntryPointNotFoundException
            Return ""
        End Try
    End Function

    Shared Function PtrToString(ptr As IntPtr) As String
        If ptr = IntPtr.Zero Then
            Return ""
        End If

        Return Marshal.PtrToStringUTF8(ptr)
    End Function

    Private Sub Free()
        If Node <> IntPtr.Zero Then
            vsFreeNode(Node)
            Node = IntPtr.Zero
        End If

        If Script <> IntPtr.Zero Then
            vsFreeScript(Script)
            Script = IntPtr.Zero
        End If
    End Sub

    Private Disposed As Boolean

    Sub Dispose() Implements IDisposable.Dispose
        SyncLock LockObject
            If Not Disposed Then
                Disposed = True
                Free()

                If Buffer <> IntPtr.Zero Then
                    Marshal.FreeHGlobal(Buffer)
                    Buffer = IntPtr.Zero
                End If
            End If
        End SyncLock
    End Sub

    Const E_FAIL As Integer = &H80004005

    <DllImport("vsscript", CallingConvention:=CallingConvention.Cdecl)>
    Shared Function getVSScriptAPI(version As Integer) As IntPtr
    End Function

    <DllImport("vsscript", CallingConvention:=CallingConvention.Cdecl)>
    Shared Function getVSScriptAPILastError() As IntPtr
    End Function

    <UnmanagedFunctionPointer(CallingConvention.Cdecl)>
    Delegate Function GetVSAPIDelegate(version As Integer) As IntPtr

    <UnmanagedFunctionPointer(CallingConvention.Cdecl)>
    Delegate Function CreateScriptDelegate(core As IntPtr) As IntPtr

    <UnmanagedFunctionPointer(CallingConvention.Cdecl)>
    Delegate Function EvaluateFileDelegate(handle As IntPtr, <MarshalAs(UnmanagedType.LPUTF8Str)> scriptFilename As String) As Integer

    <UnmanagedFunctionPointer(CallingConvention.Cdecl)>
    Delegate Function GetErrorDelegate(handle As IntPtr) As IntPtr

    <UnmanagedFunctionPointer(CallingConvention.Cdecl)>
    Delegate Function GetOutputNodeDelegate(handle As IntPtr, index As Integer) As IntPtr

    <UnmanagedFunctionPointer(CallingConvention.Cdecl)>
    Delegate Sub FreeScriptDelegate(handle As IntPtr)

    <UnmanagedFunctionPointer(CallingConvention.Cdecl)>
    Delegate Sub EvalSetWorkingDirDelegate(handle As IntPtr, setCWD As Integer)

    <UnmanagedFunctionPointer(CallingConvention.Cdecl)>
    Delegate Sub FreeNodeDelegate(node As IntPtr)

    <UnmanagedFunctionPointer(CallingConvention.Cdecl)>
    Delegate Function GetNodeTypeDelegate(node As IntPtr) As Integer

    <UnmanagedFunctionPointer(CallingConvention.Cdecl)>
    Delegate Function GetVideoInfoDelegate(node As IntPtr) As IntPtr

    <UnmanagedFunctionPointer(CallingConvention.Cdecl)>
    Delegate Sub FreeFrameDelegate(frame As IntPtr)

    <UnmanagedFunctionPointer(CallingConvention.Cdecl)>
    Delegate Function GetStrideDelegate(frame As IntPtr, plane As Integer) As IntPtr

    <UnmanagedFunctionPointer(CallingConvention.Cdecl)>
    Delegate Function GetReadPtrDelegate(frame As IntPtr, plane As Integer) As IntPtr

    <UnmanagedFunctionPointer(CallingConvention.Cdecl)>
    Delegate Function GetVideoFrameFormatDelegate(frame As IntPtr) As IntPtr

    <UnmanagedFunctionPointer(CallingConvention.Cdecl)>
    Delegate Function GetFrameSizeDelegate(frame As IntPtr, plane As Integer) As Integer

    <UnmanagedFunctionPointer(CallingConvention.Cdecl)>
    Delegate Function GetFrameDelegate(n As Integer, node As IntPtr, errorMsg As IntPtr, bufSize As Integer) As IntPtr
End Class

'creates Eto bitmaps from BGRA frame data
Public Class FrameHelp
    Private Shared SwapRedBlue As Boolean?

    'the byte order of Eto bitmaps depends on the platform, GTK pixbufs use RGBA
    Shared Function IsRedBlueSwapped() As Boolean
        If Not SwapRedBlue.HasValue Then
            Using bmp As New Bitmap(1, 1, PixelFormat.Format32bppRgba)
                bmp.SetPixel(0, 0, Color.FromArgb(255, 0, 0, 255))

                Using data = bmp.Lock()
                    SwapRedBlue = Marshal.ReadByte(data.Data, 0) = 255
                End Using
            End Using
        End If

        Return SwapRedBlue.Value
    End Function

    Shared Function CreateBitmap(frame As FrameData) As Bitmap
        Return CreateBitmap(frame.Data, frame.Width, frame.Height, frame.Stride)
    End Function

    Shared Function CreateBitmap(bgra As Byte(), width As Integer, height As Integer, stride As Integer) As Bitmap
        Dim bmp As New Bitmap(width, height, PixelFormat.Format32bppRgb)
        CopyToBitmap(bmp, bgra, width, height, stride)
        Return bmp
    End Function

    Shared Sub CopyToBitmap(bmp As Bitmap, bgra As Byte(), width As Integer, height As Integer, stride As Integer)
        Dim swap = IsRedBlueSwapped()

        Using data = bmp.Lock()
            Dim line(width * 4 - 1) As Byte

            For y = 0 To height - 1
                Dim srcOffset = If(data.Flipped, (height - 1 - y), y) * stride

                If swap Then
                    For x = 0 To width - 1
                        Dim i = x * 4
                        line(i) = bgra(srcOffset + i + 2)
                        line(i + 1) = bgra(srcOffset + i + 1)
                        line(i + 2) = bgra(srcOffset + i)
                        line(i + 3) = 255
                    Next
                Else
                    Array.Copy(bgra, srcOffset, line, 0, width * 4)

                    For x = 0 To width - 1
                        line(x * 4 + 3) = 255
                    Next
                End If

                Marshal.Copy(line, 0, data.Data + y * data.ScanWidth, width * 4)
            Next
        End Using
    End Sub

    Shared Function CreateBitmap(server As IFrameServer, position As Integer) As Bitmap
        Dim frame = FrameData.Create(server, position)

        If frame Is Nothing Then
            Return Nothing
        End If

        Return CreateBitmap(frame)
    End Function
End Class

Public Class FrameServerHelp
    Shared Function GetSynthPath() As String
        Return If(IsAviSynth(), Package.AviSynth.Path, LinuxNames.FindLibrary("libvapoursynth-script.so.0"))
    End Function

    Shared Function GetAviSynthInstallPath() As String
        Return LinuxNames.FindLibrary("libavisynth.so")
    End Function

    Shared Function GetVapourSynthInstallPath() As String
        Return LinuxNames.FindLibrary("libvapoursynth.so")
    End Function

    Shared Function IsAviSynthPortable() As Boolean
        Return s.AviSynthMode = FrameServerMode.Portable
    End Function

    Shared Function IsVapourSynthPortable() As Boolean
        Return s.VapourSynthMode = FrameServerMode.Portable
    End Function

    Shared Function IsPortable() As Boolean
        If (IsAviSynth() AndAlso IsAviSynthPortable()) OrElse
            (IsVapourSynth() AndAlso IsVapourSynthPortable()) Then

            Return True
        End If
    End Function

    Shared Function IsAviSynthSystemInstalled() As Boolean
        Return GetAviSynthInstallPath() <> ""
    End Function

    'avs2pipemod is rarely packaged on Linux, ffmpeg usually reads AviSynth scripts
    Shared Function GetAviSynthPipeTool() As String
        Return If(Package.avs2pipemod.Path <> "", "avs2pipemod", "ffmpeg")
    End Function

    Shared Function IsAviSynth() As Boolean
        Return p.Script.IsAviSynth
    End Function

    Shared Function IsVapourSynth() As Boolean
        Return p.Script.Engine = ScriptEngine.VapourSynth
    End Function

    'VFW doesn't exist on Linux
    Shared Function IsVfwUsed() As Boolean
        Return False
    End Function

    Shared Function IsffmpegUsed() As Boolean
        If "avs".EqualsAny(p.AudioTracks.Select(Function(x) x.AudioProfile.File.Ext).ToArray()) Then
            Return True
        End If

        If TypeOf p.VideoEncoder Is ffmpegEnc OrElse p.VideoEncoder.GetCommandLine(True, True).ContainsEx("ffmpeg") Then
            Return True
        End If
    End Function

    Shared Sub MoveFiles(srcDir As String, targetDir As String, fileNames As String())
        For Each name In fileNames
            Dim src = Path.Combine(srcDir, name)
            Dim dest = Path.Combine(targetDir, name)
            Try
                FileHelp.Move(src, dest)
            Catch ex As Exception
                MsgError("Could not move file from:" + BR2 + src + BR2 +
                         "to:" + BR2 + dest + BR2 + ex.Message)
            End Try
        Next
    End Sub

    'the Windows version moves tools next to the AviSynth DLL, not needed on Linux
    Shared Sub AviSynthToolPath()
    End Sub

    Shared Function CommandLineContainsAvsDllPath() As Boolean
        Return p.VideoEncoder.GetCommandLine(True, True).ContainsEx(Package.AviSynth.Path)
    End Function
End Class

Public Enum FrameServerMode
    <DispName("Use portable directly")> Portable
    <DispName("Use installed directly")> Installed
    <DispName("Use installed via VFW")> VFW
End Enum
