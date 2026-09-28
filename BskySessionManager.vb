Namespace Bdiu
#Const EncryptSavedSession = True
#Const CustomSessionSerialize = True
    Public Class BskySessionManager
        Public Property BskyAgent As idunno.Bluesky.BlueskyAgent
        Public Property Credentials As BskyCredentials

#If CustomSessionSerialize Then
        Private Class SessionData
            Property AccessJwt As BskyToken
            Property RefreshToken As BskyToken
            Property AuthenticationType As idunno.AtProto.Authentication.AuthenticationType
            Property Service As Uri
        End Class
        Public Class BskyToken
            Property Header As Dictionary(Of String, Object)
            Property Payload As Dictionary(Of String, Object)
            Property Signature As Byte()
        End Class
#End If

        ''' <summary>
        ''' Executes a Bluesky API action and automatically saves the session if the token expiration date shifted in the background.
        ''' </summary>
        Public Async Function ExecuteWithAutoSave(Of T)(
    apiAction As System.Func(Of System.Threading.Tasks.Task(Of T))
) As System.Threading.Tasks.Task(Of T)

            ' 1. Capture the expiration timestamp before running the API call
            Dim expiresBefore As System.DateTimeOffset = If(Me.BskyAgent.Credentials?.ExpiresOn, System.DateTimeOffset.MinValue)

            ' 2. Execute the actual library method (e.g., sending a post, fetching a feed)
            Dim result As T = Await apiAction.Invoke()

            ' 3. Capture the expiration timestamp after the call
            Dim expiresAfter As System.DateTimeOffset = If(Me.BskyAgent.Credentials?.ExpiresOn, System.DateTimeOffset.MinValue)

            ' 4. If the timestamp changed, a silent background refresh occurred
            If expiresBefore <> expiresAfter AndAlso expiresAfter > System.DateTimeOffset.UtcNow Then
                ' Save the freshly rotated tokens safely back to the encrypted DPAPI file
                Call Me.SaveSession()
            End If

            Return result
        End Function

        ''' <summary>
        ''' Securely encrypts and stores the active session credentials via DPAPI.
        ''' </summary>
        Public Sub SaveSession()
            Call SaveSession(Me.BskyAgent)
        End Sub
#If CustomSessionSerialize Then
        ''' <summary>
        ''' Serializes the Bluesky Json Web Tokens in the AccessCredentials for storing them in a local file
        ''' </summary>
        Private Shared Function BskyTokensSerialize(cred As idunno.AtProto.Authentication.AccessCredentials) As String
            Return BskyTokensSerialize(
                    accessJwt:=cred.AccessJwt,
                    refreshToken:=cred.RefreshToken,
                    authenticationType:=cred.AuthenticationType,
                    service:=cred.Service)
        End Function

        ''' <summary>
        ''' Serializes the Bluesky Json Web Tokens for storing them in a local file
        ''' </summary>
        Private Shared Function BskyTokensSerialize(accessJwt As String, refreshToken As String, authenticationType As idunno.AtProto.Authentication.AuthenticationType, service As System.Uri) As String
            Dim tokens As New SessionData With {
                .AccessJwt = BskyTokenPrepareSerialize(accessJwt),
                .RefreshToken = BskyTokenPrepareSerialize(refreshToken),
                .AuthenticationType = authenticationType,
                .Service = service
            }
            Return System.Text.Json.JsonSerializer.Serialize(tokens)
        End Function

        Private Shared Function BskyTokenSerialize(token As String) As String
            Return System.Text.Json.JsonSerializer.Serialize(BskyTokenPrepareSerialize(token))
        End Function

        Private Shared Function BskyTokenPrepareSerialize(token As String) As BskyToken
            Dim handler As New System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler

            ' Read the JWT token without validating its signature
            Dim jwtToken As System.IdentityModel.Tokens.Jwt.JwtSecurityToken = handler.ReadJwtToken(token)

            Dim bsJwt As New BskyToken With {
                .Header = jwtToken.Header.ToDictionary,
                .Payload = jwtToken.Payload.ToDictionary,
                .Signature = System.Buffers.Text.Base64Url.DecodeFromChars(jwtToken.RawSignature)
            }
            Return bsJwt
        End Function

        Private Shared Function BskyTokenDeserialize(serializedJson As String) As String
            Return BskyTokenDeserialize(System.Text.Json.JsonSerializer.Deserialize(Of BskyToken)(serializedJson))
        End Function

        Private Shared Function BskyTokensDeserializeToSessionData(serializedJson As String) As SessionData
            Return System.Text.Json.JsonSerializer.Deserialize(Of SessionData)(serializedJson)
        End Function

        ''' <summary>
        ''' Converts the serialized tokens back to a AccessCredentials object
        ''' </summary>
        Private Shared Function BskyTokensDeserialize(serializedJson As String) As idunno.AtProto.Authentication.AccessCredentials
            Dim sess = BskyTokensDeserializeToSessionData(serializedJson)

            Return New idunno.AtProto.Authentication.AccessCredentials(
                service:=sess.Service,
                authenticationType:=sess.AuthenticationType,
                accessJwt:=BskyTokenDeserialize(sess.AccessJwt),
                refreshToken:=BskyTokenDeserialize(sess.RefreshToken)
                )
        End Function

        ''' <summary>
        ''' Converts the serialized token object back to a valid Bluesky Json Web Token
        ''' </summary>
        Private Shared Function BskyTokenDeserialize(token As BskyToken) As String
            Dim tokenPartsJson(1) As String
            Dim tokenParts(2) As String

            ' Configure options to allow relaxed escaping
            Dim options As New System.Text.Json.JsonSerializerOptions() With {
            .Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        }

            tokenPartsJson(0) = System.Text.Json.JsonSerializer.Serialize(token.Header, options)
            tokenPartsJson(1) = System.Text.Json.JsonSerializer.Serialize(token.Payload, options)

            tokenParts(0) = System.Buffers.Text.Base64Url.EncodeToChars(System.Text.Encoding.UTF8.GetBytes(tokenPartsJson(0)))
            tokenParts(1) = System.Buffers.Text.Base64Url.EncodeToChars(System.Text.Encoding.UTF8.GetBytes(tokenPartsJson(1)))
            tokenParts(2) = System.Buffers.Text.Base64Url.EncodeToChars(token.Signature)

            Dim assembledToken = String.Join("."c, tokenParts)

            Return assembledToken
        End Function
