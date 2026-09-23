<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class LogControl
    Inherits System.Windows.Forms.UserControl

    'UserControl overrides dispose to clean up the component list.
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
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        rtbLog = New Logger.ScrollingRichTextBox()
        ucControls = New LogControlsOnly()
        sfdLogExport = New SaveFileDialog()
        SuspendLayout()
        ' 
        ' rtbLog
        ' 
        rtbLog.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        rtbLog.Location = New Point(0, 0)
        rtbLog.Margin = New Padding(0)
        rtbLog.Name = "rtbLog"
        rtbLog.ReadOnly = True
        rtbLog.Size = New Size(78, 115)
        rtbLog.TabIndex = 30
        rtbLog.Text = ""
        ' 
        ' ucControls
        ' 
        ucControls.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        ucControls.Location = New Point(84, 0)
        ucControls.MinimumSize = New Size(116, 115)
        ucControls.Name = "ucControls"
        ucControls.Size = New Size(116, 115)
        ucControls.TabIndex = 31
        ' 
        ' sfdLogExport
        ' 
        sfdLogExport.DefaultExt = "rtf"
        sfdLogExport.Filter = "Rich-Text-Format|*.rtf|Plain text|*.txt"
        sfdLogExport.Title = "Export the current log to a file."
        ' 
        ' LogControl
        ' 
        AutoScaleMode = AutoScaleMode.Inherit
        AutoSizeMode = AutoSizeMode.GrowAndShrink
        Controls.Add(rtbLog)
        Controls.Add(ucControls)
        Name = "LogControl"
        Size = New Size(200, 115)
        ResumeLayout(False)
    End Sub
    Friend WithEvents ucControls As LogControlsOnly
    Private WithEvents objLogger As Logger.Logger
    Protected Friend WithEvents rtbLog As Logger.ScrollingRichTextBox
    Private WithEvents sfdLogExport As SaveFileDialog

End Class
