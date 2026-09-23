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
        btnCallWindowsLogin = New Button()
        txtBskyHandle = New TextBox()
        rbtSetBskyPwd = New RadioButton()
        lblBskyHandle = New Label()
        Label1 = New Label()
        btnClearCredentials = New Button()
        SuspendLayout()
        ' 
        ' btnCallWindowsLogin
        ' 
        btnCallWindowsLogin.Location = New Point(178, 141)
        btnCallWindowsLogin.Name = "btnCallWindowsLogin"
        btnCallWindowsLogin.Size = New Size(145, 29)
        btnCallWindowsLogin.TabIndex = 5
        btnCallWindowsLogin.Text = "Set credentials"
        btnCallWindowsLogin.UseVisualStyleBackColor = True
        ' 
        ' txtBskyHandle
        ' 
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
        rbtSetBskyPwd.Enabled = False
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
        ' Label1
        ' 
        Label1.AutoSize = True
        Label1.Location = New Point(12, 9)
        Label1.Name = "Label1"
        Label1.Size = New Size(320, 60)
        Label1.TabIndex = 0
        Label1.Text = "Setup your Bluesky handle and your Bluesky App Password." & vbCrLf & vbCrLf & "You can find your App Password under:" & vbCrLf & " Settings -> Privacy and security -> App passwords" & vbCrLf
        ' 
        ' btnClearCredentials
        ' 
        btnClearCredentials.Location = New Point(12, 141)
        btnClearCredentials.Name = "btnClearCredentials"
        btnClearCredentials.Size = New Size(145, 29)
        btnClearCredentials.TabIndex = 4
        btnClearCredentials.Text = "Clear credentials"
        btnClearCredentials.UseVisualStyleBackColor = True
        ' 
        ' BdiuCredentialForm
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(335, 182)
        Controls.Add(btnClearCredentials)
        Controls.Add(Label1)
        Controls.Add(lblBskyHandle)
        Controls.Add(rbtSetBskyPwd)
        Controls.Add(txtBskyHandle)
        Controls.Add(btnCallWindowsLogin)
        Name = "BdiuCredentialForm"
        Text = "Setup Bluesky App Password"
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents btnCallWindowsLogin As Button
    Friend WithEvents txtBskyHandle As TextBox
    Friend WithEvents rbtSetBskyPwd As RadioButton
    Friend WithEvents lblBskyHandle As Label
    Friend WithEvents Label1 As Label
    Friend WithEvents btnClearCredentials As Button
End Class
