<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FormLicenses
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
        lstLicenses = New ListBox()
        txtLicense = New RichTextBox()
        SuspendLayout()
        ' 
        ' lstLicenses
        ' 
        lstLicenses.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left
        lstLicenses.FormattingEnabled = True
        lstLicenses.Items.AddRange(New Object() {"idunno.Bluesky", "Magick.NET"})
        lstLicenses.Location = New Point(12, 12)
        lstLicenses.Name = "lstLicenses"
        lstLicenses.Size = New Size(136, 124)
        lstLicenses.TabIndex = 0
        ' 
        ' txtLicense
        ' 
        txtLicense.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        txtLicense.Location = New Point(154, 12)
        txtLicense.Multiline = True
        txtLicense.Name = "txtLicense"
        txtLicense.ReadOnly = True
        txtLicense.ScrollBars = RichTextBoxScrollBars.Vertical
        txtLicense.Size = New Size(329, 124)
        txtLicense.TabIndex = 1
        ' 
        ' FormLicenses
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(490, 142)
        Controls.Add(txtLicense)
        Controls.Add(lstLicenses)
        Name = "FormLicenses"
        Text = "Licenses"
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents lstLicenses As ListBox
    Friend WithEvents txtLicense As RichTextBox
End Class