#End If
        Public Shared Sub SaveSession(agent As idunno.Bluesky.BlueskyAgent)
            If agent Is Nothing OrElse agent.Credentials Is Nothing Then Return
#If CustomSessionSerialize Then
            ' Custom serializer
            Dim json = BskyTokensSerialize(agent.Credentials)
#Else
            ' Direct serialization of the original library credentials object
            Dim json As String = System.Text.Json.JsonSerializer.Serialize(agent.Credentials)
#End If
            Dim plainBytes As Byte() = System.Text.Encoding.UTF8.GetBytes(json)

#If EncryptSavedSession Then
            ' Encrypt via DPAPI (CurrentUser scope ensures only this Windows user on this machine can decrypt it)
            Dim encryptedBytes As Byte() = System.Security.Cryptography.ProtectedData.Protect(plainBytes, GetDynamicEntropy, System.Security.Cryptography.DataProtectionScope.CurrentUser)

            ' Save encrypted session data to a file
            Call System.IO.File.WriteAllBytes(Bdiu.BdiuHelper.SessionFilePath, encryptedBytes)
#Else
            Call System.IO.File.WriteAllBytes(Bdiu.BdiuHelper.SessionFilePath, plainBytes)
#End If
        End Sub


        ''' <summary>
        ''' Decrypts the session file and injects an AccessCredentials object directly into the agent.
        ''' This enables instant offline authentication without a network handshake.
        ''' </summary>
        Public Function TryLoadSessionCredentialsLocally() As Boolean
            If Not System.IO.File.Exists(Bdiu.BdiuHelper.SessionFilePath) Then Return False

            Try
                Dim encryptedBytes As Byte() = System.IO.File.ReadAllBytes(Bdiu.BdiuHelper.SessionFilePath)

                Dim plainBytes As Byte()
                If DpapiValidator.IsValidDpapiBlob(encryptedBytes) Then
                    Dim entropy As Byte() = GetDynamicEntropy()

                    ' Decrypt via DPAPI
                    plainBytes = System.Security.Cryptography.ProtectedData.Unprotect(
                         encryptedBytes,
                         entropy,
                         System.Security.Cryptography.DataProtectionScope.CurrentUser
                        )
                Else
                    ' Data is unencrypted
                    plainBytes = encryptedBytes
                End If

                Dim json As String = System.Text.Encoding.UTF8.GetString(plainBytes)

