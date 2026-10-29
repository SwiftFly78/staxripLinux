
Namespace UI
    'helpers to build Eto layouts with spacing based on the font height
    Public Module LayoutHelp
        Function FH(factor As Single) As Integer
            Return CInt(EtoHelp.FontHeight * factor)
        End Function

        ReadOnly Property DefaultSpacing As Size
            Get
                Return New Size(FH(0.5), FH(0.35))
            End Get
        End Property

        ReadOnly Property DefaultPadding As Padding
            Get
                Return New Padding(FH(0.7))
            End Get
        End Property

        Function HRow(ParamArray controls As Control()) As StackLayout
            Dim ret As New StackLayout With {
                .Orientation = Orientation.Horizontal,
                .Spacing = DefaultSpacing.Width,
                .VerticalContentAlignment = VerticalAlignment.Center}

            For Each i In controls
                If i IsNot Nothing Then
                    ret.Items.Add(New StackLayoutItem(i))
                End If
            Next

            Return ret
        End Function

        'row with controls aligned to the right, used for dialog buttons
        Function ButtonRow(ParamArray controls As Control()) As TableLayout
            Dim cells As New List(Of TableCell) From {New TableCell(Nothing, True)}

            For Each i In controls
                If i IsNot Nothing Then
                    cells.Add(New TableCell(i))
                End If
            Next

            Return New TableLayout(New TableRow(cells)) With {.Spacing = DefaultSpacing}
        End Function

        'row with left and right aligned controls
        Function SplitRow(left As Control(), right As Control()) As TableLayout
            Dim cells As New List(Of TableCell)

            For Each i In left
                If i IsNot Nothing Then
                    cells.Add(New TableCell(i))
                End If
            Next

            cells.Add(New TableCell(Nothing, True))

            For Each i In right
                If i IsNot Nothing Then
                    cells.Add(New TableCell(i))
                End If
            Next

            Return New TableLayout(New TableRow(cells)) With {.Spacing = DefaultSpacing}
        End Function

        Function ScaledRow(ParamArray cells As Object()) As TableRow
            Return New TableRow(ToCells(cells)) With {.ScaleHeight = True}
        End Function

        Function Row(ParamArray cells As Object()) As TableRow
            Return New TableRow(ToCells(cells))
        End Function

        Private Function ToCells(cells As Object()) As List(Of TableCell)
            Dim ret As New List(Of TableCell)

            For Each i In cells
                If TypeOf i Is TableCell Then
                    ret.Add(DirectCast(i, TableCell))
                Else
                    ret.Add(New TableCell(DirectCast(i, Control)))
                End If
            Next

            Return ret
        End Function

        Function Scaled(control As Control) As TableCell
            Return New TableCell(control, True)
        End Function

        Function Table(ParamArray rows As TableRow()) As TableLayout
            Return New TableLayout(rows) With {.Spacing = DefaultSpacing}
        End Function

        Function PaddedTable(ParamArray rows As TableRow()) As TableLayout
            Return New TableLayout(rows) With {.Spacing = DefaultSpacing, .Padding = DefaultPadding}
        End Function

        'the bottom row of a dialog with its buttons, separated from the content by a line
        Function CreateActionArea(content As Control) As Panel
            Dim ret As New Panel With {.Content = content, .Padding = New Padding(FH(0.7))}
            ret.AddStyleClass("staxrip-action-area")
            Return ret
        End Function

        'the padded content of a dialog above the action area with its buttons
        Function DialogContent(actions As Control, ParamArray rows As TableRow()) As TableLayout
            Return New TableLayout(
                New TableRow(PaddedTable(rows)) With {.ScaleHeight = True},
                New TableRow(CreateActionArea(actions)))
        End Function

        Function CreateButton(text As String, Optional result As DialogResult = DialogResult.None) As ButtonEx
            Return New ButtonEx With {.Text = text, .DialogResult = result, .MinimumSize = New Size(FH(6), 0)}
        End Function
    End Module
End Namespace
