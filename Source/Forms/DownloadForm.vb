
Imports System.Net.Http
Imports System.Threading

Imports StaxRip.UI

Public Class DownloadForm
    Inherits DialogBase

    Public WithEvents bnCancel As ButtonEx
    Public ProgressBar As ProgressBar

    Private ReadOnly Cancellation As New CancellationTokenSource
    Private ReadOnly Url As String

    Property Path As String

    Sub New(url As String, path As String)
        Me.Url = url
        Me.Path = path
        Text = $"Download - {g.DefaultCommands.GetApplicationDetails()}"
        ProgressBar = New ProgressBar With {.MaxValue = 1000}
        bnCancel = CreateButton("Cancel")
        AbortButton = bnCancel
        Content = PaddedTable(Row(Scaled(ProgressBar)), Row(ButtonRow(bnCancel)))
        ClientSize = New Size(FH(30), FH(5))
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        Download()
    End Sub

    Async Sub Download()
        Try
            Using client As New HttpClient
                client.DefaultRequestHeaders.Add("User-Agent", "StaxRip")
                client.DefaultRequestHeaders.Referrer = New Uri(Url)

                Using response = Await client.GetAsync(Url, HttpCompletionOption.ResponseHeadersRead, Cancellation.Token)
                    response.EnsureSuccessStatusCode()
                    Dim total = response.Content.Headers.ContentLength.GetValueOrDefault

                    Using source = Await response.Content.ReadAsStreamAsync(Cancellation.Token)
                        Using target = File.Create(Path)
                            Dim buffer(81919) As Byte
                            Dim received As Long

                            While True
                                Dim count = Await source.ReadAsync(buffer, 0, buffer.Length, Cancellation.Token)

                                If count = 0 Then
                                    Exit While
                                End If

                                Await target.WriteAsync(buffer, 0, count, Cancellation.Token)
                                received += count
                                UpdateProgress(received, total)
                            End While
                        End Using
                    End Using
                End Using
            End Using

            DialogResult = DialogResult.OK
        Catch ex As OperationCanceledException
            DialogResult = DialogResult.Cancel
        Catch ex As Exception
            MsgError("Download failed.", ex.Message)
            DialogResult = DialogResult.Cancel
        End Try
    End Sub

    Sub UpdateProgress(received As Long, total As Long)
        Dim unit = PrefixedSize(2)
        Dim receivedText = (received / unit.Factor).ToString("0.#")

        If total > 0 Then
            Text = $"Download - {receivedText} {unit.Unit} of {(total / unit.Factor):0.#} {unit.Unit}"
            ProgressBar.Value = CInt(received * 1000 / total)
        Else
            Text = $"Download - {receivedText} {unit.Unit}"
        End If
    End Sub

    Sub bnCancel_Click(sender As Object, e As EventArgs) Handles bnCancel.Click
        Cancellation.Cancel()
    End Sub
End Class
