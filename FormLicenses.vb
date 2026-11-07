#Disable Warning IDE1006 ' Naming Styles
Public Class FormLicenses

    ' Helper class to hold our parsed data rows
    Public Class PackageLicense
        Public Property PackageName As String = String.Empty
        Public ReadOnly Property PackageIdPrefix As String
            Get
                Dim dotIndex = PackageName.IndexOf("."c)
                If dotIndex >= 0 Then
                    Return PackageName.Substring(0, dotIndex)
                Else
                    Return PackageName
                End If
            End Get
        End Property
        Public Property LicenseName As String = String.Empty
        Public Property DependencyType As String = String.Empty
        Public Property CopyrightHolder As String = String.Empty
        Public Property ProjectUrl As String = String.Empty
        Public Property Version As String = String.Empty
        Public Property PackageIdPrefixHasTopLevel As Boolean = Nothing

        Public Overrides Function ToString() As String
            Return "[" & DependencyType & "] " & PackageName
        End Function
    End Class

    Private Sub LoadLicenses()
        Dim currentAssembly As System.Reflection.Assembly = System.Reflection.Assembly.GetExecutingAssembly()
        Dim topLevelPackageIdPrefixes As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

        ' "CompileTimeLicenses.txt" matches the LogicalName defined inside the .vbproj target
        Using stream As System.IO.Stream = currentAssembly.GetManifestResourceStream("CompileTimeLicenses.txt")
            If stream IsNot Nothing Then
                Using reader As New System.IO.StreamReader(stream)
                    Call lstLicenses.Items.Clear()

                    ' Temporary in-memory list to collect unsorted parsed row data objects
                    Dim rawList As New List(Of PackageLicense)()

                    ' Read the embedded assembly asset stream row by row
                    While Not reader.EndOfStream
                        Dim line As String = reader.ReadLine()
                        If Not String.IsNullOrWhiteSpace(line) AndAlso line.Contains("|"c) Then
                            Dim parts As String() = line.Split("|"c)

                            ' Verify that the record contains all 6 required metadata segments
                            If parts.Length >= 6 Then
                                Dim lic As New PackageLicense() With {
                                    .PackageName = parts(0).Trim(),
                                    .LicenseName = parts(1).Trim(),
                                    .DependencyType = parts(2).Trim(),
                                    .CopyrightHolder = parts(3).Trim(),
                                    .ProjectUrl = parts(4).Trim(),
                                    .Version = parts(5).Trim()
                                }

                                If lic.DependencyType.Equals("Top-Level", StringComparison.OrdinalIgnoreCase) Then
                                    Call topLevelPackageIdPrefixes.Add(lic.PackageIdPrefix)
                                End If

                                ' Replace "Microsoft" with "Microsoft Corporation" as that is the value that shows up in the official license files
                                If lic.CopyrightHolder.Equals("Microsoft", StringComparison.CurrentCultureIgnoreCase) Then
                                    lic.CopyrightHolder = "Microsoft Corporation"
                                End If

                                Call rawList.Add(lic)
                            End If
                        End If
                    End While

                    For Each lic In rawList
                        lic.PackageIdPrefixHasTopLevel = topLevelPackageIdPrefixes.Contains(lic.PackageIdPrefix)
                    Next

                    ' Sort the list
                    Dim sortedList = rawList.OrderBy(Function(p) p.PackageIdPrefix.Equals("Microsoft", StringComparison.OrdinalIgnoreCase)) _
                                            .ThenByDescending(Function(p) p.PackageIdPrefixHasTopLevel) _
                                            .ThenBy(Function(p) p.CopyrightHolder) _
                                            .ThenBy(Function(p) p.ProjectUrl) _
                                            .ThenBy(Function(p) p.DependencyType) _
                                            .ThenBy(Function(p) p.PackageName) _
                                            .ToList()

                    ' Append the fully pre-sorted metadata collection records into the WinForms UI control elements
                    For Each lic In sortedList
                        Call lstLicenses.Items.Add(lic)
                    Next
                End Using
            Else
                txtLicense.Text = "No embedded license report manifest found inside the application assembly."
        End If
        End Using
    End Sub

    Private Sub lstLicenses_SelectedIndexChanged(sender As Object, e As EventArgs) Handles lstLicenses.SelectedIndexChanged
        Dim resources As New System.ComponentModel.ComponentResourceManager(GetType(FormLicenses))
        If lstLicenses.SelectedItem IsNot Nothing AndAlso TypeOf lstLicenses.SelectedItem Is PackageLicense Then
            Dim selectedPackage As PackageLicense = CType(lstLicenses.SelectedItem, PackageLicense)
            Dim displayedText As String = ""

            ' Clean header formatting
            Dim header As String = $"Package:       {selectedPackage.PackageName}{Environment.NewLine}" &
                                   $"Version:        {selectedPackage.Version}{Environment.NewLine}" &
                                   $"Type:             {selectedPackage.DependencyType}{Environment.NewLine}" &
                                   $"Author/CR:   {selectedPackage.CopyrightHolder}{Environment.NewLine}" &
                                   $"Project-URL: {selectedPackage.ProjectUrl}{Environment.NewLine}" &
                                   $"License:         {selectedPackage.LicenseName}{Environment.NewLine}" &
                                   $"---------------------------------------------------------{Environment.NewLine}{Environment.NewLine}"

            Dim resourceName As String = String.Empty

            If selectedPackage.LicenseName.Contains("MIT", StringComparison.OrdinalIgnoreCase) Then
                resourceName = "MIT"
            ElseIf selectedPackage.LicenseName.Contains("APACHE", StringComparison.OrdinalIgnoreCase) Then
                resourceName = "Apache License 2.0"
            End If

            If Not String.IsNullOrEmpty(resourceName) Then

                displayedText = Resources.GetString(resourceName)

                If selectedPackage.LicenseName.Contains("MIT", StringComparison.OrdinalIgnoreCase) Then
                    displayedText = displayedText.Replace("[HOLDER]", selectedPackage.CopyrightHolder)
                    displayedText = displayedText.Replace("[YEAR]", DateTime.Now.Year.ToString())
                End If
            End If

            If String.IsNullOrWhiteSpace(displayedText) Then
                displayedText = $"This library ({selectedPackage.PackageName}) is governed by the {selectedPackage.LicenseName} license agreement. Copyright (c) by {selectedPackage.CopyrightHolder}."
            End If

            txtLicense.Text = header & displayedText
        End If
    End Sub

    Private Sub FormLicenses_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Width = 900
        Me.Height = 600
        Call LoadLicenses()
    End Sub
End Class
#Enable Warning IDE1006