
'WinForms like wrappers around the Eto file, folder and color dialogs,
'they marshal to the UI thread and accept WinForms filter strings.
Public Class FileDialogHelp
    'GTK matches the patterns of filters case sensitive, like *.mp4 doesn't show C0001.MP4 of cameras
    Shared Function GetCaseInsensitivePattern(extension As String) As String
        Return String.Concat(extension.Select(Function(c) If(Char.IsLetter(c), $"[{Char.ToLowerInvariant(c)}{Char.ToUpperInvariant(c)}]", c.ToString)))
    End Function

    Shared Function ParseFilter(filter As String, Optional ignoreCase As Boolean = False) As List(Of FileFilter)
        Dim ret As New List(Of FileFilter)

        If String.IsNullOrWhiteSpace(filter) Then
            Return ret
        End If

        Dim parts = filter.Split("|"c)

        For x = 0 To parts.Length - 2 Step 2
            Dim name = parts(x).Trim
            Dim patterns = parts(x + 1).Split({";"c}, StringSplitOptions.RemoveEmptyEntries)
            Dim extensions As New List(Of String)

            For Each pattern In patterns
                Dim p = pattern.Trim

                If p = "*.*" OrElse p = "*" Then
                    extensions.Add("*")
                ElseIf p.StartsWith("*.") Then
                    extensions.Add(p.Substring(1))
                ElseIf p.StartsWith(".") Then
                    extensions.Add(p)
                Else
                    extensions.Add(p)
                End If
            Next

            If ignoreCase Then
                extensions = extensions.Select(Function(i) If(i.StartsWith("."), GetCaseInsensitivePattern(i), i)).ToList
            End If

            ret.Add(New FileFilter(name, extensions.ToArray))
        Next

        Return ret
    End Function

    Shared Function GetInitDir(ParamArray paths As String()) As String
        For Each i In paths
            If i <> "" AndAlso Directory.Exists(i) Then
                Return i
            End If

            If i <> "" AndAlso File.Exists(i) Then
                Return Path.GetDirectoryName(i)
            End If
        Next

        Return Nothing
    End Function
End Class

Public Class OpenFileDialog
    Implements IDisposable

    Property Filter As String
    Property FileName As String = ""
    Property FileNames As String() = {}
    Property InitialDirectory As String
    Property Multiselect As Boolean
    Property Title As String
    Property DefaultExt As String
    Property RestoreDirectory As Boolean
    Property CheckFileExists As Boolean = True

    Sub SetInitDir(ParamArray paths As String())
        InitialDirectory = FileDialogHelp.GetInitDir(paths)
    End Sub

    Sub SetFilter(values As IEnumerable(Of String))
        Filter = FileTypes.GetFilter(values)
    End Sub

    Function ShowDialog(Optional owner As Control = Nothing) As DialogResult
        Return UIInvoke(Function()
                            Using dialog As New Eto.Forms.OpenFileDialog
                                dialog.MultiSelect = Multiselect
                                dialog.Title = If(Title <> "", Title, "Open File")

                                For Each i In FileDialogHelp.ParseFilter(Filter, True)
                                    dialog.Filters.Add(i)
                                Next

                                Dim dir = If(InitialDirectory <> "", InitialDirectory, FileDialogHelp.GetInitDir(FileName))

                                If dir <> "" AndAlso Directory.Exists(dir) Then
                                    dialog.Directory = New Uri(dir)
                                End If

                                Dim parent = If(GetParentWindow(owner), GetOwnerWindow())

                                If dialog.ShowDialog(parent) = DialogResult.Ok Then
                                    FileNames = dialog.Filenames.ToArray
                                    FileName = If(FileNames.Length > 0, FileNames(0), dialog.FileName)

                                    If FileNames.Length = 0 AndAlso FileName <> "" Then
                                        FileNames = {FileName}
                                    End If

                                    Return DialogResult.Ok
                                End If

                                Return DialogResult.Cancel
                            End Using
                        End Function)
    End Function

    Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Public Class SaveFileDialog
    Implements IDisposable

    Property Filter As String
    Property FileName As String = ""
    Property InitialDirectory As String
    Property Title As String
    Property DefaultExt As String
    Property AddExtension As Boolean = True
    Property OverwritePrompt As Boolean = True
    Property RestoreDirectory As Boolean

    Sub SetInitDir(ParamArray paths As String())
        InitialDirectory = FileDialogHelp.GetInitDir(paths)
    End Sub

    Sub SetFilter(values As IEnumerable(Of String))
        Filter = FileTypes.GetFilter(values)
    End Sub

    Function ShowDialog(Optional owner As Control = Nothing) As DialogResult
        Return UIInvoke(Function()
                            Using dialog As New Eto.Forms.SaveFileDialog
                                dialog.Title = If(Title <> "", Title, "Save File")

                                For Each i In FileDialogHelp.ParseFilter(Filter)
                                    dialog.Filters.Add(i)
                                Next

                                Dim dir = If(InitialDirectory <> "", InitialDirectory, FileDialogHelp.GetInitDir(FileName))

                                If dir <> "" AndAlso Directory.Exists(dir) Then
                                    dialog.Directory = New Uri(dir)
                                End If

                                If FileName <> "" Then
                                    dialog.FileName = Path.GetFileName(FileName)
                                End If

                                Dim parent = If(GetParentWindow(owner), GetOwnerWindow())

                                If dialog.ShowDialog(parent) = DialogResult.Ok Then
                                    FileName = dialog.FileName

                                    If AddExtension AndAlso DefaultExt <> "" AndAlso Path.GetExtension(FileName) = "" Then
                                        FileName += "." + DefaultExt.TrimStart("."c)
                                    End If

                                    Return DialogResult.Ok
                                End If

                                Return DialogResult.Cancel
                            End Using
                        End Function)
    End Function

    Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Public Class FolderBrowserDialog
    Implements IDisposable

    Property SelectedPath As String = ""
    Property Description As String
    Property ShowNewFolderButton As Boolean = True
    Property Title As String

    Sub SetSelectedPath(path As String)
        If path <> "" AndAlso Directory.Exists(path) Then
            SelectedPath = path
        End If
    End Sub

    Function ShowDialog(Optional owner As Control = Nothing) As DialogResult
        Return UIInvoke(Function()
                            Using dialog As New SelectFolderDialog
                                dialog.Title = If(Title <> "", Title, If(Description <> "", Description, "Select Folder"))

                                If SelectedPath <> "" AndAlso Directory.Exists(SelectedPath) Then
                                    dialog.Directory = SelectedPath
                                End If

                                Dim parent = If(GetParentWindow(owner), GetOwnerWindow())

                                If dialog.ShowDialog(parent) = DialogResult.Ok Then
                                    SelectedPath = dialog.Directory
                                    Return DialogResult.Ok
                                End If

                                Return DialogResult.Cancel
                            End Using
                        End Function)
    End Function

    Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class

Public Class ColorDialog
    Implements IDisposable

    Property Color As Color
    Property FullOpen As Boolean
    Property AnyColor As Boolean

    Function ShowDialog(Optional owner As Control = Nothing) As DialogResult
        Return UIInvoke(Function()
                            Using dialog As New Eto.Forms.ColorDialog
                                dialog.Color = Color
                                dialog.AllowAlpha = False

                                Dim parent = If(GetParentWindow(owner), GetOwnerWindow())

                                If dialog.ShowDialog(parent) = DialogResult.Ok Then
                                    Color = dialog.Color
                                    Return DialogResult.Ok
                                End If

                                Return DialogResult.Cancel
                            End Using
                        End Function)
    End Function

    Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class
