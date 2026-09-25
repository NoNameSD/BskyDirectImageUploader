Option Compare Binary
Option Explicit On
Option Strict On
Imports System.IO
Imports System.Runtime.InteropServices

Namespace Bdiu
    Public Module BdiuHelper

        Public Const IsDevelopmentVersion = True

        Public ReadOnly ColorWarning As Color = Color.FromArgb(&HE4, &HA1, &H1B)

        Public Const BackSlashChar As Char = "\"c
        Public Const DotChar As Char = "."c
        Public Const HyphenChar As Char = "-"c

        Public Const ZeroByte As Byte = 0
        Public Const OneByte As Byte = 1

        ' Paths for secure credentials storage
        Public ReadOnly ConfigFolder As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "bsky-ps")
        Public ReadOnly HandleFilePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "handle.txt")
        Public ReadOnly PasswordFilePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "password.enc")

        Public Function GetHashOfSelf() As String
            Dim oHash = System.Security.Cryptography.SHA1.Create
            Dim sFilePath = System.Reflection.Assembly.GetEntryAssembly().Location ' File of compiled dll

            If String.IsNullOrEmpty(sFilePath) Then
                sFilePath = System.Environment.ProcessPath ' Path of exe
            End If

            Dim fileStream =
                New System.IO.FileStream(
                    path:=sFilePath,
                    access:=System.IO.FileAccess.Read,
                    share:=System.IO.FileShare.ReadWrite Or IO.FileShare.Delete,
                    mode:=System.IO.FileMode.Open)

            Dim iHashBytes = oHash.ComputeHash(fileStream)

            Call fileStream.Close()

            Return Convert.ToBase64String(iHashBytes)
        End Function

        <System.Runtime.InteropServices.DllImport("shell32.dll", CharSet:=System.Runtime.InteropServices.CharSet.Unicode)>
        Private Function FindExecutable _
           (ByVal lpFile As String,
            ByVal lpDirectory As String,
            ByVal lpResult As String) As System.IntPtr
        End Function

        Public Function FindExecutable(ByRef FileSearch As String) As String
            Return FindExecutable(FileSearch, Application.StartupPath())
        End Function

        Public Function FindExecutable(ByRef FileSearch As String, ByRef SearchPath As String) As String
            Const MIN_SUCCESS_LNG As Byte = &H20
            Const MAX_PATH As Integer = &H104
            Dim retPath As New String(vbNullChar.Single, MAX_PATH)

            Dim [return] = FindExecutable(
                    lpFile:=FileSearch,
                    lpDirectory:=SearchPath,
                    lpResult:=retPath)

            Dim returnInt = [return].ToInt32

            If returnInt < MIN_SUCCESS_LNG Then
                Return Constants.vbNullString
            End If

            If retPath.Contains(vbNullChar.Single) Then
                Return retPath.Substring(0, retPath.IndexOf(vbNullChar.Single))
            Else
                Return retPath
            End If
        End Function
    End Module

    Public Module WinApiFunctions
        ' --- Windows API Setup ---
        ' Import the required functions from user32.dll
        <DllImport("user32.dll", CharSet:=CharSet.Auto)>
        Private Function GetSystemMenu(hWnd As IntPtr, bRevert As Boolean) As IntPtr
        End Function

        <DllImport("user32.dll", CharSet:=CharSet.Auto)>
        Private Function EnableMenuItem(hMenu As IntPtr, uIDEnableItem As Integer, uEnable As Integer) As Boolean
        End Function

        ' Constants needed for modifying the system menu
        Private Const SC_CLOSE As Integer = &HF060
        Private Const MF_BYCOMMAND As Integer = &H0
        Private Const MF_ENABLED As Integer = &H0
        Private Const MF_GRAYED As Integer = &H1
        Private Const MF_DISABLED As Integer = &H2

        ''' <summary>
        ''' Enables the visual X button
        ''' </summary>
        ''' <param name="owner">Form that will be modified</param>
        Public Sub EnableFormCloseButton(owner As System.Windows.Forms.IWin32Window)
            Dim hMenu As IntPtr = GetSystemMenu(owner.Handle, False)

            If hMenu <> IntPtr.Zero Then
                Call EnableMenuItem(hMenu, SC_CLOSE, MF_BYCOMMAND Or MF_ENABLED)
            End If
        End Sub

        ''' <summary>
        ''' Disables the visual X button
        ''' </summary>
        ''' <param name="owner">Form that will be modified</param>
        Public Sub DisableFormCloseButton(owner As System.Windows.Forms.IWin32Window)
            Dim hMenu As IntPtr = GetSystemMenu(owner.Handle, False)

            If hMenu <> IntPtr.Zero Then
                Call EnableMenuItem(hMenu, SC_CLOSE, MF_BYCOMMAND Or MF_GRAYED Or MF_DISABLED)
            End If
        End Sub
    End Module
End Namespace