
Imports System.Text.RegularExpressions

Namespace UI
    'Button that shows a menu to select a value, menu paths are separated with " | "
    Public Class MenuButton
        Inherits ButtonEx

        Private ValueValue As Object

        Event ValueChangedUser(value As Object)

        Property Items As New List(Of Object)
        Property Menu As New ContextMenuStripEx
        Property ShowPath As Boolean

        Sub New()
            MyBase.New()
            ShowMenuSymbol = True
            AddHandler Menu.Opening, AddressOf MenuOpening
        End Sub

        Sub MenuOpening(sender As Object, e As EventArgs)
            Dim allItems = Menu.GetItems().OfType(Of MenuItemEx).ToList()
            Dim selectedItem = allItems.Where(Function(x) Value IsNot Nothing AndAlso Value.Equals(x.Tag)).OrderBy(Function(x) If(x.Path, "").Length).FirstOrDefault()
            Dim selectedPath = If(selectedItem?.Path, "")

            For Each item In allItems
                Dim isSelected = item Is selectedItem
                Dim isParentOfSelected = item.Path <> "" AndAlso selectedPath.StartsWith(item.Path + " | ")
                item.Marked = isSelected OrElse isParentOfSelected
            Next
        End Sub

        Property Value As Object
            Get
                Return ValueValue
            End Get
            Set(value As Object)
                If Menu.Items.Count = 0 AndAlso TypeOf value Is System.Enum Then
                    For Each i In System.Enum.GetValues(value.GetType)
                        If Not IsBrowsable(i) Then Continue For
                        Dim text = DispNameAttribute.GetValueForEnum(i)
                        Dim temp = i
                        Items.Add(temp)
                        MenuItemEx.Add(Menu.Items, text, Sub(o As Object) OnAction(text, o), temp, Nothing).Tag = temp
                    Next
                End If

                Dim newText = ""

                If value IsNot Nothing Then
                    For Each i In Menu.GetItems.OfType(Of MenuItemEx)()
                        If value.Equals(i.Tag) Then
                            newText = If(ShowPath AndAlso i.Path <> "", i.Path, i.Text)
                            Exit For
                        End If
                    Next
                End If

                If newText = "" AndAlso value IsNot Nothing Then
                    newText = value.ToString
                End If

                Text = newText
                ValueValue = value
            End Set
        End Property

        'enum values of features that only exist for Windows are hidden with <Browsable(False)>
        Shared Function IsBrowsable(value As Object) As Boolean
            Dim field = value.GetType.GetField(value.ToString)
            Return field Is Nothing OrElse field.GetCustomAttributes(GetType(ComponentModel.BrowsableAttribute), False).
                OfType(Of ComponentModel.BrowsableAttribute).All(Function(i) i.Browsable)
        End Function

        Protected Overrides Sub OnClick(e As EventArgs)
            Menu.Show(Me, New PointF(0, Height))
            MyBase.OnClick(e)
        End Sub

        Protected Overridable Sub OnValueChanged(value As Object)
            RaiseEvent ValueChangedUser(value)
        End Sub

        Sub AddRange(items As IEnumerable(Of Object))
            For Each i In items
                Add(i.ToString, i, Nothing)
            Next
        End Sub

        Function Add(path As String, obj As Object, Optional tip As String = Nothing) As MenuItemEx
            Items.Add(obj)
            Dim name = path

            If name.Contains("|") Then
                name = name.RightLast("|").Trim
            End If

            Dim ret = MenuItemEx.Add(Menu.Items, path, Sub(o As Object) OnAction(name, o), obj, tip)
            ret.Tag = obj
            ret.Path = path
            Return ret
        End Function

        Sub Clear()
            Items.Clear()
            Menu.Items.Clear()
        End Sub

        Sub OnAction(text As String, value As Object)
            Me.Value = value
            OnValueChanged(value)
        End Sub

        Function GetValue(Of T)() As T
            Return DirectCast(Value, T)
        End Function

        Function GetInt() As Integer
            Return DirectCast(Value, Integer)
        End Function
    End Class
End Namespace
