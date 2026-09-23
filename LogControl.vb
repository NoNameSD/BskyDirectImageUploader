Imports System.Diagnostics.Eventing.Reader

Public Class LogControl

    Public Event BuildLogExportFileDialog(sfd As SaveFileDialog)

    <System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)>
    Public Property Logger As Logger.Logger
        Get
            Return objLogger
        End Get
        Set(_LoggerSet As Logger.Logger)
            objLogger = _LoggerSet
        End Set
    End Property

    Public ReadOnly Property MaxLogEntriesNUD As NumericUpDown
        Get
            Return Me.ucControls.MaxLogEntriesNUD
        End Get
    End Property

    Private Sub RefreshRichTextLogHandler(sender As Object, e As EventArgs) Handles objLogger.LogEntriesUpdated
        Call RefreshRichTextLog()
    End Sub

    Private Delegate Sub RefreshRichTextLogCallback()

    Private Sub RefreshRichTextLog()
        If Me.rtbLog Is Nothing Then Return
        ' See https://stackoverflow.com/a/10775421
        If Me.rtbLog.InvokeRequired Then
            Dim RefreshRichTextLogCallback As RefreshRichTextLogCallback = New RefreshRichTextLogCallback(AddressOf RefreshRichTextLog)
            Me.Invoke(RefreshRichTextLogCallback)
        Else
            Dim rtfString = Me.Logger.GetLogAsRichText(False, True)
            Try
                If Me.rtbLog.IsDisposed Then
                Else
                    Me.rtbLog.Rtf = rtfString
                    If Me.ucControls.LogAutoScroll Then Call Me.rtbLog.ScrollToBottom()
                End If
            Catch ex As Exception
                Call Me.Logger.AddToLog($"Error '{ex.Message}' while trying to add RichTextLog.", Color.DarkViolet, False)
            End Try
        End If
    End Sub

    Private Sub rtbLog_Resize(sender As Object, e As EventArgs) Handles rtbLog.Resize
        If Me.ucControls.LogAutoScroll Then Call Me.rtbLog.ScrollToBottom()
    End Sub

    Private Sub btnLogClear_Click(sender As Object, e As EventArgs) Handles ucControls.ClickedLogClear
        Call Me.Logger.Clear()
    End Sub

    Private Sub nudLogEntries_ValueChanged(sender As Object, e As System.EventArgs) Handles ucControls.MaxLogEntriesValueChanged
        If Me.Logger Is Nothing Then Return
        Dim MaxEntries = CUInt(ucControls.MaxLogEntriesNUD.Value)
        If Me.Logger.MaxEntries <> MaxEntries Then
            Me.Logger.MaxEntries = MaxEntries
        End If
    End Sub

    Private Sub btnLogExport_Click(sender As Object, e As EventArgs) Handles ucControls.ClickedLogExport
        RaiseEvent BuildLogExportFileDialog(sfdLogExport)

        Me.sfdLogExport.ShowDialog(owner:=Me)
    End Sub

    Private Sub sfdLogExport_FileOk(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles sfdLogExport.FileOk
        Try
            ' Create a new file
            Dim FileStream As _
            New System.IO.FileStream(
                path:=sfdLogExport.FileName,
              access:=IO.FileAccess.Write,
               share:=IO.FileShare.Read,
                mode:=IO.FileMode.Create)

            Dim WriterEncoding As System.Text.Encoding

            Dim WriteString As String = Constants.vbNullString

            Select Case Me.sfdLogExport.FilterIndex
                Case 1
                    ' Rich-Text-File

                    WriterEncoding = System.Text.Encoding.Latin1
                    WriteString = Me.Logger.GetLogAsRichText(False, True)

                Case Else ' 2
                    ' Plain-Text

                    WriterEncoding = New System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier:=True, throwOnInvalidBytes:=True)
                    WriteString = Me.Logger.GetLogAsPlainText(False, True)
            End Select

            Dim Writer As New _
                        System.IO.BinaryWriter(
                            output:=FileStream,
                            encoding:=WriterEncoding,
                            leaveOpen:=False)

            Call Writer.Write(WriteString.ToCharArray)

            Call Writer.Close()

        Catch ex As System.Exception
            Call Bdiu.BDIUExceptionDisplay.DisplayExceptionAsMessageBox(owner:=Me.Parent, ex:=ex)
        End Try
    End Sub
End Class