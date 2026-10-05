Imports BskyDirectImageUploader.Bdiu.OxiPngOptimize

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class OxiPngSelectForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(OxiPngSelectForm))
        txtOxiPngPath = New TextBox()
        lblOxiPngPath = New Label()
        lblOxiPngDesc = New Label()
        btnDownloadOxiPng = New Button()
        lblOxiPngArgs = New Label()
        txtOxiPngArgs = New TextBox()
        grpImages = New GroupBox()
        btnClearImg = New Button()
        btnSelectImg = New Button()
        lstImages = New ListBox()
        btnExecute = New Button()
        cmbOxiArgPresets = New ComboBox()
        btnCancel = New Button()
        ucOxiLog = New LogControl()
        grpImages.SuspendLayout()
        SuspendLayout()
        ' 
        ' txtOxiPngPath
        ' 
        txtOxiPngPath.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        txtOxiPngPath.Location = New Point(94, 12)
        txtOxiPngPath.Name = "txtOxiPngPath"
        txtOxiPngPath.PlaceholderText = "Path to oxipng.exe"
        txtOxiPngPath.ReadOnly = True
        txtOxiPngPath.Size = New Size(487, 23)
        txtOxiPngPath.TabIndex = 1
        ' 
        ' lblOxiPngPath
        ' 
        lblOxiPngPath.AutoSize = True
        lblOxiPngPath.Location = New Point(12, 15)
        lblOxiPngPath.Name = "lblOxiPngPath"
        lblOxiPngPath.Size = New Size(76, 15)
        lblOxiPngPath.TabIndex = 0
        lblOxiPngPath.Text = "Oxipng Path:"
        ' 
        ' lblOxiPngDesc
        ' 
        lblOxiPngDesc.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        lblOxiPngDesc.AutoSize = True
        lblOxiPngDesc.Location = New Point(12, 38)
        lblOxiPngDesc.Name = "lblOxiPngDesc"
        lblOxiPngDesc.Size = New Size(487, 15)
        lblOxiPngDesc.TabIndex = 3
        lblOxiPngDesc.Text = "The Oxipng executable must be either in %PATH% or in the same directory as this program."
        ' 
        ' btnDownloadOxiPng
        ' 
        btnDownloadOxiPng.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnDownloadOxiPng.Location = New Point(587, 11)
        btnDownloadOxiPng.Name = "btnDownloadOxiPng"
        btnDownloadOxiPng.Size = New Size(79, 25)
        btnDownloadOxiPng.TabIndex = 2
        btnDownloadOxiPng.Text = "Download"
        btnDownloadOxiPng.UseVisualStyleBackColor = True
        ' 
        ' lblOxiPngArgs
        ' 
        lblOxiPngArgs.AutoSize = True
        lblOxiPngArgs.Location = New Point(19, 70)
        lblOxiPngArgs.Name = "lblOxiPngArgs"
        lblOxiPngArgs.Size = New Size(69, 15)
        lblOxiPngArgs.TabIndex = 4
        lblOxiPngArgs.Text = "Arguments:"
        ' 
        ' txtOxiPngArgs
        ' 
        txtOxiPngArgs.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        txtOxiPngArgs.Location = New Point(94, 67)
        txtOxiPngArgs.Name = "txtOxiPngArgs"
        txtOxiPngArgs.PlaceholderText = "Arguments used for Oxipng"
        txtOxiPngArgs.Size = New Size(451, 23)
        txtOxiPngArgs.TabIndex = 5
        ' 
        ' grpImages
        ' 
        grpImages.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left
        grpImages.Controls.Add(btnClearImg)
        grpImages.Controls.Add(btnSelectImg)
        grpImages.Controls.Add(lstImages)
        grpImages.Location = New Point(12, 96)
        grpImages.Name = "grpImages"
        grpImages.Size = New Size(241, 166)
        grpImages.TabIndex = 7
        grpImages.TabStop = False
        grpImages.Text = "Images"
        ' 
        ' btnClearImg
        ' 
        btnClearImg.Location = New Point(6, 22)
        btnClearImg.Name = "btnClearImg"
        btnClearImg.Size = New Size(114, 23)
        btnClearImg.TabIndex = 8
        btnClearImg.Text = "Clear"
        btnClearImg.UseVisualStyleBackColor = True
        ' 
        ' btnSelectImg
        ' 
        btnSelectImg.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        btnSelectImg.Location = New Point(121, 22)
        btnSelectImg.Name = "btnSelectImg"
        btnSelectImg.Size = New Size(114, 23)
        btnSelectImg.TabIndex = 9
        btnSelectImg.Text = "Select"
        btnSelectImg.UseVisualStyleBackColor = True
        ' 
        ' lstImages
        ' 
        lstImages.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        lstImages.FormattingEnabled = True
        lstImages.Location = New Point(6, 48)
        lstImages.Name = "lstImages"
        lstImages.Size = New Size(229, 109)
        lstImages.TabIndex = 10
        ' 
        ' btnExecute
        ' 
        btnExecute.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        btnExecute.Location = New Point(259, 118)
        btnExecute.Name = "btnExecute"
        btnExecute.Size = New Size(404, 23)
        btnExecute.TabIndex = 12
        btnExecute.Text = "Start PNG optimization"
        btnExecute.UseVisualStyleBackColor = True
        ' 
        ' cmbOxiArgPresets
        ' 
        cmbOxiArgPresets.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        cmbOxiArgPresets.FormattingEnabled = True
        cmbOxiArgPresets.Location = New Point(551, 67)
        cmbOxiArgPresets.Name = "cmbOxiArgPresets"
        cmbOxiArgPresets.Size = New Size(112, 23)
        cmbOxiArgPresets.TabIndex = 6
        ' 
        ' btnCancel
        ' 
        btnCancel.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        btnCancel.Enabled = False
        btnCancel.Location = New Point(259, 96)
        btnCancel.Name = "btnCancel"
        btnCancel.Size = New Size(404, 23)
        btnCancel.TabIndex = 11
        btnCancel.Text = "Cancel"
        btnCancel.UseVisualStyleBackColor = True
        ' 
        ' ucOxiLog
        ' 
        ucOxiLog.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        ucOxiLog.AutoSizeMode = AutoSizeMode.GrowAndShrink
        ucOxiLog.Location = New Point(259, 147)
        ucOxiLog.Name = "ucOxiLog"
        ucOxiLog.Size = New Size(404, 115)
        ucOxiLog.TabIndex = 13
        ' 
        ' OxiPngSelectForm
        ' 
        AllowDrop = True
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(675, 268)
        Controls.Add(ucOxiLog)
        Controls.Add(btnCancel)
        Controls.Add(cmbOxiArgPresets)
        Controls.Add(btnExecute)
        Controls.Add(grpImages)
        Controls.Add(lblOxiPngArgs)
        Controls.Add(txtOxiPngArgs)
        Controls.Add(btnDownloadOxiPng)
        Controls.Add(lblOxiPngDesc)
        Controls.Add(lblOxiPngPath)
        Controls.Add(txtOxiPngPath)
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        Name = "OxiPngSelectForm"
        Text = "Optimize PNGs with Oxipng"
        grpImages.ResumeLayout(False)
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents txtOxiPngPath As TextBox
    Friend WithEvents lblOxiPngPath As Label
    Friend WithEvents lblOxiPngDesc As Label
    Friend WithEvents btnDownloadOxiPng As Button
    Friend WithEvents lblOxiPngArgs As Label
    Friend WithEvents txtOxiPngArgs As TextBox
    Friend WithEvents grpImages As GroupBox
    Friend WithEvents btnClearImg As Button
    Friend WithEvents btnSelectImg As Button
    Friend WithEvents lstImages As ListBox
    Friend WithEvents btnExecute As Button
    Friend WithEvents cmbOxiArgPresets As ComboBox
    Friend WithEvents btnCancel As Button
    Friend WithEvents ucOxiLog As LogControl
End Class
