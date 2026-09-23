#Disable Warning IDE1006 ' Naming Styles
Imports System.Runtime.InteropServices
Imports System.Threading
Imports BskyDirectImageUploader.Bdiu

Public Class OxiPngSelectForm

    Private imagesArray As New HashSet(Of String)

    ' Track whether closing is allowed
    Private _allowClose As Boolean = True
    Private Property AllowClose As Boolean
        Get
            Return _allowClose
        End Get
        Set(value As Boolean)
            If value Then
                ' Re-enables the visual X button
                Call Bdiu.WinApiFunctions.EnableFormCloseButton(Me)
            Else
                ' Disables the visual X button
                Call Bdiu.WinApiFunctions.DisableFormCloseButton(Me)
            End If
            _allowClose = value
        End Set
    End Property

    <System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Content)>
    Private WithEvents OxiPngOptInt As Bdiu.OxiPngOptimize
    <System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)>
    Public Property OxiPngOpt As Bdiu.OxiPngOptimize
        Get
            Return OxiPngOptInt
        End Get
        Set(value As Bdiu.OxiPngOptimize)
            OxiPngOptInt = value
        End Set
    End Property
    Private _cts As CancellationTokenSource

    Private Sub OxiPngOpt_ProcessDataReceived(sender As Object, data As String, isErrorData As Boolean) Handles OxiPngOptInt.ProcessDataReceived
        Call AddToLog(data, isErrorData)
    End Sub

    Private Sub OxiPngSelectForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If OxiPngOpt Is Nothing Then
            OxiPngOpt = New Bdiu.OxiPngOptimize
        End If
        cmbOxiArgPresets.DataSource = [Enum].GetValues(Of Bdiu.OxiPngOptimize.OxiPngOptimizePresets)()
        Call OxiPngOpt.TryLocateOxipngPath()
        Me.txtOxiPngPath.Text = OxiPngOpt.OxipngPath
        Me.txtOxiPngArgs.Text = OxiPngOpt.OxipngArguments

        Me.btnExecute.Enabled = Not String.IsNullOrWhiteSpace(OxiPngOpt.OxipngPath)

        Me.ucOxiLog.Logger = New Logger.Logger(Byte.MaxValue)
        Me.ucOxiLog.MaxLogEntriesNUD.Value = Me.ucOxiLog.Logger.MaxEntries
        'Me.ucOxiLog.rtbLog.PlaceholderText = "Console output from Oxipng"

        Call UpdateArgsTextBox()
        ' Add handle of preset ComboBox
        AddHandler cmbOxiArgPresets.SelectedIndexChanged, AddressOf cmbOxiArgPresets_SelectedIndexChanged
    End Sub

    Private Sub UpdateArgsTextBox() Handles OxiPngOptInt.ArgsUpdated
        Me.txtOxiPngArgs.Text = OxiPngOpt.OxipngArguments
        Me.cmbOxiArgPresets.SelectedItem = OxiPngOpt.OxipngArgumentPreset
    End Sub

    Private Sub btnDownloadOxiPng_Click(sender As Object, e As EventArgs) Handles btnDownloadOxiPng.Click
        Try
            Using process As New System.Diagnostics.Process
                process.StartInfo.UseShellExecute = True
                process.StartInfo.FileName = "https://github.com/oxipng/oxipng/releases#:~:text=x86%5F64%2Dpc%2Dwindows%2Dmsvc%2Ezip"
                Call process.Start()
            End Using
        Catch ex As Exception
            Call Bdiu.BDIUExceptionDisplay.DisplayExceptionAsMessageBox(owner:=Me, ex:=ex)
        End Try
    End Sub


    Private Sub AddToLog(text As String, isError As Boolean)
        If isError Then
            Call Me.ucOxiLog.Logger.AddToLog(text, System.Drawing.Color.Red)
        Else
            Call Me.ucOxiLog.Logger.AddToLog(text)
        End If
    End Sub
    Private Sub AddToLog(text As String, entryColor As System.Drawing.Color)
        Call Me.ucOxiLog.Logger.AddToLog(text, entryColor)
    End Sub

    Private Sub AddImageSecurely(path As String)
        Call AddImageSecurely(path, True)
    End Sub

    Private Sub AddImageSecurely(path As String, onlyAllowSafeFileTypesToAdd As Boolean)
        Dim ext As String = IO.Path.GetExtension(path).ToLower()

        If onlyAllowSafeFileTypesToAdd Then
            Dim allowedExts As String() = {".png", ".apng"}

            If Not allowedExts.Contains(ext) Then Return
        End If

        ' Add image to internal array
        If imagesArray.Add(path) Then
            ' Add to Image list displayed to user (if path wasnt already added before)
            Call lstImages.Items.Add(IO.Path.GetFileName(path))
        End If
    End Sub

    Private Sub RemoveAllImages()
        Call imagesArray.Clear()
        Call lstImages.Items.Clear()
    End Sub

    Private Sub btnClearImg_Click(sender As Object, e As EventArgs) Handles btnClearImg.Click
        Call RemoveAllImages()
    End Sub

    Private Sub btnSelectImg_Click(sender As Object, e As EventArgs) Handles btnSelectImg.Click
        Dim FileDialog As New OpenFileDialog With {
            .Filter = "PNG (*.png;*.apng)|*.png;*.apng|All Files (*.*)|*.*",
            .Multiselect = True,
            .RestoreDirectory = True,
            .Title = "Select PNG images to optimize (Hold CTRL to select multiple)"
        }

        Dim DummyForm As New Form() With {.TopMost = True}
        Dim DialogResult As DialogResult = FileDialog.ShowDialog(DummyForm)

        If DialogResult = DialogResult.OK Then
            Dim ImagePaths As String() = FileDialog.FileNames

            For Each path As String In ImagePaths
                Call Me.AddImageSecurely(path, False)
            Next
        End If
    End Sub

    ' Drag & Drop Event Handlers
    Private Sub OxiPngSelectForm_DragEnter(sender As Object, e As DragEventArgs) Handles Me.DragEnter
        If e.Data.GetDataPresent(DataFormats.FileDrop) Then e.Effect = DragDropEffects.Copy
    End Sub

    Private Sub OxiPngSelectForm_DragDrop(sender As Object, e As DragEventArgs) Handles Me.DragDrop
        Dim files As String() = CType(e.Data.GetData(DataFormats.FileDrop), String())
        For Each file As String In files
            Call AddImageSecurely(file)
        Next
    End Sub

    Private Sub txtOxiPngArgs_LostFocus(sender As Object, e As EventArgs) Handles txtOxiPngArgs.LostFocus
        Me.OxiPngOpt.OxipngArguments = Me.txtOxiPngArgs.Text.Trim()
    End Sub

    Private Sub cmbOxiArgPresets_SelectedIndexChanged(sender As Object, e As EventArgs)
        Try
            Me.OxiPngOpt.OxipngArgumentPreset = CType(cmbOxiArgPresets.SelectedItem, Bdiu.OxiPngOptimize.OxiPngOptimizePresets)
        Catch ex As Exception
            Call Bdiu.BDIUExceptionDisplay.DisplayExceptionAsMessageBox(Me, ex, MessageBoxIcon.Warning)
        End Try
    End Sub

    Private Async Sub btnExecute_Click(sender As Object, e As EventArgs) Handles btnExecute.Click
        _cts = New CancellationTokenSource()

        If imagesArray.Count = 0 Then
            AddToLog("No PNG files provided.", Color.DarkGray)
            Return
        End If

        Call AddToLog($"{imagesArray.Count} PNGs provided. Starting optimization...", False)

        ' disable buttons to prevent double-clicks
        btnExecute.Enabled = False
        btnClearImg.Enabled = False
        btnSelectImg.Enabled = False
        AllowClose = False
        btnCancel.Enabled = True

        ' Run the optimization asynchronously on a background thread to keep UI responsive
        Try
            Await Me.OxiPngOpt.OptimizePngFiles(imagesArray.ToArray, _cts.Token)
        Catch ex As OperationCanceledException
            Call AddToLog("Process aborted by user.", Color.DarkMagenta)
        Catch ex As Exception
            Call AddToLog($"Process Error: {ex.Message}", Color.DarkMagenta)
        Finally
            _cts.Dispose()
            btnExecute.Enabled = True
            btnClearImg.Enabled = True
            btnSelectImg.Enabled = True
            AllowClose = True
            btnCancel.Enabled = False

            If OxiPngOpt.ProcessExitCode.HasValue Then
                AddToLog($"Exit code: 0x{OxiPngOpt.ProcessExitCode.Value:X8}", Color.DarkTurquoise)
            Else
                AddToLog($"Exit code: N/A", Color.DarkTurquoise)
            End If

            If OxiPngOpt.JsonOut Is Nothing Then
                AddToLog("ERROR: Oxipng didn't finish optimizing!", True)
            Else
                AddToLog("Processing completed!", Color.Green)
            End If
        End Try
    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        If _cts IsNot Nothing AndAlso Not _cts.IsCancellationRequested Then
            AddToLog("Aborting...", Color.Gray)
            _cts.Cancel()
        End If
    End Sub

    Private Sub btnLogExport_Click(sfd As SaveFileDialog)
        sfd.FileName = String.Concat("OxiPng_LogExport.", sfd.DefaultExt)
    End Sub
End Class
#Enable Warning IDE1006 ' Naming Styles