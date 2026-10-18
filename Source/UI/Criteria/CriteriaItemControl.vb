
Namespace UI
    Public Class CriteriaItemControl
        Inherits Panel

        Public WithEvents mbProperties As MenuButton
        Public WithEvents tePropertiesIdentifier As TextEdit
        Public WithEvents mbCondition As MenuButton
        Public WithEvents teValue As TextEdit
        Public WithEvents bnRemove As ButtonEx

        Private ReadOnly OwnerControl As CriteriaControl

        Sub New(allCriteria As List(Of Criteria), owner As CriteriaControl)
            OwnerControl = owner
            mbProperties = New MenuButton With {.Width = FH(12)}
            tePropertiesIdentifier = New TextEdit With {.Width = FH(6), .Enabled = False}
            mbCondition = New MenuButton With {.Width = FH(8), .Enabled = False}
            teValue = New TextEdit With {.Enabled = False}
            bnRemove = New ButtonEx("Remove")

            Content = New TableLayout(New TableRow(
                New TableCell(mbProperties),
                New TableCell(tePropertiesIdentifier),
                New TableCell(mbCondition),
                New TableCell(teValue, True),
                New TableCell(bnRemove))) With {.Spacing = New Size(FH(0.3), 0)}

            For Each c In allCriteria
                If c.Name?.StartsWith("Source ") Then
                    mbProperties.Add("Source | " + c.Name, c, c.Description)
                ElseIf c.Name?.StartsWith("Target ") Then
                    mbProperties.Add("Target | " + c.Name, c, c.Description)
                ElseIf c.Name?.StartsWith("Audio ") Then
                    mbProperties.Add("Audio | " + c.Name, c, c.Description)
                ElseIf c.Name?.StartsWith("Crop ") Then
                    mbProperties.Add("Crop | " + c.Name, c, c.Description)
                ElseIf c.Name?.StartsWith("Encoder ") Then
                    mbProperties.Add("Encoder | " + c.Name, c, c.Description)
                ElseIf c.Name?.StartsWith("AviSynth/VapourSynth") Then
                    mbProperties.Add("AviSynth/VapourSynth | " + c.Name, c, c.Description)
                ElseIf Not c.Name?.EndsWith(" Directory") AndAlso Not c.Name?.EndsWith(" File") Then
                    mbProperties.Add(c.Name, c, c.Description)
                End If

                If c.Name?.EndsWith(" Directory") Then
                    mbProperties.Add("Directories | " + c.Name, c, c.Description)
                End If

                If c.Name?.EndsWith(" Folder") Then
                    mbProperties.Add("Folders | " + c.Name, c, c.Description)
                End If

                If c.Name?.EndsWith(" File") Then
                    mbProperties.Add("Files | " + c.Name, c, c.Description)
                End If
            Next
        End Sub

        Private CriteriaValue As Criteria

        Property Criteria As Criteria
            Get
                Return CriteriaValue
            End Get
            Set(Value As Criteria)
                If Value IsNot Nothing Then
                    CriteriaValue = DirectCast(ObjectHelp.GetCopy(Value), Criteria)

                    For Each c As Criteria In mbProperties.Items
                        If c.Name = Criteria.Name Then
                            mbProperties.Value = c
                            Exit For
                        End If
                    Next

                    tePropertiesIdentifier.Enabled = CriteriaValue.PropertyIdentifierNeeded
                    tePropertiesIdentifier.SetTextWithoutTextChangedEvent(CriteriaValue.PropertyIdentifier)
                    tePropertiesIdentifier_TextChanged()

                    mbCondition.Enabled = True
                    mbCondition.Clear()
                    mbCondition.AddRange(CriteriaValue.ConditionNames)
                    mbCondition.Value = CriteriaValue.ConditionName

                    teValue.Enabled = True
                    teValue.SetTextWithoutTextChangedEvent(CriteriaValue.ValueString)
                End If
            End Set
        End Property

        Sub bnRemove_Click(sender As Object, e As EventArgs) Handles bnRemove.Click
            OwnerControl.RemoveItem(Me)
        End Sub

        Sub teValue_TextChanged() Handles teValue.TextChanged
            If Criteria Is Nothing Then Exit Sub
            Criteria.ValueString = teValue.Text
        End Sub

        Sub tePropertiesIdentifier_TextChanged() Handles tePropertiesIdentifier.TextChanged
            If Criteria Is Nothing Then Exit Sub
            Criteria.PropertyIdentifier = tePropertiesIdentifier.Text

            If String.IsNullOrWhiteSpace(Criteria.PropertyIdentifier) AndAlso Criteria.PropertyIdentifierNeeded Then
                tePropertiesIdentifier.BackColor = ThemeManager.CurrentTheme.General.DangerForeColor
            Else
                tePropertiesIdentifier.BackColor = teValue.BackColor
            End If
        End Sub

        Sub mbCondition_ValueChangedUser(value As Object) Handles mbCondition.ValueChangedUser
            If Criteria Is Nothing Then Exit Sub
            Criteria.ConditionName = mbCondition.Value.ToString
        End Sub

        Sub mbProperties_ValueChangedUser(value As Object) Handles mbProperties.ValueChangedUser
            Criteria = DirectCast(value, Criteria)
        End Sub
    End Class
End Namespace
