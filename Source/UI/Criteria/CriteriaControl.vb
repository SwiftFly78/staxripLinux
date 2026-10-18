
Namespace UI
    Public Class CriteriaControl
        Inherits Scrollable

        Private ReadOnly Stack As New StackLayout With {
            .Orientation = Orientation.Vertical,
            .HorizontalContentAlignment = HorizontalAlignment.Stretch,
            .Spacing = FH(0.2)}

        Property AllCriteria As List(Of Criteria)

        Sub New()
            ExpandContentWidth = True
            ExpandContentHeight = False
            Border = BorderType.None
            Content = Stack
        End Sub

        Sub AddItem(criteria As Criteria)
            Dim c As New CriteriaItemControl(AllCriteria, Me) With {.Criteria = criteria}
            Stack.Items.Add(New StackLayoutItem(c))
        End Sub

        Sub RemoveItem(item As CriteriaItemControl)
            Dim stackItem = Stack.Items.FirstOrDefault(Function(i) i.Control Is item)

            If stackItem IsNot Nothing Then
                Stack.Items.Remove(stackItem)
            End If
        End Sub

        Property CriteriaList() As List(Of Criteria)
            Get
                Dim ret As New List(Of Criteria)

                For Each i In Stack.Items.Select(Function(x) x.Control).OfType(Of CriteriaItemControl)
                    If i.Criteria IsNot Nothing Then
                        ret.Add(i.Criteria)
                    End If
                Next

                Return ret
            End Get
            Set(Value As List(Of Criteria))
                If Value IsNot Nothing Then
                    For Each i As Criteria In Value
                        AddItem(i)
                    Next
                End If
            End Set
        End Property
    End Class
End Namespace
