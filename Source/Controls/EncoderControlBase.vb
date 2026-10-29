
Imports StaxRip.UI

'quick settings of a video encoder shown in the main window
Public Class EncoderControlBase
    Inherits Panel

    Public WithEvents lv As ListViewEx
    Public WithEvents blCompCheck As ButtonEx
    Public cbOverrideName As ToggleCheckBox
    Public cms As ContextMenuStripEx

    Sub New()
        lv = New ListViewEx With {
            .View = View.Details,
            .HeaderStyle = ColumnHeaderStyle.None,
            .FullRowSelect = True,
            .MultiSelect = False,
            .ShowContextMenuOnLeftClick = True,
            .DimFirstColumn = True,
            .EllipsizeLastColumn = True,
            .Border = BorderType.None}

        lv.Columns.AddRange({New ColumnHeader, New ColumnHeader})
        cms = New ContextMenuStripEx
        lv.ContextMenuStrip = cms

        blCompCheck = New ButtonEx With {.Text = "Run Compressibility Check", .MinimumSize = Size.Empty}
        cbOverrideName = New ToggleCheckBox With {.Text = "Override target name"}

        Dim bottom As New TableLayout(New TableRow(
            New TableCell(cbOverrideName),
            New TableCell(Nothing, True),
            New TableCell(blCompCheck))) With {.Spacing = New Size(FH(0.5), 0)}

        Content = New TableLayout(
            New TableRow(lv) With {.ScaleHeight = True},
            New TableRow(bottom)) With {.Spacing = New Size(0, FH(0.35))}
    End Sub

    Protected Overrides Sub OnSizeChanged(e As EventArgs)
        MyBase.OnSizeChanged(e)

        If Width > 0 Then
            lv.Columns(0).Width = CInt(Width * 0.32)
        End If
    End Sub
End Class
