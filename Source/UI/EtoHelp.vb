
Imports System.Runtime.ExceptionServices
Imports System.Runtime.InteropServices
Imports System.Threading

'UI thread marshaling and font helpers for the Eto.Forms UI
Public Module EtoHelp
    Private MainThreadID As Integer = -1

    Sub InitMainThread()
        MainThreadID = Thread.CurrentThread.ManagedThreadId
    End Sub

    ReadOnly Property IsUIThread As Boolean
        Get
            Return MainThreadID = -1 OrElse Thread.CurrentThread.ManagedThreadId = MainThreadID
        End Get
    End Property

    Sub UIInvoke(action As Action)
        If IsUIThread OrElse Application.Instance Is Nothing Then
            action()
            Exit Sub
        End If

        Dim exception As Exception = Nothing

        Application.Instance.Invoke(Sub()
                                        Try
                                            action()
                                        Catch ex As Exception
                                            exception = ex
                                        End Try
                                    End Sub)

        If exception IsNot Nothing Then
            ExceptionDispatchInfo.Capture(exception).Throw()
        End If
    End Sub

    Function UIInvoke(Of T)(func As Func(Of T)) As T
        If IsUIThread OrElse Application.Instance Is Nothing Then
            Return func()
        End If

        Dim ret As T
        UIInvoke(Sub() ret = func())
        Return ret
    End Function

    Sub UIBeginInvoke(action As Action)
        If Application.Instance Is Nothing Then
            action()
        Else
            Application.Instance.AsyncInvoke(Sub()
                                                 Try
                                                     action()
                                                 Catch ex As Exception
                                                     g.OnException(ex)
                                                 End Try
                                             End Sub)
        End If
    End Sub

    'replacement for WinForms DoEvents(), processes pending events without blocking
    Sub DoEvents()
        If IsUIThread AndAlso Application.Instance IsNot Nothing Then
            Dim count = 0

            While Global.Gtk.Application.EventsPending() AndAlso count < 1000
                Global.Gtk.Application.RunIteration(False)
                count += 1
            End While
        End If
    End Sub

    'GTK swallows all key events while an unrealized widget like a hidden control has the focus
    Sub UnsetHiddenFocus(window As Window)
        Dim gtkWindow = TryCast(window.ControlObject, Global.Gtk.Window)
        Dim focus = gtkWindow?.Focus

        If focus IsNot Nothing AndAlso Not focus.IsRealized Then
            gtkWindow.Focus = Nothing
        End If
    End Sub

    <DllImport("libgtk-3.so.0")>
    Private Sub gtk_widget_override_background_color(widget As IntPtr, state As Integer, color As IntPtr)
    End Sub

    'Eto can't unset a background color, GTK also applies it to selected text
    Sub ClearBackgroundColor(control As Control)
        Dim widget = TryCast(control.ControlObject, Global.Gtk.Widget)

        If widget IsNot Nothing Then
            gtk_widget_override_background_color(widget.Handle, 0, IntPtr.Zero)
        End If
    End Sub

    'blocks until the window is closed while processing events
    Sub RunModalLoop(window As Window)
        Dim isClosed = False
        AddHandler window.Closed, Sub() isClosed = True

        Dim gtkWindow = TryCast(window.ControlObject, Global.Gtk.Window)

        If gtkWindow IsNot Nothing Then
            gtkWindow.Modal = True
        End If

        While Not isClosed AndAlso Not window.IsDisposed
            Global.Gtk.Application.RunIteration(True)
        End While
    End Sub

    Private DefaultFontValue As Font
    Private FontHeightValue As Integer

    Function GetDefaultFont() As Font
        If DefaultFontValue Is Nothing Then
            DefaultFontValue = SystemFonts.Default()
        End If

        Return DefaultFontValue
    End Function

    'line height of the default font in logical pixels, used for layout calculations
    ReadOnly Property FontHeight As Integer
        Get
            If FontHeightValue = 0 Then
                FontHeightValue = Math.Max(10, CInt(Math.Ceiling(GetDefaultFont().LineHeight)))
            End If

            Return FontHeightValue
        End Get
    End Property

    Function MeasureText(text As String, Optional font As Font = Nothing) As SizeF
        If font Is Nothing Then
            font = GetDefaultFont()
        End If

        If String.IsNullOrEmpty(text) Then
            Return New SizeF(0, font.LineHeight)
        End If

        Return font.MeasureString(text)
    End Function

    Function MeasureTextWidth(text As String, Optional font As Font = Nothing) As Integer
        Return CInt(Math.Ceiling(MeasureText(text, font).Width))
    End Function

    ReadOnly Property IsDarkMode As Boolean
        Get
            Try
                Dim c = SystemColors.ControlBackground
                Return (c.R * 0.299 + c.G * 0.587 + c.B * 0.114) < 0.5
            Catch
                Return False
            End Try
        End Get
    End Property

    Function ToEtoColor(value As System.Drawing.Color) As Color
        Return Color.FromArgb(value.R, value.G, value.B, value.A)
    End Function

    Function ToDrawingColor(value As Color) As System.Drawing.Color
        Return System.Drawing.Color.FromArgb(value.Ab, value.Rb, value.Gb, value.Bb)
    End Function

    Function GetParentWindow(control As Control) As Window
        If control Is Nothing Then
            Return Nothing
        End If

        Dim win = TryCast(control, Window)

        If win IsNot Nothing Then
            Return win
        End If

        Return control.ParentWindow
    End Function

    'the window that should own dialogs shown without an explicit owner
    Function GetOwnerWindow() As Window
        Dim active = g.ActiveForm

        If active IsNot Nothing AndAlso Not active.IsDisposed AndAlso active.Visible Then
            Return active
        End If

        If g.ProcForm IsNot Nothing AndAlso Not g.ProcForm.IsDisposed AndAlso g.ProcForm.Visible Then
            Return g.ProcForm
        End If

        If g.MainForm IsNot Nothing AndAlso Not g.MainForm.IsDisposed AndAlso g.MainForm.Visible Then
            Return g.MainForm
        End If

        Return Nothing
    End Function
End Module

'WinForms like clipboard access
Public NotInheritable Class Clipboard
    Shared ReadOnly Property Instance As Eto.Forms.Clipboard
        Get
            Return Eto.Forms.Clipboard.Instance
        End Get
    End Property

    Shared Sub SetText(text As String)
        UIInvoke(Sub() Instance.Text = text)
    End Sub

    Shared Function GetText() As String
        Return UIInvoke(Function() If(Instance.ContainsText, Instance.Text, ""))
    End Function

    Shared Function ContainsText() As Boolean
        Return UIInvoke(Function() Instance.ContainsText)
    End Function
End Class
