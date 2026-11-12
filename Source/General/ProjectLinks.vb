Imports StaxRip.UI

'the pages of StaxRipLinux on GitHub
Public Class ProjectLinks
    Public Const Repository As String = "https://github.com/SwiftFly78/staxripLinux"
    Public Const Documentation As String = Repository & "/blob/master/Docs/README.md"
    Public Const Releases As String = Repository & "/releases"
    Public Const NewIssue As String = Repository & "/issues/new/choose"
    Public Const Support As String = Repository & "?tab=readme-ov-file#contribution--support"
    Public Const LatestReleaseApi As String = "https://api.github.com/repos/SwiftFly78/staxripLinux/releases/latest"

    'a page of the documentation, like Usage/Macros.md
    Shared Function GetDocumentationPage(page As String) As String
        Return Repository & "/blob/master/Docs/" & page
    End Function

    'the issue form for tool updates with the name and version of the tool filled in
    Shared Function GetToolUpdateRequest(pack As Package) As String
        Dim ret = Repository & "/issues/new?template=03-update_request.yml" &
            "&title=" & Uri.EscapeDataString("Update " & pack.Name) &
            "&name=" & Uri.EscapeDataString(pack.Name)

        If pack.Version <> "" Then
            ret &= "&oldversion=" & Uri.EscapeDataString(pack.Version)
        End If

        Return ret
    End Function

    'the menus of older settings link the pages of StaxRip
    Shared ReadOnly Property ReplacedLinks As New Dictionary(Of String, String) From {
        {"https://github.com/staxrip/staxrip", Repository},
        {"https://github.com/staxrip/staxrip/tree/master/Docs/README.md", Documentation},
        {"https://github.com/staxrip/staxrip/blob/master/CHANGELOG.md", Releases},
        {"https://github.com/staxrip/staxrip?tab=readme-ov-file#contribution--support", Support},
        {"https://github.com/staxrip/staxrip/issues/new/choose", NewIssue}
    }

    Shared Sub ReplaceLinks(item As CustomMenuItem)
        If item Is Nothing Then
            Exit Sub
        End If

        Dim params = item.Parameters
        Dim link As String = Nothing

        If params.Count > 0 AndAlso TypeOf params(0) Is String AndAlso ReplacedLinks.TryGetValue(CStr(params(0)), link) Then
            params(0) = link
        End If

        For Each i In item.SubItems
            ReplaceLinks(i)
        Next
    End Sub
End Class
