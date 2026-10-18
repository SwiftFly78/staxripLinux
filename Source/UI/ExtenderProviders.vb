
Imports System.Text.RegularExpressions

Namespace UI
    'validates the text of text edits with a regex pattern when they lose the focus
    Public Class ValidationProvider
        Private Patterns As New Dictionary(Of TextEdit, String)
        Private Defaults As New Dictionary(Of TextEdit, String)

        Function GetPattern(c As TextEdit) As String
            If Patterns.ContainsKey(c) Then
                Return Patterns(c)
            End If

            Return ""
        End Function

        Sub SetPattern(c As TextEdit, value As String)
            If Not Patterns.ContainsKey(c) Then
                AddHandler c.LostFocus, AddressOf Validating
                AddHandler c.GotFocus, AddressOf Enter
            End If

            Patterns(c) = value
        End Sub

        Sub Validating(sender As Object, e As EventArgs)
            Dim c = DirectCast(sender, TextEdit)

            If Not Regex.IsMatch(c.Text, Patterns(c)) Then
                MsgWarn("""" + c.Text + """ is no valid input.")

                If Defaults.ContainsKey(c) Then
                    c.Text = Defaults(c)
                End If
            End If
        End Sub

        Sub Enter(sender As Object, e As EventArgs)
            Dim c = DirectCast(sender, TextEdit)
            Defaults(c) = c.Text
        End Sub
    End Class
End Namespace
