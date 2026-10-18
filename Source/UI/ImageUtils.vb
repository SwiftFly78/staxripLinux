Imports System.Runtime.InteropServices

'symbol images rely on Windows icon fonts, buttons use text symbols on Linux
Public Class ImageHelp
    Shared Function GetSymbolImage(symbol As Symbol, Optional color As Color = Nothing, Optional fontSize As Integer = 12) As Image
        Return Nothing
    End Function

    Shared Sub Save(bmp As Bitmap, path As String, format As ImageFormat)
        If format = ImageFormat.Jpeg Then
            SaveJpeg(bmp, path, 75)
        ElseIf format = ImageFormat.Bitmap AndAlso bmp IsNot Nothing Then
            Using pixbuf = GetPixbufWithoutAlpha(bmp)
                pixbuf.Savev(path, "bmp", {}, {})
            End Using
        Else
            bmp?.Save(path, format)
        End If
    End Sub

    Shared Sub SaveJpeg(bmp As Bitmap, path As String, quality As Integer)
        If bmp Is Nothing Then
            Exit Sub
        End If

        Using pixbuf = GetPixbufWithoutAlpha(bmp)
            pixbuf.Savev(path, "jpeg", {"quality"}, {Math.Max(1, Math.Min(100, quality)).ToString})
        End Using
    End Sub

    'gdk-pixbuf with the glycin backend writes images with alpha channel as grayscale JPEG
    Shared Function GetPixbufWithoutAlpha(bmp As Bitmap) As Global.Gdk.Pixbuf
        'the pixbuf of the bitmap including what was drawn with a Graphics object
        Dim src = Eto.GtkSharp.GtkConversions.ToGdk(bmp, bmp.Size)
        Dim ret As New Global.Gdk.Pixbuf(Global.Gdk.Colorspace.Rgb, False, 8, src.Width, src.Height)
        Dim channels = src.NChannels
        Dim srcLine(src.Width * channels - 1) As Byte
        Dim dstLine(src.Width * 3 - 1) As Byte

        For y = 0 To src.Height - 1
            Marshal.Copy(src.Pixels + y * src.Rowstride, srcLine, 0, srcLine.Length)

            For x = 0 To src.Width - 1
                Array.Copy(srcLine, x * channels, dstLine, x * 3, 3)
            Next

            Marshal.Copy(dstLine, 0, ret.Pixels + y * ret.Rowstride, dstLine.Length)
        Next

        Return ret
    End Function
End Class
