<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class BdiuBskyPostBuilderForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        txtCaption = New TextBox()
        txtReplyUrl = New TextBox()
        lstImages = New ListBox()
        lblGraphemeCount = New Label()
        chkSexual = New CheckBox()
        chkNudity = New CheckBox()
        chkGraphic = New CheckBox()
        chkPorn = New CheckBox()
        btnSubmit = New Button()
        grpLabels = New GroupBox()
        txtWarningLabels = New TextBox()
        grpCaption = New GroupBox()
        lblByteCount = New Label()
        cboLanguageAdd = New ComboBox()
        grpImages = New GroupBox()
        txtMimeType = New TextBox()
        lblMimeType = New Label()
        lblHeight = New Label()
        nudHeight = New NumericUpDown()
        lblWidth = New Label()
        nudWidth = New NumericUpDown()
        btnClearImg = New Button()
        txtAltText = New TextBox()
        btnSelectImg = New Button()
        chkOptPng = New CheckBox()
        btnOpenOxiPngForm = New Button()
        grpReplyTo = New GroupBox()
        chkQuote = New CheckBox()
        grpLog = New GroupBox()
        ucLog = New LogControl()
        btnCancel = New Button()
        grpLanguages = New GroupBox()
        txtLanguages = New TextBox()
        lblLanguageAdd = New Label()
        lblSetCreatedAtPicker = New Label()
        dtpCreatedAt = New DateTimePicker()
        gtpCreatedAt = New GroupBox()
        cmbSetDateFromAttachType = New ComboBox()
        btnSetCreatedAtDuringPosting = New CheckBox()
        txtCreatedAt = New TextBox()
        lblSetCreatedAtIso = New Label()
        btnSetCreatedAtByAttachments = New CheckBox()
        grpLabels.SuspendLayout()
        grpCaption.SuspendLayout()
        grpImages.SuspendLayout()
        CType(nudHeight, ComponentModel.ISupportInitialize).BeginInit()
        CType(nudWidth, ComponentModel.ISupportInitialize).BeginInit()
        grpReplyTo.SuspendLayout()
        grpLog.SuspendLayout()
        grpLanguages.SuspendLayout()
        gtpCreatedAt.SuspendLayout()
        SuspendLayout()
        ' 
        ' txtCaption
        ' 
        txtCaption.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        txtCaption.Location = New Point(6, 22)
        txtCaption.Multiline = True
        txtCaption.Name = "txtCaption"
        txtCaption.PlaceholderText = "Write your post caption (optional)"
        txtCaption.Size = New Size(329, 63)
        txtCaption.TabIndex = 14
        ' 
        ' txtReplyUrl
        ' 
        txtReplyUrl.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        txtReplyUrl.Location = New Point(6, 15)
        txtReplyUrl.Name = "txtReplyUrl"
        txtReplyUrl.PlaceholderText = "URL to the post you are replying to (optional)"
        txtReplyUrl.Size = New Size(402, 23)
        txtReplyUrl.TabIndex = 12
        ' 
        ' lstImages
        ' 
        lstImages.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        lstImages.FormattingEnabled = True
        lstImages.Location = New Point(6, 48)
        lstImages.Name = "lstImages"
        lstImages.Size = New Size(239, 64)
        lstImages.TabIndex = 3
        ' 
        ' lblGraphemeCount
        ' 
        lblGraphemeCount.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        lblGraphemeCount.AutoSize = True
        lblGraphemeCount.Location = New Point(3, 88)
        lblGraphemeCount.Name = "lblGraphemeCount"
        lblGraphemeCount.Size = New Size(110, 15)
        lblGraphemeCount.TabIndex = 15
        lblGraphemeCount.Text = "Characters: ??? / ???"
        lblGraphemeCount.TextAlign = ContentAlignment.BottomLeft
        ' 
        ' chkSexual
        ' 
        chkSexual.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        chkSexual.AutoSize = True
        chkSexual.Location = New Point(6, 22)
        chkSexual.Name = "chkSexual"
        chkSexual.Size = New Size(83, 19)
        chkSexual.TabIndex = 18
        chkSexual.Text = "Suggestive"
        chkSexual.UseVisualStyleBackColor = True
        ' 
        ' chkNudity
        ' 
        chkNudity.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        chkNudity.AutoSize = True
        chkNudity.Location = New Point(6, 47)
        chkNudity.Name = "chkNudity"
        chkNudity.Size = New Size(129, 19)
        chkNudity.TabIndex = 19
        chkNudity.Text = "Nudity (non-erotic)"
        chkNudity.UseVisualStyleBackColor = True
        ' 
        ' chkGraphic
        ' 
        chkGraphic.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        chkGraphic.AutoSize = True
        chkGraphic.Location = New Point(6, 97)
        chkGraphic.Name = "chkGraphic"
        chkGraphic.Size = New Size(103, 19)
        chkGraphic.TabIndex = 21
        chkGraphic.Text = "Graphic Media"
        chkGraphic.UseVisualStyleBackColor = True
        ' 
        ' chkPorn
        ' 
        chkPorn.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        chkPorn.AutoSize = True
        chkPorn.Location = New Point(6, 72)
        chkPorn.Name = "chkPorn"
        chkPorn.Size = New Size(91, 19)
        chkPorn.TabIndex = 20
        chkPorn.Text = "Adult (Porn)"
        chkPorn.UseVisualStyleBackColor = True
        ' 
        ' btnSubmit
        ' 
        btnSubmit.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnSubmit.Location = New Point(601, 346)
        btnSubmit.Name = "btnSubmit"
        btnSubmit.Size = New Size(126, 23)
        btnSubmit.TabIndex = 31
        btnSubmit.Text = "Submit"
        btnSubmit.UseVisualStyleBackColor = True
        ' 
        ' grpLabels
        ' 
        grpLabels.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Right
        grpLabels.Controls.Add(txtWarningLabels)
        grpLabels.Controls.Add(chkSexual)
        grpLabels.Controls.Add(chkNudity)
        grpLabels.Controls.Add(chkGraphic)
        grpLabels.Controls.Add(chkPorn)
        grpLabels.Location = New Point(595, 56)
        grpLabels.Name = "grpLabels"
        grpLabels.Size = New Size(138, 264)
        grpLabels.TabIndex = 29
        grpLabels.TabStop = False
        grpLabels.Text = "Warning Labels"
        ' 
        ' txtWarningLabels
        ' 
        txtWarningLabels.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        txtWarningLabels.Location = New Point(6, 122)
        txtWarningLabels.Multiline = True
        txtWarningLabels.Name = "txtWarningLabels"
        txtWarningLabels.Size = New Size(126, 136)
        txtWarningLabels.TabIndex = 22
        ' 
        ' grpCaption
        ' 
        grpCaption.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        grpCaption.Controls.Add(lblGraphemeCount)
        grpCaption.Controls.Add(lblByteCount)
        grpCaption.Controls.Add(txtCaption)
        grpCaption.Location = New Point(254, 56)
        grpCaption.Name = "grpCaption"
        grpCaption.Size = New Size(341, 106)
        grpCaption.TabIndex = 13
        grpCaption.TabStop = False
        grpCaption.Text = "Post caption"
        ' 
        ' lblByteCount
        ' 
        lblByteCount.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        lblByteCount.Location = New Point(181, 88)
        lblByteCount.Name = "lblByteCount"
        lblByteCount.Size = New Size(157, 15)
        lblByteCount.TabIndex = 16
        lblByteCount.Text = "Bytes: ???? / ????"
        lblByteCount.TextAlign = ContentAlignment.BottomRight
        ' 
        ' cboLanguageAdd
        ' 
        cboLanguageAdd.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        cboLanguageAdd.AutoCompleteMode = AutoCompleteMode.SuggestAppend
        cboLanguageAdd.AutoCompleteSource = AutoCompleteSource.ListItems
        cboLanguageAdd.FormattingEnabled = True
        cboLanguageAdd.Location = New Point(99, 19)
        cboLanguageAdd.Name = "cboLanguageAdd"
        cboLanguageAdd.Size = New Size(236, 23)
        cboLanguageAdd.TabIndex = 19
        ' 
        ' grpImages
        ' 
        grpImages.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left
        grpImages.Controls.Add(txtMimeType)
        grpImages.Controls.Add(lblMimeType)
        grpImages.Controls.Add(lblHeight)
        grpImages.Controls.Add(nudHeight)
        grpImages.Controls.Add(lblWidth)
        grpImages.Controls.Add(nudWidth)
        grpImages.Controls.Add(btnClearImg)
        grpImages.Controls.Add(txtAltText)
        grpImages.Controls.Add(btnSelectImg)
        grpImages.Controls.Add(lstImages)
        grpImages.Controls.Add(chkOptPng)
        grpImages.Controls.Add(btnOpenOxiPngForm)
        grpImages.Location = New Point(3, 12)
        grpImages.Name = "grpImages"
        grpImages.Size = New Size(251, 352)
        grpImages.TabIndex = 0
        grpImages.TabStop = False
        grpImages.Text = "Images"
        ' 
        ' txtMimeType
        ' 
        txtMimeType.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        txtMimeType.Location = New Point(82, 141)
        txtMimeType.Name = "txtMimeType"
        txtMimeType.PlaceholderText = "MIME-Type of current image"
        txtMimeType.Size = New Size(163, 23)
        txtMimeType.TabIndex = 9
        ' 
        ' lblMimeType
        ' 
        lblMimeType.AutoSize = True
        lblMimeType.Location = New Point(6, 144)
        lblMimeType.Name = "lblMimeType"
        lblMimeType.Size = New Size(70, 15)
        lblMimeType.TabIndex = 8
        lblMimeType.Text = "MIME-Type:"
        ' 
        ' lblHeight
        ' 
        lblHeight.AutoSize = True
        lblHeight.Location = New Point(126, 117)
        lblHeight.Name = "lblHeight"
        lblHeight.Size = New Size(46, 15)
        lblHeight.TabIndex = 6
        lblHeight.Text = "Height:"
        ' 
        ' nudHeight
        ' 
        nudHeight.Location = New Point(175, 115)
        nudHeight.Maximum = New Decimal(New Integer() {-1, -1, -1, 0})
        nudHeight.Name = "nudHeight"
        nudHeight.Size = New Size(70, 23)
        nudHeight.TabIndex = 7
        ' 
        ' lblWidth
        ' 
        lblWidth.AutoSize = True
        lblWidth.Location = New Point(6, 117)
        lblWidth.Name = "lblWidth"
        lblWidth.Size = New Size(42, 15)
        lblWidth.TabIndex = 4
        lblWidth.Text = "Width:"
        ' 
        ' nudWidth
        ' 
        nudWidth.Location = New Point(50, 115)
        nudWidth.Maximum = New Decimal(New Integer() {-1, -1, -1, 0})
        nudWidth.Name = "nudWidth"
        nudWidth.Size = New Size(70, 23)
        nudWidth.TabIndex = 5
        ' 
        ' btnClearImg
        ' 
        btnClearImg.Location = New Point(6, 22)
        btnClearImg.Name = "btnClearImg"
        btnClearImg.Size = New Size(114, 23)
        btnClearImg.TabIndex = 1
        btnClearImg.Text = "Clear"
        btnClearImg.UseVisualStyleBackColor = True
        ' 
        ' txtAltText
        ' 
        txtAltText.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        txtAltText.Location = New Point(6, 166)
        txtAltText.Multiline = True
        txtAltText.Name = "txtAltText"
        txtAltText.PlaceholderText = "Alt text for current image"
        txtAltText.Size = New Size(239, 142)
        txtAltText.TabIndex = 10
        ' 
        ' btnSelectImg
        ' 
        btnSelectImg.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnSelectImg.Location = New Point(131, 22)
        btnSelectImg.Name = "btnSelectImg"
        btnSelectImg.Size = New Size(114, 23)
        btnSelectImg.TabIndex = 2
        btnSelectImg.Text = "Select"
        btnSelectImg.UseVisualStyleBackColor = True
        ' 
        ' chkOptPng
        ' 
        chkOptPng.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        chkOptPng.AutoSize = True
        chkOptPng.Location = New Point(6, 312)
        chkOptPng.Name = "chkOptPng"
        chkOptPng.Size = New Size(121, 34)
        chkOptPng.TabIndex = 11
        chkOptPng.Text = "Optimize PNGs" & vbCrLf & "(Requires Oxipng)"
        chkOptPng.UseVisualStyleBackColor = True
        ' 
        ' btnOpenOxiPngForm
        ' 
        btnOpenOxiPngForm.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnOpenOxiPngForm.Location = New Point(133, 318)
        btnOpenOxiPngForm.Name = "btnOpenOxiPngForm"
        btnOpenOxiPngForm.Size = New Size(91, 23)
        btnOpenOxiPngForm.TabIndex = 12
        btnOpenOxiPngForm.Text = "Setup Args"
        btnOpenOxiPngForm.UseVisualStyleBackColor = True
        ' 
        ' grpReplyTo
        ' 
        grpReplyTo.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        grpReplyTo.Controls.Add(chkQuote)
        grpReplyTo.Controls.Add(txtReplyUrl)
        grpReplyTo.Location = New Point(254, 12)
        grpReplyTo.Name = "grpReplyTo"
        grpReplyTo.Size = New Size(479, 43)
        grpReplyTo.TabIndex = 11
        grpReplyTo.TabStop = False
        grpReplyTo.Text = "Reply to"
        ' 
        ' chkQuote
        ' 
        chkQuote.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        chkQuote.AutoSize = True
        chkQuote.Location = New Point(414, 17)
        chkQuote.Name = "chkQuote"
        chkQuote.Size = New Size(59, 19)
        chkQuote.TabIndex = 23
        chkQuote.Text = "Quote"
        chkQuote.UseVisualStyleBackColor = True
        ' 
        ' grpLog
        ' 
        grpLog.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        grpLog.Controls.Add(ucLog)
        grpLog.Location = New Point(3, 364)
        grpLog.Name = "grpLog"
        grpLog.Size = New Size(740, 136)
        grpLog.TabIndex = 40
        grpLog.TabStop = False
        grpLog.Text = "Log"
        ' 
        ' ucLog
        ' 
        ucLog.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        ucLog.AutoSizeMode = AutoSizeMode.GrowAndShrink
        ucLog.Location = New Point(6, 17)
        ucLog.Name = "ucLog"
        ucLog.Size = New Size(728, 115)
        ucLog.TabIndex = 40
        ' 
        ' btnCancel
        ' 
        btnCancel.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnCancel.Enabled = False
        btnCancel.Location = New Point(601, 322)
        btnCancel.Name = "btnCancel"
        btnCancel.Size = New Size(126, 23)
        btnCancel.TabIndex = 30
        btnCancel.Text = "Cancel"
        btnCancel.UseVisualStyleBackColor = True
        ' 
        ' grpLanguages
        ' 
        grpLanguages.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        grpLanguages.Controls.Add(txtLanguages)
        grpLanguages.Controls.Add(lblLanguageAdd)
        grpLanguages.Controls.Add(cboLanguageAdd)
        grpLanguages.Location = New Point(254, 162)
        grpLanguages.Name = "grpLanguages"
        grpLanguages.Size = New Size(341, 77)
        grpLanguages.TabIndex = 17
        grpLanguages.TabStop = False
        grpLanguages.Text = "Languages"
        ' 
        ' txtLanguages
        ' 
        txtLanguages.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        txtLanguages.Location = New Point(6, 48)
        txtLanguages.Name = "txtLanguages"
        txtLanguages.PlaceholderText = "Comma separated list of BCP-47 language codes"
        txtLanguages.Size = New Size(329, 23)
        txtLanguages.TabIndex = 20
        ' 
        ' lblLanguageAdd
        ' 
        lblLanguageAdd.AutoSize = True
        lblLanguageAdd.Location = New Point(6, 22)
        lblLanguageAdd.Name = "lblLanguageAdd"
        lblLanguageAdd.Size = New Size(87, 15)
        lblLanguageAdd.TabIndex = 18
        lblLanguageAdd.Text = "Add Language:"
        ' 
        ' lblSetCreatedAtPicker
        ' 
        lblSetCreatedAtPicker.AutoSize = True
        lblSetCreatedAtPicker.Location = New Point(6, 23)
        lblSetCreatedAtPicker.Name = "lblSetCreatedAtPicker"
        lblSetCreatedAtPicker.Size = New Size(69, 15)
        lblSetCreatedAtPicker.TabIndex = 22
        lblSetCreatedAtPicker.Text = "Date picker:"
        ' 
        ' dtpCreatedAt
        ' 
        dtpCreatedAt.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        dtpCreatedAt.Location = New Point(81, 17)
        dtpCreatedAt.Name = "dtpCreatedAt"
        dtpCreatedAt.Size = New Size(254, 23)
        dtpCreatedAt.TabIndex = 23
        ' 
        ' gtpCreatedAt
        ' 
        gtpCreatedAt.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        gtpCreatedAt.Controls.Add(cmbSetDateFromAttachType)
        gtpCreatedAt.Controls.Add(btnSetCreatedAtDuringPosting)
        gtpCreatedAt.Controls.Add(txtCreatedAt)
        gtpCreatedAt.Controls.Add(lblSetCreatedAtIso)
        gtpCreatedAt.Controls.Add(lblSetCreatedAtPicker)
        gtpCreatedAt.Controls.Add(dtpCreatedAt)
        gtpCreatedAt.Controls.Add(btnSetCreatedAtByAttachments)
        gtpCreatedAt.Location = New Point(254, 239)
        gtpCreatedAt.Name = "gtpCreatedAt"
        gtpCreatedAt.Size = New Size(341, 125)
        gtpCreatedAt.TabIndex = 21
        gtpCreatedAt.TabStop = False
        gtpCreatedAt.Text = "Created at"
        ' 
        ' cmbSetDateFromAttachType
        ' 
        cmbSetDateFromAttachType.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        cmbSetDateFromAttachType.DropDownStyle = ComboBoxStyle.DropDownList
        cmbSetDateFromAttachType.FlatStyle = FlatStyle.Flat
        cmbSetDateFromAttachType.FormattingEnabled = True
        cmbSetDateFromAttachType.Items.AddRange(New Object() {"Created", "Modified"})
        cmbSetDateFromAttachType.Location = New Point(195, 93)
        cmbSetDateFromAttachType.Name = "cmbSetDateFromAttachType"
        cmbSetDateFromAttachType.Size = New Size(73, 23)
        cmbSetDateFromAttachType.Sorted = True
        cmbSetDateFromAttachType.TabIndex = 28
        ' 
        ' btnSetCreatedAtDuringPosting
        ' 
        btnSetCreatedAtDuringPosting.AutoSize = True
        btnSetCreatedAtDuringPosting.Location = New Point(6, 74)
        btnSetCreatedAtDuringPosting.Name = "btnSetCreatedAtDuringPosting"
        btnSetCreatedAtDuringPosting.Size = New Size(248, 19)
        btnSetCreatedAtDuringPosting.TabIndex = 26
        btnSetCreatedAtDuringPosting.Text = "Set the value based on the time of posting"
        btnSetCreatedAtDuringPosting.UseVisualStyleBackColor = True
        ' 
        ' txtCreatedAt
        ' 
        txtCreatedAt.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        txtCreatedAt.Location = New Point(81, 45)
        txtCreatedAt.Name = "txtCreatedAt"
        txtCreatedAt.Size = New Size(254, 23)
        txtCreatedAt.TabIndex = 25
        ' 
        ' lblSetCreatedAtIso
        ' 
        lblSetCreatedAtIso.AutoSize = True
        lblSetCreatedAtIso.Location = New Point(6, 48)
        lblSetCreatedAtIso.Name = "lblSetCreatedAtIso"
        lblSetCreatedAtIso.Size = New Size(55, 15)
        lblSetCreatedAtIso.TabIndex = 24
        lblSetCreatedAtIso.Text = "ISO 8601:"
        ' 
        ' btnSetCreatedAtByAttachments
        ' 
        btnSetCreatedAtByAttachments.AutoSize = True
        btnSetCreatedAtByAttachments.Location = New Point(6, 95)
        btnSetCreatedAtByAttachments.Name = "btnSetCreatedAtByAttachments"
        btnSetCreatedAtByAttachments.Size = New Size(293, 19)
        btnSetCreatedAtByAttachments.TabIndex = 27
        btnSetCreatedAtByAttachments.Text = "Set value by highest attachment                         date"
        btnSetCreatedAtByAttachments.UseVisualStyleBackColor = True
        ' 
        ' BdiuBskyPostBuilderForm
        ' 
        AllowDrop = True
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(744, 500)
        Controls.Add(gtpCreatedAt)
        Controls.Add(grpLanguages)
        Controls.Add(btnCancel)
        Controls.Add(grpReplyTo)
        Controls.Add(grpImages)
        Controls.Add(grpCaption)
        Controls.Add(grpLabels)
        Controls.Add(btnSubmit)
        Controls.Add(grpLog)
        Name = "BdiuBskyPostBuilderForm"
        Text = "Build Bluesky Post"
        grpLabels.ResumeLayout(False)
        grpLabels.PerformLayout()
        grpCaption.ResumeLayout(False)
        grpCaption.PerformLayout()
        grpImages.ResumeLayout(False)
        grpImages.PerformLayout()
        CType(nudHeight, ComponentModel.ISupportInitialize).EndInit()
        CType(nudWidth, ComponentModel.ISupportInitialize).EndInit()
        grpReplyTo.ResumeLayout(False)
        grpReplyTo.PerformLayout()
        grpLog.ResumeLayout(False)
        grpLanguages.ResumeLayout(False)
        grpLanguages.PerformLayout()
        gtpCreatedAt.ResumeLayout(False)
        gtpCreatedAt.PerformLayout()
        ResumeLayout(False)
    End Sub
    Friend WithEvents lstImages As ListBox
    Friend WithEvents chkSexual As CheckBox
    Friend WithEvents chkNudity As CheckBox
    Friend WithEvents chkGraphic As CheckBox
    Friend WithEvents chkPorn As CheckBox
    Friend WithEvents btnSubmit As Button
    Friend WithEvents grpLabels As GroupBox
    Friend WithEvents grpCaption As GroupBox
    Private WithEvents lblGraphemeCount As Label
    Private WithEvents lblByteCount As Label
    Friend WithEvents grpImages As GroupBox
    Friend WithEvents btnSelectImg As Button
    Friend WithEvents grpReplyTo As GroupBox
    Friend WithEvents btnClearImg As Button
    Friend WithEvents txtWarningLabels As TextBox
    Friend WithEvents grpLog As GroupBox
    Friend WithEvents ucLog As LogControl
    Friend WithEvents chkOptPng As CheckBox
    Friend WithEvents btnOpenOxiPngForm As Button
    Friend WithEvents btnCancel As Button
    Private WithEvents txtCaption As TextBox
    Private WithEvents txtReplyUrl As TextBox
    Private WithEvents txtAltText As TextBox
    Friend WithEvents lblWidth As Label
    Friend WithEvents nudWidth As NumericUpDown
    Friend WithEvents lblHeight As Label
    Friend WithEvents nudHeight As NumericUpDown
    Friend WithEvents lblMimeType As Label
    Friend WithEvents txtMimeType As TextBox
    Friend WithEvents chkQuote As CheckBox
    Friend WithEvents cboLanguageAdd As ComboBox
    Friend WithEvents grpLanguages As GroupBox
    Friend WithEvents lblLanguageAdd As Label
    Friend WithEvents txtLanguages As TextBox
    Friend WithEvents lblSetCreatedAtPicker As Label
    Friend WithEvents dtpCreatedAt As DateTimePicker
    Friend WithEvents gtpCreatedAt As GroupBox
    Friend WithEvents lblSetCreatedAtIso As Label
    Friend WithEvents txtCreatedAt As TextBox
    Friend WithEvents btnSetCreatedAtDuringPosting As CheckBox
    Friend WithEvents btnSetCreatedAtByAttachments As CheckBox
    Friend WithEvents cmbSetDateFromAttachType As ComboBox

End Class
