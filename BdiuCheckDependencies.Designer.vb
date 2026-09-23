<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class BdiuCheckDependencies
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
        btnCheck = New Button()
        lblDepRequired = New Label()
        lblPython = New Label()
        lblAtproto = New Label()
        txtPython = New TextBox()
        txtAtproto = New TextBox()
        Label1 = New Label()
        txtOxipng = New TextBox()
        lblOxipng = New Label()
        SuspendLayout()
        ' 
        ' btnCheck
        ' 
        btnCheck.Location = New Point(127, 12)
        btnCheck.Name = "btnCheck"
        btnCheck.Size = New Size(109, 23)
        btnCheck.TabIndex = 0
        btnCheck.Text = "Check"
        btnCheck.UseVisualStyleBackColor = True
        ' 
        ' lblDepRequired
        ' 
        lblDepRequired.AutoSize = True
        lblDepRequired.Location = New Point(12, 56)
        lblDepRequired.Name = "lblDepRequired"
        lblDepRequired.Size = New Size(176, 15)
        lblDepRequired.TabIndex = 1
        lblDepRequired.Text = "Required for posting to BlueSky:"
        ' 
        ' lblPython
        ' 
        lblPython.AutoSize = True
        lblPython.Location = New Point(38, 89)
        lblPython.Name = "lblPython"
        lblPython.Size = New Size(48, 15)
        lblPython.TabIndex = 2
        lblPython.Text = "Python:"
        ' 
        ' lblAtproto
        ' 
        lblAtproto.AutoSize = True
        lblAtproto.Location = New Point(60, 126)
        lblAtproto.Name = "lblAtproto"
        lblAtproto.Size = New Size(49, 15)
        lblAtproto.TabIndex = 3
        lblAtproto.Text = "atproto:"
        ' 
        ' txtPython
        ' 
        txtPython.Location = New Point(88, 86)
        txtPython.Name = "txtPython"
        txtPython.Size = New Size(148, 23)
        txtPython.TabIndex = 4
        ' 
        ' txtAtproto
        ' 
        txtAtproto.Location = New Point(115, 123)
        txtAtproto.Name = "txtAtproto"
        txtAtproto.Size = New Size(121, 23)
        txtAtproto.TabIndex = 5
        ' 
        ' Label1
        ' 
        Label1.AutoSize = True
        Label1.Location = New Point(12, 178)
        Label1.Name = "Label1"
        Label1.Size = New Size(218, 15)
        Label1.TabIndex = 6
        Label1.Text = "Required for losslessly optimizing PNGs:"
        ' 
        ' txtOxipng
        ' 
        txtOxipng.Location = New Point(88, 211)
        txtOxipng.Name = "txtOxipng"
        txtOxipng.Size = New Size(148, 23)
        txtOxipng.TabIndex = 8
        ' 
        ' lblOxipng
        ' 
        lblOxipng.AutoSize = True
        lblOxipng.Location = New Point(37, 214)
        lblOxipng.Name = "lblOxipng"
        lblOxipng.Size = New Size(49, 15)
        lblOxipng.TabIndex = 7
        lblOxipng.Text = "Oxipng:"
        ' 
        ' BdiuCheckDependencies
        ' 
        AutoScaleDimensions = New SizeF(7.0F, 15.0F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(532, 258)
        Controls.Add(txtOxipng)
        Controls.Add(lblOxipng)
        Controls.Add(Label1)
        Controls.Add(txtAtproto)
        Controls.Add(txtPython)
        Controls.Add(lblAtproto)
        Controls.Add(lblPython)
        Controls.Add(lblDepRequired)
        Controls.Add(btnCheck)
        Name = "BdiuCheckDependencies"
        Text = "Check Dependencies"
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents btnCheck As Button
    Friend WithEvents lblDepRequired As Label
    Friend WithEvents lblPython As Label
    Friend WithEvents lblAtproto As Label
    Friend WithEvents txtPython As TextBox
    Friend WithEvents txtAtproto As TextBox
    Friend WithEvents Label1 As Label
    Friend WithEvents txtOxipng As TextBox
    Friend WithEvents lblOxipng As Label
End Class
