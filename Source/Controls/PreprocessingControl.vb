
Imports StaxRip.UI

Public Class PreprocessingControl
    Inherits Panel
    Implements IPage, ISaveValues

    Public WithEvents lv As ListViewEx
    Public WithEvents bnAdd As ButtonEx
    Public WithEvents bnRemove As ButtonEx
    Public WithEvents bnEdit As ButtonEx
    Public WithEvents bnUp As ButtonEx
    Public WithEvents bnDown As ButtonEx
    Public WithEvents bnRestore As ButtonEx

    Property Node As TreeNode Implements IPage.Node
    Property Path As String Implements IPage.Path
    Property TipProvider As TipProvider Implements IPage.TipProvider
    Property FormSizeScaleFactor As SizeF Implements IPage.FormSizeScaleFactor

    Sub New()
        lv = New ListViewEx With {.CheckBoxes = True, .View = View.Details, .HeaderStyle = ColumnHeaderStyle.None, .FullRowSelect = True}
        lv.Columns.Add("Demuxer")
        bnAdd = New ButtonEx("Add...")
        bnRemove = New ButtonEx("Remove")
        bnEdit = New ButtonEx("Edit...")
        bnUp = New ButtonEx("Up")
        bnDown = New ButtonEx("Down")
        bnRestore = New ButtonEx("Restore...")

        lv.UpButton = bnUp
        lv.DownButton = bnDown
        lv.RemoveButton = bnRemove

        Content = New TableLayout(
            New TableRow(lv) With {.ScaleHeight = True},
            New TableRow(HRow(bnAdd, bnRemove, bnEdit, bnUp, bnDown, bnRestore))) With {.Spacing = DefaultSpacing}

        TipProvider = New TipProvider(Nothing)

        For Each i In ObjectHelp.GetCopy(Of List(Of Demuxer))(s.Demuxers)
            AddItem(i)
        Next

        TipProvider.TipsFunc = AddressOf GetTips

        lv.SingleSelectionButtons = {bnEdit}
        lv.UpdateControls()
    End Sub

    Function GetTips() As StringPairList
        Dim ret As New StringPairList

        ret.Add("Preprocessing", "The preprocessing menu defines a set of apps that are executed to demux, index or re-mux source files. The applications are executed one by one starting from the top so the next application can use the output from the previous application as input. It's possible to add custom apps by defining command lines.")

        For Each i As ListViewItem In lv.Items
            If TypeOf i.Tag Is CommandLineDemuxer Then
                Dim muxer = DirectCast(i.Tag, CommandLineDemuxer)
                Dim help = muxer.GetHelp

                If help <> "" Then
                    ret.Add(muxer.Name, help)
                End If
            End If
        Next

        Return ret
    End Function

    Function AddItem(value As Demuxer) As ListViewItem
        Dim ret = lv.Items.Add(value.ToString)
        ret.Checked = value.Active
        ret.Tag = value
        Return ret
    End Function

    Sub SaveValues() Implements ISaveValues.SaveValues
        s.Demuxers.Clear()

        For Each i As ListViewItem In lv.Items
            Dim demuxer = DirectCast(i.Tag, Demuxer)
            demuxer.Active = i.Checked
            s.Demuxers.Add(demuxer)
        Next
    End Sub

    Sub lv_SelectedIndexChanged(sender As Object, e As EventArgs) Handles lv.SelectedIndexChanged
        bnEdit.Enabled = lv.SelectedItems.Count = 1 AndAlso
            DirectCast(lv.SelectedItems(0).Tag, Demuxer).HasConfigDialog
    End Sub

    Sub bnAdd_Click(sender As Object, e As EventArgs) Handles bnAdd.Click
        Dim sb As New SelectionBox(Of Demuxer)
        sb.Title = "New Demuxer"
        sb.Text = "Please select a Demuxer."

        Dim cli As New CommandLineDemuxer
        cli.Name = "Command Line Demuxer"
        sb.AddItem(cli)

        For Each i In Demuxer.GetDefaults()
            sb.AddItem(i)
        Next

        If sb.Show = DialogResult.OK AndAlso sb.SelectedValue.ShowConfigDialog = DialogResult.OK Then
            AddItem(sb.SelectedValue).Selected = True
        End If
    End Sub

    Sub bnEdit_Click(sender As Object, e As EventArgs) Handles bnEdit.Click
        Dim listItem = lv.SelectedItems(0)
        Dim demuxer = DirectCast(listItem.Tag, Demuxer)
        demuxer.ShowConfigDialog()
        listItem.Text = demuxer.ToString
    End Sub

    Sub bnRestore_Click(sender As Object, e As EventArgs) Handles bnRestore.Click
        If MsgQuestion("Restore defaults?") = DialogResult.OK Then
            lv.Items.Clear()

            For Each dmx In Demuxer.GetDefaults()
                AddItem(dmx)
            Next

            lv.UpdateControls()
        End If
    End Sub
End Class
