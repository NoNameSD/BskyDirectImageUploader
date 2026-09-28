#Disable Warning IDE1006 ' Naming Styles
Public Class BdiuCredentialForm

    <System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)>
    Public Property BskyMgmt As BskyDirectImageUploader.Bdiu.BskySessionManager

    Private Sub btnEnterCredentials_Click(sender As Object, e As EventArgs) Handles btnEnterCredentials.Click
        If Me.BskyMgmt Is Nothing Then
            Me.BskyMgmt = New BskyDirectImageUploader.Bdiu.BskySessionManager
        End If
        Call Me.BskyMgmt.EnterCredentials(Me.Handle)
        Call RefreshForm()
    End Sub

    Public Sub RefreshForm()
        If Me.BskyMgmt Is Nothing Then
            Me.BskyMgmt = New BskyDirectImageUploader.Bdiu.BskySessionManager
        End If
        Dim credentialLoadSuccess As Boolean
        Try
            If Me.BskyMgmt.Credentials Is Nothing Then
                Me.BskyMgmt.Credentials = BskyDirectImageUploader.Bdiu.BskySessionManager.BskyCredentials.LoadNewFromFile

                credentialLoadSuccess = Me.BskyMgmt.Credentials IsNot Nothing
            Else
                If String.IsNullOrWhiteSpace(Me.BskyMgmt.Credentials.BskyHandle) Then
                    credentialLoadSuccess = Me.BskyMgmt.Credentials.LoadFromFile()
                Else
                    credentialLoadSuccess = True
                End If
            End If

            If credentialLoadSuccess Then
                Me.txtBskyHandle.Text = Me.BskyMgmt.Credentials.BskyHandle
                Me.rbtSetBskyPwd.Checked = True
            Else
                Me.txtBskyHandle.Text = Nothing
                Me.rbtSetBskyPwd.Checked = False
            End If

            If Not Me.BskyMgmt.BskyAgentIsAuthenticated Then
                Call Me.BskyMgmt.TryLoadSessionCredentialsLocally()
            End If

            Me.rbtActiveSession.Checked = Me.BskyMgmt.BskyAgentIsAuthenticated

            If Me.BskyMgmt.BskyAgent Is Nothing Then
                Me.txtBskyDid.Text = Nothing
                Call DisableSessionValidDatePicker()
                Me.btnLogin.Enabled = True
                Me.btnLogout.Enabled = False
            Else
                Me.txtBskyDid.Text = Me.BskyMgmt.BskyAgent.Did
                If Me.BskyMgmt.BskyAgent.Credentials IsNot Nothing Then
                    Call EnableSessionValidDatePicker()
                    Me.dtpSessionValid.Value = Me.BskyMgmt.BskyAgent.Credentials.ExpiresOn.LocalDateTime
                    Me.dtpRefreshValid.Value = Bdiu.BskySessionManager.GetTokenExpiry(Me.BskyMgmt.BskyAgent.Credentials.RefreshToken).LocalDateTime
                    Me.btnLogin.Enabled = False
                    Me.btnLogout.Enabled = True
                Else
                    Call DisableSessionValidDatePicker()
                    Me.btnLogin.Enabled = True
                    Me.btnLogout.Enabled = False
                End If
            End If

        Catch ex As Exception
            Call Bdiu.BDIUExceptionDisplay.DisplayExceptionAsMessageBox(Me, ex)
        End Try
    End Sub
    Private Sub DisableSessionValidDatePicker()
        Me.dtpSessionValid.Enabled = False
        Me.dtpSessionValid.Format = DateTimePickerFormat.Custom
        Me.dtpSessionValid.CustomFormat = " "
        Me.dtpRefreshValid.Enabled = Me.dtpSessionValid.Enabled
        Me.dtpRefreshValid.Format = Me.dtpSessionValid.Format
        Me.dtpRefreshValid.CustomFormat = Me.dtpSessionValid.CustomFormat
    End Sub
    Private Sub EnableSessionValidDatePicker()
        Me.dtpSessionValid.Enabled = True
        Me.dtpSessionValid.Format = DateTimePickerFormat.Custom
        Me.dtpSessionValid.CustomFormat = Bdiu.BdiuHelper.CurrentCultureDateTimeFormat()
        Me.dtpRefreshValid.Enabled = Me.dtpSessionValid.Enabled
        Me.dtpRefreshValid.Format = Me.dtpSessionValid.Format
        Me.dtpRefreshValid.CustomFormat = Me.dtpSessionValid.CustomFormat
    End Sub

    Private Sub btnClearCredentials_Click(sender As Object, e As EventArgs) Handles btnClearCredentials.Click
        Call ClearCredentials()
    End Sub

    Public Sub ClearCredentials()
        If Me.BskyMgmt IsNot Nothing AndAlso Me.BskyMgmt.Credentials IsNot Nothing Then
            Call Me.BskyMgmt.Credentials.ClearCredentials()
        End If
        Call RefreshForm()
    End Sub

    Private Sub BdiuCredentialForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Call RefreshForm()
    End Sub

    ''' <summary>
    ''' Logout of the current session and remove locally stored session file
    ''' </summary>
    Private Sub btnLogout_Click(sender As Object, e As EventArgs) Handles btnLogout.Click
        If Me.BskyMgmt IsNot Nothing Then
            Try
                Call Me.BskyMgmt.Logout(Nothing)
            Catch ex As Exception
                Call Bdiu.BDIUExceptionDisplay.DisplayExceptionAsMessageBox(Me, ex)
            End Try
            Call RefreshForm()
        End If
    End Sub

    Private Sub btnLogin_Click(sender As Object, e As EventArgs) Handles btnLogin.Click
        Try
            Call Me.BskyMgmt.ResumeSessionOrLogInIfNotLoggedIn(Nothing)
        Catch ex As System.Security.Cryptography.CryptographicException
            Call MessageBox.Show(Me, $"Decrypting the stored credentials failed.{Environment.NewLine}Please re-enter your credentials.", "Login failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Catch ex As BskyDirectImageUploader.Bdiu.BskyUploadPayload.BlueskyLoginException
            Call MessageBox.Show(Me, $"Login to Bluesky failed.{Environment.NewLine}Please check your credentials", "Login failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Catch ex As Exception
            Call MessageBox.Show(Me, $"Error when trying to log into Bluesky: {ex.Message}", "Login failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
        Call RefreshForm()
    End Sub

    Private Sub btnTestSession_Click(sender As Object, e As EventArgs) Handles btnTestSession.Click
        If Me.BskyMgmt IsNot Nothing Then
            Dim ex As System.Exception = Nothing
            If Me.BskyMgmt.TestSession(ex) Then
                Call MessageBox.Show(Me, $"The session is valid or was extended with the refresh token.", "Session valid", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Else
                Dim message = $"The session is expired and could not be extended with the refresh token."
                If ex IsNot Nothing Then
                    message += $"{Environment.NewLine}{Environment.NewLine}{ex.Message}"
                End If
                Call MessageBox.Show(Me, message, "Session invalid/expired", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        End If
    End Sub

    Private Sub btnSaveSession_Click(sender As Object, e As EventArgs) Handles btnSaveSession.Click
        If Me.BskyMgmt IsNot Nothing Then
            Call Me.BskyMgmt.SaveSession()
        End If
    End Sub

    Private Sub btnRefreshSession_Click(sender As Object, e As EventArgs) Handles btnRefreshSession.Click
        If Me.BskyMgmt IsNot Nothing AndAlso Me.BskyMgmt.BskyAgent IsNot Nothing Then
            Dim success As Boolean
            Dim exc As Exception = Nothing
            Try
                Dim tsk = Me.BskyMgmt.BskyAgent.RefreshCredentials()
                Call tsk.Wait()
                success = tsk.Result
            Catch ex As Exception
                exc = ex
            End Try
            If success Then
                Call MessageBox.Show(Me, "Session successfully extended with the refresh token", "Session extended", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Call Me.BskyMgmt.SaveSession()
            Else
                Dim message = $"The session could not be extended with the refresh token."
                If exc IsNot Nothing Then
                    message += $"{Environment.NewLine}{Environment.NewLine}{exc.Message}"
                End If
                Call MessageBox.Show(Me, message, "Session invalid/expired", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        End If
        Call RefreshForm()
    End Sub
End Class
#Enable Warning IDE1006 ' Naming Styles