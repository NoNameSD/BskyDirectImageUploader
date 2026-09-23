<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class BdiuMainForm
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
        btnCreatePost = New Button()
        btnSetupBskyCredentials = New Button()
        btnOxiPngForm = New Button()
        SuspendLayout()
        ' 
        ' btnCreatePost
        ' 
        btnCreatePost.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        btnCreatePost.Location = New Point(12, 12)
        btnCreatePost.Name = "btnCreatePost"
        btnCreatePost.Size = New Size(250, 50)
        btnCreatePost.TabIndex = 0
        btnCreatePost.Text = "Create BlueSky image post" & vbCrLf & "without lossy compression "
        btnCreatePost.UseVisualStyleBackColor = True
        ' 
        ' btnSetupBskyCredentials
        ' 
        btnSetupBskyCredentials.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        btnSetupBskyCredentials.Location = New Point(12, 68)
        btnSetupBskyCredentials.Name = "btnSetupBskyCredentials"
        btnSetupBskyCredentials.Size = New Size(250, 50)
        btnSetupBskyCredentials.TabIndex = 1
        btnSetupBskyCredentials.Text = "Setup BlueSky credentials"
        btnSetupBskyCredentials.UseVisualStyleBackColor = True
        ' 
        ' btnOxiPngForm
        ' 
        btnOxiPngForm.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        btnOxiPngForm.Location = New Point(12, 124)
        btnOxiPngForm.Name = "btnOxiPngForm"
        btnOxiPngForm.Size = New Size(250, 50)
        btnOxiPngForm.TabIndex = 2
        btnOxiPngForm.Text = "Optimize PNG files losslessly"
        btnOxiPngForm.UseVisualStyleBackColor = True
        ' 
        ' BdiuMainForm
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(274, 185)
        Controls.Add(btnOxiPngForm)
        Controls.Add(btnSetupBskyCredentials)
        Controls.Add(btnCreatePost)
        Name = "BdiuMainForm"
        Text = "Bsky Direct Image Uploader"
        ResumeLayout(False)
    End Sub

    Friend WithEvents btnCreatePost As Button
    Friend WithEvents btnSetupBskyCredentials As Button
    Friend WithEvents btnOxiPngForm As Button
End Class
