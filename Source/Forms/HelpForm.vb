
'help documents are written to a temp HTML file and shown with the default browser
Public Class HelpForm
    Implements IDisposable

    Private DocumentValue As HelpDocument
    Private DocumentPath As String

    ReadOnly Property Doc As HelpDocument
        Get
            If DocumentValue Is Nothing Then
                DocumentPath = Path.Combine(Folder.Temp, "staxrip-help-" + Guid.NewGuid.ToString("N").Substring(0, 12) + ".htm")
                Dim filePath = DocumentPath
                AddHandler AppDomain.CurrentDomain.ProcessExit, Sub() FileHelp.Delete(filePath)
                DocumentValue = New HelpDocument(filePath)
            End If

            Return DocumentValue
        End Get
    End Property

    Overloads Shared Sub ShowDialog(heading As String, tips As StringPairList)
        ShowDialog(heading, tips, Nothing)
    End Sub

    Overloads Shared Sub ShowDialog(heading As String, tips As StringPairList, summary As String)
        Dim form As New HelpForm()
        form.Doc.WriteStart(heading)

        If summary IsNot Nothing Then
            form.Doc.WriteParagraph(summary)
        End If

        form.Doc.WriteTips(tips)
        form.Show()
    End Sub

    Sub Show()
        If DocumentValue Is Nothing Then
            Exit Sub
        End If

        DocumentValue.WriteDocument()
        g.ShellExecute(DocumentPath)
    End Sub

    Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class
