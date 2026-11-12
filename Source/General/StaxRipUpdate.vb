
Imports System.Net.Http
Imports System.Text.Json
Imports System.Text.RegularExpressions
Imports System.Threading.Tasks

Imports Microsoft.VisualBasic
Imports StaxRip.UI

'checks the releases of StaxRipLinux on GitHub and downloads the AppImage of a newer version
Public Class StaxRipUpdate
    Shared ReadOnly HttpClient As HttpClient = CreateHttpClient()

    Class Release
        Property Tag As String
        Property Version As Version
        Property Page As String
        Property Assets As New List(Of ReleaseAsset)
    End Class

    Class ReleaseAsset
        Property Name As String
        Property Url As String
        Property Size As Long
    End Class

    Shared Function CreateHttpClient() As HttpClient
        Dim ret As New HttpClient With {.Timeout = TimeSpan.FromSeconds(30)}
        'the GitHub API rejects requests without user agent
        ret.DefaultRequestHeaders.UserAgent.ParseAdd("StaxRipLinux")
        Return ret
    End Function

    Shared Sub SetFirstRunOnCurrentVersion()
        Dim key = g.DefaultCommands.GetApplicationDetails()
        If s.FirstRunOnVersion.Key <> key Then
            s.FirstRunOnVersion = New KeyValuePair(Of String, Date)(key, Date.Now)
        End If
    End Sub

    Shared Sub ShowUpdateQuestion()
        If Not g.IsDevelopmentPC AndAlso s.CheckForUpdatesQuestion Then
            Using td As New TaskDialog(Of String)()
                td.Title = "Check for updates"
                td.Icon = TaskIcon.Question
                td.Content = "Would you like StaxRipLinux to check for new versions periodically?" + BR +
                             "Each time it is checked, only the releases of StaxRipLinux on GitHub are queried."

                td.AddCommand("Yes")
                td.AddCommand("No")
                td.AddCommand("Ask me later")

                Dim answer = td.Show
                s.CheckForUpdatesQuestion = Not answer.EqualsAny("Yes", "No")
                s.CheckForUpdates = answer = "Yes"
            End Using
        End If
    End Sub

    'the version of this build, builds after a version or with changes count as this version
    Shared Function GetCurrentVersion() As Version
        Return ParseVersion(BuildInfo.PortVersion)
    End Function

    Shared Function ParseVersion(value As String) As Version
        Dim match = Regex.Match(If(value, ""), "^v?(\d+)\.(\d+)\.(\d+)")

        If match.Success Then
            Return New Version(match.Groups(1).Value.ToInt, match.Groups(2).Value.ToInt, match.Groups(3).Value.ToInt)
        End If

        Return Nothing
    End Function

    Shared Async Function GetLatestReleaseAsync() As Task(Of Release)
        Dim json = Await HttpClient.GetStringAsync(ProjectLinks.LatestReleaseApi)
        Dim ret As New Release

        Using doc = JsonDocument.Parse(json)
            Dim root = doc.RootElement
            ret.Tag = root.GetProperty("tag_name").GetString()
            ret.Page = root.GetProperty("html_url").GetString()
            ret.Version = ParseVersion(ret.Tag)

            For Each asset In root.GetProperty("assets").EnumerateArray()
                ret.Assets.Add(New ReleaseAsset With {
                    .Name = asset.GetProperty("name").GetString(),
                    .Url = asset.GetProperty("browser_download_url").GetString(),
                    .Size = asset.GetProperty("size").GetInt64()})
            Next
        End Using

        Return ret
    End Function

    'the AppImage file of this instance, empty when StaxRipLinux doesn't run as AppImage
    Shared ReadOnly Property AppImagePath As String
        Get
            Return If(Environment.GetEnvironmentVariable("APPIMAGE"), "")
        End Get
    End Property

    'the full AppImage contains the tools in the Apps folder, the slim one uses the tools of the system
    Shared Function GetAppImage(release As Release, full As Boolean) As ReleaseAsset
        Return release.Assets.FirstOrDefault(Function(i) i.Name.EndsWith(".AppImage") AndAlso i.Name.Contains("-slim-") <> full)
    End Function

    Shared Async Sub CheckForUpdateAsync(Optional force As Boolean = False)
        If Not s.CheckForUpdates AndAlso Not force Then Exit Sub

        SetFirstRunOnCurrentVersion()

        Dim hours = Conversion.Fix((DateTime.Now - s.FirstRunOnVersion.Value).TotalHours)
        Dim diffHoursToCheck = 24
        diffHoursToCheck = If(hours < 96, 12, diffHoursToCheck)
        diffHoursToCheck = If(hours < 72, 9, diffHoursToCheck)
        diffHoursToCheck = If(hours < 48, 6, diffHoursToCheck)
        diffHoursToCheck = If(hours < 24, 3, diffHoursToCheck)

        Dim proceed = (Date.Now - s.CheckForUpdatesLastRequest).TotalHours >= diffHoursToCheck

        If Not (proceed OrElse force) Then Exit Sub

        'exceptions of an Async Sub are unhandled and terminate StaxRip
        Try
            Dim current = GetCurrentVersion()

            If current Is Nothing Then
                If force Then
                    MsgInfo("The version of this build is unknown.", "Only builds from a git clone with the tags of StaxRipLinux have a version.")
                End If

                Exit Sub
            End If

            Dim release = Await GetLatestReleaseAsync()
            s.CheckForUpdatesLastRequest = DateTime.Now
            Dim dismissed = ParseVersion(s.CheckForUpdatesDismissed)

            If release.Version Is Nothing OrElse release.Version <= current OrElse
                (Not force AndAlso dismissed IsNot Nothing AndAlso dismissed >= release.Version) Then

                If force Then
                    MsgInfo("No update available.", $"This is StaxRipLinux {BuildInfo.PortVersion}, the newest release is {release.Tag}.")
                End If

                Exit Sub
            End If

            ShowUpdate(release, "", Directory.Exists(Folder.Apps), True)
        Catch ex As Exception
            If force Then
                MsgError("Checking for updates failed.", ex.Message)
            End If
        End Try
    End Sub

    'tools of the full AppImage are updated with new versions of StaxRipLinux
    Shared Async Sub UpdateToolAsync(pack As Package)
        If pack.IsSystemInstalled Then
            MsgInfo(pack.Name + " is installed system wide, please update it with the package manager.", pack.Path)
            Exit Sub
        End If

        Try
            Dim release = Await GetLatestReleaseAsync()
            Dim current = GetCurrentVersion()
            Dim isNewer = release.Version IsNot Nothing AndAlso (current Is Nothing OrElse release.Version > current)
            Dim version = If(pack.Version <> "", " " + pack.Version, "")

            If pack.IsBundled Then
                If isNewer Then
                    ShowUpdate(release, $"{pack.Name}{version} is part of the AppImage, new versions of StaxRipLinux update it.", True, False)
                Else
                    MsgInfo($"{pack.Name} is up to date.", $"{pack.Name}{version} is part of StaxRipLinux {BuildInfo.PortVersion}, the newest version.")
                End If
            ElseIf isNewer Then
                ShowUpdate(release, $"StaxRipLinux doesn't update {pack.Name}. The full AppImage contains most tools, new versions of StaxRipLinux update them.", True, False)
            Else
                MsgInfo($"StaxRipLinux doesn't update {pack.Name}.", "The full AppImage contains most tools, new versions of StaxRipLinux update them. " +
                        "Tools of the system are updated with the package manager.")
            End If
        Catch ex As Exception
            MsgError("Checking for updates failed.", ex.Message)
        End Try
    End Sub

    'offers the AppImage of a newer release, the full one or the one that matches this instance
    Shared Sub ShowUpdate(release As Release, note As String, full As Boolean, canDismiss As Boolean)
        Dim appImage = GetAppImage(release, full)

        Using td As New TaskDialog(Of String)
            td.Title = $"StaxRipLinux {release.Tag} is available"
            td.Icon = TaskIcon.Shield
            td.Content = If(note <> "", note + BR2, "") + $"This is StaxRipLinux {BuildInfo.PortVersion}."

            If appImage IsNot Nothing AndAlso AppImagePath <> "" Then
                Dim size = $"{appImage.Size / PrefixedSize(2).Factor:0} {PrefixedSize(2).Unit}"
                td.AddCommand("Download the AppImage", $"{appImage.Name}, {size}", "download")
            End If

            td.AddCommand("Open the release page", "The changes and the AppImages of the release", "release")

            If canDismiss Then
                td.AddCommand($"Dismiss {release.Tag}", "Don't show this version again", "dismiss")
            End If

            Select Case td.Show()
                Case "download"
                    DownloadAppImage(release, appImage)
                Case "release"
                    g.ShellExecute(release.Page)
                Case "dismiss"
                    s.CheckForUpdatesDismissed = release.Version.ToString()
            End Select
        End Using
    End Sub

    Shared Sub DownloadAppImage(release As Release, appImage As ReleaseAsset)
        Try
            DownloadAppImageFile(release, appImage)
        Catch ex As Exception
            g.ShowException(ex, "Downloading the AppImage failed")
        End Try
    End Sub

    Private Shared Sub DownloadAppImageFile(release As Release, appImage As ReleaseAsset)
        Dim target As String

        Using dialog As New SaveFileDialog
            dialog.Title = "Save the AppImage"
            dialog.SetInitDir(Path.GetDirectoryName(AppImagePath))
            dialog.FileName = appImage.Name

            If dialog.ShowDialog() <> DialogResult.OK Then
                Exit Sub
            End If

            target = dialog.FileName
        End Using

        'renamed when it's complete, a running AppImage of the same name keeps its file
        Dim partFile = target + ".part"

        Using form As New DownloadForm(appImage.Url, partFile)
            If form.ShowDialog() <> DialogResult.OK OrElse Not File.Exists(partFile) Then
                Exit Sub
            End If
        End Using

        File.Move(partFile, target, True)
        File.SetUnixFileMode(target, File.GetUnixFileMode(target) Or
            UnixFileMode.UserExecute Or UnixFileMode.GroupExecute Or UnixFileMode.OtherExecute)

        Dim content = $"It was saved as {target}." + BR2 + "Close StaxRipLinux and start the new AppImage."

        'the settings folder next to an AppImage belongs to its file name
        Dim config = AppImagePath + ".config"
        Dim newConfig = target + ".config"

        If Directory.Exists(config) AndAlso Not Directory.Exists(newConfig) Then
            Try
                FolderHelp.CopyDirectory(config, newConfig)
                content += BR2 + $"The settings were copied to {newConfig}."
            Catch ex As Exception
                content += BR2 + $"Copying the settings to {newConfig} failed: {ex.Message}"
            End Try
        End If

        Using td As New TaskDialog(Of String)
            td.Title = $"StaxRipLinux {release.Tag} was downloaded"
            td.Icon = TaskIcon.Info
            td.Content = content
            td.AddCommand("Show in File Manager", "explore")
            td.AddButton("OK", "")

            If td.Show() = "explore" Then
                g.SelectFileWithExplorer(target)
            End If
        End Using
    End Sub
End Class
