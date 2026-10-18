
Imports System.Runtime.InteropServices
Imports Microsoft.VisualBasic

'Frame server for AviSynth+ scripts using the C API of libavisynth.so,
'preview scripts are converted to RGB32 so frames are packed BGRA.
Public Class AviSynthServer
    Implements IFrameServer

    Property Info As ServerInfo Implements IFrameServer.Info
    ReadOnly Property [Error] As String Implements IFrameServer.Error

    Private Shared ReadOnly LockObject As New Object

    Private Env As IntPtr
    Private Clip As IntPtr
    Private Buffer As IntPtr
    Private BufferSize As Integer

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

    Sub Open(path As String)
        Try
            Env = avs_create_script_environment(6)
        Catch ex As DllNotFoundException
            _Error = "AviSynth+ isn't installed, libavisynth.so could not be loaded." + BR2 + ex.Message
            Exit Sub
        End Try

        If Env = IntPtr.Zero Then
            _Error = "Failed to create the AviSynth+ script environment."
            Exit Sub
        End If

        Dim pathPtr = Marshal.StringToCoTaskMemUTF8(path)

        Try
            Dim arg As New AVS_Value With {.Type = AscW("s"c), .ArraySize = 0, .Data = pathPtr}
            Dim result = avs_invoke(Env, "Import", arg, IntPtr.Zero)

            Try
                If result.Type = AscW("e"c) Then
                    _Error = Marshal.PtrToStringUTF8(result.Data)
                    Exit Sub
                End If

                If result.Type <> AscW("c"c) Then
                    _Error = "The AviSynth script didn't return a clip."
                    Exit Sub
                End If

                Clip = avs_take_clip(result, Env)
            Finally
                avs_release_value(result)
            End Try
        Finally
            Marshal.FreeCoTaskMem(pathPtr)
        End Try

        If Clip = IntPtr.Zero Then
            _Error = "Failed to get the AviSynth clip."
            Exit Sub
        End If

        Dim vi = avs_get_video_info(Clip)

        Dim info As New ServerInfo With {
            .Width = Marshal.ReadInt32(vi, 0),
            .Height = Marshal.ReadInt32(vi, 4),
            .FrameRateNum = Marshal.ReadInt32(vi, 8),
            .FrameRateDen = Marshal.ReadInt32(vi, 12),
            .FrameCount = Marshal.ReadInt32(vi, 16),
            .ColorSpace = CType(Marshal.ReadInt32(vi, 20), ColorSpace)
        }

        Me.Info = info
    End Sub

    Function GetFrame(position As Integer, ByRef data As IntPtr, ByRef pitch As Integer) As Integer Implements IFrameServer.GetFrame
        SyncLock LockObject
            If Clip = IntPtr.Zero Then
                Return E_FAIL
            End If

            Dim frame = avs_get_frame(Clip, position)

            If frame = IntPtr.Zero Then
                Dim err = avs_clip_get_error(Clip)
                _Error = If(err = IntPtr.Zero, "Failed to get frame " & position, Marshal.PtrToStringUTF8(err))
                Return E_FAIL
            End If

            Try
                Dim srcPitch = avs_get_pitch_p(frame, 0)
                Dim src = avs_get_read_ptr_p(frame, 0)
                Dim size = srcPitch * Info.Height

                If BufferSize < size Then
                    If Buffer <> IntPtr.Zero Then
                        Marshal.FreeHGlobal(Buffer)
                    End If

                    Buffer = Marshal.AllocHGlobal(size)
                    BufferSize = size
                End If

                Dim bytes(size - 1) As Byte
                Marshal.Copy(src, bytes, 0, size)
                Marshal.Copy(bytes, 0, Buffer, size)
                data = Buffer
                pitch = srcPitch
            Finally
                avs_release_video_frame(frame)
            End Try

            Return 0
        End SyncLock
    End Function

    Private Disposed As Boolean

    Sub Dispose() Implements IDisposable.Dispose
        SyncLock LockObject
            If Not Disposed Then
                Disposed = True

                If Clip <> IntPtr.Zero Then
                    avs_release_clip(Clip)
                    Clip = IntPtr.Zero
                End If

                If Env <> IntPtr.Zero Then
                    avs_delete_script_environment(Env)
                    Env = IntPtr.Zero
                End If

                If Buffer <> IntPtr.Zero Then
                    Marshal.FreeHGlobal(Buffer)
                    Buffer = IntPtr.Zero
                End If
            End If
        End SyncLock
    End Sub

    Const E_FAIL As Integer = &H80004005

    <StructLayout(LayoutKind.Sequential)>
    Structure AVS_Value
        Public Type As Short
        Public ArraySize As Short
        Public Data As IntPtr
    End Structure

    <DllImport("avisynth", CallingConvention:=CallingConvention.Cdecl)>
    Shared Function avs_create_script_environment(version As Integer) As IntPtr
    End Function

    <DllImport("avisynth", CallingConvention:=CallingConvention.Cdecl)>
    Shared Function avs_invoke(env As IntPtr, <MarshalAs(UnmanagedType.LPUTF8Str)> name As String, args As AVS_Value, argNames As IntPtr) As AVS_Value
    End Function

    <DllImport("avisynth", CallingConvention:=CallingConvention.Cdecl)>
    Shared Function avs_take_clip(value As AVS_Value, env As IntPtr) As IntPtr
    End Function

    <DllImport("avisynth", CallingConvention:=CallingConvention.Cdecl)>
    Shared Sub avs_release_value(value As AVS_Value)
    End Sub

    <DllImport("avisynth", CallingConvention:=CallingConvention.Cdecl)>
    Shared Function avs_get_video_info(clip As IntPtr) As IntPtr
    End Function

    <DllImport("avisynth", CallingConvention:=CallingConvention.Cdecl)>
    Shared Function avs_get_frame(clip As IntPtr, n As Integer) As IntPtr
    End Function

    <DllImport("avisynth", CallingConvention:=CallingConvention.Cdecl)>
    Shared Function avs_clip_get_error(clip As IntPtr) As IntPtr
    End Function

    <DllImport("avisynth", CallingConvention:=CallingConvention.Cdecl)>
    Shared Sub avs_release_clip(clip As IntPtr)
    End Sub

    <DllImport("avisynth", CallingConvention:=CallingConvention.Cdecl)>
    Shared Sub avs_release_video_frame(frame As IntPtr)
    End Sub

    <DllImport("avisynth", CallingConvention:=CallingConvention.Cdecl)>
    Shared Function avs_get_read_ptr_p(frame As IntPtr, plane As Integer) As IntPtr
    End Function

    <DllImport("avisynth", CallingConvention:=CallingConvention.Cdecl)>
    Shared Function avs_get_pitch_p(frame As IntPtr, plane As Integer) As Integer
    End Function

    <DllImport("avisynth", CallingConvention:=CallingConvention.Cdecl)>
    Shared Sub avs_delete_script_environment(env As IntPtr)
    End Sub
End Class