#If CustomSessionSerialize Then
                Dim credentials = BskyTokensDeserialize(json)
#Else
                ' Materialize directly into the required library type
                Dim credentials As idunno.AtProto.Authentication.AccessCredentials =
            System.Text.Json.JsonSerializer.Deserialize(Of idunno.AtProto.Authentication.AccessCredentials)(json)
#End If
                If Me.BskyAgent Is Nothing Then
                    Me.BskyAgent = New idunno.Bluesky.BlueskyAgent()
                End If

                ' Inject credentials directly into the agent
                Me.BskyAgent.Credentials = credentials
                Return True
            Catch ex As System.Security.Cryptography.CryptographicException
                ' DPAPI decryption failed
            Catch ex As System.Exception
                ' Other error
            End Try

            Return False
        End Function

        ''' <summary>
        ''' Logs out the agent from the Bluesky servers and deletes the encrypted local session file.
        ''' </summary>
        Public Function Logout(ct As System.Threading.CancellationToken) As Boolean
            ' Terminate the session on the Bluesky servers if authenticated
            If Me.BskyAgent IsNot Nothing AndAlso Me.BskyAgent.IsAuthenticated Then
                Try
                    Dim task = Me.BskyAgent.Logout(ct)
                    Call task.Wait(2000, ct)
                Catch ex As System.Exception
                    ' Session already invalid
                End Try

                ' Clear the in-memory session credentials immediately
                Me.BskyAgent.Credentials = Nothing
            End If

            ' Remove the local encrypted credential file from the disk
            Try
                If System.IO.File.Exists(Bdiu.BdiuHelper.SessionFilePath) Then
                    System.IO.File.Delete(Bdiu.BdiuHelper.SessionFilePath)
                    Return True
                End If
            Catch ex As System.Exception
                ' File could not be deleted (Probably locked by another process)
            End Try

            ' Return true if at least the file was cleared, meaning the app is in a logged-out state
            Return Not System.IO.File.Exists(Bdiu.BdiuHelper.SessionFilePath)
        End Function

        ''' <summary>
        ''' Checks if the BlueSky agent has the IsAuthenticated flag set.
        ''' </summary>
        Public ReadOnly Property BskyAgentIsAuthenticated As Boolean
            Get
                If Me.BskyAgent Is Nothing Then
                    Return False
                Else
                    Return Me.BskyAgent.IsAuthenticated
                End If
            End Get
        End Property
        Public Shared Function GetTokenExpiry(token As String) As DateTimeOffset
            Try
                Dim handler As New System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler()

                ' Read the JWT token without validating its signature
                Dim jwtToken As System.IdentityModel.Tokens.Jwt.JwtSecurityToken = handler.ReadJwtToken(token)

                ' Retrieve the 'ValidTo' property (implicitly converts UTC DateTime to DateTimeOffset)
                Return jwtToken.ValidTo
            Catch ex As Exception
                ' Handle cases where the token is malformed or not a valid JWT
                Throw New ArgumentException("Invalid token format.", ex)
            End Try
        End Function
        Public Function TestSession() As Boolean
            Return TestSession(Nothing)
        End Function
        Public Function TestSession(ByRef outException As System.Exception) As Boolean
            Try
                ' Capture Access Token before Call
                Dim tokenBefore As String = If(Me.BskyAgent?.Credentials.AccessJwt, String.Empty)

                ' We fetch the current actor profile to force a real network validation
                Dim profileTask = Me.BskyAgent.GetProfile()
                Call profileTask.Wait()
                Dim profileResult = profileTask.Result

                If profileResult.Succeeded Then
                    ' Success! The tokens are valid (or were just successfully refreshed).

                    ' Capture Access Token after Call
                    Dim tokenAfter As String = If(Me.BskyAgent?.Credentials.AccessJwt, String.Empty)

                    ' 4. If idunno performed a silent background refresh, the tokens will differ
                    If Not String.IsNullOrEmpty(tokenAfter) AndAlso tokenBefore <> tokenAfter Then
                        ' Save the freshly rotated tokens safely back to the encrypted file
                        Me.SaveSession()
                    End If
                End If
                Return Me.BskyAgent.IsAuthenticated
            Catch ex As idunno.AtProto.AuthenticationRequiredException
                ' Expired tokens
                outException = ex
                Return False
            Catch ex As System.Exception
                ' Network error or expired tokens
                outException = ex
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Checks if there is a current active valid BlueSky session.
        ''' If not it first tries to load the locally stored session data.
        ''' If that fails it tries to log in with the cached or locally stored credentials to create a new session.
        ''' </summary>
        ''' <param name="ct">Optional CancellationToken</param>
        Public Sub ResumeSessionOrLogInIfNotLoggedIn(ct As System.Threading.CancellationToken)
            If Me.BskyAgentIsAuthenticated Then
                ' Already authenticated. No Action needed
            Else
                If TryLoadSessionCredentialsLocally() Then
                    If Me.BskyAgentIsAuthenticated Then
                        ' Successfully resumed the session and verified its validity
                    Else
                        ' Resumed session is expired
                        ' New login with credentials required
                        Call LoginWithCredentialsToBsky(ct)
                    End If
                Else
                    ' No session data stored or resuming failed
                    ' New login with credentials required
                    Call LoginWithCredentialsToBsky(ct)
                End If
            End If
        End Sub

        ''' <summary>
        ''' Tries to log in with the cached or locally stored credentials to create a new session.
        ''' </summary>
        ''' <param name="ct">Optional CancellationToken</param>
        Public Sub LoginWithCredentialsToBsky(ct As System.Threading.CancellationToken)
            If Me.BskyAgent Is Nothing Then
                Me.BskyAgent = New idunno.Bluesky.BlueskyAgent
            Else
                Me.BskyAgent.Logout(ct)
            End If
            If Me.Credentials Is Nothing Then
                Me.Credentials = BskyCredentials.LoadNewFromFile

                If Me.Credentials Is Nothing Then
                    Throw New BskyDirectImageUploader.Bdiu.BskyUploadPayload.BlueskyLoginException($"No Bluesky credentials have been set up.")
                End If
            End If

            Dim loginTask = Me.BskyAgent.Login(
                    identifier:=Me.Credentials.BskyHandle,
                    password:=Me.Credentials.BskyPasswordPlain,
                    cancellationToken:=ct)
            Call loginTask.Wait(ct)
            Dim loginResult = loginTask.Result

            ' Save the session to a local file
            Call Me.SaveSession()

            If Not loginResult.Succeeded Then
                Throw New BskyDirectImageUploader.Bdiu.BskyUploadPayload.BlueskyLoginException($"Bluesky login failed. Please check credentials. {loginResult.AtErrorDetail.Error}: {loginResult.AtErrorDetail.Message}", loginResult.StatusCode)
            End If
        End Sub

        Public Sub EnterCredentials(ByVal parentHandle As IntPtr)
            If Me.Credentials Is Nothing Then
                Me.Credentials = BskyCredentials.EnterCredentialsToNew(parentHandle)
            Else
                Call Me.Credentials.EnterCredentials(parentHandle)
            End If
        End Sub

        Public Class BskyCredentials
            Public Property BskyHandle As String
            Public Property BskyPasswordEncrypted As Byte()

            Sub New(bskyHandle As String, passwordPlain As String)
                Me.BskyHandle = bskyHandle
                Me.BskyPasswordPlain = passwordPlain
            End Sub
            Sub New(bskyHandle As String, passwordEncrypted() As Byte)
                Me.BskyHandle = bskyHandle
                Me.BskyPasswordEncrypted = passwordEncrypted
            End Sub
            Public Property BskyPasswordPlain As String
                Get
                    If Me.BskyPasswordEncrypted Is Nothing Then
                        Return Nothing
                    Else
                        Dim entropy As Byte() = GetDynamicEntropy()
                        Return System.Text.Encoding.UTF8.GetString(System.Security.Cryptography.ProtectedData.Unprotect(Me.BskyPasswordEncrypted, entropy, System.Security.Cryptography.DataProtectionScope.CurrentUser))
                    End If
                End Get
                Set(passwordPlain As String)
                    If String.IsNullOrEmpty(passwordPlain) Then
                        Me.BskyPasswordEncrypted = Nothing
                    Else
                        Dim entropy As Byte() = GetDynamicEntropy()
                        Me.BskyPasswordEncrypted = System.Security.Cryptography.ProtectedData.Protect(System.Text.Encoding.UTF8.GetBytes(passwordPlain), entropy, System.Security.Cryptography.DataProtectionScope.CurrentUser)
                    End If
                End Set
            End Property
            Public Sub EnterCredentials(ByVal parentHandle As IntPtr)
                Dim userhandle As String = ""
                Dim password As String = ""
                Dim saveChecked As Boolean

                ' Call modern API wrapper
                If TryEnterCredentials(
            parentHandle,
            userhandle,
            password,
            saveChecked) Then

                    ' Success: Credentials captured successfully in plain text
                    ' Encrypt password natively via Windows Data Protection API (DPAPI)
                    Me.BskyHandle = userhandle
                    Me.BskyPasswordPlain = password

                    If saveChecked Then
                        ' Save credentials
                        Me.SaveToFile()
                    Else
                        ' Delete the old credential files
                        BskyCredentials.ClearCredentialFiles()
                    End If
                Else
                    ' Cancelled
                End If
            End Sub
            Public Shared Function EnterCredentialsToNew(ByVal parentHandle As IntPtr) As BskyCredentials
                Dim userhandle As String = ""
                Dim password As String = ""
                Dim saveChecked As Boolean

                ' Call modern API wrapper
                If TryEnterCredentials(
            parentHandle,
            userhandle,
            password,
            saveChecked) Then

                    ' Success: Credentials captured successfully in plain text
                    ' Encrypt password natively via Windows Data Protection API (DPAPI)
                    Dim cred As New BskyCredentials(userhandle, password)

                    If saveChecked Then
                        ' Save credentials
                        cred.SaveToFile()
                    Else
                        ' Delete the old credential files
                        BskyCredentials.ClearCredentialFiles()
                    End If
                    Return cred
                Else
                    ' Cancelled
                    Return Nothing
                End If
            End Function

            Private Shared Function TryEnterCredentials(ByVal parentHandle As IntPtr, ByRef outUserhandle As String, ByRef outPassword As String, ByRef outSaveChecked As Boolean) As Boolean
                ' Call modern API wrapper
                Return Bdiu.BskySessionManager.BskyCredentials.NativeLogin.ShowLogin(
            parentHandle,
            "Bluesky Authentication",
            "Please enter your Bluesky Handle and Bluesky App Password.",
            outUserhandle,
            outPassword,
            outSaveChecked)
            End Function

            ''' <summary>
            ''' Delete stored credential files and internal values (Handle + App Password)
            ''' </summary>
            Public Sub ClearCredentials()
                Me.BskyHandle = Nothing
                Me.BskyPasswordPlain = Nothing
                Call ClearCredentialFiles()
            End Sub
            Public Shared Sub ClearCredentialFiles()
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
            End Sub

            Public Sub SaveToFile()
                System.IO.File.WriteAllText(Bdiu.BdiuHelper.HandleFilePath, Me.BskyHandle, System.Text.Encoding.UTF8)
                System.IO.File.WriteAllBytes(Bdiu.BdiuHelper.PasswordFilePath, Me.BskyPasswordEncrypted)
            End Sub
            Public Function LoadFromFile() As Boolean
                Dim handle As String = Nothing
                Dim encryptedBytes As Byte() = Nothing

                ' Load Windows DPAPI encrypted credentials (tied to current Windows User Account)
                If TryGetCredentialsFromFile(handle, encryptedBytes) Then
                    Me.BskyHandle = handle
                    Me.BskyPasswordEncrypted = encryptedBytes
                    Return True
                Else
                    Return False
                End If
            End Function
            Public Shared Function LoadNewFromFile() As BskyCredentials
                Dim handle As String = Nothing
                Dim encryptedBytes As Byte() = Nothing

                ' Load Windows DPAPI encrypted credentials (tied to current Windows User Account)
                If TryGetCredentialsFromFile(handle, encryptedBytes) Then
                    Return New BskyCredentials(handle, encryptedBytes)
                Else
                    Return Nothing
                End If
            End Function

            ''' <summary>
            ''' Tries to read the locally stored user handle and app password
            ''' </summary>
            ''' <param name="outHandle">User handle</param>
            ''' <param name="outBskyPasswordEncrypted">DPAPI encrypted app password</param>
            ''' <returns>Success if true</returns>
            Public Shared Function TryGetCredentialsFromFile(ByRef outHandle As String, ByRef outBskyPasswordEncrypted() As Byte) As Boolean
                Try
                    If System.IO.File.Exists(Bdiu.BdiuHelper.HandleFilePath) Then
                        outHandle = System.IO.File.ReadAllText(Bdiu.BdiuHelper.HandleFilePath).Trim()
                    Else
                        Return False
                    End If

                    If System.IO.File.Exists(Bdiu.BdiuHelper.PasswordFilePath) Then
                        outBskyPasswordEncrypted = System.IO.File.ReadAllBytes(Bdiu.BdiuHelper.PasswordFilePath)
                        Return True
                    Else
                        Return False
                    End If
                Catch ex As Exception
                    Return False
                End Try
            End Function

            Public Class NativeLogin

                ' Structure defining the dialog appearance (64-bit compatible)
                <System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet:=System.Runtime.InteropServices.CharSet.Unicode)>
                Public Structure CREDUI_INFO
                    Public cbSize As Integer
                    Public hwndParent As IntPtr
                    Public pszMessageText As String
                    Public pszCaptionText As String
                    Public hbmBanner As IntPtr
                End Structure

                ' Modern Windows 10 / 11 API for credential prompt
                <System.Runtime.InteropServices.DllImport("credui.dll", CharSet:=System.Runtime.InteropServices.CharSet.Unicode)>
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
                <System.Runtime.InteropServices.DllImport("credui.dll", CharSet:=System.Runtime.InteropServices.CharSet.Unicode)>
                Private Shared Function CredUnPackAuthenticationBuffer(
            ByVal dwFlags As Integer,
            ByVal pvAuthBuffer As IntPtr,
            ByVal cbAuthBuffer As UInteger,
            ByVal pszUserName As System.Text.StringBuilder,
            ByRef pcchUserName As Integer,
            ByVal pszDomainName As System.Text.StringBuilder,
            ByRef pcchDomainName As Integer,
            ByVal pszPassword As System.Text.StringBuilder,
            ByRef pcchPassword As Integer) As Boolean
                End Function

                ' Function to release memory allocated by the Windows API
                <System.Runtime.InteropServices.DllImport("ole32.dll", SetLastError:=True)>
                Private Shared Sub CoTaskMemFree(ByVal pv As IntPtr)
                End Sub

                ''' <summary>
                ''' Invokes the native Windows login dialog and returns username & password as plain text.
                ''' </summary>
                Public Shared Function ShowLogin(
                ByVal parentHandle As IntPtr,
                ByRef outUsername As String,
                ByRef outPassword As String,
                ByRef outSaveChecked As Boolean) As Boolean

                    Return NativeLogin.ShowLogin(
                    parentHandle,
                    "Authentication Required",
                    "Please enter your credentials.",
                    outUsername,
                    outPassword,
                    outSaveChecked)
                End Function

                ''' <summary>
                ''' Invokes the native Windows login dialog and returns username & password as plain text.
                ''' </summary>
                Public Shared Function ShowLogin(
                ByVal parentHandle As IntPtr,
                ByVal captionText As String,
                ByVal messageText As String,
                ByRef outUsername As String,
                ByRef outPassword As String,
                ByRef outSaveChecked As Boolean) As Boolean
                    ' 1. Configure dialog properties
                    Dim uiInfo As New CREDUI_INFO()
                    uiInfo.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(uiInfo)
                    uiInfo.hwndParent = parentHandle
                    uiInfo.pszCaptionText = captionText
                    uiInfo.pszMessageText = messageText

                    ' Prepare buffers for the native memory pointer
                    Dim authPackage As UInteger = 0
                    Dim outAuthBuffer As IntPtr = IntPtr.Zero
                    Dim outAuthBufferSize As UInteger = 0

                    Const CREDUIWIN_GENERIC As UInteger = &H1
                    Const CREDUIWIN_CHECKBOX As UInteger = &H2

                    Dim flags As UInteger = CREDUIWIN_GENERIC Or CREDUIWIN_CHECKBOX

                    ' 2. Invoke the dialog (CREDUIWIN_GENERIC enforces standard input fields without AD validation)
                    Dim result As Integer = CredUIPromptForWindowsCredentials(
                uiInfo, 0, authPackage, IntPtr.Zero, 0, outAuthBuffer, outAuthBufferSize, outSaveChecked, flags
            )

                    ' If result = 0 (ERROR_SUCCESS), the user entered data and clicked OK
                    If result = 0 AndAlso outAuthBuffer <> IntPtr.Zero Then
                        Dim maxLen As Integer = 256
                        Dim usernameBuf As New System.Text.StringBuilder(maxLen)
                        Dim domainBuf As New System.Text.StringBuilder(maxLen)
                        Dim passwordBuf As New System.Text.StringBuilder(maxLen)

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
        End Class
    End Class
End Namespace