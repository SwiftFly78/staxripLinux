
Imports StaxRip.UI

'Renders frames of a frame server into a drawable, used by the preview and crop windows
Public Class VideoRenderer
    Implements IDisposable

    Property CropLeft As Integer
    Property CropTop As Integer
    Property CropRight As Integer
    Property CropBottom As Integer
    Property ShowInfo As Boolean
    Property Info As ServerInfo
    Property BackColor As ColorHSL

    'letterboxes the video instead of stretching it to the control size
    Property KeepAspectRatio As Boolean

    'applies the sample aspect ratio of the frame properties that source filters like ffms2 set
    Property UseSampleAspectRatio As Boolean

    'display aspect ratio that replaces the one of the frames, 0 keeps it
    Property DisplayAspectRatio As Double

    Private BitmapSampleAspectRatio As Double = 1

    Private Server As IFrameServer
    Private Control As Drawable
    Private PositionValue As Integer
    Private Bitmap As Bitmap
    Private BitmapPosition As Integer = -1

    Sub New(control As Drawable, server As IFrameServer)
        Me.Server = server
        Me.Control = control
        Info = server.Info
        ApplyTheme()
        AddHandler control.Paint, AddressOf OnPaint
    End Sub

    Property Position As Integer
        Get
            Return PositionValue
        End Get
        Set(value As Integer)
            PositionValue = value

            If PositionValue > Server.Info.FrameCount - 1 Then
                PositionValue = Server.Info.FrameCount - 1
            End If

            If PositionValue < 0 Then
                PositionValue = 0
            End If
        End Set
    End Property

    Sub ApplyTheme()
        If s.CropColor.IsEmpty Then
            BackColor = If(IsDarkMode, New ColorHSL(0, 0, 0.1, 1), New ColorHSL(0, 0, 0.2, 1))
        Else
            BackColor = s.CropColor
        End If
    End Sub

    Sub Draw()
        Control.Invalidate()
    End Sub

    Function GetBitmap() As Bitmap
        If Bitmap IsNot Nothing AndAlso BitmapPosition = Position Then
            Return Bitmap
        End If

        Dim frame = FrameData.Create(Server, Position)

        If frame Is Nothing Then
            Return Bitmap
        End If

        If Bitmap Is Nothing OrElse Bitmap.Width <> frame.Width OrElse Bitmap.Height <> frame.Height Then
            Bitmap?.Dispose()
            Bitmap = New Bitmap(frame.Width, frame.Height, PixelFormat.Format32bppRgb)
        End If

        FrameHelp.CopyToBitmap(Bitmap, frame.Data, frame.Width, frame.Height, frame.Stride)
        BitmapPosition = Position
        BitmapSampleAspectRatio = GetSampleAspectRatio(Server.FrameProperties)
        Return Bitmap
    End Function

    Shared Function GetSampleAspectRatio(props As Dictionary(Of String, String)) As Double
        Dim num, den As Integer

        If props IsNot Nothing AndAlso props.ContainsKey("_SARNum") AndAlso props.ContainsKey("_SARDen") AndAlso
            Integer.TryParse(props("_SARNum"), num) AndAlso Integer.TryParse(props("_SARDen"), den) AndAlso num > 0 AndAlso den > 0 Then

            Return num / den
        End If

        Return 1
    End Function

    'the aspect ratio the video is shown with
    ReadOnly Property VideoAspectRatio As Double
        Get
            If Server.Info.Width = 0 OrElse Server.Info.Height = 0 Then Return 0
            If DisplayAspectRatio > 0 Then Return DisplayAspectRatio
            Dim ret = Server.Info.Width / Server.Info.Height
            If UseSampleAspectRatio Then ret *= BitmapSampleAspectRatio
            Return ret
        End Get
    End Property

    Function GetTargetRect() As RectangleF
        Dim width As Single = Control.Width
        Dim height As Single = Control.Height

        If Not KeepAspectRatio OrElse Server.Info.Width = 0 OrElse Server.Info.Height = 0 OrElse height = 0 Then
            Return New RectangleF(0, 0, width, height)
        End If

        Dim videoAR = VideoAspectRatio

        If width / height > videoAR Then
            Dim w = CSng(height * videoAR)
            Return New RectangleF((width - w) / 2, 0, w, height)
        Else
            Dim h = CSng(width / videoAR)
            Return New RectangleF(0, (height - h) / 2, width, h)
        End If
    End Function

    Sub OnPaint(sender As Object, e As PaintEventArgs)
        Dim gx = e.Graphics
        gx.FillRectangle(BackColor.ToColor, 0, 0, Control.Width, Control.Height)

        Dim bmp = GetBitmap()

        If bmp Is Nothing OrElse Server.Info.Width = 0 OrElse Server.Info.Height = 0 Then
            Exit Sub
        End If

        gx.ImageInterpolation = ImageInterpolation.High

        Dim target = GetTargetRect()
        Dim scaleX = target.Width / Server.Info.Width
        Dim scaleY = target.Height / Server.Info.Height

        Dim sourceRect As New RectangleF(CropLeft, CropTop,
            Math.Max(1, Server.Info.Width - CropLeft - CropRight),
            Math.Max(1, Server.Info.Height - CropTop - CropBottom))

        Dim destRect As New RectangleF(target.X + CSng(CropLeft * scaleX), target.Y + CSng(CropTop * scaleY),
            CSng(target.Width - CropLeft * scaleX - CropRight * scaleX),
            CSng(target.Height - CropTop * scaleY - CropBottom * scaleY))

        gx.DrawImage(bmp, sourceRect, destRect)

        If ShowInfo Then
            DrawInfoText(gx)
        End If
    End Sub

    Sub DrawInfoText(gx As Graphics)
        Dim text = Info.GetInfoText(Position)
        Dim font = FontManager.GetCodeFont(-1)
        Dim size = gx.MeasureString(font, text)
        Dim padding = font.LineHeight / 3
        Dim left = font.LineHeight / 2

        gx.FillRectangle(Color.FromArgb(0, 0, 0, 180), left, left, size.Width + padding * 2, size.Height + padding * 2)
        gx.DrawText(font, Colors.White, left + padding, left + padding, text)
    End Sub

    Sub Dispose() Implements IDisposable.Dispose
        RemoveHandler Control.Paint, AddressOf OnPaint
        Bitmap?.Dispose()
        Bitmap = Nothing
    End Sub
End Class
