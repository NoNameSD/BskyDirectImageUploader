<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class BdiuCredentialForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
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
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        btnEnterCredentials = New Button()
        txtBskyHandle = New TextBox()
        rbtSetBskyPwd = New RadioButton()
        lblBskyHandle = New Label()
        lblMainInfo = New Label()
        btnClearCredentials = New Button()
        lblBskyDID = New Label()
        txtBskyDid = New TextBox()
        rbtActiveSession = New RadioButton()
        btnLogout = New Button()
        btnLogin = New Button()
        dtpSessionValid = New ReadOnlyDateTimePicker()
        lblSessionValid = New Label()
        lblTokenInfo = New Label()
        btnTestSession = New Button()
        btnSaveSession = New Button()
        lblRefreshValid = New Label()
        dtpRefreshValid = New ReadOnlyDateTimePicker()
        btnRefreshSession = New Button()
        SuspendLayout()
        ' 
        ' btnEnterCredentials
        ' 
        btnEnterCredentials.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnEnterCredentials.Location = New Point(178, 351)
        btnEnterCredentials.Name = "btnEnterCredentials"
        btnEnterCredentials.Size = New Size(145, 29)
        btnEnterCredentials.TabIndex = 17
        btnEnterCredentials.Text = "Set credentials"
        btnEnterCredentials.UseVisualStyleBackColor = True
        ' 
        ' txtBskyHandle
        ' 
        txtBskyHandle.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        txtBskyHandle.Location = New Point(109, 82)
        txtBskyHandle.Name = "txtBskyHandle"
        txtBskyHandle.ReadOnly = True
        txtBskyHandle.Size = New Size(214, 23)
        txtBskyHandle.TabIndex = 2
        txtBskyHandle.WordWrap = False
        ' 
        ' rbtSetBskyPwd
        ' 
        rbtSetBskyPwd.AutoCheck = False
        rbtSetBskyPwd.AutoSize = True
        rbtSetBskyPwd.Location = New Point(109, 111)
        rbtSetBskyPwd.Name = "rbtSetBskyPwd"
        rbtSetBskyPwd.Size = New Size(143, 19)
        rbtSetBskyPwd.TabIndex = 3
        rbtSetBskyPwd.TabStop = True
        rbtSetBskyPwd.Text = "Password has been set"
        rbtSetBskyPwd.UseVisualStyleBackColor = True
        ' 
        ' lblBskyHandle
        ' 
        lblBskyHandle.AutoSize = True
        lblBskyHandle.Location = New Point(12, 85)
        lblBskyHandle.Name = "lblBskyHandle"
        lblBskyHandle.Size = New Size(91, 15)
        lblBskyHandle.TabIndex = 1
        lblBskyHandle.Text = "Bluesky Handle:"
        ' 
        ' lblMainInfo
        ' 
        lblMainInfo.AutoSize = True
        lblMainInfo.Location = New Point(12, 9)
        lblMainInfo.Name = "lblMainInfo"
        lblMainInfo.Size = New Size(320, 60)
        lblMainInfo.TabIndex = 0
        lblMainInfo.Text = "Setup your Bluesky handle and your Bluesky App Password." & vbCrLf & vbCrLf & "You can find your App Password under:" & vbCrLf & " Settings -> Privacy and security -> App passwords" & vbCrLf
        ' 
        ' btnClearCredentials
        ' 
        btnClearCredentials.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        btnClearCredentials.Location = New Point(12, 351)
        btnClearCredentials.Name = "btnClearCredentials"
        btnClearCredentials.Size = New Size(145, 29)
        btnClearCredentials.TabIndex = 16
        btnClearCredentials.Text = "Clear credentials"
        btnClearCredentials.UseVisualStyleBackColor = True
        ' 
        ' lblBskyDID
        ' 
        lblBskyDID.AutoSize = True
        lblBskyDID.Location = New Point(12, 139)
        lblBskyDID.Name = "lblBskyDID"
        lblBskyDID.Size = New Size(72, 15)
        lblBskyDID.TabIndex = 4
        lblBskyDID.Text = "Bluesky DID:"
        ' 
        ' txtBskyDid
        ' 
        txtBskyDid.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        txtBskyDid.Location = New Point(109, 136)
        txtBskyDid.Name = "txtBskyDid"
        txtBskyDid.ReadOnly = True
        txtBskyDid.Size = New Size(214, 23)
        txtBskyDid.TabIndex = 5
        txtBskyDid.WordWrap = False
        ' 
        ' rbtActiveSession
        ' 
        rbtActiveSession.AutoCheck = False
        rbtActiveSession.AutoSize = True
        rbtActiveSession.Location = New Point(109, 165)
        rbtActiveSession.Name = "rbtActiveSession"
        rbtActiveSession.Size = New Size(142, 19)
        rbtActiveSession.TabIndex = 6
        rbtActiveSession.TabStop = True
        rbtActiveSession.Text = "Active Bluesky session"
        rbtActiveSession.UseVisualStyleBackColor = True
        ' 
        ' btnLogout
        ' 
        btnLogout.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        btnLogout.ImageKey = "(none)"
        btnLogout.Location = New Point(12, 316)
        btnLogout.Name = "btnLogout"
        btnLogout.Size = New Size(145, 29)
        btnLogout.TabIndex = 14
        btnLogout.Text = "Logout session"
        btnLogout.UseVisualStyleBackColor = True
        ' 
        ' btnLogin
        ' 
        btnLogin.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnLogin.Location = New Point(178, 316)
        btnLogin.Name = "btnLogin"
        btnLogin.Size = New Size(145, 29)
        btnLogin.TabIndex = 15
        btnLogin.Text = "Login session"
        btnLogin.UseVisualStyleBackColor = True
        ' 
        ' dtpSessionValid
        ' 
        dtpSessionValid.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        dtpSessionValid.CausesValidation = False
        dtpSessionValid.Location = New Point(155, 190)
        dtpSessionValid.Name = "dtpSessionValid"
        dtpSessionValid.ReadOnly = True
        dtpSessionValid.Size = New Size(168, 23)
        dtpSessionValid.TabIndex = 8
        ' 
        ' lblSessionValid
        ' 
        lblSessionValid.AutoSize = True
        lblSessionValid.Location = New Point(12, 194)
        lblSessionValid.Name = "lblSessionValid"
        lblSessionValid.Size = New Size(137, 15)
        lblSessionValid.TabIndex = 7
        lblSessionValid.Text = "Session token valid until:"
        ' 
        ' lblTokenInfo
        ' 
        lblTokenInfo.AutoSize = True
        lblTokenInfo.Location = New Point(12, 249)
        lblTokenInfo.Name = "lblTokenInfo"
        lblTokenInfo.Size = New Size(271, 30)
        lblTokenInfo.TabIndex = 11
        lblTokenInfo.Text = "Please note that the refresh token can expire " & vbCrLf & "before the date above due to prolonged inactivity." & vbCrLf
        ' 
        ' btnTestSession
        ' 
        btnTestSession.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnTestSession.Location = New Point(129, 285)
        btnTestSession.Name = "btnTestSession"
        btnTestSession.Size = New Size(94, 23)
        btnTestSession.TabIndex = 12
        btnTestSession.Text = "Test Session"
        btnTestSession.UseVisualStyleBackColor = True
        ' 
        ' btnSaveSession
        ' 
        btnSaveSession.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnSaveSession.Location = New Point(229, 285)
        btnSaveSession.Name = "btnSaveSession"
        btnSaveSession.Size = New Size(94, 23)
        btnSaveSession.TabIndex = 13
        btnSaveSession.Text = "Save Session"
        btnSaveSession.UseVisualStyleBackColor = True
        ' 
        ' lblRefreshValid
        ' 
        lblRefreshValid.AutoSize = True
        lblRefreshValid.Location = New Point(12, 223)
        lblRefreshValid.Name = "lblRefreshValid"
        lblRefreshValid.Size = New Size(137, 15)
        lblRefreshValid.TabIndex = 9
        lblRefreshValid.Text = "Refresh token valid until:"
        ' 
        ' dtpRefreshValid
        ' 
        dtpRefreshValid.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        dtpRefreshValid.CausesValidation = False
        dtpRefreshValid.Location = New Point(155, 219)
        dtpRefreshValid.Name = "dtpRefreshValid"
        dtpRefreshValid.ReadOnly = True
        dtpRefreshValid.Size = New Size(168, 23)
        dtpRefreshValid.TabIndex = 10
        ' 
        ' btnRefreshSession
        ' 
        btnRefreshSession.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnRefreshSession.Location = New Point(12, 285)
        btnRefreshSession.Name = "btnRefreshSession"
        btnRefreshSession.Size = New Size(111, 23)
        btnRefreshSession.TabIndex = 11
        btnRefreshSession.Text = "Refresh Session"
        btnRefreshSession.UseVisualStyleBackColor = True
        ' 
        ' BdiuCredentialForm
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(335, 392)
        Controls.Add(btnRefreshSession)
        Controls.Add(lblRefreshValid)
        Controls.Add(dtpRefreshValid)
        Controls.Add(btnSaveSession)
        Controls.Add(btnTestSession)
        Controls.Add(lblTokenInfo)
        Controls.Add(lblSessionValid)
        Controls.Add(dtpSessionValid)
        Controls.Add(btnLogout)
        Controls.Add(btnLogin)
        Controls.Add(rbtActiveSession)
        Controls.Add(lblBskyDID)
        Controls.Add(txtBskyDid)
        Controls.Add(btnClearCredentials)
        Controls.Add(lblMainInfo)
        Controls.Add(lblBskyHandle)
        Controls.Add(rbtSetBskyPwd)
        Controls.Add(txtBskyHandle)
        Controls.Add(btnEnterCredentials)
        Name = "BdiuCredentialForm"
        Text = "Setup Bluesky App Password"
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents btnEnterCredentials As Button
    Friend WithEvents txtBskyHandle As TextBox
    Friend WithEvents rbtSetBskyPwd As RadioButton
    Friend WithEvents lblBskyHandle As Label
    Friend WithEvents lblMainInfo As Label
    Friend WithEvents btnClearCredentials As Button
    Friend WithEvents lblBskyDID As Label
    Friend WithEvents txtBskyDid As TextBox
    Friend WithEvents rbtActiveSession As RadioButton
    Friend WithEvents btnLogout As Button
    Friend WithEvents btnLogin As Button
    Friend WithEvents dtpSessionValid As ReadOnlyDateTimePicker
    Friend WithEvents lblSessionValid As Label
    Friend WithEvents lblTokenInfo As Label
    Friend WithEvents btnTestSession As Button
    Friend WithEvents btnSaveSession As Button
    Friend WithEvents lblRefreshValid As Label
    Friend WithEvents dtpRefreshValid As ReadOnlyDateTimePicker
    Friend WithEvents btnRefreshSession As Button
End Class
