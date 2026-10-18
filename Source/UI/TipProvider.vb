
Namespace UI
    'Shows short help texts as tooltip and the full help text on right-click
    Public Class TipProvider
        Implements IDisposable

        Private TipTitles As New Dictionary(Of Control, String)
        Private TipTexts As New Dictionary(Of Control, String)
        Private Registered As New HashSet(Of Control)

        Property TipsFunc As Func(Of StringPairList)

        Sub New(Optional component As Object = Nothing)
        End Sub

        Sub Dispose() Implements IDisposable.Dispose
            TipTexts.Clear()
            TipTitles.Clear()
        End Sub

        Shared Function FormatTip(value As String) As String
            If value = "" Then
                Return Nothing
            End If

            value = value.TrimEnd("."c)

            If HelpDocument.MustConvert(value) Then
                value = HelpDocument.ConvertMarkup(value, True)
            End If

            If value.Length > 400 Then
                value = value.Substring(0, 400) + "..." + BR + "(right-click for full help)"
            End If

            Return value
        End Function

        Function GetTipText(ctrl As Control) As String
            If TipTexts.ContainsKey(ctrl) Then
                Return TipTexts(ctrl)
            End If

            Return ""
        End Function

        Sub SetTipText(c As Control, value As String)
            TipTexts(c) = value
            Init(value, c)
        End Sub

        Sub SetTip(tipText As String, tipTitle As String, c As Control)
            TipTitles(c) = tipTitle
            SetTipText(c, tipText)
        End Sub

        Sub SetTip(tipText As String, ParamArray controls As Control())
            If tipText = "" Then Exit Sub

            Dim title = ""

            For Each ctrl In controls
                Dim text = GetControlText(ctrl)

                If (TypeOf ctrl Is Label OrElse TypeOf ctrl Is CheckBox) AndAlso text <> "" Then
                    title = FormatName(text)
                End If
            Next

            For Each ctrl In controls
                TipTexts(ctrl) = tipText

                If title <> "" Then
                    TipTitles(ctrl) = title
                End If

                Init(tipText, ctrl)
            Next
        End Sub

        Shared Function GetControlText(ctrl As Control) As String
            If TypeOf ctrl Is Label Then Return DirectCast(ctrl, Label).Text
            If TypeOf ctrl Is CheckBox Then Return DirectCast(ctrl, CheckBox).Text
            If TypeOf ctrl Is ButtonEx Then Return DirectCast(ctrl, ButtonEx).Text
            If TypeOf ctrl Is Button Then Return DirectCast(ctrl, Button).Text
            If TypeOf ctrl Is LinkButton Then Return DirectCast(ctrl, LinkButton).Text
            Return ""
        End Function

        Sub Init(tipText As String, control As Control)
            control.ToolTip = FormatTip(tipText)

            If Not Registered.Contains(control) Then
                Registered.Add(control)
                AddHandler control.MouseDown, AddressOf TipMouseDown
            End If
        End Sub

        Sub TipMouseDown(sender As Object, e As MouseEventArgs)
            Dim ctrl = DirectCast(sender, Control)

            If e.Buttons = MouseButtons.Alternate AndAlso Not HasContextMenu(ctrl) Then
                e.Handled = True
                ShowHelp(ctrl)
            End If
        End Sub

        Sub ShowHelp(ctrl As Control)
            Dim tip = GetTip(ctrl)
            g.ShowHelp(tip.Name, tip.Value)
        End Sub

        Function GetTip(ctrl As Control) As StringPair
            Dim ret As New StringPair

            If TipTitles.ContainsKey(ctrl) Then
                ret.Name = FormatName(TipTitles(ctrl))
            Else
                ret.Name = FormatName(GetControlText(ctrl))
            End If

            If TipTexts.ContainsKey(ctrl) Then
                ret.Value = TipTexts(ctrl)
            End If

            Return ret
        End Function

        Function HasContextMenu(ctrl As Control) As Boolean
            If TypeOf ctrl Is TextControl OrElse TypeOf ctrl Is TextEdit OrElse TypeOf ctrl Is NumericStepper Then
                Return True
            End If

            Return ctrl.ContextMenu IsNot Nothing
        End Function

        Shared Function FormatName(value As String) As String
            If value Is Nothing Then
                Return ""
            End If

            value = value.Trim

            If value.Contains("&") AndAlso Not value.Contains(" & ") Then
                value = value.Replace("&", "")
            End If

            If value.EndsWith("...") Then
                value = value.TrimEnd("."c)
            End If

            If value.EndsWith(":") Then
                value = value.TrimEnd(":"c)
            End If

            Return value
        End Function

        Function GetTips() As StringPairList
            Dim ret As New StringPairList
            Dim temp As New List(Of String)

            For Each ctrl In TipTexts.Keys
                If Not ctrl.IsDisposed AndAlso ctrl.Visible Then
                    Dim pair As New StringPair
                    pair.Value = TipTexts(ctrl)

                    If TipTitles.ContainsKey(ctrl) Then
                        pair.Name = FormatName(TipTitles(ctrl))
                    Else
                        pair.Name = FormatName(GetControlText(ctrl))
                    End If

                    If Not temp.Contains(pair.Name) Then
                        temp.Add(pair.Name)
                        ret.Add(pair)
                    End If
                End If
            Next

            If TipsFunc IsNot Nothing Then
                ret.AddRange(TipsFunc.Invoke)
            End If

            ret.Sort()

            Return ret
        End Function
    End Class
End Namespace
