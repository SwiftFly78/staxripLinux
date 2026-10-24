
Imports System.Net.Http
Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions

Imports Microsoft.VisualBasic
Imports StaxRip.UI

Public Class ToolUpdate
    Property Package As Package
    Property DownloadFile As String
    Property ExtractDir As String
    Property TargetDir As String
    Property UseCurl As Boolean

    Private HttpClient As New HttpClient
    Private UpdateUI As IUpdateUI

    Sub New(pack As Package, updateUI As IUpdateUI)
        Package = pack
        TargetDir = pack.Directory

        If TargetDir = "" Then
            TargetDir = Path.Combine(Folder.Apps, "Tools", pack.Name)
        End If
        Me.UpdateUI = updateUI
    End Sub

    Async Sub Update()
        If Package.IsSystemInstalled Then
            MsgInfo(Package.Name + " is installed system wide, please update it with the package manager.", Package.Path)
            Exit Sub
        End If

        'exceptions of an Async Sub are unhandled and terminate StaxRip
        Try
            Dim content = Await HttpClient.GetStringAsync(Package.DownloadURL)
            Dim matches = Regex.Matches(content, "href=(""|')[^ ]+\.(7z|zip|tar\.gz|tar\.xz|tar\.bz2|tar\.zst|tgz)(""|')")

            For Each match As Match In matches
                Dim url = match.Value

                If Ignore(url) Then Continue For
                If Package.Include <> "" AndAlso Not url.Contains(Package.Include) Then Continue For

                url = url.Substring(6, url.Length - 7)

                If Not url.StartsWith("http") Then
                    Dim match2 = Regex.Match(Package.DownloadURL, "https?://[^/]+")
                    url = match2.Value + If(url.StartsWith("/"), "", "/") + url
                End If

                DownloadFile = IO.Path.Combine(Folder.Desktop, IO.Path.GetFileName(url))
                Download(url)
                Exit For
            Next
        Catch ex As Exception
            g.ShowException(ex, "Update of " + Package.Name + " failed")
        End Try
    End Sub

    Sub Download(url As String)
        'TaskDialog trims URLs
        If MsgQuestion("Download the file shown below?", url) = DialogResult.OK Then

            Using form As New DownloadForm(url, DownloadFile)
                If form.ShowDialog() = DialogResult.OK AndAlso DownloadFile.FileExists Then
                    If DownloadFile.FileExists Then
                        Extract()
                    Else
                        MsgError("Downloaded file is missing.")
                    End If
                Else
                    FileHelp.Delete(DownloadFile)
                    MsgInfo("Download was canceled or failed.")
                End If
            End Using
        End If
    End Sub

    Sub Extract()
        Dim isTar = DownloadFile.ToLowerInvariant.EndsWithAny(".tar.gz", ".tar.xz", ".tar.bz2", ".tar.zst", ".tgz")

        If Not isTar AndAlso DownloadFile.Ext <> "7z" AndAlso DownloadFile.Ext <> "zip" Then
            Exit Sub
        End If

        Dim base = DownloadFile.Base

        If base.ToLowerInvariant.EndsWith(".tar") Then
            base = base.Substring(0, base.Length - 4)
        End If

        ExtractDir = Path.Combine(DownloadFile.Dir, base)
        Directory.CreateDirectory(ExtractDir)

        Using pr As New Process
            If isTar Then
                pr.StartInfo.FileName = "tar"
                pr.StartInfo.ArgumentList.Add("-xf")
                pr.StartInfo.ArgumentList.Add(DownloadFile)
                pr.StartInfo.ArgumentList.Add("-C")
                pr.StartInfo.ArgumentList.Add(ExtractDir)
            Else
                If Package.SevenZip.Path = "" Then
                    UpdatePackageDialog()
                    MsgError("7-Zip (7z) is required to extract " + DownloadFile.FileName)
                    Exit Sub
                End If

                pr.StartInfo.FileName = Package.SevenZip.Path
                pr.StartInfo.ArgumentList.Add("x")
                pr.StartInfo.ArgumentList.Add("-y")
                pr.StartInfo.ArgumentList.Add(DownloadFile)
                pr.StartInfo.ArgumentList.Add("-o" + ExtractDir)
            End If

            pr.StartInfo.UseShellExecute = False
            pr.Start()
            pr.WaitForExit()

            If pr.ExitCode <> 0 Then
                UpdatePackageDialog()
                MsgError("Extraction failed with error exit code " & pr.ExitCode)
                Exit Sub
            End If
        End Using

        If Not File.Exists(Path.Combine(ExtractDir, Package.Filename)) Then
            Dim subDirs As New List(Of String)

            For Each subDir In Directory.GetDirectories(ExtractDir, "*", SearchOption.AllDirectories)
                If (Path.Combine(subDir, Package.Filename)).FileExists AndAlso Not Ignore(subDir) Then
                    subDirs.Add(subDir)
                End If
            Next

            If subDirs.Count > 1 Then
                UpdatePackageDialog()

                Using td As New TaskDialog(Of String)
                    td.Title = "Choose subfolder to extract."

                    For Each subDir In subDirs
                        Dim name = subDir.Replace(ExtractDir, "").TrimEnd(Path.DirectorySeparatorChar)
                        td.AddCommand(name, subDir)
                    Next

                    If td.Show.DirExists Then
                        ExtractDir = td.SelectedValue
                    End If
                End Using
            ElseIf subDirs.Count = 1 Then
                ExtractDir = subDirs(0)
            End If
        End If

        If Not (Path.Combine(ExtractDir, Package.Filename)).FileExists Then
            UpdatePackageDialog()
            MsgError("File missing after extraction.")
            Exit Sub
        End If

        DeleteOldFiles()
    End Sub

    Sub DeleteOldFiles()
        Dim entries = Directory.GetFileSystemEntries(TargetDir)
        entries = entries.Where(Function(item) Not item.FileName.EqualsAny(Package.Keep)).ToArray
        Dim names = entries.Select(Function(item) item.FileName)
        Dim list = String.Join(BR, names)
        UpdatePackageDialog()

        If MsgQuestion("Delete current files?",
            "Delete current files in:" + BR2 + TargetDir + BR2 + list) = DialogResult.OK Then

            For Each file In Directory.GetFiles(TargetDir)
                If file.FileName.EqualsAny(Package.Keep) Then
                    Continue For
                End If

                FileHelp.Delete(file, RecycleOption.SendToRecycleBin)
            Next

            For Each folder In Directory.GetDirectories(TargetDir)
                If folder.FileName.EqualsAny(Package.Keep) Then
                    Continue For
                End If

                FolderHelp.Delete(folder, RecycleOption.SendToRecycleBin)
            Next
        Else
            UpdatePackageDialog()
            MsgInfo("Update was canceled.")
            Exit Sub
        End If

        CopyFiles()
    End Sub

    Sub CopyFiles()
        Dim entries = Directory.GetFileSystemEntries(ExtractDir)
        Dim names = entries.Select(Function(item) item.FileName)
        Dim list = String.Join(BR, names)
        UpdatePackageDialog()

        If MsgQuestion("Copy new files?",
            "Copy new files from:" + BR2 + ExtractDir + BR2 + "to:" + BR2 +
            TargetDir + BR2 + list) = DialogResult.OK Then

            For Each file In Directory.GetFiles(ExtractDir)
                FileHelp.Copy(file, Path.Combine(TargetDir, file.FileName))
            Next

            For Each folder In Directory.GetDirectories(ExtractDir)
                FolderHelp.Copy(folder, Path.Combine(TargetDir, folder.FileName))
            Next
        Else
            UpdatePackageDialog()
            MsgInfo("Update was canceled.")
            Exit Sub
        End If

        FolderHelp.Delete(ExtractDir, RecycleOption.SendToRecycleBin)
        EditVersion()
    End Sub

    Sub EditVersion()
        Dim msg = "What's the name of the new version?" + BR2 + DownloadFile.FileName

        UpdatePackageDialog()
        Dim input = InputBox.Show(msg, DownloadFile.Base)

        If input <> "" Then
            Package.SetVersion(input.Replace(";", "_").Trim)
            UpdatePackageDialog()
            g.DefaultCommands.Test()
        End If
    End Sub

    Function Ignore(value As String) As Boolean
        If value.ContainsAny(Package.Exclude) Then
            Return True
        End If

        Dim lower = value.ToLowerInvariant
        Dim x86 = {"_win32", "/x86", "-x86", "32-bit", "-win32", "i686", "i386"}

        'x86_64 and x86-64 are the names of 64-bit Linux builds
        If Environment.Is64BitProcess AndAlso lower.Replace("x86_64", "").Replace("x86-64", "").ContainsAny(x86) Then
            Return True
        End If

        If lower.ContainsAny("windows", "win64", "-win", "_win", ".exe", "macos", "darwin", "-osx", "_osx") Then
            Return True
        End If

        Dim isArm = RuntimeInformation.OSArchitecture = Architecture.Arm64
        Dim otherArch = If(isArm, {"x86_64", "amd64", "x64"}, {"aarch64", "arm64", "armv7", "armhf"})

        If lower.ContainsAny(otherArch) Then
            Return True
        End If
    End Function

    Sub UpdatePackageDialog()
        UpdateUI.UpdateUI()
    End Sub
End Class
