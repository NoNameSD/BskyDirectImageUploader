Option Strict On
Option Explicit On

Public Class ReadOnlyDateTimePicker
    Inherits DateTimePicker

    Private _isReadOnly As Boolean = False

    <System.ComponentModel.Category("Behavior")>
    <System.ComponentModel.DefaultValue(False)>
    <System.ComponentModel.Description("Controls whether the user can change the date value.")>
    Public Property [ReadOnly]() As Boolean
        Get
            Return _isReadOnly
        End Get
        Set(ByVal value As Boolean)
            _isReadOnly = value
        End Set
    End Property

    Protected Overrides Sub OnDropDown(e As EventArgs)
        If _isReadOnly Then
            ' Immediately close the drop-down calendar if read-only
            SendKeys.Send("{ESC}")
            Return
        End If
        MyBase.OnDropDown(e)
    End Sub

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        If _isReadOnly Then
            ' Suppress all key presses (arrow keys, typing) if read-only
            e.SuppressKeyPress = True
            Return
        End If
        MyBase.OnKeyDown(e)
    End Sub
End Class