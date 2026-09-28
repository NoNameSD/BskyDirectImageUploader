Option Compare Binary
Option Explicit On
Option Strict On

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
        Public ReadOnly HandleFilePath As String = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "handle.txt")
        Public ReadOnly PasswordFilePath As String = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "password.enc")
        Public ReadOnly SessionFilePath As String = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bsky_session.dat")
        Public ReadOnly EntropyFilePath As String = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "entropy.dat")

        ''' <summary>
        ''' Generates a dynamic, user-specific entropy byte array for DPAPI encryption.
        ''' DPAPI itself is already secure, this is just an added layer of security.
        ''' </summary>
        Friend Function GetDynamicEntropy() As Byte()
            ' Hardcoded salt prefix
            Dim prefixSalt As Byte() = New Byte() {&H7A, &HAE, &H4F, &HC7, &H5B, &HFF, &H84, &HED, &H31, &HA0, &HB9, &H66, &H45, &HA1, &HE0, &H9C, &HE7, &HA1, &H65, &HF8, &HC4, &HB5, &H4F, &H81, &H7B, &HCC, &HB6, &H3F, &HB0, &HD0, &H31, &HA3, &HC4, &H23, &HD3, &H7B, &HBF, &H9C, &HF8, &H5F, &H64, &HA3, &HE7, &H82, &HDA, &H1C}

            ' Hardcoded salt suffix
            Dim suffixSalt As Byte() = New Byte() {&H80, &HDF, &H3E, &H96, &H4B, &HDA, &H68, &H18, &HD6, &HA9, &H94, &H29, &H3C, &HCD, &HA6, &H52, &H5F, &HD0, &HAD, &HFE, &H29, &HE1, &H74, &H30, &HE4, &HB3, &HD7, &HB0, &H82, &HA5, &HBA, &HA8, &H3E, &HE2, &H4, &H16}

            ' Get the bytes of the current Windows username
            Dim userName As String = System.Environment.UserName
            Dim userNameBytes As Byte() = System.Text.Encoding.UTF8.GetBytes(userName)

            ' Also store additional entropy Bytes as a file
            Dim storedEntropy As Byte()
            If System.IO.File.Exists(Bdiu.BdiuHelper.EntropyFilePath) Then
                ' Load entropy from existing file
                storedEntropy = System.IO.File.ReadAllBytes(Bdiu.BdiuHelper.EntropyFilePath)
            Else
                ' Entropy file does not yet exist or has been deleted.
                ' Creating new one.
                ' This means however existing encrypted data cannot be decrypted anymore.
                ReDim storedEntropy(System.Security.Cryptography.RandomNumberGenerator.GetInt32(24, 65))
                Call System.Security.Cryptography.RandomNumberGenerator.Fill(storedEntropy)
                Call System.IO.File.WriteAllBytes(Bdiu.BdiuHelper.EntropyFilePath, storedEntropy)
            End If

            ' Calculate the total length required for the combined array
            Dim totalLength As Integer = storedEntropy.Length + prefixSalt.Length + userNameBytes.Length + suffixSalt.Length
            Dim combinedBytes(totalLength - 1) As Byte

            ' Copy the prefix salt into the beginning of the array
            Call System.Buffer.BlockCopy(prefixSalt, 0, combinedBytes, 0, prefixSalt.Length)

            ' Copy the username bytes right after the prefix salt
            Call System.Buffer.BlockCopy(userNameBytes, 0, combinedBytes, prefixSalt.Length, userNameBytes.Length)

            ' Copy the suffix salt after the username
            Dim suffixOffset As Integer = prefixSalt.Length + userNameBytes.Length
            Call System.Buffer.BlockCopy(suffixSalt, 0, combinedBytes, suffixOffset, suffixSalt.Length)

            ' Copy the locally stored entropy data at the end
            suffixOffset += suffixSalt.Length
            Call System.Buffer.BlockCopy(storedEntropy, 0, combinedBytes, suffixOffset, storedEntropy.Length)

            Return combinedBytes
        End Function
        Public Function CurrentCultureDateTimeFormat() As String
            ' Get the cultural settings of the current user's system
            Dim activeCulture As System.Globalization.CultureInfo = System.Globalization.CultureInfo.CurrentCulture

            ' Get all patterns for the general short date/time format ("g") and pick the first one
            ' The 'g'c defines the character literal for the "g" specifier
            Dim culturePattern As String = activeCulture.DateTimeFormat.GetAllDateTimePatterns("g"c)(0)

            ' Append seconds right after the minute pattern ("mm")
            If culturePattern.Contains("mm") Then
                culturePattern = culturePattern.Replace("mm", "mm:ss")
            End If

            Return culturePattern
        End Function

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
        <System.Runtime.InteropServices.DllImport("user32.dll", CharSet:=System.Runtime.InteropServices.CharSet.Auto)>
        Private Function GetSystemMenu(hWnd As IntPtr, bRevert As Boolean) As IntPtr
        End Function

        <System.Runtime.InteropServices.DllImport("user32.dll", CharSet:=System.Runtime.InteropServices.CharSet.Auto)>
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