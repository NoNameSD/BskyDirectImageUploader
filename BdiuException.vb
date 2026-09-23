Namespace Bdiu
    Public Class BDIUExceptionDisplay
        Public Shared Function DisplayExceptionAsMessageBox(owner As IWin32Window, ex As System.Exception) As System.Windows.Forms.DialogResult
            Return DisplayExceptionAsMessageBox(owner, ex, MessageBoxIcon.Error)
        End Function
        Public Shared Function DisplayExceptionAsMessageBox(owner As IWin32Window, ex As System.Exception, icon As MessageBoxIcon) As System.Windows.Forms.DialogResult
            Return System.Windows.Forms.MessageBox.Show(
            owner:=owner,
            text:=ex.Message,
            caption:=ex.GetType.ToString,
            buttons:=MessageBoxButtons.OK,
            icon:=icon
        )
        End Function
    End Class
End Namespace