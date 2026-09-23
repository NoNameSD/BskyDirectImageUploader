Imports System.ComponentModel

Public Class BdiuCheckDependencies
    Sub Main()
        Dim pythonVersion As String = ""

        Dim isPythonInstalled As Boolean = CheckCommand("python", "--version", pythonVersion)

        If isPythonInstalled Then
            Me.txtPython.Text = ($"Installed version: {pythonVersion.Trim()}")
        Else
            Me.txtPython.Text = ("Python is missing or not added to %PATH%")
            Return
        End If

        Dim isPackageInstalled As Boolean = CheckCommand("python", "-c ""import atproto""", "")

        If isPackageInstalled Then
            Console.WriteLine("✅ Paket 'atproto' ist installiert.")
        Else
            Console.WriteLine("❌ Paket 'atproto' ist NICHT installiert.")
        End If
    End Sub

    Private Function CheckCommand(command As String, arguments As String, ByRef output As String) As Boolean
        Try
            Dim startInfo As New ProcessStartInfo With {
                .FileName = command,
                .Arguments = arguments,
                .RedirectStandardOutput = True,
                .RedirectStandardError = True,
                .UseShellExecute = False,
                .CreateNoWindow = True
            }

            Using process As Process = Process.Start(startInfo)
                If process Is Nothing Then
                    output = String.Empty
                    Return False
                End If

                Dim stdout As String = process.StandardOutput.ReadToEnd()
                Dim stderr As String = process.StandardError.ReadToEnd()
                process.WaitForExit()

                ' Wenn der ExitCode 0 ist, war der Befehl erfolgreich
                If process.ExitCode = 0 Then
                    output = If(Not String.IsNullOrWhiteSpace(stdout), stdout, stderr)
                    Return True
                End If

                output = stderr
                Return False
            End Using

        Catch ex As Win32Exception
            ' Tritt auf, wenn die ausführbare Datei 'python' gar nicht im PATH gefunden wird
            output = String.Empty
            Return False
        End Try
    End Function
End Class