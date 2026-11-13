
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

    'the text of the info overlay, the info of the script if not set
    Property InfoTextFunc As Func(Of String)

    'the video of this renderer is drawn right of the split position, which is 0 to 1 of the width
    Property SplitRenderer As VideoRenderer
    Property SplitPosition As Single = 0.5

    'the name shown in the split view when there is no info overlay
    Property Title As String

    'scale of the video instead of fitting it into the control, 1 shows a video pixel per screen pixel
    Property Zoom As Single

    'offset of the zoomed video from the center of the control
    Property Offset As PointF

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

        If Zoom > 0 AndAlso Server.Info.Height > 0 Then
            Dim pixelSize = If(Control.ParentWindow?.LogicalPixelSize, 1.0F)
            Dim h = Server.Info.Height * Zoom / pixelSize
            Dim w = CSng(h * VideoAspectRatio)

            'the video can't be moved further than its edges
            Dim maxX = Math.Max(0, (w - width) / 2)
            Dim maxY = Math.Max(0, (h - height) / 2)
            Offset = New PointF(Math.Max(-maxX, Math.Min(maxX, Offset.X)), Math.Max(-maxY, Math.Min(maxY, Offset.Y)))

            Return New RectangleF((width - w) / 2 + Offset.X, (height - h) / 2 + Offset.Y, w, h)
        End If

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

        Dim split = SplitRenderer
        Dim splitBitmap = split?.GetBitmap()

        'the other video is scaled to the same rectangle, so the split line shows the same image part of both
        If splitBitmap IsNot Nothing AndAlso split.Server.Info.Width > 0 AndAlso split.Server.Info.Height > 0 Then
            Dim position = Math.Max(0, Math.Min(1, SplitPosition))
            Dim splitX = target.X + target.Width * position
            Dim w = split.Server.Info.Width
            Dim h = split.Server.Info.Height

            gx.DrawImage(splitBitmap, New RectangleF(w * position, 0, Math.Max(1, w * (1 - position)), h),
                New RectangleF(splitX, target.Y, target.Width * (1 - position), target.Height))

            gx.DrawLine(New Pen(Colors.White, 2), splitX, target.Y, splitX, target.Bottom)
        End If

        Dim font = FontManager.GetCodeFont(-1)

        If ShowInfo Then
            DrawInfoText(gx)

            If splitBitmap IsNot Nothing Then
                DrawInfoText(gx, split.GetInfoText(), font, Control.Width)
            End If
        ElseIf splitBitmap IsNot Nothing Then
            DrawInfoText(gx, Title, font)
            DrawInfoText(gx, split.Title, font, Control.Width)
        End If
    End Sub

    Function GetInfoText() As String
        Return If(InfoTextFunc IsNot Nothing, InfoTextFunc.Invoke(), Info.GetInfoText(Position))
    End Function

    Sub DrawInfoText(gx As Graphics)
        DrawInfoText(gx, GetInfoText(), FontManager.GetCodeFont(-1))
    End Sub

    'draws the text in a dark box at the top left, or at the top right of the given width
    Shared Sub DrawInfoText(gx As Graphics, text As String, font As Font, Optional rightEdge As Single = 0)
        If text = "" Then Exit Sub

        Dim size = gx.MeasureString(font, text)
        Dim padding = font.LineHeight / 3
        Dim margin = font.LineHeight / 2
        Dim boxWidth = size.Width + padding * 2
        Dim left = If(rightEdge > 0, rightEdge - margin - boxWidth, margin)

        gx.FillRectangle(Color.FromArgb(0, 0, 0, 180), left, margin, boxWidth, size.Height + padding * 2)
        gx.DrawText(font, Colors.White, left + padding, margin + padding, text)
    End Sub

    Sub Dispose() Implements IDisposable.Dispose
        RemoveHandler Control.Paint, AddressOf OnPaint
        Bitmap?.Dispose()
        Bitmap = Nothing
    End Sub
End Class
