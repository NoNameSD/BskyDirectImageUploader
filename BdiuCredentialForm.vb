Imports System.Runtime.InteropServices
Imports System.Security.Cryptography
Imports System.Text

#Disable Warning IDE1006 ' Naming Styles
Public Class BdiuCredentialForm

    Private Sub btnCallWindowsLogin_Click(sender As Object, e As EventArgs) Handles btnCallWindowsLogin.Click
        Call EnterCredentials()
    End Sub

    Public Sub EnterCredentials()
        Dim userhandle As String = ""
        Dim password As String = ""

        ' Call modern API wrapper
        If NativeLogin.ShowLogin(
            Me.Handle,
            "Bluesky Authentication",
            "Please enter your Bluesky Handle and Bluesky App Password.",
            userhandle,
            password) Then

            ' Success: Credentials captured successfully in plain text
            ' Encrypt password natively via Windows Data Protection API (DPAPI)
            Dim passBytes As Byte() = Encoding.UTF8.GetBytes(password)
            Dim encryptedBytes As Byte() = ProtectedData.Protect(passBytes, Nothing, DataProtectionScope.CurrentUser)

            System.IO.File.WriteAllText(Bdiu.BdiuHelper.HandleFilePath, userhandle, Encoding.UTF8)
            System.IO.File.WriteAllBytes(Bdiu.BdiuHelper.PasswordFilePath, encryptedBytes)
        Else
            ' Cancelled or closed by the user
        End If
        Call RefreshCredentials()
    End Sub

    Public Sub RefreshCredentials()

        ' Decrypt credentials securely using Windows DPAPI (tied to current Windows User Account)
        If System.IO.File.Exists(Bdiu.BdiuHelper.HandleFilePath) Then
            Dim handle As String = System.IO.File.ReadAllText(Bdiu.BdiuHelper.HandleFilePath).Trim()

            Me.txtBskyHandle.Text = handle
        Else
            Me.txtBskyHandle.Text = Nothing
        End If

        If System.IO.File.Exists(Bdiu.BdiuHelper.PasswordFilePath) Then
            Dim encryptedBytes As Byte() = System.IO.File.ReadAllBytes(Bdiu.BdiuHelper.PasswordFilePath)

            Try
                Call ProtectedData.Unprotect(encryptedBytes, Nothing, DataProtectionScope.CurrentUser)
                Me.rbtSetBskyPwd.Checked = True
            Catch ex As Exception
                Me.rbtSetBskyPwd.Checked = False
            End Try
        Else
            Me.rbtSetBskyPwd.Checked = False
        End If
    End Sub

    Public Class NativeLogin

        ' Structure defining the dialog appearance (64-bit compatible)
        <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Auto)>
        Public Structure CREDUI_INFO
            Public cbSize As Integer
            Public hwndParent As IntPtr
            Public pszMessageText As String
            Public pszCaptionText As String
            Public hbmBanner As IntPtr
        End Structure

        ' Modern Windows 10 / 11 API for credential prompt
        <DllImport("credui.dll", CharSet:=CharSet.Auto)>
        Private Shared Function CredUIPromptForWindowsCredentials(
        ByRef pUiInfo As CREDUI_INFO,
        ByVal dwAuthError As Integer,
        ByRef pulAuthPackage As UInteger,
        ByVal pvInAuthBuffer As IntPtr,
        ByVal ulInAuthBufferSize As UInteger,
        ByRef ppvOutAuthBuffer As IntPtr,
        ByRef pulOutAuthBufferSize As UInteger,
        ByRef pfSave As Boolean,
        ByVal dwFlags As UInteger) As Integer
        End Function

        ' Function to unpack the secure Windows authentication buffer into cleartext strings
        <DllImport("credui.dll", CharSet:=CharSet.Auto)>
        Private Shared Function CredUnPackAuthenticationBuffer(
        ByVal dwFlags As Integer,
        ByVal pvAuthBuffer As IntPtr,
        ByVal cbAuthBuffer As UInteger,
        ByVal pszUserName As StringBuilder,
        ByRef pcchUserName As Integer,
        ByVal pszDomainName As StringBuilder,
        ByRef pcchDomainName As Integer,
        ByVal pszPassword As StringBuilder,
        ByRef pcchPassword As Integer) As Boolean
        End Function

        ' Function to release memory allocated by the Windows API
        <DllImport("ole32.dll", SetLastError:=True)>
        Private Shared Sub CoTaskMemFree(ByVal pv As IntPtr)
        End Sub

        ' Dialog control flags
        Private Const CREDUIWIN_GENERIC As UInteger = &H1

        ''' <summary>
        ''' Invokes the native Windows login dialog and returns username & password as plain text.
        ''' </summary>
        Public Shared Function ShowLogin(
            ByVal parentHandle As IntPtr,
            ByRef outUsername As String,
            ByRef outPassword As String) As Boolean

            Return NativeLogin.ShowLogin(
                parentHandle,
                "Authentication Required",
                "Please enter your credentials.",
                outUsername,
                outPassword)
        End Function

        ''' <summary>
        ''' Invokes the native Windows login dialog and returns username & password as plain text.
        ''' </summary>
        Public Shared Function ShowLogin(
            ByVal parentHandle As IntPtr,
            ByVal captionText As String,
            ByVal messageText As String,
            ByRef outUsername As String,
            ByRef outPassword As String) As Boolean
            ' 1. Configure dialog properties
            Dim uiInfo As New CREDUI_INFO()
            uiInfo.cbSize = Marshal.SizeOf(uiInfo)
            uiInfo.hwndParent = parentHandle
            uiInfo.pszCaptionText = captionText
            uiInfo.pszMessageText = messageText

            ' Prepare buffers for the native memory pointer
            Dim authPackage As UInteger = 0
            Dim outAuthBuffer As IntPtr = IntPtr.Zero
            Dim outAuthBufferSize As UInteger = 0
            Dim saveChecked As Boolean = False ' Left false; since checkbox flag is omitted, it won't show up

            ' 2. Invoke the dialog (CREDUIWIN_GENERIC enforces standard input fields without AD validation)
            Dim result As Integer = CredUIPromptForWindowsCredentials(
            uiInfo, 0, authPackage, IntPtr.Zero, 0, outAuthBuffer, outAuthBufferSize, saveChecked, CREDUIWIN_GENERIC
        )

            ' If result = 0 (ERROR_SUCCESS), the user entered data and clicked OK
            If result = 0 AndAlso outAuthBuffer <> IntPtr.Zero Then
                Dim maxLen As Integer = 256
                Dim usernameBuf As New StringBuilder(maxLen)
                Dim domainBuf As New StringBuilder(maxLen)
                Dim passwordBuf As New StringBuilder(maxLen)

                ' 3. Unpack the encrypted Windows buffer directly into cleartext strings
                Dim unpackResult As Boolean = CredUnPackAuthenticationBuffer(
                0, outAuthBuffer, outAuthBufferSize,
                usernameBuf, maxLen, domainBuf, maxLen, passwordBuf, maxLen
            )

                ' Immediately release the allocated system memory
                Call CoTaskMemFree(outAuthBuffer)

                If unpackResult Then
                    outUsername = usernameBuf.ToString()
                    outPassword = passwordBuf.ToString()
                    Return True
                End If
            End If

            Return False
        End Function
    End Class

    Private Sub btnClearCredentials_Click(sender As Object, e As EventArgs) Handles btnClearCredentials.Click
        Call ClearCredentials()
    End Sub

    Public Sub ClearCredentials()
        ' Delete stored credential files
        If System.IO.File.Exists(Bdiu.BdiuHelper.HandleFilePath) Then
            Try
                System.IO.File.Delete(Bdiu.BdiuHelper.HandleFilePath)
            Catch ex As Exception

            End Try
        End If

        If System.IO.File.Exists(Bdiu.BdiuHelper.PasswordFilePath) Then
            Try
                System.IO.File.Delete(Bdiu.BdiuHelper.PasswordFilePath)
            Catch ex As Exception

            End Try
        End If
        Call RefreshCredentials()
    End Sub

    Private Sub BdiuCredentialForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Call RefreshCredentials()
    End Sub
End Class
#Enable Warning IDE1006 ' Naming Styles