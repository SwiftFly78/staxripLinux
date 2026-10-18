
Imports StaxRip.UI

Public Class CodeForm
    Inherits FormBase

    Public rtb As RichTextBoxEx

    Property Find As String
    Property CodeText As String

    Sub New(text As String, find As String, Optional wordwrap As Boolean = False)
        rtb = New RichTextBoxEx With {.ReadOnly = True, .WordWrap = wordwrap, .Font = FontManager.GetCodeFont()}
        rtb.Text = text
        Content = rtb

        RestoreClientSize(50, 35)

        Me.Find = find
        CodeText = text
    End Sub

    Sub CodeForm_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        If Find <> "" Then
            FindByIndex(0)
        End If
    End Sub

    Sub FindByIndex(startIndex As Integer)
        If startIndex >= CodeText.Length Then
            Exit Sub
        End If

        Dim found = rtb.Find(Find, startIndex)

        If found < 0 Then
            Exit Sub
        End If

        Dim start = found

        If start > startIndex Then
            While True
                start -= 1

                If start = 0 Then
                    Exit While
                End If

                If CodeText(start) = BR(1) Then
                    Exit While
                End If
            End While

            If (found - start) > 15 Then
                FindByIndex(found + Find.Length)
            End If
        End If
    End Sub

    Sub CodeForm_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If e.KeyData = Keys.Escape Then
            Close()
        End If
    End Sub
End Class
