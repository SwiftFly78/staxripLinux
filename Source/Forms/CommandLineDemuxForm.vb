
Imports StaxRip.UI

Public Class CommandLineDemuxForm
    Inherits DialogBase

    Public WithEvents tbName As TextEdit
    Public WithEvents tbInput As TextEdit
    Public WithEvents tbInputFormats As TextEdit
    Public WithEvents tbVideoOut As TextEdit
    Public WithEvents tbCommand As TextEdit
    Public WithEvents tbArguments As TextEdit
    Public WithEvents tbSourceFilters As TextEdit
    Public WithEvents bnCancel As ButtonEx
    Public WithEvents bnOK As ButtonEx
    Public WithEvents bnBrowse As ButtonEx
    Public WithEvents bnArguments As ButtonEx
    Public WithEvents cmsArguments As ContextMenuStripEx
    Public WithEvents MacrosToolStripMenuItem As MenuItemEx
    Public WithEvents HelpToolStripMenuItem As MenuItemEx
    Public TipProvider As TipProvider

    Private Sub InitializeComponent()
        tbName = New TextEdit
        tbInput = New TextEdit
        tbInputFormats = New TextEdit
        tbVideoOut = New TextEdit
        tbCommand = New TextEdit
        tbArguments = New TextEdit With {.Multiline = True}
        tbSourceFilters = New TextEdit
        bnOK = CreateButton("OK", DialogResult.OK)
        bnCancel = CreateButton("Cancel", DialogResult.Cancel)
        bnBrowse = New ButtonEx("...")
        MacrosToolStripMenuItem = New MenuItemEx("Macros")
        HelpToolStripMenuItem = New MenuItemEx("Help")
        cmsArguments = New ContextMenuStripEx
        cmsArguments.Items.Add(MacrosToolStripMenuItem)
        cmsArguments.Items.Add(HelpToolStripMenuItem)
        bnArguments = New ButtonEx With {.ShowMenuSymbol = True, .ContextMenuStrip = cmsArguments}
        TipProvider = New TipProvider

        Dim lName As New LabelEx With {.Text = "Demuxer Name:"}
        Dim lInput As New LabelEx With {.Text = "Input File Types:"}
        Dim lOutput As New LabelEx With {.Text = "Output File Type:"}
        Dim lFormats As New LabelEx With {.Text = "Input Formats:"}
        Dim lFilters As New LabelEx With {.Text = "Source Filters:"}
        Dim lCommand As New LabelEx With {.Text = "Command:"}
        Dim lArguments As New LabelEx With {.Text = "Arguments:", .VerticalAlignment = VerticalAlignment.Top}

        TipProvider.SetTipText(lName, "If the demuxer name matches the name of a bundled app then help and description is provided.")
        TipProvider.SetTipText(lInput, "Input file types to be handled by the demuxer.")
        TipProvider.SetTipText(lOutput, "Single video file type the demuxer outputs.")
        TipProvider.SetTipText(lFormats, "Formats the demuxer handles (vc1 mpeg2 avc hevc)")

        Dim argButtonColumn As New TableLayout(New TableRow(bnArguments), New TableRow With {.ScaleHeight = True})

        Content = PaddedTable(
            Row(lName, Scaled(tbName), Nothing),
            Row(lInput, Scaled(tbInput), Nothing),
            Row(lOutput, Scaled(tbVideoOut), Nothing),
            Row(lFormats, Scaled(tbInputFormats), Nothing),
            Row(lFilters, Scaled(tbSourceFilters), Nothing),
            Row(lCommand, Scaled(tbCommand), bnBrowse),
            ScaledRow(lArguments, Scaled(tbArguments), argButtonColumn),
            Row(Nothing, ButtonRow(bnOK, bnCancel), Nothing))

        DefaultButton = bnOK
        AbortButton = bnCancel
        Text = $"Demux Configuration - {g.DefaultCommands.GetApplicationDetails()}"
        ScaleClientSize(45, 22)
    End Sub

    Private Target As CommandLineDemuxer
    Private Temp As CommandLineDemuxer

    Sub New(demuxer As CommandLineDemuxer)
        MyBase.New()
        InitializeComponent()

        Target = demuxer
        Temp = ObjectHelp.GetCopy(Of CommandLineDemuxer)(demuxer)

        tbName.Text = Temp.Name
        tbInput.Text = Temp.InputExtensions.ToArray.Join(" ")
        tbInputFormats.Text = Temp.InputFormats.ToArray.Join(" ")
        tbSourceFilters.Text = Temp.SourceFilters.ToArray.Join(" ")
        tbVideoOut.Text = Temp.OutputExtensions.ToArray.Join(" ")
        tbArguments.Text = Temp.Arguments
        tbCommand.Text = Temp.Command
    End Sub

    Function ConvertFormat(input As String) As String
        If input.Contains("MPEG Video") Then
            input = input.Replace("MPEG Video", "mpeg2")
        End If

        If input.Contains("VC-1") Then
            input = input.Replace("VC-1", "vc1")
        End If

        Return input.ToLowerInvariant
    End Function

    Protected Overrides Sub OnClosed(e As EventArgs)
        If DialogResult = DialogResult.OK Then
            Target.Name = tbName.Text
            Target.InputExtensions = tbInput.Text.ToLowerInvariant.SplitNoEmptyAndWhiteSpace(",", ";", " ")
            Target.InputFormats = ConvertFormat(tbInputFormats.Text).SplitNoEmptyAndWhiteSpace(",", ";", " ")
            Target.OutputExtensions = tbVideoOut.Text.ToLowerInvariant.SplitNoEmptyAndWhiteSpace(",", ";", " ")
            Target.SourceFilters = tbSourceFilters.Text.SplitNoEmptyAndWhiteSpace(",", ";", " ")
            Target.Command = tbCommand.Text
            Target.Arguments = tbArguments.Text
        End If

        MyBase.OnClosed(e)
    End Sub

    Sub tbCommand_DoubleClick(sender As Object, e As EventArgs) Handles tbCommand.DoubleClick
        Using dialog As New OpenFileDialog
            If dialog.ShowDialog() = DialogResult.OK Then
                tbCommand.Text = dialog.FileName
            End If
        End Using
    End Sub

    Sub tbArguments_DoubleClick(sender As Object, e As EventArgs) Handles tbArguments.DoubleClick
        MacrosForm.ShowDialogForm()
    End Sub

    Sub bnBrowse_Click(sender As Object, e As EventArgs) Handles bnBrowse.Click
        tbCommand_DoubleClick(Nothing, Nothing)
    End Sub

    Sub MacrosToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles MacrosToolStripMenuItem.Click
        MacrosForm.ShowDialogForm()
    End Sub

    Sub HelpToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles HelpToolStripMenuItem.Click
        For Each pack In Package.Items.Values
            If tbName.Text = pack.Name Then
                pack.ShowHelp()

                Exit Sub
            End If
        Next

        MsgWarn("The demuxer name '" + tbName.Text + "' does not match with the name of one of StaxRipLinux's apps. StaxRipLinux includes the following apps:" + BR2 +
                Package.Items.Values.Where(Function(package) Not TypeOf package Is PluginPackage).Select(Function(package) package.Name).ToArray.Sort.Join(", "))
    End Sub

    Sub DemuxForm_HelpRequested(sender As Object, e As EventArgs) Handles Me.HelpRequested
        Dim form As New HelpForm()
        form.Doc.WriteStart(Text)
        form.Doc.WriteTips(TipProvider.GetTips)
        form.Show()
    End Sub
End Class
