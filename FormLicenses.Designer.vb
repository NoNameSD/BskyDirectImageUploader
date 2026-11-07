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
        scLicenses = New SplitContainer()
        CType(scLicenses, ComponentModel.ISupportInitialize).BeginInit()
        scLicenses.Panel1.SuspendLayout()
        scLicenses.Panel2.SuspendLayout()
        scLicenses.SuspendLayout()
        SuspendLayout()
        ' 
        ' lstLicenses
        ' 
        lstLicenses.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        lstLicenses.FormattingEnabled = True
        lstLicenses.HorizontalScrollbar = True
        lstLicenses.Location = New Point(3, 3)
        lstLicenses.Name = "lstLicenses"
        lstLicenses.Size = New Size(298, 124)
        lstLicenses.TabIndex = 0
        ' 
        ' txtLicense
        ' 
        txtLicense.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        txtLicense.Location = New Point(3, 3)
        txtLicense.Name = "txtLicense"
        txtLicense.ReadOnly = True
        txtLicense.ScrollBars = RichTextBoxScrollBars.Vertical
        txtLicense.Size = New Size(419, 124)
        txtLicense.TabIndex = 1
        txtLicense.Text = ""
        ' 
        ' scLicenses
        ' 
        scLicenses.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        scLicenses.Location = New Point(0, 0)
        scLicenses.Name = "scLicenses"
        ' 
        ' scLicenses.Panel1
        ' 
        scLicenses.Panel1.Controls.Add(lstLicenses)
        ' 
        ' scLicenses.Panel2
        ' 
        scLicenses.Panel2.Controls.Add(txtLicense)
        scLicenses.Size = New Size(733, 135)
        scLicenses.SplitterDistance = 304
        scLicenses.TabIndex = 2
        ' 
        ' FormLicenses
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(735, 137)
        Controls.Add(scLicenses)
        Name = "FormLicenses"
        Text = "Licenses"
        scLicenses.Panel1.ResumeLayout(False)
        scLicenses.Panel2.ResumeLayout(False)
        CType(scLicenses, ComponentModel.ISupportInitialize).EndInit()
        scLicenses.ResumeLayout(False)
        ResumeLayout(False)
    End Sub

    Friend WithEvents lstLicenses As ListBox
    Friend WithEvents txtLicense As RichTextBox
    Friend WithEvents scLicenses As SplitContainer
End Class
