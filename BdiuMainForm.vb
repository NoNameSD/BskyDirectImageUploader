#Disable Warning IDE1006 ' Naming Styles

Public Class BdiuMainForm

    <System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)>
    Public Property OxiPngOpt As New BskyDirectImageUploader.Bdiu.OxiPngOptimize
    <System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)>
    Public Property BskyMgmt As BskyDirectImageUploader.Bdiu.BskySessionManager

    Private Sub btnCreatePost_Click(sender As Object, e As EventArgs) Handles btnCreatePost.Click
        Dim subForm As New BdiuBskyPostBuilderForm With {
                .Icon = Me.Icon,
                .StartPosition = FormStartPosition.CenterParent,
                .OxiPngOpt = OxiPngOpt,
                .BskyMgmt = Me.BskyMgmt
            }

        Call Me.Hide()
        Call subForm.ShowDialog(owner:=Me)
        Call Me.Show()
        Me.BskyMgmt = subForm.BskyMgmt
        Call subForm.Dispose()
    End Sub

    Private Sub btnSetupBskyCredentials_Click(sender As Object, e As EventArgs) Handles btnSetupBskyCredentials.Click
        ' Prevent the user from resizing the window borders
        ' Remove the maximize and minimize buttons (optional, but standard for dialogs)
        ' Center the dialog relative to the parent form (recommended)
        Dim subForm As New BdiuCredentialForm With {
                .Icon = Me.Icon,
                .FormBorderStyle = FormBorderStyle.FixedDialog,
                .MaximizeBox = False,
                .MinimizeBox = False,
                .StartPosition = FormStartPosition.CenterParent,
                .BskyMgmt = Me.BskyMgmt
            }

        Call Me.Hide()
        Call subForm.ShowDialog(owner:=Me)
        Call Me.Show()
        Me.BskyMgmt = subForm.BskyMgmt
        Call subForm.Dispose()
    End Sub

    Private Sub btnOxiPngForm_Click(sender As Object, e As EventArgs) Handles btnOxiPngForm.Click
        Dim subForm As New OxiPngSelectForm With {
                .Icon = Me.Icon,
                .StartPosition = FormStartPosition.CenterParent,
                .OxiPngOpt = OxiPngOpt
            }
        Call Me.Hide()
        Call subForm.ShowDialog(owner:=Me)
        Call Me.Show()
        Call subForm.Dispose()
    End Sub

    Private Sub BdiuMainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Text = String.Format("{0} - Version {1}", My.Application.Info.Title, My.Application.Info.Version.ToString())
    End Sub

    Private Sub btnShowLibraries_Click(sender As Object, e As EventArgs) Handles btnShowLibraries.Click
        Dim subForm As New FormLicenses With {
                .Icon = Me.Icon,
                .StartPosition = FormStartPosition.CenterParent
            }
        Call Me.Hide()
        Call subForm.ShowDialog(owner:=Me)
        Call Me.Show()
        Call subForm.Dispose()
    End Sub
End Class
#Enable Warning IDE1006 ' Naming Styles