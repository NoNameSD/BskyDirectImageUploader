Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Threading

Namespace Bdiu
    Public Class OxiPngOptimize

        Public Event ArgsUpdated()
        Public Event ProcessDataReceived(sender As Object, data As String, isErrorData As Boolean)

        ' Track the current process instance so it can be killed if canceled
        Private _currentProcess As Process

        Public Sub New()
            Call Me.LoadOxipngArgumentPreset(OxiPngOptimizePresets.MaxOpt)
        End Sub

        Public Enum OxiPngOptimizePresets
            MidOpt = &B0000_0010_0000_0000
            MidOptInterlace = &B0000_0010_0000_0001
            MaxOpt = &B0000_0100_0000_0000
            MaxOptInterlace = &B0000_0100_0000_0001
            Custom = &B1111_1111_1111_1111
        End Enum

        ''' <summary>
        ''' Path of the file "<see href="https://github.com/oxipng/oxipng/releases">oxipng.exe</see>".
        ''' </summary>
        Public Property OxipngPath As String
        Private _oxipngArguments As String
        Public Property OxipngArguments As String
            Get
                Return _oxipngArguments
            End Get
            Set(value As String)
                If _oxipngArguments IsNot Nothing AndAlso _oxipngArguments.Equals(value) Then
                    ' Nothing changed
                Else
                    _oxipngArguments = value
                    RaiseEvent ArgsUpdated()
                End If
            End Set
        End Property

        Private jsonOutEnabledByGivenArgs As Boolean = False
        Private _jsonOut As String

        Private _processExitCode As Nullable(Of Integer)
        Public Property ProcessExitCode As Nullable(Of Integer)
            Get
                Return _processExitCode
            End Get
            Private Set(value As Nullable(Of Integer))
                _processExitCode = value
            End Set
        End Property

        Public Property JsonOut As String
            Get
                Return _jsonOut
            End Get
            Private Set(value As String)
                _jsonOut = value
            End Set
        End Property

        Public Property OxipngArgumentPreset As OxiPngOptimizePresets
            Get
                Return GetPresetForOxipngArguments(Me.OxipngArguments)
            End Get
            Set(value As OxiPngOptimizePresets)
                If value <> OxiPngOptimizePresets.Custom Then
                    Call LoadOxipngArgumentPreset(value)
                End If
            End Set
        End Property

        Public Shared Function GetPresetForOxipngArguments(oxipngArguments As String) As OxiPngOptimizePresets
            Select Case oxipngArguments
                Case GetPresetOxipngArguments(OxiPngOptimizePresets.MidOpt)
                    Return OxiPngOptimizePresets.MidOpt
                Case GetPresetOxipngArguments(OxiPngOptimizePresets.MidOptInterlace)
                    Return OxiPngOptimizePresets.MidOptInterlace
                Case GetPresetOxipngArguments(OxiPngOptimizePresets.MaxOpt)
                    Return OxiPngOptimizePresets.MaxOpt
                Case GetPresetOxipngArguments(OxiPngOptimizePresets.MaxOptInterlace)
                    Return OxiPngOptimizePresets.MaxOptInterlace
                Case Else
                    Return OxiPngOptimizePresets.Custom
            End Select
        End Function

        Public Shared Function GetPresetOxipngArguments(preset As OxiPngOptimizePresets) As String
            Select Case preset
                Case OxiPngOptimizePresets.MidOpt
                    Return "--opt 4 --preserve -vv"
                Case OxiPngOptimizePresets.MidOptInterlace
                    Return "--opt 4 --preserve --interlace on --force -vv"
                Case OxiPngOptimizePresets.MaxOpt
                    Return "--opt max --zopfli --preserve -vv"
                Case OxiPngOptimizePresets.MaxOptInterlace
                    Return "--opt max --zopfli --interlace on --force --preserve -vv"
                Case Else
                    Throw New InvalidEnumArgumentException("preset", preset, preset.GetType())
            End Select
        End Function

        Public Sub LoadOxipngArgumentPreset(preset As OxiPngOptimizePresets)
            Me.OxipngArguments = GetPresetOxipngArguments(preset)
        End Sub

        Public Function TryLocateOxipngPath() As Boolean
            Dim files() As String = System.IO.Directory.GetFiles(
                 path:=Application.StartupPath(),
        searchPattern:="oxipng.exe",
         searchOption:=System.IO.SearchOption.AllDirectories)

            Select Case files.Length
                Case 0
                    Return Me.TryLocateOxipngPathWithFindExecutable()
                Case Else
                    Me.OxipngPath = files.First
                    Return True
            End Select
        End Function

        Private Function TryLocateOxipngPathWithFindExecutable() As Boolean
            Dim RetPath = Bdiu.BdiuHelper.FindExecutable("oxipng.exe", Application.StartupPath())

            If String.IsNullOrWhiteSpace(RetPath) Then
                Return False
            End If

            Me.OxipngPath = RetPath
            Return True
        End Function

        ''' <exception cref="System.IO.FileNotFoundException"/>
        Public Sub LocateOxipngPathIfNotSet()
            If String.IsNullOrWhiteSpace(Me.OxipngPath) Then
                Call Me.TryLocateOxipngPath()
            End If
            If String.IsNullOrWhiteSpace(Me.OxipngPath) Then
                Throw New System.IO.FileNotFoundException("Location of oxipng.exe not found")
            End If
        End Sub

        ' This event triggers whenever oxipng outputs text (stdout)
        Private Sub Process_OutputDataReceived(sender As Object, e As DataReceivedEventArgs)
            If e.Data Like "{""results"":*" Then
                If jsonOutEnabledByGivenArgs Then
                    ' Also append JSON to log, if this argument was explicitly set
                    RaiseEvent ProcessDataReceived(sender, e.Data, False)
                End If
                Me.JsonOut = e.Data
            Else
                ' Process events run on background threads; marshal back to UI
                RaiseEvent ProcessDataReceived(sender, e.Data, False)
            End If
        End Sub

        ' This event triggers whenever oxipng outputs text (stderr)
        Private Sub Process_ErrorDataReceived(sender As Object, e As DataReceivedEventArgs)
            ' OxiPng outputs most of its regular output as stderr
            ' Don't actually mark it as error data
            RaiseEvent ProcessDataReceived(sender, e.Data, False)
        End Sub

        Public Sub DiscardReturnData()
            Me.JsonOut = Nothing
            Me.ProcessExitCode = Nothing
        End Sub

        Private Sub KillProcessTree()
            Try
                ' Ensure the process is active and hasn't already exited
                If _currentProcess IsNot Nothing AndAlso Not _currentProcess.HasExited Then
                    ' .Kill(True) kills the process and all of its child processes (.NET Core / .NET 5+)
                    _currentProcess.Kill(True)
                End If
            Catch ex As Exception
                ' Ignore errors if the process managed to close right as we killed it
            End Try
        End Sub

        Public Async Function OptimizePngFiles(filePaths As String(), ct As CancellationToken) As Task
            Call LocateOxipngPathIfNotSet()
            Dim exitProcess As Boolean
            Dim oxPngArgs As String = Me.OxipngArguments
            Call DiscardReturnData()

            Do
                Dim psi = Me.CreateProcessStartInfoOptimizePngFiles(filePaths, oxPngArgs)
                _currentProcess = New Process With {
                    .StartInfo = psi
                }

                ' Attach event handlers to intercept streams asynchronously
                AddHandler _currentProcess.OutputDataReceived, AddressOf Process_OutputDataReceived
                AddHandler _currentProcess.ErrorDataReceived, AddressOf Process_ErrorDataReceived

                ' Register a callback that kills the process immediately if the token is canceled
                Using ct.Register(Sub() KillProcessTree())
                    Try
                        ' Launch the process
                        _currentProcess.Start()
                        _currentProcess.BeginOutputReadLine()
                        _currentProcess.BeginErrorReadLine()

                        ' Wait for the process to exit or for cancellation to throw an exception
                        Await _currentProcess.WaitForExitAsync(ct)

                        Me.ProcessExitCode = _currentProcess.ExitCode

                        Select Case Me.ProcessExitCode
                            Case 0
                                ' Success
                                exitProcess = True
                            Case &HC000001D ' Illegal instruction error
                                RaiseEvent ProcessDataReceived(Me, "ERROR:", True)
                                RaiseEvent ProcessDataReceived(Me, $"Returned exit code: 0x{Me.ProcessExitCode.Value:X8} (Illegal Instruction)", True)
                                RaiseEvent ProcessDataReceived(Me, Nothing, False)
                                If filePaths.Length < 2 Then
                                    exitProcess = True
                                Else
                                    RaiseEvent ProcessDataReceived(Me, "Oxipng likely crashed because too many large images were being processed simultaneously.", True)

                                    ' If this attempt didn't use the sequential argument, try again with that one
                                    If oxPngArgs.Contains("--sequential") Then
                                        exitProcess = True
                                    Else
                                        RaiseEvent ProcessDataReceived(Me, "Trying again with added parameter --sequential to process each image individually.", False)
                                        oxPngArgs &= " --sequential"
                                        exitProcess = False
                                    End If
                                End If
                            Case Else
                                ' Other Error
                                exitProcess = True
                        End Select

                    Catch ex As OperationCanceledException
                        ' Re-throw to signal the Form that cancellation was successful
                        Throw
                    Catch ex As Exception
                        Throw New Exception($"Execution Error: {ex.Message}", ex)
                    Finally
                        ' Clean up resources and detach the handlers using RemoveHandler
                        If _currentProcess IsNot Nothing Then
                            RemoveHandler _currentProcess.OutputDataReceived, AddressOf Process_OutputDataReceived
                            RemoveHandler _currentProcess.ErrorDataReceived, AddressOf Process_ErrorDataReceived

                            _currentProcess.Dispose()
                            _currentProcess = Nothing
                        End If
                    End Try
                End Using
            Loop Until exitProcess
        End Function

        ''' <exception cref="System.IO.FileNotFoundException"/>
        Public Function CreateProcessStartInfoOptimizePngFiles(filePaths As String(), ByVal oxipngArguments As String) As System.Diagnostics.ProcessStartInfo
            Call LocateOxipngPathIfNotSet()
            Return CreateProcessStartInfoOptimizePngFiles(
                exePath:=Me.OxipngPath,
                filePaths:=filePaths,
                oxipngArguments:=oxipngArguments,
                outJsonOutEnabledByGivenArgs:=jsonOutEnabledByGivenArgs)
        End Function

        ''' <exception cref="System.IO.FileNotFoundException"/>
        Public Function CreateProcessStartInfoOptimizePngFiles(filePaths As String()) As System.Diagnostics.ProcessStartInfo
            Call LocateOxipngPathIfNotSet()
            Return CreateProcessStartInfoOptimizePngFiles(
                exePath:=Me.OxipngPath,
                filePaths:=filePaths,
                oxipngArguments:=Me.OxipngArguments,
                outJsonOutEnabledByGivenArgs:=jsonOutEnabledByGivenArgs)
        End Function

        Public Shared Function CreateProcessStartInfoOptimizePngFiles(
            ByVal exePath As String,
            ByVal filePaths As String(),
            ByVal oxipngArguments As String) As System.Diagnostics.ProcessStartInfo
            Dim jsonOutEnabledByGivenArgs As Boolean

            Return CreateProcessStartInfoOptimizePngFiles(
                exePath:=exePath,
                filePaths:=filePaths,
                oxipngArguments:=oxipngArguments,
                outJsonOutEnabledByGivenArgs:=jsonOutEnabledByGivenArgs)
        End Function

        Public Shared Function CreateProcessStartInfoOptimizePngFiles(
            ByVal exePath As String,
            ByVal filePaths As String(),
            ByVal oxipngArguments As String,
            ByRef outJsonOutEnabledByGivenArgs As Boolean) As System.Diagnostics.ProcessStartInfo

            ' Always force JSON output
            ' This way we can verify if the process went through successfully
            ' Additionally return boolean if the JSON output was already enabled by the given arguments
            If oxipngArguments.Contains("-j") OrElse oxipngArguments.Contains("--json") Then
                outJsonOutEnabledByGivenArgs = True
            Else
                outJsonOutEnabledByGivenArgs = False
                oxipngArguments &= " --json"
            End If

            ' Wrap each file path in quotes to handle paths with spaces safely
            For Each filePath In filePaths
                oxipngArguments &= $" ""{filePath}"""
            Next

            ' Configure the process start settings
            Return New ProcessStartInfo() With {
                .FileName = exePath,
                .Arguments = oxipngArguments,
                .UseShellExecute = False,
                .RedirectStandardOutput = True,
                .RedirectStandardError = True,
                .CreateNoWindow = True
            }
        End Function
    End Class
End Namespace