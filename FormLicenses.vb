#Disable Warning IDE1006 ' Naming Styles
Public Class FormLicenses


    Private Sub lstLicenses_SelectedIndexChanged(sender As Object, e As EventArgs) Handles lstLicenses.SelectedIndexChanged
        Dim resources As New System.ComponentModel.ComponentResourceManager(GetType(FormLicenses))

        Select Case lstLicenses.SelectedItem.ToString()
            Case "idunno.Bluesky"
                txtLicense.Text =
                    resources.GetString("LICENSE_idunno.Bluesky")

            Case "Magick.NET"
                txtLicense.Text =
                    resources.GetString("LICENSE_Magick.NET")

            Case "IdentityModel Extensions for .NET"
                txtLicense.Text =
                    resources.GetString("LICENSE_azure-activedirectory-identitymodel-extensions-for-dotnet")

            Case Else
                txtLicense.Text =
                    Nothing
        End Select
    End Sub

    Private Sub FormLicenses_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Width = 700
        Me.Height = 500
    End Sub
End Class
#Enable Warning IDE1006