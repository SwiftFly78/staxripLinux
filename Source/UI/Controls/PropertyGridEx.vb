
Namespace UI
    'editor for the parameters of commands, shows one row per GridProperty
    Public Class PropertyGridEx
        Inherits Scrollable

        Event PropertyValueChanged(sender As Object, e As EventArgs)

        Private SelectedObjectValue As GridTypeDescriptor

        Sub New()
            ExpandContentWidth = True
            ExpandContentHeight = False
            Border = BorderType.Bezel
        End Sub

        Property SelectedObject As GridTypeDescriptor
            Get
                Return SelectedObjectValue
            End Get
            Set(value As GridTypeDescriptor)
                SelectedObjectValue = value
                Rebuild()
            End Set
        End Property

        Sub Rebuild()
            Dim table As New TableLayout With {.Spacing = New Size(8, 4), .Padding = New Padding(6)}

            If SelectedObjectValue IsNot Nothing Then
                For Each gp In SelectedObjectValue.Properties
                    Dim label As New Label With {.Text = gp.Name, .VerticalAlignment = VerticalAlignment.Center, .Wrap = WrapMode.None}

                    If gp.Description <> "" Then
                        label.ToolTip = gp.Description
                    End If

                    Dim editor = CreateEditor(gp)

                    If gp.Description <> "" Then
                        editor.ToolTip = gp.Description
                    End If

                    table.Rows.Add(New TableRow(New TableCell(label), New TableCell(editor, True)))
                Next
            End If

            table.Rows.Add(New TableRow With {.ScaleHeight = True})
            Content = table
        End Sub

        Sub OnValueChanged(gp As GridProperty, value As Object)
            gp.Value = value
            RaiseEvent PropertyValueChanged(Me, EventArgs.Empty)
        End Sub

        Function CreateEditor(gp As GridProperty) As Control
            Dim value = gp.Value

            If Not gp.DefaultValues.NothingOrEmpty Then
                Dim dd As New DropDown

                For Each i In gp.DefaultValues
                    dd.Items.Add(i.ToString)
                Next

                dd.SelectedIndex = Array.FindIndex(gp.DefaultValues, Function(i) i.Equals(value) OrElse i.ToString = value?.ToString)
                AddHandler dd.SelectedIndexChanged, Sub()
                                                       If dd.SelectedIndex > -1 Then
                                                           OnValueChanged(gp, gp.DefaultValues(dd.SelectedIndex))
                                                       End If
                                                   End Sub
                Return dd
            End If

            If TypeOf value Is Boolean Then
                Dim cb As New CheckBox With {.Checked = DirectCast(value, Boolean)}
                AddHandler cb.CheckedChanged, Sub() OnValueChanged(gp, cb.Checked.GetValueOrDefault)
                Return cb
            End If

            If value IsNot Nothing AndAlso value.GetType.IsEnum Then
                Dim values = System.Enum.GetValues(value.GetType).Cast(Of Object).ToArray
                Dim dd As New DropDown

                For Each i In values
                    dd.Items.Add(DispNameAttribute.GetValueForEnum(i))
                Next

                dd.SelectedIndex = Array.IndexOf(values, value)
                AddHandler dd.SelectedIndexChanged, Sub()
                                                       If dd.SelectedIndex > -1 Then
                                                           OnValueChanged(gp, values(dd.SelectedIndex))
                                                       End If
                                                   End Sub
                Return dd
            End If

            If TypeOf value Is Integer OrElse TypeOf value Is Long OrElse TypeOf value Is Double OrElse
                TypeOf value Is Single OrElse TypeOf value Is Decimal Then

                Dim isInt = TypeOf value Is Integer OrElse TypeOf value Is Long
                Dim ns As New NumericStepper With {
                    .MinValue = Integer.MinValue,
                    .MaxValue = Integer.MaxValue,
                    .DecimalPlaces = If(isInt, 0, 3),
                    .Value = Convert.ToDouble(value)}

                AddHandler ns.ValueChanged, Sub() OnValueChanged(gp, Convert.ChangeType(ns.Value, value.GetType))
                Return ns
            End If

            If TypeOf value Is String() Then
                Dim tb As New TextBox With {.ReadOnly = True, .Text = String.Join("; ", DirectCast(value, String()))}
                Dim bn As New Button With {.Text = "..."}
                AddHandler bn.Click, Sub()
                                         Dim lines = String.Join(BR, DirectCast(gp.Value, String()))
                                         Dim edited = EditLines(gp.Name, lines)

                                         If edited IsNot Nothing Then
                                             Dim arr = edited.SplitLinesNoEmpty
                                             tb.Text = String.Join("; ", arr)
                                             OnValueChanged(gp, arr)
                                         End If
                                     End Sub
                Return New TableLayout(New TableRow(New TableCell(tb, True), New TableCell(bn))) With {.Spacing = New Size(4, 0)}
            End If

            If TypeOf value Is String OrElse value Is Nothing Then
                Dim tb As New TextBox With {.Text = If(CStr(value), "")}
                AddHandler tb.TextChanged, Sub() OnValueChanged(gp, tb.Text)

                Dim editor = If(gp.TypeEditor Is Nothing, Nothing, TryCast(Activator.CreateInstance(gp.TypeEditor), IValueEditor))

                If editor Is Nothing Then
                    Return tb
                End If

                Dim bn As New Button With {.Text = "..."}
                AddHandler bn.Click, Sub()
                                         Dim ret = CStr(editor.EditValue(tb.Text))

                                         If ret IsNot Nothing Then
                                             tb.Text = ret
                                         End If
                                     End Sub
                Return New TableLayout(New TableRow(New TableCell(tb, True), New TableCell(bn))) With {.Spacing = New Size(4, 0)}
            End If

            Return New Label With {.Text = value.ToString, .VerticalAlignment = VerticalAlignment.Center}
        End Function

        Function EditLines(title As String, text As String) As String
            Using dialog As New DialogBase
                dialog.Text = title
                dialog.Resizable = True
                dialog.ClientSize = New Size(FontHeight * 30, FontHeight * 16)

                Dim ta As New TextArea With {.Text = text, .AcceptsReturn = True}
                Dim ok As New Button With {.Text = "OK"}
                Dim cancel As New Button With {.Text = "Cancel"}
                AddHandler ok.Click, Sub() dialog.Close(DialogResult.Ok)
                AddHandler cancel.Click, Sub() dialog.Close(DialogResult.Cancel)
                dialog.AbortButton = cancel
                dialog.PositiveButtons.Add(ok)
                dialog.NegativeButtons.Add(cancel)
                dialog.Content = New TableLayout(New TableRow(New TableCell(ta)) With {.ScaleHeight = True}) With {.Padding = New Padding(8)}

                If dialog.ShowDialog(Me) = DialogResult.Ok Then
                    Return ta.Text
                End If
            End Using

            Return Nothing
        End Function
    End Class
End Namespace
