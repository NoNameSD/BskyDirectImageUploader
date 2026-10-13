#Disable Warning IDE1006 ' Naming Styles

Public Class BdiuBskyPostBuilderForm

    Const BskyMaxImageSize As Long = 2000000
    Const BskyMaxVideoSize As Long = 300000000

    Private postData As New Bdiu.BskyUploadPostData

    Friend Event WarningLabelsUpdated()

    ' Bluesky's strict limits
    Private ReadOnly MaxGraphemes As Integer = idunno.Bluesky.Maximum.PostLengthInGraphemes
    Private ReadOnly MaxBytes As Integer = idunno.Bluesky.Maximum.PostLengthInBytes

    Private ReadOnly LanguageList As List(Of Bdiu.LanguageItem) = Bdiu.LanguageItem.GetLanguageList

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

        Call Me.UpdateCharCount()
        Call Me.SetAttachmentDataOnForm()
        RemoveHandler cboLanguageAdd.SelectedIndexChanged, AddressOf cboLanguage_SelectedIndexChanged
        Call Me.SetLanguageBoxData(Me.cboLanguageAdd)
        Call Me.RefreshPostLanguage()
        AddHandler cboLanguageAdd.SelectedIndexChanged, AddressOf cboLanguage_SelectedIndexChanged
        RemoveHandler cboCaptionLanguage.SelectedIndexChanged, AddressOf cboLanguage_SelectedIndexChanged
        RemoveHandler txtCaptionLanguage.Validated, AddressOf txtCaptionLanguage_Validated
        Call Me.SetLanguageBoxData(Me.cboCaptionLanguage)
        Call Me.RefreshAttachmentCaptionLanguage()

        Me.cmbSetDateFromAttachType.Text = "Modified"
        Me.chkSetCreatedAtDuringPosting.Checked = True
        Call Me.RefreshCreatedAtBox()
        Call Me.RefreshedAnimatedFlag()

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
    Private Sub SetLanguageBoxData(cboLang As ComboBox)
        cboLang.DataSource = New List(Of Bdiu.LanguageItem)(Me.LanguageList)
        cboLang.DisplayMember = "DisplayName" ' What the user sees
        cboLang.ValueMember = "Bcp47Tag"      ' The actual BCP-47 code stored behind it
        Call Me.SetSystemLanguageAsDefault(cboLang)
    End Sub

    Private Sub SetSystemLanguageAsDefault(cboLang As ComboBox)
        ' PRESELECT THE SYSTEM LANGUAGE 
        ' Get the current Windows UI BCP-47 language tag (e.g., "en-US")
        Dim systemLanguage As String = System.Globalization.CultureInfo.CurrentUICulture.Name

        Me.postData.AddLanguage(systemLanguage)

        ' Assign it to SelectedValue. If it exists in your whitelist, it auto-selects.
        cboLang.SelectedValue = systemLanguage

        Dim languageList = DirectCast(Me.cboLanguageAdd.DataSource, List(Of BskyDirectImageUploader.Bdiu.LanguageItem))

        ' Fallback: If the exact system language isn't in the list (Shouldnt happen, as all installed languages are in the list)
        If cboLang.SelectedValue Is Nothing AndAlso systemLanguage.Contains("-"c) Then
            Dim baseLanguage As String = systemLanguage.Split("-"c)(0) ' Extracts "en" from "en-CA"

            ' Look for an item in your list that starts with that base language
            Dim fallbackItem = languageList.FirstOrDefault(Function(x) x.Bcp47Tag.StartsWith(baseLanguage, StringComparison.OrdinalIgnoreCase))

            If fallbackItem IsNot Nothing Then
                cboLang.SelectedValue = fallbackItem.Bcp47Tag
            End If
        End If

        ' Fallback to english if even that is not found
        If cboLang.SelectedValue Is Nothing Then

            Dim fallbackItem = languageList.FirstOrDefault(Function(x) x.Bcp47Tag.StartsWith("en", StringComparison.OrdinalIgnoreCase))

            If fallbackItem IsNot Nothing Then
                cboLang.SelectedValue = fallbackItem.Bcp47Tag
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
        Call Me.AddAttachmentsSecurely(files)
    End Sub

    Private Sub RemoveAllAttachments()
        If Me.postData.Attachments IsNot Nothing Then
            Call Me.postData.Attachments.Clear()
        End If
        Call lstAttachments.Items.Clear()
        Me.txtAltText.Text = Nothing
        Me.txtAltText.Enabled = False
        Me.txtMimeType.Text = Nothing
        Me.txtMimeType.Enabled = False
        Call Me.ClearAttachmentDimensionsNud()
        Me.nudWidth.Enabled = False
        Me.nudHeight.Enabled = False
        Call Me.RefreshedAnimatedFlag()
    End Sub

    Private Sub AddAttachmentsSecurely(paths() As String)
        For Each path In paths

            Dim ext As String = IO.Path.GetExtension(path).ToLower()
            Dim fileName As String = IO.Path.GetFileName(path)

            ' Add to new attachment data object
            Dim attchData As New Bdiu.BskyUploadPostData.BskyUploadAttachmentData(filePath:=path)

            ' Get the dimensions and mimetype of the file
            Try
                Call attchData.LoadAttachmentData()
            Catch ex As Exception
                ' Ignore exceptions when reading the metadata
            End Try

            If attchData.IsAnimated Then
                ' Animated image or video
                ' 300MB Server limit verification
                Dim fileInfo As New System.IO.FileInfo(path)
                If fileInfo.Length > BskyMaxVideoSize Then
                    Dim messageText As String = $"The file '{fileInfo.Name}' is too large!{Environment.NewLine}({Math.Round(fileInfo.Length / 1000 / 1000, 2)} MB / {Math.Round(fileInfo.Length / 1024 / 1024, 2)} MiB){Environment.NewLine}{Environment.NewLine}Bluesky limits video blobs to {Math.Round(BskyMaxVideoSize / 1000 / 1000, 2)} MB / {Math.Round(BskyMaxVideoSize / 1024 / 1024, 2)} MiB.{Environment.NewLine}{Environment.NewLine}Add the file regardless?"

                    If MessageBox.Show(messageText,
                                       "Size Limit Reached",
                                       MessageBoxButtons.YesNo,
                                       MessageBoxIcon.Exclamation) <> DialogResult.Yes Then
                        Return
                    End If
                End If

                If Me.postData.Attachments IsNot Nothing Then
                    If Me.postData.Attachments.Count = 1 Then
                        If MessageBox.Show($"1 Attachment already added.{Environment.NewLine}Bluesky will reject this post, if you add more videos / animated images.{Environment.NewLine}{Environment.NewLine}Add more regardless?", "Hard Limit Reached", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) <> DialogResult.Yes Then
                            Return
                        End If
                    End If
                End If
            Else
                ' Static image
                ' 2MB Server limit verification
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

                If Me.postData.Attachments IsNot Nothing Then
                    If Me.postData.Attachments.Count = 10 Then
                        If MessageBox.Show($"10 Attachments already added.{Environment.NewLine}Bluesky technically allows up to 20 images per post, but will only display the first 10 on the site.{Environment.NewLine}{Environment.NewLine}Add more regardless?", "Soft Limit Reached", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) <> DialogResult.Yes Then
                            Return
                        End If
                    End If
                    If Me.postData.Attachments.Count = 20 Then
                        If MessageBox.Show($"20 Attachments already added.{Environment.NewLine}Bluesky will reject this post, if you add more images.{Environment.NewLine}{Environment.NewLine}Add more regardless?", "Hard Limit Reached", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) <> DialogResult.Yes Then
                            Return
                        End If
                    End If
                End If
            End If

            If Me.postData.Languages IsNot Nothing AndAlso Me.postData.Languages.Count > 0 Then
                ' By Default set the Caption language from the primary post language
                attchData.CaptionLanguage = Me.postData.Languages.First
            End If

            Me.postData.AddAttachment(attchData)

            Call Me.AddToLog($"Added file {fileName}", False)
            If attchData.Dimensions IsNot Nothing Then
                Call Me.AddToLog($"Read dimensions {attchData.Dimensions.Width} x {attchData.Dimensions.Height} from {fileName}", False)
            Else
                Call Me.AddToLog($"Warning: No dimensions read from {fileName}", Bdiu.BdiuHelper.ColorWarning)
            End If
            If String.IsNullOrWhiteSpace(attchData.MimeType) Then
                Call Me.AddToLog($"Warning: No MIME-Type read from {fileName}", Bdiu.BdiuHelper.ColorWarning)
            Else
                Call Me.AddToLog($"Read MIME-Type {attchData.MimeType} from {fileName}", False)
            End If

            ' Attachment list displayed to user
            Call lstAttachments.Items.Add(fileName)
        Next

        If lstAttachments.SelectedIndex = -1 And Not (Me.postData.Attachments Is Nothing OrElse Me.postData.Attachments.Count = 0) Then
            lstAttachments.SelectedIndex = 0 ' Select first item
        End If

        Call Me.RefreshCreatedAtBox()
    End Sub

    ' Real-time Live Character Counter (300 limit)
    Private Sub txtCaption_TextChanged(sender As Object, e As EventArgs) Handles txtCaption.TextChanged
        Call UpdateCharCount()
    End Sub

    Private Sub UpdateCharCount()
        Dim text As String = txtCaption.Text

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
        Me.grpAttachments.Enabled = False
        Me.grpReplyTo.Enabled = False
        Me.grpCaption.Enabled = False
        Me.grpLabels.Enabled = False
        Me.btnSubmit.Enabled = False
        Call Bdiu.WinApiFunctions.DisableFormCloseButton(Me)
        Me.btnCancel.Enabled = True
    End Sub
    Public Sub EnableFormAfterProcessFinish()
        Me.grpAttachments.Enabled = True
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
        If Me.postData.Attachments Is Nothing Then
            Call AddToLog("No attachments provided. Skip PNG optimization.", Color.DarkGray)
            Return True
        End If

        Dim pngFiles As New HashSet(Of String)
        Dim allowedExts As String() = {".png", ".apng"}


        For Each image In Me.postData.Attachments
            Dim imagePath = image.FilePath

            If allowedExts.Contains(System.IO.Path.GetExtension(imagePath)) Then
                Call pngFiles.Add(imagePath)
            End If
        Next


        If pngFiles.Count = 0 Then
            Call AddToLog("None of the provided attachments is a PNG. Skip optimization.", Color.DarkGray)
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

    Private Sub lstAttachments_SelectedIndexChanged(sender As Object, e As EventArgs) Handles lstAttachments.SelectedIndexChanged
        Call Me.SetAttachmentDataOnForm()
    End Sub

    Private Sub SetAltTextBoxData()
        If lstAttachments.SelectedIndex = -1 OrElse Me.postData.Attachments Is Nothing OrElse Me.postData.Attachments.Count = 0 Then
            Me.txtAltText.Enabled = False
        Else
            Me.txtAltText.Text = Me.postData.Attachments.Item(lstAttachments.SelectedIndex).AltText
            Me.txtAltText.Enabled = True
        End If
    End Sub

    Private Sub txtAltText_Validated(sender As Object, e As EventArgs) Handles txtAltText.Validated
        If lstAttachments.SelectedIndex = -1 OrElse Me.postData.Attachments Is Nothing OrElse Me.postData.Attachments.Count = 0 Then
            Exit Sub
        End If
        Me.postData.Attachments.Item(lstAttachments.SelectedIndex).AltText = Me.txtAltText.Text
    End Sub

    Private Sub txtMimeType_Validated(sender As Object, e As EventArgs) Handles txtMimeType.Validated
        If lstAttachments.SelectedIndex = -1 OrElse Me.postData.Attachments Is Nothing OrElse Me.postData.Attachments.Count = 0 Then
            Exit Sub
        End If
        Me.postData.Attachments.Item(lstAttachments.SelectedIndex).MimeType = Me.txtMimeType.Text
    End Sub

    Private Sub SetMimeTypeTextBoxData()
        If lstAttachments.SelectedIndex = -1 OrElse Me.postData.Attachments Is Nothing OrElse Me.postData.Attachments.Count = 0 Then
            Me.txtMimeType.Enabled = False
        Else
            Me.txtMimeType.Text = Me.postData.Attachments.Item(lstAttachments.SelectedIndex).MimeType
            Me.txtMimeType.Enabled = True
        End If
    End Sub

    Private Sub btnSelectAttch_Click(sender As Object, e As EventArgs) Handles btnSelectAttch.Click
        Dim FileDialog As New OpenFileDialog With {
            .Filter = "Bluesky Supported Images (*.png;*.jpg;*.jpeg;*.jfif;*.webp;*.avif;*.svg)|*.png;*.jpg;*.jpeg;*.jfif;*.webp;*.avif,*.svg|All Files (*.*)|*.*",
            .Multiselect = True,
            .RestoreDirectory = True,
            .Title = "Select 1 or more Images (Hold CTRL to select multiple)"
        }

        Dim DummyForm As New Form() With {.TopMost = True}
        Dim DialogResult As DialogResult = FileDialog.ShowDialog(DummyForm)

        If DialogResult = DialogResult.OK Then
            Call Me.AddAttachmentsSecurely(FileDialog.FileNames)
        End If
    End Sub

    Private Sub btnClearAttch_Click(sender As Object, e As EventArgs) Handles btnClearAttch.Click
        Call RemoveAllAttachments()
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

    Private Sub SetAttachmentDataOnForm()
        Call Me.SetAttachmentDimensionsData()
        Call Me.SetAltTextBoxData()
        Call Me.SetMimeTypeTextBoxData()
        Call Me.RefreshedAnimatedFlag()
        Call Me.RefreshAttachmentCaptionLanguage()
        Call Me.RefreshAttachmentCaptionFile()
    End Sub

    Private Sub ClearAttachmentDimensionsNud()
        Call ClearAttachmentWidthNud()
        Call ClearAttachmentHeightNud()
    End Sub
    Private Sub ClearAttachmentWidthNud()
        Me.nudWidth.Value = 0
        Me.nudWidth.Text = Nothing
    End Sub
    Private Sub ClearAttachmentHeightNud()
        Me.nudHeight.Value = 0
        Me.nudHeight.Text = Nothing
    End Sub

    Private Sub SetAttachmentDimensionsData()

        If lstAttachments.SelectedIndex = -1 OrElse Me.postData.Attachments Is Nothing OrElse Me.postData.Attachments.Count = 0 Then
            Me.nudWidth.Enabled = False
            Me.nudHeight.Enabled = False
            Call Me.ClearAttachmentDimensionsNud()
        Else
            Dim attchDim = Me.postData.Attachments.Item(lstAttachments.SelectedIndex).Dimensions

            If attchDim IsNot Nothing Then
                If attchDim.Width.HasValue Then
                    Dim val = attchDim.Width.Value
                    Me.nudWidth.Text = CStr(val)
                    Me.nudWidth.Value = val
                Else
                    Call Me.ClearAttachmentWidthNud()
                End If
                If attchDim.Height.HasValue Then
                    Dim val = attchDim.Height.Value
                    Me.nudHeight.Text = CStr(val)
                    Me.nudHeight.Value = val
                Else
                    Call Me.ClearAttachmentHeightNud()
                End If
            Else
                Call Me.ClearAttachmentDimensionsNud()
            End If
            Me.nudWidth.Enabled = True
            Me.nudHeight.Enabled = True
        End If
    End Sub

    Private Sub nudWidth_Leave(sender As Object, e As EventArgs) Handles nudWidth.Leave
        If lstAttachments.SelectedIndex = -1 OrElse Me.postData.Attachments Is Nothing OrElse Me.postData.Attachments.Count = 0 Then
        Else
            Dim attchDim = Me.postData.Attachments.Item(lstAttachments.SelectedIndex).Dimensions

            If attchDim IsNot Nothing Then
                If String.IsNullOrWhiteSpace(Me.nudWidth.Text) Then
                    attchDim.Width = Nothing
                    Call Me.SetAttachmentDimensionsData()
                Else
                    Select Case Me.nudWidth.Value
                        Case 0, > UInt32.MaxValue, < UInt32.MinValue
                            attchDim.Width = Nothing
                        Case Else
                            attchDim.Width = CType(Me.nudWidth.Value, System.UInt32)
                    End Select
                End If
            Else
                Select Case Me.nudWidth.Value
                    Case 0, > UInt32.MaxValue, < UInt32.MinValue
                    Case Else
                        Me.postData.Attachments.Item(lstAttachments.SelectedIndex).Dimensions = New Bdiu.BskyUploadPostData.BskyUploadAttachmentData.ImageDimensions(CType(Me.nudWidth.Value, System.UInt32), 0)
                End Select
            End If
        End If
    End Sub

    Private Sub nudHeight_Leave(sender As Object, e As EventArgs) Handles nudHeight.Leave
        If lstAttachments.SelectedIndex = -1 OrElse Me.postData.Attachments Is Nothing OrElse Me.postData.Attachments.Count = 0 Then
        Else
            Dim attchDim = Me.postData.Attachments.Item(lstAttachments.SelectedIndex).Dimensions

            If attchDim IsNot Nothing Then
                If String.IsNullOrWhiteSpace(Me.nudHeight.Text) Then
                    attchDim.Height = Nothing
                    Call Me.SetAttachmentDimensionsData()
                Else
                    Select Case Me.nudHeight.Value
                        Case 0, > UInt32.MaxValue, < UInt32.MinValue
                            attchDim.Height = Nothing
                        Case Else
                            attchDim.Height = CType(Me.nudHeight.Value, System.UInt32)
                    End Select
                End If
            Else
                Select Case Me.nudHeight.Value
                    Case 0, > UInt32.MaxValue, < UInt32.MinValue
                    Case Else
                        Me.postData.Attachments.Item(lstAttachments.SelectedIndex).Dimensions = New Bdiu.BskyUploadPostData.BskyUploadAttachmentData.ImageDimensions(0, CType(Me.nudHeight.Value, System.UInt32))
                End Select
            End If
        End If
    End Sub
    Public Sub RefreshAttachmentCaptionFile()
        If lstAttachments.SelectedIndex = -1 OrElse Me.postData.Attachments Is Nothing OrElse Me.postData.Attachments.Count = 0 Then
            Me.txtCaptionPath.Text = Nothing
        Else
            Me.txtCaptionPath.Text = Me.postData.Attachments.Item(lstAttachments.SelectedIndex).CaptionFilePath

            ' Jump to the end to directly see the filename
            Me.txtCaptionPath.SelectionStart = txtCaptionPath.Text.Length
            Call Me.txtCaptionPath.ScrollToCaret()
        End If
        If String.IsNullOrWhiteSpace(Me.txtCaptionPath.Text) Then
            Me.txtCaptionLanguage.Enabled = False
            Me.cboCaptionLanguage.Enabled = False
        Else
            Me.txtCaptionLanguage.Enabled = True
            Me.cboCaptionLanguage.Enabled = True
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

    Private Sub cboLanguage_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboLanguageAdd.SelectedIndexChanged, cboCaptionLanguage.SelectedIndexChanged
        If TypeOf sender IsNot ComboBox Then
            Return
        End If
        Dim cboSender = DirectCast(sender, ComboBox)

        If cboSender.SelectedValue IsNot Nothing Then
            Dim selectedVal = cboSender.SelectedValue
            Dim selectedBcp47 As String

            If TypeOf selectedVal Is Bdiu.LanguageItem Then
                selectedBcp47 = DirectCast(selectedVal, Bdiu.LanguageItem).Bcp47Tag
            ElseIf TypeOf selectedVal Is String Then
                selectedBcp47 = DirectCast(selectedVal, String)
            Else
                Return
            End If

            If cboSender Is cboLanguageAdd Then
                If Me.postData.AddLanguage(selectedBcp47) Then
                    Call RefreshPostLanguage()
                End If
            ElseIf cboSender Is cboCaptionLanguage Then
                Me.postData.Attachments.Item(lstAttachments.SelectedIndex).CaptionLanguage = selectedBcp47
                Call RefreshAttachmentCaptionLanguage()
            End If
        End If
    End Sub

    Private Sub RefreshPostLanguage()
        If Me.postData.Languages Is Nothing Then
            Me.txtLanguages.Text = Nothing
        Else
            Me.txtLanguages.Text = String.Join(", ", Me.postData.Languages)
        End If
    End Sub

    Private Sub RefreshAttachmentCaptionLanguage()
        If lstAttachments.SelectedIndex = -1 OrElse Me.postData.Attachments Is Nothing OrElse Me.postData.Attachments.Count = 0 Then
            Return
        End If
        RemoveHandler cboCaptionLanguage.SelectedIndexChanged, AddressOf cboLanguage_SelectedIndexChanged
        RemoveHandler txtCaptionLanguage.Validated, AddressOf txtCaptionLanguage_Validated
        If Me.postData.Attachments.Item(lstAttachments.SelectedIndex).CaptionLanguage Is Nothing Then
            Me.cboCaptionLanguage.SelectedValue = String.Empty ' Set to empty string instead of null to prevent a crash
        Else
            Me.cboCaptionLanguage.SelectedValue = Me.postData.Attachments.Item(lstAttachments.SelectedIndex).CaptionLanguage
        End If
        Me.txtCaptionLanguage.Text = Me.postData.Attachments.Item(lstAttachments.SelectedIndex).CaptionLanguage
        AddHandler cboCaptionLanguage.SelectedIndexChanged, AddressOf cboLanguage_SelectedIndexChanged
        AddHandler txtCaptionLanguage.Validated, AddressOf txtCaptionLanguage_Validated
    End Sub

    Private Sub txtLanguages_Validated(sender As Object, e As EventArgs) Handles txtLanguages.Validated
        Me.postData.ClearLanguages()
        If String.IsNullOrWhiteSpace(txtLanguages.Text) Then
            Me.postData.Languages = Nothing
            Call RefreshPostLanguage()
            Return
        End If

        ' Split by commas
        Dim rawTokens As String() = txtLanguages.Text.Split(","c)

        For Each token As String In rawTokens
            ' Clean up spaces
            Dim cleanTag As String = token.Trim()
            Me.postData.AddLanguage(cleanTag)
        Next
        Call RefreshPostLanguage()
    End Sub

    Private Sub txtCaptionLanguage_Validated(sender As Object, e As EventArgs) Handles txtCaptionLanguage.Validated
        Me.postData.ClearLanguages()
        If String.IsNullOrWhiteSpace(Me.txtCaptionLanguage.Text) Then
            Me.postData.Attachments.Item(lstAttachments.SelectedIndex).CaptionLanguage = Nothing
            Call Me.RefreshAttachmentCaptionLanguage()
            Return
        End If

        Me.postData.Attachments.Item(lstAttachments.SelectedIndex).CaptionLanguage = Me.txtCaptionLanguage.Text.Trim()

        Call Me.RefreshAttachmentCaptionLanguage()
    End Sub

    Public Sub RefreshCreatedAtBox()
        If Me.chkSetCreatedAtDuringPosting.Checked Then
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
            If Me.chkSetCreatedAtByAttachments.Checked AndAlso Me.postData.Attachments IsNot Nothing AndAlso Me.postData.Attachments.Count > 0 Then
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

    Private Sub chkSetCreatedAtDuringPosting_CheckedChanged(sender As Object, e As EventArgs) Handles chkSetCreatedAtDuringPosting.CheckedChanged
        If Me.chkSetCreatedAtDuringPosting.Checked Then
            Me.chkSetCreatedAtByAttachments.Checked = False
        End If
        Call Me.RefreshCreatedAtBox()
    End Sub
    Private Sub chkSetCreatedAtByAttachments_CheckedChanged(sender As Object, e As EventArgs) Handles chkSetCreatedAtByAttachments.CheckedChanged
        If Me.chkSetCreatedAtByAttachments.Checked Then
            Me.chkSetCreatedAtDuringPosting.Checked = False
        End If
        Call Me.RefreshCreatedAtBox()
    End Sub
    Private Sub cmbSetDateFromAttachType_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbSetDateFromAttachType.SelectedIndexChanged
        If Me.chkSetCreatedAtByAttachments.Checked Then
            Call Me.RefreshCreatedAtBox()
        End If
    End Sub

    Private Sub chkIsAnimated_CheckedChanged(sender As Object, e As EventArgs) Handles chkIsAnimated.CheckedChanged
        If lstAttachments.SelectedIndex = -1 OrElse Me.postData.Attachments Is Nothing OrElse Me.postData.Attachments.Count = 0 Then
        Else
            Me.postData.Attachments.Item(lstAttachments.SelectedIndex).IsAnimated = chkIsAnimated.Checked
        End If
        Call RefreshedAnimatedFlag()
    End Sub

    Private Sub RefreshedAnimatedFlag()
        If lstAttachments.SelectedIndex = -1 OrElse Me.postData.Attachments Is Nothing OrElse Me.postData.Attachments.Count = 0 Then
            Me.chkIsAnimated.Enabled = False
            Me.chkAsGIF.Enabled = False
            Me.chkIsAnimated.CheckState = CheckState.Indeterminate
            Me.chkAsGIF.CheckState = CheckState.Indeterminate
            Me.grpAttachmentCaptions.Enabled = False
            Exit Sub
        End If
        Dim attch = Me.postData.Attachments.Item(lstAttachments.SelectedIndex)
        Me.chkIsAnimated.Enabled = True
        Me.chkAsGIF.Enabled = attch.IsAnimated
        Me.grpAttachmentCaptions.Enabled = attch.IsAnimated
        If attch.IsAnimated Then
            Me.chkIsAnimated.CheckState = CheckState.Checked
            If attch.PresentAsGif Then
                Me.chkAsGIF.CheckState = CheckState.Checked
            Else
                Me.chkAsGIF.CheckState = CheckState.Unchecked
            End If
        Else
            Me.chkIsAnimated.CheckState = CheckState.Unchecked
            attch.PresentAsGif = False
            Me.chkAsGIF.CheckState = CheckState.Unchecked
        End If
    End Sub

    Private Sub chkAsGIF_CheckedChanged(sender As Object, e As EventArgs) Handles chkAsGIF.CheckedChanged
        If lstAttachments.SelectedIndex = -1 OrElse Me.postData.Attachments Is Nothing OrElse Me.postData.Attachments.Count = 0 Then
            Exit Sub
        End If
        Me.postData.Attachments.Item(lstAttachments.SelectedIndex).PresentAsGif = chkAsGIF.Checked
        Call RefreshedAnimatedFlag()
    End Sub

    Private Sub btnClearCaption_Click(sender As Object, e As EventArgs) Handles btnClearCaption.Click
        If lstAttachments.SelectedIndex = -1 OrElse Me.postData.Attachments Is Nothing OrElse Me.postData.Attachments.Count = 0 Then
            Exit Sub
        End If
        Me.postData.Attachments.Item(lstAttachments.SelectedIndex).CaptionFilePath = Nothing
        Call Me.RefreshAttachmentCaptionFile()
    End Sub

    Private Sub btnSelectCaption_Click(sender As Object, e As EventArgs) Handles btnSelectCaption.Click
        If lstAttachments.SelectedIndex = -1 OrElse Me.postData.Attachments Is Nothing OrElse Me.postData.Attachments.Count = 0 Then
            Exit Sub
        End If
        Dim FileDialog As New OpenFileDialog With {
            .Filter = "Bluesky Supported Captions (*.vtt)|*.vtt|All Files (*.*)|*.*",
            .Multiselect = False,
            .RestoreDirectory = True,
            .Title = "Select caption file"
        }

        Dim DummyForm As New Form() With {.TopMost = True}
        Dim DialogResult As DialogResult = FileDialog.ShowDialog(DummyForm)

        If DialogResult = DialogResult.OK Then
            Me.postData.Attachments.Item(lstAttachments.SelectedIndex).CaptionFilePath = FileDialog.FileName
        End If
        Call Me.RefreshAttachmentCaptionFile()
    End Sub
End Class
#Enable Warning IDE1006 ' Naming Styles