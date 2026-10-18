
Imports StaxRip.UI

'quick settings of a video encoder shown in the main window
Public Class EncoderControlBase
    Inherits Panel

    Public WithEvents lv As ListViewEx
    Public WithEvents blConfigCodec As ButtonLabel
    Public WithEvents blConfigContainer As ButtonLabel
    Public WithEvents blCompCheck As ButtonLabel
    Public tblOverrideName As ToggleButtonLabel
    Public cms As ContextMenuStripEx

    Sub New()
        lv = New ListViewEx With {
            .View = View.Details,
            .HeaderStyle = ColumnHeaderStyle.None,
            .FullRowSelect = True,
            .MultiSelect = False,
            .ShowContextMenuOnLeftClick = True}

        lv.Columns.AddRange({New ColumnHeader, New ColumnHeader})
        cms = New ContextMenuStripEx
        lv.ContextMenuStrip = cms

        blConfigCodec = New ButtonLabel With {.Text = "Options"}
        blConfigContainer = New ButtonLabel With {.Text = "Container Options"}
        blCompCheck = New ButtonLabel With {.Text = "Run Compressibility Check"}
        tblOverrideName = New ToggleButtonLabel With {.Text = "Target Name Override"}

        Dim bottom As New TableLayout(New TableRow(
            New TableCell(blConfigCodec),
            New TableCell(Nothing, True),
            New TableCell(tblOverrideName),
            New TableCell(Nothing, True),
            New TableCell(blConfigContainer))) With {.Spacing = New Size(FH(0.3), 0)}

        Content = New TableLayout(
            New TableRow(lv) With {.ScaleHeight = True},
            New TableRow(blCompCheck),
            New TableRow(bottom)) With {.Spacing = New Size(0, FH(0.15))}
    End Sub

    Protected Overrides Sub OnSizeChanged(e As EventArgs)
        MyBase.OnSizeChanged(e)

        If Width > 0 Then
            lv.Columns(0).Width = CInt(Width * 0.32)
        End If
    End Sub
End Class
