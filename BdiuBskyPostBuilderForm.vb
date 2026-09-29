#Disable Warning IDE1006 ' Naming Styles

Public Class BdiuBskyPostBuilderForm

    Const BskyMaxImageSize As Long = 2000000

    'Private imagesArray As New List(Of Dictionary(Of String, String))()
    'Private warningLabels As New HashSet(Of String)
    Private postData As New Bdiu.BskyUploadPostData

    Friend Event WarningLabelsUpdated()

    <System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)>
    Public Property BskyMgmt As BskyDirectImageUploader.Bdiu.BskySessionManager
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
    Private _cts As System.Threading.CancellationTokenSource

    Private Sub BskyPostBuilder_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.ucLog.Logger = New Logger.Logger(Byte.MaxValue)
        Me.ucLog.MaxLogEntriesNUD.Value = Me.ucLog.Logger.MaxEntries

        ' Check if we received images directly via Windows Drag & Drop onto the .exe icon
        Dim args As String() = Environment.GetCommandLineArgs()
        If args.Length > 1 Then
            Call AddImagesSecurely(args, True)
        End If

        Call Me.UpdateCharCount()
        Call Me.SetImageDataOnForm()
        RemoveHandler cboLanguageAdd.SelectedIndexChanged, AddressOf cboLanguageAdd_SelectedIndexChanged
        Call Me.SetLanguageBoxData()
        Call Me.RefreshLanguageTextBox()
        AddHandler cboLanguageAdd.SelectedIndexChanged, AddressOf cboLanguageAdd_SelectedIndexChanged

        Me.cmbSetDateFromAttachType.Text = "Modified"
        Me.btnSetCreatedAtDuringPosting.Checked = True
        Call RefreshCreatedAtBox()

        If Me.BskyMgmt Is Nothing Then
            Me.BskyMgmt = New BskyDirectImageUploader.Bdiu.BskySessionManager
        End If
        If Not Me.BskyMgmt.BskyAgentIsAuthenticated Then
            Call Me.BskyMgmt.TryLoadSessionCredentialsLocally()
        End If
    End Sub

    ''' <summary>
    ''' Bind list of all cultures (with LanguageId) installed on the machine to the Language combo box
    ''' </summary>
    Private Sub SetLanguageBoxData()
        Me.cboLanguageAdd.DataSource = Bdiu.LanguageItem.GetLanguageList
        Me.cboLanguageAdd.DisplayMember = "DisplayName" ' What the user sees
        Me.cboLanguageAdd.ValueMember = "Bcp47Tag"      ' The actual BCP-47 code stored behind it
        Call Me.SetSystemLanguageAsDefault()
    End Sub

    Private Sub SetSystemLanguageAsDefault()
        ' PRESELECT THE SYSTEM LANGUAGE 
        ' Get the current Windows UI BCP-47 language tag (e.g., "en-US")
        Dim systemLanguage As String = System.Globalization.CultureInfo.CurrentUICulture.Name

        Me.postData.AddLanguage(systemLanguage)

        ' Assign it to SelectedValue. If it exists in your whitelist, it auto-selects.
        Me.cboLanguageAdd.SelectedValue = systemLanguage

        Dim languageList = DirectCast(Me.cboLanguageAdd.DataSource, List(Of BskyDirectImageUploader.Bdiu.LanguageItem))

        ' Fallback: If the exact system language isn't in the list (Shouldnt happen, as all installed languages are in the list)
        If cboLanguageAdd.SelectedValue Is Nothing AndAlso systemLanguage.Contains("-"c) Then
            Dim baseLanguage As String = systemLanguage.Split("-"c)(0) ' Extracts "en" from "en-CA"

            ' Look for an item in your list that starts with that base language
            Dim fallbackItem = languageList.FirstOrDefault(Function(x) x.Bcp47Tag.StartsWith(baseLanguage, StringComparison.OrdinalIgnoreCase))

            If fallbackItem IsNot Nothing Then
                Me.cboLanguageAdd.SelectedValue = fallbackItem.Bcp47Tag
            End If
        End If

        ' Fallback to english if even that is not found
        If cboLanguageAdd.SelectedValue Is Nothing Then

            Dim fallbackItem = languageList.FirstOrDefault(Function(x) x.Bcp47Tag.StartsWith("en", StringComparison.OrdinalIgnoreCase))

            If fallbackItem IsNot Nothing Then
                Me.cboLanguageAdd.SelectedValue = fallbackItem.Bcp47Tag
            End If
        End If
    End Sub

    Private Sub OxiPngOpt_ProcessDataReceived(sender As Object, data As String, isErrorData As Boolean) Handles OxiPngOptInt.ProcessDataReceived
        Call AddToLog(data, isErrorData)
    End Sub
    Private Sub BskyUpload_ProcessDataReceived(sender As Object, data As String, isErrorData As Boolean)
        Call AddToLog(data, isErrorData)
    End Sub

    Private Sub AddToLog(text As String, isError As Boolean)
        If isError Then
            Call Me.ucLog.Logger.AddToLog(text, System.Drawing.Color.Red)
        Else
            Call Me.ucLog.Logger.AddToLog(text)
        End If
    End Sub
    Private Sub AddToLog(text As String, entryColor As System.Drawing.Color)
        Call Me.ucLog.Logger.AddToLog(text, entryColor)
    End Sub

    ' Drag & Drop Event Handlers
    Private Sub BskyPostBuilder_DragEnter(sender As Object, e As DragEventArgs) Handles Me.DragEnter
        If e.Data.GetDataPresent(DataFormats.FileDrop) Then e.Effect = DragDropEffects.Copy
    End Sub

    Private Sub BskyPostBuilder_DragDrop(sender As Object, e As DragEventArgs) Handles Me.DragDrop
        Dim files As String() = CType(e.Data.GetData(DataFormats.FileDrop), String())
        Call AddImagesSecurely(files, False)
    End Sub

    Private Sub RemoveAllImages()
        If Me.postData.Images IsNot Nothing Then
            Call Me.postData.Images.Clear()
        End If
        Call lstImages.Items.Clear()
        Me.txtAltText.Text = Nothing
        Me.txtAltText.Enabled = False
        Me.txtMimeType.Text = Nothing
        Me.txtMimeType.Enabled = False
        Call Me.ClearImageDimensionsNud()
        Me.nudWidth.Enabled = False
        Me.nudHeight.Enabled = False
    End Sub

    Private Sub AddImagesSecurely(paths() As String, onlyAllowSafeFileTypesToAdd As Boolean)
        For Each path In paths

            Dim ext As String = IO.Path.GetExtension(path).ToLower()
            Dim fileName As String = IO.Path.GetFileName(path)

            If onlyAllowSafeFileTypesToAdd Then
                Dim allowedExts As String() = {".png", ".jpg", ".jpeg", ".jfif", ".webp", ".avif"}

                If Not allowedExts.Contains(ext) Then Return
            End If

            ' Strict 2MB Server limit verification
            Dim fileInfo As New System.IO.FileInfo(path)
            If fileInfo.Length > BskyMaxImageSize Then
                Dim messageText As String = $"The file '{fileInfo.Name}' is too large!{Environment.NewLine}({Math.Round(fileInfo.Length / 1000 / 1000, 2)} MB / {Math.Round(fileInfo.Length / 1024 / 1024, 2)} MiB){Environment.NewLine}{Environment.NewLine}Bluesky limits uncompressed blobs to {Math.Round(BskyMaxImageSize / 1000 / 1000, 2)} MB / {Math.Round(BskyMaxImageSize / 1024 / 1024, 2)} MiB.{Environment.NewLine}{Environment.NewLine}Add the image regardless?"

                If {".png", ".apng"}.Contains(ext) Then
                    messageText += $"{Environment.NewLine}{Environment.NewLine}Enabling PNG image optimization may bring the filesize under the threshold."
                End If

                If MessageBox.Show(messageText,
                                   "Size Limit Reached",
                                   MessageBoxButtons.YesNo,
                                   MessageBoxIcon.Exclamation) <> DialogResult.Yes Then
                    Return
                End If
            End If

            If Me.postData.Images IsNot Nothing Then
                If Me.postData.Images.Count = 10 Then
                    If MessageBox.Show($"10 images already added.{Environment.NewLine}Bluesky technically allows up to 20 images per post, but will only display the first 10 on the site.{Environment.NewLine}{Environment.NewLine}Add more regardless?", "Soft Limit Reached", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) <> DialogResult.Yes Then
                        Return
                    End If
                End If
                If Me.postData.Images.Count = 20 Then
                    If MessageBox.Show($"20 images already added.{Environment.NewLine}Bluesky will reject this post, if you add more images.{Environment.NewLine}{Environment.NewLine}Add more regardless?", "Hard Limit Reached", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) <> DialogResult.Yes Then
                        Return
                    End If
                End If
            End If

            ' Add to new image data object
            Dim imgData As New Bdiu.BskyUploadPostData.BskyUploadImageData(filePath:=path)

            ' Get the image dimensions and mimetype of the file
            Try
                Call imgData.LoadImageData()
            Catch ex As Exception
                ' Ignore exceptions when reading the image metadata
            End Try
            Me.postData.AddImage(imgData)

            Call Me.AddToLog($"Added image {fileName}", False)
            If imgData.Dimensions IsNot Nothing Then
                Call Me.AddToLog($"Read dimensions {imgData.Dimensions.Width} x {imgData.Dimensions.Height} from {fileName}", False)
            Else
                Call Me.AddToLog($"Warning: No image dimensions read from {fileName}", Bdiu.BdiuHelper.ColorWarning)
            End If
            If String.IsNullOrWhiteSpace(imgData.MimeType) Then
                Call Me.AddToLog($"Warning: No MIME-Type read from {fileName}", Bdiu.BdiuHelper.ColorWarning)
            Else
                Call Me.AddToLog($"Read MIME-Type {imgData.MimeType} from {fileName}", False)
            End If

            ' Image list displayed to user
            Call lstImages.Items.Add(fileName)
        Next
        Call Me.RefreshCreatedAtBox()
    End Sub

    ' Real-time Live Character Counter (300 limit)
    Private Sub txtCaption_TextChanged(sender As Object, e As EventArgs) Handles txtCaption.TextChanged
        Call UpdateCharCount()
    End Sub

    Private Sub UpdateCharCount()
        Dim text As String = txtCaption.Text

        ' Bluesky's strict limits
        Const MaxGraphemes As Integer = 300
        Const MaxBytes As Integer = 3000

        ' 1. Count Grapheme Clusters (Visual characters like Bluesky)
        ' Use StringInfo to correctly split text by text elements (graphemes)
        Dim graphemeEnumerator As System.Globalization.TextElementEnumerator = System.Globalization.StringInfo.GetTextElementEnumerator(text)
        Dim graphemeCount As Integer = 0

        While graphemeEnumerator.MoveNext()
            graphemeCount += 1
        End While

        ' 2. Count Bytes (The AT Protocol 3KB safety limit)
        Dim byteCount As Integer = System.Text.Encoding.UTF8.GetByteCount(text)

        ' 3. Update UI Elements
        lblGraphemeCount.Text = $"Characters: {graphemeCount} / {MaxGraphemes}"
        lblByteCount.Text = $"Bytes: {byteCount} / {MaxBytes}"

        ' 4. Validation & Feedback
        Dim isGraphemeValid As Boolean = (graphemeCount <= MaxGraphemes)
        Dim isByteValid As Boolean = (byteCount <= MaxBytes)

        If isGraphemeValid AndAlso isByteValid Then
            lblGraphemeCount.ForeColor = Color.DarkGreen
            lblByteCount.ForeColor = Color.DarkGreen
            btnSubmit.Enabled = True
        Else
            ' Highlight errors in red if they exceed limits
            If Not isGraphemeValid Then lblGraphemeCount.ForeColor = Color.Red
            If Not isByteValid Then lblByteCount.ForeColor = Color.Red
            btnSubmit.Enabled = False
        End If
    End Sub

    Public Sub DisableFormForProcess()
        Me.grpImages.Enabled = False
        Me.grpReplyTo.Enabled = False
        Me.grpCaption.Enabled = False
        Me.grpLabels.Enabled = False
        Me.btnSubmit.Enabled = False
        Call Bdiu.WinApiFunctions.DisableFormCloseButton(Me)
        Me.btnCancel.Enabled = True
    End Sub
    Public Sub EnableFormAfterProcessFinish()
        Me.grpImages.Enabled = True
        Me.grpReplyTo.Enabled = True
        Me.grpCaption.Enabled = True
        Me.grpLabels.Enabled = True
        Me.btnSubmit.Enabled = True
        Call Bdiu.WinApiFunctions.EnableFormCloseButton(Me)
        Me.btnCancel.Enabled = False
    End Sub

    ' Actual process of sending the post data to Bluesky
    Private Async Sub btnSubmit_Click(sender As Object, e As EventArgs) Handles btnSubmit.Click
        If MessageBox.Show(Me, "Send the post data to Bluesky?", "Confirm posting", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then
            Return
        End If

        Call Me.DisableFormForProcess()

        _cts = New System.Threading.CancellationTokenSource()
        Using registration = _cts.Token.Register(Sub()
                                                     Call EnableFormAfterProcessFinish()
                                                 End Sub)
            If Me.chkOptPng.Checked Then
                Dim optComplete As Boolean = Await Me.OptimizePngFiles(_cts.Token)

                If Not optComplete Or _cts.IsCancellationRequested Then
                    ' Error or cancelled
                    Call Me.AddToLog("Upload aborted", Color.DarkGray)
                    _cts.Dispose()
                    Call EnableFormAfterProcessFinish()
                    Return
                End If
            End If

            ' Initialize Upload Class with the Credentials and Post data
            Dim upl As New Bdiu.BskyUploadPayload(bskyMgmt:=Me.BskyMgmt, bskyPost:=postData)

            ' Post the data to Bluesky
            Try
                AddHandler upl.ProcessDataReceived, AddressOf BskyUpload_ProcessDataReceived
                Await upl.PostToBsky(_cts.Token)

                ' Success if no error is thrown
                Call MessageBox.Show("Successfully posted to Bluesky!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As System.Security.Cryptography.CryptographicException
                Call MsgBox($"Error decrypting the Bluesky App Password.{Environment.NewLine}Access credentials must be re-entered.", MsgBoxStyle.Exclamation, "Decryption Error")
                Call PromptCredentialsInput()
            Catch ex As OperationCanceledException
                Call AddToLog("Process aborted by user.", Color.DarkMagenta)
            Catch ex As BskyDirectImageUploader.Bdiu.BskyUploadPayload.BlueskyException
                Call AddToLog(ex.Message, True)
                If TypeOf ex Is BskyDirectImageUploader.Bdiu.BskyUploadPayload.BlueskyLoginException Then
                    Call PromptCredentialsInput()
                End If
            Catch ex As Exception
                Call AddToLog(ex.ToString(), True)
            Finally
                RemoveHandler upl.ProcessDataReceived, AddressOf BskyUpload_ProcessDataReceived
                Call EnableFormAfterProcessFinish()
            End Try

        End Using
    End Sub

    Private Sub PromptCredentialsInput()
        Dim subForm As New BdiuCredentialForm With {
            .Icon = Me.Icon,
            .FormBorderStyle = FormBorderStyle.FixedDialog,
            .MaximizeBox = False,
            .MinimizeBox = False,
            .StartPosition = FormStartPosition.CenterParent,
            .BskyMgmt = Me.BskyMgmt
        }
        Call subForm.ShowDialog(owner:=Me)
        Me.BskyMgmt = subForm.BskyMgmt
        Call subForm.Dispose()
    End Sub
    Private Async Function OptimizePngFiles() As Task(Of Boolean)
        _cts = New System.Threading.CancellationTokenSource()
        Dim returnVal = Await OptimizePngFiles(_cts.Token)
        _cts.Dispose()
        Return returnVal
    End Function

    Private Async Function OptimizePngFiles(ct As System.Threading.CancellationToken) As Task(Of Boolean)
        If Me.postData.Images Is Nothing Then
            Call AddToLog("No Images provided. Skip PNG optimization.", Color.DarkGray)
            Return True
        End If

        Dim pngFiles As New HashSet(Of String)
        Dim allowedExts As String() = {".png", ".apng"}


        For Each image In Me.postData.Images
            Dim imagePath = image.FilePath

            If allowedExts.Contains(System.IO.Path.GetExtension(imagePath)) Then
                Call pngFiles.Add(imagePath)
            End If
        Next


        If pngFiles.Count = 0 Then
            Call AddToLog("None of the provided images is a PNG. Skip optimization.", Color.DarkGray)
            Return True
        End If

        Call OxiPngOpt.DiscardReturnData()

        ' Run the optimization asynchronously on a background thread to keep UI responsive
        Try
            Await OxiPngOpt.OptimizePngFiles(pngFiles.ToArray, ct)
        Catch ex As OperationCanceledException
            Call AddToLog("Process aborted by user.", Color.DarkMagenta)
        Catch ex As Exception
            Call AddToLog($"Process Error: {ex.Message}", Color.DarkMagenta)
        Finally
            If OxiPngOpt.ProcessExitCode.HasValue Then
                AddToLog($"Exit code: 0x{OxiPngOpt.ProcessExitCode.Value:X8}", Color.DarkTurquoise)
            Else
                AddToLog($"Exit code: N/A", Color.DarkTurquoise)
            End If
        End Try
        If OxiPngOpt.JsonOut Is Nothing Then
            AddToLog("ERROR: Oxipng didn't finish optimizing!", True)
            Return False
        Else
            AddToLog("PNG Optmization completed!", Color.Green)
            Return True
        End If
    End Function

    Private Sub lstImages_SelectedIndexChanged(sender As Object, e As EventArgs) Handles lstImages.SelectedIndexChanged
        Call SetImageDataOnForm()
    End Sub

    Private Sub SetAltTextBoxData()
        If lstImages.SelectedIndex = -1 OrElse Me.postData.Images Is Nothing OrElse Me.postData.Images.Count = 0 Then
            Me.txtAltText.Enabled = False
        Else
            Me.txtAltText.Text = Me.postData.Images.Item(lstImages.SelectedIndex).AltText
            Me.txtAltText.Enabled = True
        End If
    End Sub

    Private Sub txtAltText_Validated(sender As Object, e As EventArgs) Handles txtAltText.Validated
        If lstImages.SelectedIndex = -1 OrElse Me.postData.Images Is Nothing OrElse Me.postData.Images.Count = 0 Then
            Exit Sub
        End If
        Me.postData.Images.Item(lstImages.SelectedIndex).AltText = Me.txtAltText.Text
    End Sub

    Private Sub txtMimeType_Validated(sender As Object, e As EventArgs) Handles txtMimeType.Validated
        If lstImages.SelectedIndex = -1 OrElse Me.postData.Images Is Nothing OrElse Me.postData.Images.Count = 0 Then
            Exit Sub
        End If
        Me.postData.Images.Item(lstImages.SelectedIndex).MimeType = Me.txtMimeType.Text
    End Sub

    Private Sub SetMimeTypeTextBoxData()
        If lstImages.SelectedIndex = -1 OrElse Me.postData.Images Is Nothing OrElse Me.postData.Images.Count = 0 Then
            Me.txtMimeType.Enabled = False
        Else
            Me.txtMimeType.Text = Me.postData.Images.Item(lstImages.SelectedIndex).MimeType
            Me.txtMimeType.Enabled = True
        End If
    End Sub

    Private Sub btnSelectImg_Click(sender As Object, e As EventArgs) Handles btnSelectImg.Click
        Dim FileDialog As New OpenFileDialog With {
            .Filter = "Bluesky Supported Images (*.png;*.jpg;*.jpeg;*.jfif;*.webp;*.avif;*.svg)|*.png;*.jpg;*.jpeg;*.jfif;*.webp;*.avif,*.svg|All Files (*.*)|*.*",
            .Multiselect = True,
            .RestoreDirectory = True,
            .Title = "Select 1 or more Images (Hold CTRL to select multiple)"
        }

        Dim DummyForm As New Form() With {.TopMost = True}
        Dim DialogResult As DialogResult = FileDialog.ShowDialog(DummyForm)

        If DialogResult = DialogResult.OK Then
            Call Me.AddImagesSecurely(FileDialog.FileNames, False)
        End If
    End Sub

    Private Sub btnClearImg_Click(sender As Object, e As EventArgs) Handles btnClearImg.Click
        Call RemoveAllImages()
    End Sub

    Private Sub chkSexual_CheckedChanged(sender As Object, e As EventArgs) Handles chkSexual.CheckedChanged
        If chkSexual.Checked Then
            postData.AddLabel("sexual")
        Else
            postData.RemoveLabel("sexual")
        End If
        RaiseEvent WarningLabelsUpdated()
    End Sub

    Private Sub chkNudity_CheckedChanged(sender As Object, e As EventArgs) Handles chkNudity.CheckedChanged
        If chkNudity.Checked Then
            Call Me.postData.AddLabel("nudity")
        Else
            Call Me.postData.RemoveLabel("nudity")
        End If
        RaiseEvent WarningLabelsUpdated()
    End Sub

    Private Sub chkPorn_CheckedChanged(sender As Object, e As EventArgs) Handles chkPorn.CheckedChanged
        If chkPorn.Checked Then
            Call Me.postData.AddLabel("porn")
        Else
            Call Me.postData.RemoveLabel("porn")
        End If
        RaiseEvent WarningLabelsUpdated()
    End Sub

    Private Sub chkGraphic_CheckedChanged(sender As Object, e As EventArgs) Handles chkGraphic.CheckedChanged
        If chkGraphic.Checked Then
            Call Me.postData.AddLabel("graphic-media")
        Else
            Call Me.postData.RemoveLabel("graphic-media")
        End If
        RaiseEvent WarningLabelsUpdated()
    End Sub

    Private Sub RefreshWarningLabels() Handles Me.WarningLabelsUpdated
        If Me.postData.Labels IsNot Nothing Then
            ' Remove empty warning labels
            Call Me.postData.Labels.RemoveWhere(Function(s) String.IsNullOrWhiteSpace(s))

            ' Insert all current warning line line separated to the textbox
            Me.txtWarningLabels.Text = String.Join(Environment.NewLine, Me.postData.Labels.ToArray)

            ' Update default warning label flags
            Me.chkSexual.Checked = Me.postData.Labels.Contains("sexual")
            Me.chkNudity.Checked = Me.postData.Labels.Contains("nudity")
            Me.chkPorn.Checked = Me.postData.Labels.Contains("porn")
            Me.chkGraphic.Checked = Me.postData.Labels.Contains("graphic-media")
        Else
            ' Insert all current warning line line separated to the textbox
            Me.txtWarningLabels.Text = Nothing

            ' Update default warning label flags
            Me.chkSexual.Checked = False
            Me.chkNudity.Checked = False
            Me.chkPorn.Checked = False
            Me.chkGraphic.Checked = False
        End If
    End Sub

    Private Sub txtWarningLabels_Validated(sender As Object, e As EventArgs) Handles txtWarningLabels.Validated
        Dim separators As String() = {Environment.NewLine, vbLf, vbCr}
        Dim lines As String() = Me.txtWarningLabels.Text.Split(separators, StringSplitOptions.RemoveEmptyEntries)
        Call Me.postData.ClearLabelsAndUnionWith(lines)
        RaiseEvent WarningLabelsUpdated()
    End Sub

    Private Sub txtReplyUrl_Validated(sender As Object, e As EventArgs) Handles txtReplyUrl.Validated
        If Me.txtReplyUrl.Text Is Nothing OrElse Me.txtReplyUrl.Text.Length = 0 Then
            Me.postData.ReplyUrl = Nothing
        Else
            Me.postData.ReplyUrl = Me.txtReplyUrl.Text
        End If
    End Sub

    Private Sub txtCaption_Validated(sender As Object, e As EventArgs) Handles txtCaption.Validated
        If Me.txtCaption.Text Is Nothing OrElse Me.txtCaption.Text.Length = 0 Then
            Me.postData.Text = Nothing
        Else
            Me.postData.Text = Me.txtCaption.Text
        End If
    End Sub

    Private Sub btnOpenOxiPngForm_Click(sender As Object, e As EventArgs) Handles btnOpenOxiPngForm.Click
        Dim subForm As New OxiPngSelectForm With {
                .Icon = Me.Icon,
                .StartPosition = FormStartPosition.CenterParent,
                .OxiPngOpt = OxiPngOpt
            }
        ' Temporarily remove Oxipng log handler
        ' Otherwise log entries from this object related to the subform would show up here too
        RemoveHandler OxiPngOptInt.ProcessDataReceived, AddressOf OxiPngOpt_ProcessDataReceived

        Call subForm.ShowDialog(owner:=Me)
        Call Me.Show()
        Call subForm.Dispose()

        ' Re-add Oxipng log handler
        AddHandler OxiPngOptInt.ProcessDataReceived, AddressOf OxiPngOpt_ProcessDataReceived
    End Sub

    Private Sub btnLogExport_Click(sfd As SaveFileDialog) Handles ucLog.BuildLogExportFileDialog
        sfd.FileName = String.Concat("Bdiu_LogExport.", sfd.DefaultExt)
    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        If _cts IsNot Nothing AndAlso Not _cts.IsCancellationRequested Then
            AddToLog("Aborting...", Color.Gray)
            _cts.Cancel()
        End If
    End Sub

    Private Sub SetImageDataOnForm()
        Call Me.SetImageDimensionsData()
        Call Me.SetAltTextBoxData()
        Call Me.SetMimeTypeTextBoxData()
    End Sub

    Private Sub ClearImageDimensionsNud()
        Call ClearImageWidthNud()
        Call ClearImageHeightNud()
    End Sub
    Private Sub ClearImageWidthNud()
        Me.nudWidth.Value = 0
        Me.nudWidth.Text = Nothing
    End Sub
    Private Sub ClearImageHeightNud()
        Me.nudHeight.Value = 0
        Me.nudHeight.Text = Nothing
    End Sub

    Private Sub SetImageDimensionsData()

        If lstImages.SelectedIndex = -1 OrElse Me.postData.Images Is Nothing OrElse Me.postData.Images.Count = 0 Then
            Me.nudWidth.Enabled = False
            Me.nudHeight.Enabled = False
            Call ClearImageDimensionsNud()
        Else
            Dim imgDim = Me.postData.Images.Item(lstImages.SelectedIndex).Dimensions

            If imgDim IsNot Nothing Then
                If imgDim.Width.HasValue Then
                    Dim val = imgDim.Width.Value
                    Me.nudWidth.Text = CStr(val)
                    Me.nudWidth.Value = val
                Else
                    Call ClearImageWidthNud()
                End If
                If imgDim.Height.HasValue Then
                    Dim val = imgDim.Height.Value
                    Me.nudHeight.Text = CStr(val)
                    Me.nudHeight.Value = val
                Else
                    Call ClearImageHeightNud()
                End If
            Else
                Call ClearImageDimensionsNud()
            End If
            Me.nudWidth.Enabled = True
            Me.nudHeight.Enabled = True
        End If
    End Sub

    Private Sub nudWidth_Leave(sender As Object, e As EventArgs) Handles nudWidth.Leave
        If lstImages.SelectedIndex = -1 OrElse Me.postData.Images Is Nothing OrElse Me.postData.Images.Count = 0 Then
        Else
            Dim imgDim = Me.postData.Images.Item(lstImages.SelectedIndex).Dimensions

            If imgDim IsNot Nothing Then
                If String.IsNullOrWhiteSpace(Me.nudWidth.Text) Then
                    imgDim.Width = Nothing
                    Call SetImageDimensionsData()
                Else
                    Select Case Me.nudWidth.Value
                        Case 0, > UInt32.MaxValue, < UInt32.MinValue
                            imgDim.Width = Nothing
                        Case Else
                            imgDim.Width = CType(Me.nudWidth.Value, System.UInt32)
                    End Select
                End If
            Else
                Select Case Me.nudWidth.Value
                    Case 0, > UInt32.MaxValue, < UInt32.MinValue
                    Case Else
                        Me.postData.Images.Item(lstImages.SelectedIndex).Dimensions = New Bdiu.BskyUploadPostData.BskyUploadImageData.ImageDimensions(CType(Me.nudWidth.Value, System.UInt32), 0)
                End Select
            End If
        End If
    End Sub

    Private Sub nudHeight_Leave(sender As Object, e As EventArgs) Handles nudHeight.Leave
        If lstImages.SelectedIndex = -1 OrElse Me.postData.Images Is Nothing OrElse Me.postData.Images.Count = 0 Then
        Else
            Dim imgDim = Me.postData.Images.Item(lstImages.SelectedIndex).Dimensions

            If imgDim IsNot Nothing Then
                If String.IsNullOrWhiteSpace(Me.nudHeight.Text) Then
                    imgDim.Height = Nothing
                    Call SetImageDimensionsData()
                Else
                    Select Case Me.nudHeight.Value
                        Case 0, > UInt32.MaxValue, < UInt32.MinValue
                            imgDim.Height = Nothing
                        Case Else
                            imgDim.Height = CType(Me.nudHeight.Value, System.UInt32)
                    End Select
                End If
            Else
                Select Case Me.nudHeight.Value
                    Case 0, > UInt32.MaxValue, < UInt32.MinValue
                    Case Else
                        Me.postData.Images.Item(lstImages.SelectedIndex).Dimensions = New Bdiu.BskyUploadPostData.BskyUploadImageData.ImageDimensions(0, CType(Me.nudHeight.Value, System.UInt32))
                End Select
            End If
        End If
    End Sub

    Private Sub chkQuote_CheckedChanged(sender As Object, e As EventArgs) Handles chkQuote.CheckedChanged
        Call SetReplyToOrQuoteBox()
    End Sub

    Private Sub SetReplyToOrQuoteBox()
        Me.postData.ReplyIsQuote = chkQuote.Checked
        If Me.postData.ReplyIsQuote Then
            Me.grpReplyTo.Text = "Quote post"
            Me.txtReplyUrl.PlaceholderText = "URL of the post you are quoting (optional)"
        Else
            Me.grpReplyTo.Text = "Reply to"
            Me.txtReplyUrl.PlaceholderText = "URL to the post you are replying to (optional)"
        End If
    End Sub

    Private Sub cboLanguageAdd_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboLanguageAdd.SelectedIndexChanged
        If cboLanguageAdd.SelectedValue IsNot Nothing Then
            Dim selectedVal = cboLanguageAdd.SelectedValue
            Dim selectedBcp47 As String

            If TypeOf selectedVal Is Bdiu.LanguageItem Then
                selectedBcp47 = DirectCast(selectedVal, Bdiu.LanguageItem).Bcp47Tag
            ElseIf TypeOf selectedVal Is String Then
                selectedBcp47 = DirectCast(selectedVal, String)
            Else
                Return
            End If

            If Me.postData.AddLanguage(selectedBcp47) Then
                Call RefreshLanguageTextBox()
            End If
        End If
    End Sub

    Private Sub RefreshLanguageTextBox()
        If Me.postData.Languages Is Nothing Then
            Me.txtLanguages.Text = Nothing
        Else
            Me.txtLanguages.Text = String.Join(", ", Me.postData.Languages)
        End If
    End Sub

    Private Sub txtLanguages_Validated(sender As Object, e As EventArgs) Handles txtLanguages.Validated
        Me.postData.ClearLanguages()
        If String.IsNullOrWhiteSpace(txtLanguages.Text) Then
            Me.postData.Languages = Nothing
            Call RefreshLanguageTextBox()
            Return
        End If

        ' Split by commas
        Dim rawTokens As String() = txtLanguages.Text.Split(","c)

        For Each token As String In rawTokens
            ' Clean up spaces
            Dim cleanTag As String = token.Trim()
            Me.postData.AddLanguage(cleanTag)
        Next
        Call RefreshLanguageTextBox()
    End Sub

    Public Sub RefreshCreatedAtBox()
        If Me.btnSetCreatedAtDuringPosting.Checked Then
            Me.postData.CreatedAt = Nothing
            Me.dtpCreatedAt.Enabled = False
            RemoveHandler dtpCreatedAt.ValueChanged, AddressOf dtpCreatedAt_ValueChanged
            Me.dtpCreatedAt.Format = DateTimePickerFormat.Custom
            Me.dtpCreatedAt.CustomFormat = " "
            Me.txtCreatedAt.Enabled = False
            RemoveHandler txtCreatedAt.Validated, AddressOf txtCreatedAt_Validated
            Me.txtCreatedAt.Text = Nothing
        Else
            RemoveHandler dtpCreatedAt.ValueChanged, AddressOf dtpCreatedAt_ValueChanged
            RemoveHandler txtCreatedAt.Validated, AddressOf txtCreatedAt_Validated
            If Me.btnSetCreatedAtByAttachments.Checked AndAlso Me.postData.Images IsNot Nothing AndAlso Me.postData.Images.Count > 0 Then
                If Me.cmbSetDateFromAttachType.Text.Equals("Created", StringComparison.OrdinalIgnoreCase) Then
                    Me.postData.CreatedAt = Me.postData.GetMaxCreatedAttachmentDate
                Else
                    Me.postData.CreatedAt = Me.postData.GetMaxModifiedAttachmentDate
                End If
            End If
            If Not Me.postData.CreatedAt.HasValue Then
                Me.postData.CreatedAt = DateTimeOffset.Now
            End If
            Me.dtpCreatedAt.Enabled = True
            Me.dtpCreatedAt.Value = Me.postData.CreatedAt.Value.LocalDateTime
            AddHandler dtpCreatedAt.ValueChanged, AddressOf dtpCreatedAt_ValueChanged
            Me.dtpCreatedAt.Format = DateTimePickerFormat.Custom
            Me.dtpCreatedAt.CustomFormat = Bdiu.BdiuHelper.CurrentCultureDateTimeFormat()
            Me.txtCreatedAt.Enabled = True
            Me.txtCreatedAt.Text = Me.postData.CreatedAt.Value.LocalDateTime.ToString("o") ' Convert to ISO 8601 String
            AddHandler txtCreatedAt.Validated, AddressOf txtCreatedAt_Validated
        End If
    End Sub

    Private Sub txtCreatedAt_Validated(sender As Object, e As EventArgs) Handles txtCreatedAt.Validated

        ' Parse the ISO 8601 string back into a DateTimeOffset object
        Dim parsedValue As DateTimeOffset
        If DateTimeOffset.TryParse(txtCreatedAt.Text, parsedValue) Then
            ' Set internal value
            Me.postData.CreatedAt = parsedValue
        Else
            ' Conversion failed
            ' Will be reset back to the previous value
        End If

        ' Refresh related form data
        Call RefreshCreatedAtBox()
    End Sub

    Private Sub dtpCreatedAt_ValueChanged(sender As Object, e As EventArgs) Handles dtpCreatedAt.ValueChanged
        ' Set internal value
        Me.postData.CreatedAt = New DateTimeOffset(dtpCreatedAt.Value)

        ' Refresh related form data
        Call RefreshCreatedAtBox()
    End Sub

    Private Sub btnSetCreatedAtDuringUpload_CheckedChanged(sender As Object, e As EventArgs) Handles btnSetCreatedAtDuringPosting.CheckedChanged
        If Me.btnSetCreatedAtDuringPosting.Checked Then
            Me.btnSetCreatedAtByAttachments.Checked = False
        End If
        Call Me.RefreshCreatedAtBox()
    End Sub
    Private Sub btnSetCreatedAtByAttachments_CheckedChanged(sender As Object, e As EventArgs) Handles btnSetCreatedAtByAttachments.CheckedChanged
        If Me.btnSetCreatedAtByAttachments.Checked Then
            Me.btnSetCreatedAtDuringPosting.Checked = False
        End If
        Call Me.RefreshCreatedAtBox()
    End Sub
    Private Sub cmbSetDateFromAttachType_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbSetDateFromAttachType.SelectedIndexChanged
        If Me.btnSetCreatedAtByAttachments.Checked Then
            Call Me.RefreshCreatedAtBox()
        End If
    End Sub
End Class
#Enable Warning IDE1006 ' Naming Styles