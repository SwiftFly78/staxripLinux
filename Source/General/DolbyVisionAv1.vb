
Imports System.Runtime.InteropServices

'dovi_tool only extracts the RPUs of HEVC, libdovi converts the ITU-T T.35 metadata OBUs of AV1 sources (profile 10)
'into the RPU files dovi_tool and the encoders read
Public Class DolbyVisionAv1
    Const ObuTypeMetadata = 5
    Const MetadataTypeItutT35 = 4

    <StructLayout(LayoutKind.Sequential)>
    Structure DoviData
        Public Data As IntPtr
        Public Length As UIntPtr
    End Structure

    <DllImport("dovi")>
    Private Shared Function dovi_parse_itu_t35_dovi_metadata_obu(buf As Byte(), len As UIntPtr) As IntPtr
    End Function

    <DllImport("dovi")>
    Private Shared Function dovi_rpu_get_error(rpu As IntPtr) As IntPtr
    End Function

    <DllImport("dovi")>
    Private Shared Function dovi_write_unspec62_nalu(rpu As IntPtr) As IntPtr
    End Function

    <DllImport("dovi")>
    Private Shared Sub dovi_data_free(data As IntPtr)
    End Sub

    <DllImport("dovi")>
    Private Shared Sub dovi_rpu_free(rpu As IntPtr)
    End Sub

    Shared Function IsAvailable() As Boolean
        Return NativeLibraries.IsAvailable("dovi")
    End Function

    'writes the RPUs of an AV1 stream in the low overhead OBU format to an RPU file, returns their number
    Shared Function ConvertObuStream(obuPath As String, rpuPath As String) As Integer
        Dim data = File.ReadAllBytes(obuPath)
        Dim startCode As Byte() = {0, 0, 0, 1}
        Dim count = 0
        Dim pos = 0

        Using stream = File.Create(rpuPath)
            While pos < data.Length
                Dim header = data(pos)
                Dim type = (header >> 3) And &HF
                Dim hasSize = (header And 2) <> 0
                pos += If((header And 4) <> 0, 2, 1)

                Dim size = If(hasSize, CInt(ReadLeb128(data, pos)), data.Length - pos)
                Dim payloadStart = pos
                pos += size

                If type <> ObuTypeMetadata OrElse pos > data.Length Then
                    Continue While
                End If

                Dim t35Start = payloadStart

                If ReadLeb128(data, t35Start) <> MetadataTypeItutT35 Then
                    Continue While
                End If

                Dim t35(pos - t35Start - 1) As Byte
                Array.Copy(data, t35Start, t35, 0, t35.Length)
                Dim rpu = ConvertT35Payload(t35)

                'other T.35 metadata, like HDR10+
                If rpu Is Nothing Then
                    Continue While
                End If

                stream.Write(startCode, 0, startCode.Length)
                stream.Write(rpu, 0, rpu.Length)
                count += 1
            End While
        End Using

        Return count
    End Function

    'the RPU of an ITU-T T.35 payload as an RPU file of dovi_tool stores it, escaped for HEVC, Nothing for payloads that
    'aren't Dolby Vision
    Shared Function ConvertT35Payload(t35 As Byte()) As Byte()
        Dim rpu = dovi_parse_itu_t35_dovi_metadata_obu(t35, CType(t35.Length, UIntPtr))

        Try
            If rpu = IntPtr.Zero OrElse dovi_rpu_get_error(rpu) <> IntPtr.Zero Then
                Return Nothing
            End If

            Dim nalu = dovi_write_unspec62_nalu(rpu)

            If nalu = IntPtr.Zero Then
                Return Nothing
            End If

            Try
                Dim message = dovi_rpu_get_error(rpu)

                If message <> IntPtr.Zero Then
                    Throw New ErrorAbortException("Dolby Vision metadata not extracted", Marshal.PtrToStringUTF8(message))
                End If

                'without the header 0x7C01 of the NAL unit
                Dim value = Marshal.PtrToStructure(Of DoviData)(nalu)
                Dim ret(CInt(value.Length) - 3) As Byte
                Marshal.Copy(IntPtr.Add(value.Data, 2), ret, 0, ret.Length)
                Return ret
            Finally
                dovi_data_free(nalu)
            End Try
        Finally
            If rpu <> IntPtr.Zero Then
                dovi_rpu_free(rpu)
            End If
        End Try
    End Function

    Shared Function ReadLeb128(data As Byte(), ByRef pos As Integer) As Long
        Dim ret As Long

        For i = 0 To 7
            Dim b = data(pos)
            pos += 1
            ret = ret Or (CLng(b And &H7F) << (i * 7))

            If (b And &H80) = 0 Then
                Exit For
            End If
        Next

        Return ret
    End Function
End Class
