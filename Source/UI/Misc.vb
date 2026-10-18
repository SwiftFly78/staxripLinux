
Imports System.ComponentModel

Namespace UI
    'Base class of all non-modal windows, exposes a WinForms like API
    'on top of Eto.Forms to keep the form code similar to the Windows version.
    Public Class FormBase
        Inherits Form

        Event FilesDropped(files As String())
        Event Activated(sender As Object, e As EventArgs)
        Event HelpRequested(sender As Object, e As EventArgs)

        Private FileDropValue As Boolean
        Private DefaultWidthScale As Single
        Private DefaultHeightScale As Single
        Protected SaveAndLoadSize As Boolean = True

        Sub New()
            Icon = g.Icon
            ShowActivated = True
            AddHandler GotFocus, Sub()
                                     g.ActiveForm = Me
                                     OnActivated(EventArgs.Empty)
                                 End Sub
        End Sub

        Protected Overridable Sub OnActivated(e As EventArgs)
            RaiseEvent Activated(Me, e)
        End Sub

        Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
            MyBase.OnKeyDown(e)

            If Not e.Handled AndAlso e.KeyData = Keys.F1 Then
                e.Handled = True
                RaiseEvent HelpRequested(Me, EventArgs.Empty)
            End If
        End Sub

        Property Text As String
            Get
                Return Title
            End Get
            Set(value As String)
                Title = value
            End Set
        End Property

        ReadOnly Property FontHeight As Integer
            Get
                Return EtoHelp.FontHeight
            End Get
        End Property

        ReadOnly Property Font As Font
            Get
                Return FontManager.GetDefaultFont()
            End Get
        End Property

        ReadOnly Property IsDisposingOrDisposed As Boolean
            Get
                Return IsDisposed
            End Get
        End Property

        ReadOnly Property InvokeRequired As Boolean
            Get
                Return Not IsUIThread
            End Get
        End Property

        Sub Invoke(action As Action)
            UIInvoke(action)
        End Sub

        Sub BeginInvoke(action As Action)
            UIBeginInvoke(action)
        End Sub

        Property FileDrop As Boolean
            Get
                Return FileDropValue
            End Get
            Set(value As Boolean)
                FileDropValue = value
                AllowDrop = value
            End Set
        End Property

        Protected Overrides Sub OnDragEnter(e As DragEventArgs)
            MyBase.OnDragEnter(e)

            If FileDrop AndAlso e.Data.ContainsUris Then
                e.Effects = DragEffects.Copy
            End If
        End Sub

        Protected Overrides Sub OnDragOver(e As DragEventArgs)
            MyBase.OnDragOver(e)

            If FileDrop AndAlso e.Data.ContainsUris Then
                e.Effects = DragEffects.Copy
            End If
        End Sub

        Protected Overrides Sub OnDragDrop(e As DragEventArgs)
            MyBase.OnDragDrop(e)

            If FileDrop Then
                Dim files = GetDroppedFiles(e)

                If files.Length > 0 Then
                    RaiseEvent FilesDropped(files)
                End If
            End If
        End Sub

        Shared Function GetDroppedFiles(e As DragEventArgs) As String()
            If e.Data Is Nothing OrElse Not e.Data.ContainsUris Then
                Return {}
            End If

            Return e.Data.Uris.Where(Function(u) u.IsFile).Select(Function(u) u.LocalPath).ToArray()
        End Function

        Sub Activate()
            Visible = True
            BringToFront()
            Focus()
        End Sub

        'shows the window modal, a form can't be shown as Eto dialog
        Function ShowDialog() As DialogResult
            UIInvoke(Sub()
                         Dim owner = GetOwnerWindow()

                         If owner IsNot Nothing AndAlso owner IsNot Me Then
                             Me.Owner = owner
                         End If

                         Show()
                         RunModalLoop(Me)
                     End Sub)

            Return DialogResult.Cancel
        End Function

        Sub Hide()
            Visible = False
        End Sub

        Sub Refresh()
            Invalidate()
        End Sub

        Sub SetMinimumSize(w As Single, h As Single)
            MinimumSize = New Size(CInt(FontHeight * w), CInt(FontHeight * h))
        End Sub

        Sub ScaleClientSize(w As Single, h As Single)
            ClientSize = New Size(CInt(FontHeight * w), CInt(FontHeight * h))
        End Sub

        Sub RestoreClientSize(defaultWidthScale As Single, defaultHeightScale As Single)
            Me.DefaultWidthScale = defaultWidthScale
            Me.DefaultHeightScale = defaultHeightScale
        End Sub

        Sub SaveClientSize()
            WindowHelp.SaveSize(Me)
        End Sub

        Protected Overrides Sub OnPreLoad(e As EventArgs)
            MyBase.OnPreLoad(e)
            WindowHelp.RestoreSize(Me, DefaultWidthScale, DefaultHeightScale, SaveAndLoadSize)
        End Sub

        Protected Overrides Sub OnShown(e As EventArgs)
            MyBase.OnShown(e)
            EtoHelp.UnsetHiddenFocus(Me)
            WindowHelp.EnforceMinimumSize(Me)
        End Sub

        Protected Overrides Sub OnClosing(e As CancelEventArgs)
            MyBase.OnClosing(e)

            If Not e.Cancel AndAlso SaveAndLoadSize AndAlso DefaultWidthScale <> 0 Then
                SaveClientSize()
            End If
        End Sub

        Protected Overrides Sub OnClosed(e As EventArgs)
            If g.ActiveForm Is Me Then
                g.ActiveForm = Nothing
            End If

            MyBase.OnClosed(e)
        End Sub
    End Class

    'Base class of all modal dialogs
    Public Class DialogBase
        Inherits Dialog(Of DialogResult)

        Event FilesDropped(files As String())
        Event HelpRequested(sender As Object, e As EventArgs)

        Private FileDropValue As Boolean
        Private DefaultWidthScale As Single
        Private DefaultHeightScale As Single
        Protected SaveAndLoadSize As Boolean = False
        Private PreviousActiveForm As Window
        Private InButtonClick As Boolean
        Private ButtonResult As DialogResult

        Sub New()
            Icon = g.Icon
            Resizable = True
            Maximizable = False
            Minimizable = False
            ShowInTaskbar = False
        End Sub

        Property Text As String
            Get
                Return Title
            End Get
            Set(value As String)
                Title = value
            End Set
        End Property

        ReadOnly Property FontHeight As Integer
            Get
                Return EtoHelp.FontHeight
            End Get
        End Property

        ReadOnly Property Font As Font
            Get
                Return FontManager.GetDefaultFont()
            End Get
        End Property

        ReadOnly Property IsDisposingOrDisposed As Boolean
            Get
                Return IsDisposed
            End Get
        End Property

        ReadOnly Property InvokeRequired As Boolean
            Get
                Return Not IsUIThread
            End Get
        End Property

        Sub Invoke(action As Action)
            UIInvoke(action)
        End Sub

        Sub BeginInvoke(action As Action)
            UIBeginInvoke(action)
        End Sub

        'WinForms semantics: setting the result closes the dialog, during a click of a
        'button with a DialogResult the handlers can cancel closing by setting None
        Property DialogResult As DialogResult
            Get
                Return If(InButtonClick, ButtonResult, Result)
            End Get
            Set(value As DialogResult)
                If InButtonClick Then
                    ButtonResult = value
                ElseIf value <> DialogResult.None Then
                    Close(value)
                End If
            End Set
        End Property

        Friend Sub PerformButtonClose(result As DialogResult, click As Action)
            ButtonResult = result
            InButtonClick = True

            Try
                click()
            Finally
                InButtonClick = False
            End Try

            If ButtonResult <> DialogResult.None AndAlso Not IsDisposed Then
                Close(ButtonResult)
            End If
        End Sub

        Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
            MyBase.OnKeyDown(e)

            If Not e.Handled AndAlso e.KeyData = Keys.F1 Then
                e.Handled = True
                RaiseEvent HelpRequested(Me, EventArgs.Empty)
            End If
        End Sub

        Property AcceptButton As Button
            Get
                Return DefaultButton
            End Get
            Set(value As Button)
                DefaultButton = value
            End Set
        End Property

        Property CancelButton As Button
            Get
                Return AbortButton
            End Get
            Set(value As Button)
                AbortButton = value
            End Set
        End Property

        Function ShowDialog() As DialogResult
            Return ShowDialog(Nothing)
        End Function

        Function ShowDialog(owner As Control) As DialogResult
            Return UIInvoke(Function()
                                Dim ownerWindow = If(GetParentWindow(owner), GetOwnerWindow())
                                PreviousActiveForm = g.ActiveForm
                                g.ActiveForm = Me

                                Dim ret = If(ownerWindow Is Nothing OrElse ownerWindow Is Me, ShowModal(), ShowModal(ownerWindow))

                                g.ActiveForm = PreviousActiveForm

                                If ret = DialogResult.None Then
                                    ret = DialogResult.Cancel
                                End If

                                Return ret
                            End Function)
        End Function

        Property FileDrop As Boolean
            Get
                Return FileDropValue
            End Get
            Set(value As Boolean)
                FileDropValue = value
                AllowDrop = value
            End Set
        End Property

        Protected Overrides Sub OnDragEnter(e As DragEventArgs)
            MyBase.OnDragEnter(e)

            If FileDrop AndAlso e.Data.ContainsUris Then
                e.Effects = DragEffects.Copy
            End If
        End Sub

        Protected Overrides Sub OnDragOver(e As DragEventArgs)
            MyBase.OnDragOver(e)

            If FileDrop AndAlso e.Data.ContainsUris Then
                e.Effects = DragEffects.Copy
            End If
        End Sub

        Protected Overrides Sub OnDragDrop(e As DragEventArgs)
            MyBase.OnDragDrop(e)

            If FileDrop Then
                Dim files = FormBase.GetDroppedFiles(e)

                If files.Length > 0 Then
                    RaiseEvent FilesDropped(files)
                End If
            End If
        End Sub

        Sub Hide()
            Visible = False
        End Sub

        Sub Refresh()
            Invalidate()
        End Sub

        Sub SetMinimumSize(w As Single, h As Single)
            MinimumSize = New Size(CInt(FontHeight * w), CInt(FontHeight * h))
        End Sub

        Sub ScaleClientSize(w As Single, h As Single)
            ClientSize = New Size(CInt(FontHeight * w), CInt(FontHeight * h))
        End Sub

        Sub RestoreClientSize(defaultWidthScale As Single, defaultHeightScale As Single)
            Me.DefaultWidthScale = defaultWidthScale
            Me.DefaultHeightScale = defaultHeightScale
        End Sub

        Sub SaveClientSize()
            WindowHelp.SaveSize(Me)
        End Sub

        Protected Overrides Sub OnPreLoad(e As EventArgs)
            MyBase.OnPreLoad(e)
            WindowHelp.RestoreSize(Me, DefaultWidthScale, DefaultHeightScale, SaveAndLoadSize)
        End Sub

        Protected Overrides Sub OnShown(e As EventArgs)
            MyBase.OnShown(e)
            EtoHelp.UnsetHiddenFocus(Me)
            WindowHelp.EnforceMinimumSize(Me)
        End Sub

        Protected Overrides Sub OnClosing(e As CancelEventArgs)
            MyBase.OnClosing(e)

            If Not e.Cancel AndAlso SaveAndLoadSize AndAlso DefaultWidthScale <> 0 Then
                SaveClientSize()
            End If
        End Sub
    End Class

    Public Class SizeSavingDialogBase
        Inherits DialogBase

        Sub New()
            MyBase.New()
            SaveAndLoadSize = True
            Resizable = True
        End Sub
    End Class

    Public Class WindowHelp
        Shared Function GetKey(win As Window) As String
            Return win.GetType().Name
        End Function

        Shared Sub SaveSize(win As Window)
            If s?.Storage Is Nothing OrElse win.WindowState <> WindowState.Normal Then
                Exit Sub
            End If

            Dim size = win.ClientSize
            s.Storage.SetInt(GetKey(win) + "width", size.Width)
            s.Storage.SetInt(GetKey(win) + "height", size.Height)
        End Sub

        'Eto puts the window content into a box that reports no minimum size, so windows can be smaller
        'than their content, which is cut then, the minimum size of the content is applied to the box
        Shared Sub EnforceMinimumSize(win As Window)
            Dim box = win.Content?.ToNative()?.Parent

            While box IsNot Nothing AndAlso box.GetType.Name <> "GtkShrinkableVBox"
                box = box.Parent
            End While

            Dim child = TryCast(box, Global.Gtk.Container)?.Children.FirstOrDefault

            If child Is Nothing Then
                Exit Sub
            End If

            Dim update = Sub()
                             Dim minWidth, naturalWidth, minHeight, naturalHeight As Integer
                             child.GetPreferredWidth(minWidth, naturalWidth)
                             child.GetPreferredHeightForWidth(Math.Max(minWidth, child.Allocation.Width), minHeight, naturalHeight)
                             Dim area = Screen.PrimaryScreen.WorkingArea
                             Dim w = Math.Min(minWidth, CInt(area.Width))
                             Dim h = Math.Min(minHeight, CInt(area.Height * 0.95))
                             Dim currentWidth, currentHeight As Integer
                             box.GetSizeRequest(currentWidth, currentHeight)

                             If w <> currentWidth OrElse h <> currentHeight Then
                                 box.SetSizeRequest(w, h)
                             End If
                         End Sub

            update()
            Dim pending = False

            AddHandler child.SizeAllocated, Sub(sender, e)
                                                If Not pending Then
                                                    pending = True
                                                    Application.Instance.AsyncInvoke(Sub()
                                                                                         pending = False
                                                                                         update()
                                                                                     End Sub)
                                                End If
                                            End Sub
        End Sub

        Shared Sub RestoreSize(win As Window, widthScale As Single, heightScale As Single, loadSaved As Boolean)
            If widthScale = 0 Then
                Exit Sub
            End If

            Dim fh = EtoHelp.FontHeight
            Dim defaultWidth = CInt(fh * widthScale)
            Dim defaultHeight = CInt(fh * heightScale)
            Dim w = 0
            Dim h = 0

            If loadSaved AndAlso s?.Storage IsNot Nothing Then
                w = s.Storage.GetInt(GetKey(win) + "width")
                h = s.Storage.GetInt(GetKey(win) + "height")
            End If

            If w = 0 OrElse w < defaultWidth / 2 OrElse h = 0 OrElse h < defaultHeight / 2 Then
                w = defaultWidth
                h = defaultHeight
            End If

            Try
                Dim workingArea = Screen.PrimaryScreen.WorkingArea

                If w > workingArea.Width Then w = CInt(workingArea.Width)
                If h > workingArea.Height Then h = CInt(workingArea.Height)
            Catch
            End Try

            win.ClientSize = New Size(w, h)
        End Sub
    End Class

    Public Class ListBag(Of T)
        Implements IComparable(Of ListBag(Of T))

        Property Text As String
        Property Value As T

        Sub New(text As String, value As T)
            Me.Text = text
            Me.Value = value
        End Sub

        Shared Sub SelectItem(cb As ComboBoxEx, value As T)
            For Each i In cb.Items.OfType(Of ListBag(Of T))
                If i.Value.Equals(value) Then
                    cb.SelectedItem = i
                End If
            Next
        End Sub

        Shared Function GetValue(cb As ComboBoxEx) As T
            Return DirectCast(cb.SelectedItem, ListBag(Of T)).Value
        End Function

        Shared Function GetBagsForEnumType() As ListBag(Of T)()
            Dim ret As New List(Of ListBag(Of T))

            For Each i As T In System.Enum.GetValues(GetType(T))
                ret.Add(New ListBag(Of T)(DispNameAttribute.GetValueForEnum(i), i))
            Next

            Return ret.ToArray
        End Function

        Overrides Function ToString() As String
            Return Text
        End Function

        Function CompareTo(other As ListBag(Of T)) As Integer Implements IComparable(Of ListBag(Of T)).CompareTo
            Return Text.CompareTo(other.Text)
        End Function
    End Class

    'kept for settings compatibility, window positions are managed by the window manager on Linux
    <Serializable()>
    Public Class WindowPositions
        Public Positions As New Dictionary(Of String, System.Drawing.Point)
    End Class

    Public Class DesignHelp
        Shared ReadOnly Property IsDesignMode As Boolean
            Get
                Return False
            End Get
        End Property
    End Class

    Public Class InputBox
        Property Title As String
        Property Text As String
        Property Value As String
        Property CheckBoxText As String
        Property Checked As Boolean

        Shared Function Show(title As String,
                             Optional value As String = Nothing,
                             Optional text As String = Nothing) As String

            Dim box As New InputBox
            box.Title = title
            box.Text = text
            box.Value = value

            If box.Show = DialogResult.OK Then
                Return box.Value
            Else
                Return Nothing
            End If
        End Function

        Function Show() As DialogResult
            Using td As New TaskDialog(Of DialogResult)
                td.Title = Title
                td.Content = Text
                td.InputText = If(Value, "")
                td.Buttons = TaskButton.OkCancel

                If CheckBoxText <> "" Then
                    td.CheckBoxText = CheckBoxText
                    td.CheckBoxChecked = Checked
                End If

                If td.Show() = DialogResult.OK Then
                    Checked = td.CheckBoxChecked
                    Value = td.InputText
                End If

                Return td.SelectedValue
            End Using
        End Function
    End Class
End Namespace
