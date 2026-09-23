Option Compare Binary
Option Explicit On
Imports System.IO
Imports System.Security.Cryptography
Imports System.Text
Imports SixLabors.ImageSharp

Namespace Bdiu
#Const DeletePayload = True
    Public Class BskyUploadPy

        Public Event ArgsUpdated()
        Public Event ProcessDataReceived(sender As Object, data As String, isErrorData As Boolean)

        ' Track the current process instance so it can be killed if canceled
        Private _currentProcess As Process
        Private _jsonPath As String

        Private _processExitCode As Nullable(Of Integer)
        Public Property ProcessExitCode As Nullable(Of Integer)
            Get
                Return _processExitCode
            End Get
            Private Set(value As Nullable(Of Integer))
                _processExitCode = value
            End Set
        End Property

        Public ReadOnly Property JsonPayloadName As String = "transfer_payload.json"
        Public ReadOnly Property PythonScriptName As String = "bluesky_uploader.py"


        ' This event triggers whenever process outputs text (stdout)
        Private Sub Process_OutputDataReceived(sender As Object, e As DataReceivedEventArgs)
#If DeletePayload Then
            If e IsNot Nothing _
              AndAlso String.Equals(e.Data, "Read payload", StringComparison.Ordinal) _
              AndAlso System.IO.File.Exists(_jsonPath) Then
                Call System.IO.File.Delete(_jsonPath)
            End If
#End If
            RaiseEvent ProcessDataReceived(sender, e.Data, False)
        End Sub

        ' This event triggers whenever process outputs text (stderr)
        Private Sub Process_ErrorDataReceived(sender As Object, e As DataReceivedEventArgs)
            RaiseEvent ProcessDataReceived(sender, e.Data, True)
        End Sub

        Public Sub DiscardReturnData()
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
        Public Async Function UploadToBsky(jsonPayload As Dictionary(Of String, Object), ct As System.Threading.CancellationToken) As Task
            Await UploadToBsky(System.Text.Json.JsonSerializer.Serialize(jsonPayload), ct)
        End Function

        Public Async Function UploadToBsky(jsonPayload As String, ct As System.Threading.CancellationToken) As Task
            Call DiscardReturnData()

            RaiseEvent ProcessDataReceived(Me, $"Sending payload to '{Me.PythonScriptName}'", False)

            ' Write the bridge payload file natively using System.Text.Json
            _jsonPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Me.JsonPayloadName)
            Call System.IO.File.WriteAllText(_jsonPath, jsonPayload, System.Text.Encoding.UTF8)

            Dim pythonScript As String = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Me.PythonScriptName)
            If Bdiu.BdiuHelper.IsDevelopmentVersion Then
                pythonScript = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(System.IO.Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)))), Me.PythonScriptName)
            Else
                pythonScript = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Me.PythonScriptName)
            End If

            Dim psi As New ProcessStartInfo() With {
                .FileName = "python",
                .Arguments = $"-u ""{pythonScript}"" ""{_jsonPath}""",
                .UseShellExecute = False,
                .CreateNoWindow = True,
                .RedirectStandardError = True,
                .RedirectStandardOutput = True
            }
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
                        Case Else
                            ' Other Error
                            RaiseEvent ProcessDataReceived(Me, $"Returned exit code: 0x{Me.ProcessExitCode.Value:X8}", True)
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

#If DeletePayload Then
                    If System.IO.File.Exists(_jsonPath) Then
                        Call System.IO.File.Delete(_jsonPath)
                    End If
#End If
                End Try
            End Using
        End Function
    End Class
    Public Class BskyUploadPayload
#Const UseNoneForEmptyReplyUrl = False

        Public Property BskyHandle As String
        Public Property BskyPasswordEncrypted As Byte()
        Public Property BskyPost As BskyUploadPostData

        Public ReadOnly Property PayloadData As Dictionary(Of String, Object)
            Get
                ' Build the unified payload dictionary for the JSON file
                Dim payload As New Dictionary(Of String, Object) From {
                    {"handle", Me.BskyHandle}, ' Decrypt credentials securely using Windows DPAPI (tied to current Windows User Account)
                    {"password", Encoding.UTF8.GetString(ProtectedData.Unprotect(Me.BskyPasswordEncrypted, Nothing, DataProtectionScope.CurrentUser))}
                }
                If Me.BskyPost IsNot Nothing Then
                    If Me.BskyPost.Text IsNot Nothing AndAlso Me.BskyPost.Text.Length <> 0 Then
                        Call payload.Add("text", Me.BskyPost.Text)
                    End If
                    If String.IsNullOrWhiteSpace(Me.BskyPost.ReplyUrl) Then
#If UseNoneForEmptyReplyUrl Then
                        Call payload.Add("reply_url", "NONE")
#End If
                    Else
                        Call payload.Add("reply_url", Me.BskyPost.ReplyUrl)
                    End If
                    If Me.BskyPost.Labels IsNot Nothing Then
                        Call payload.Add("labels", Me.BskyPost.Labels)
                    End If
                    If Me.BskyPost.Images IsNot Nothing Then
                        Call payload.Add("images", Me.BskyPost.ImagePayloadData)
                    End If
                End If
                Return payload
            End Get
        End Property

        Public Sub New(bskyHandle As String, bskyPasswordEncrypted As Byte(), bskyPost As BskyUploadPostData)
            Me.BskyHandle = bskyHandle
            Me.BskyPasswordEncrypted = bskyPasswordEncrypted
            Me.BskyPost = bskyPost
        End Sub

    End Class

    Public Class BskyUploadPostData
        Public Property Text As String
        Public Property ReplyUrl As String
        Public Property Labels As HashSet(Of String)
        Public Property Images As List(Of BskyUploadImageData)

        Public ReadOnly Property ImagePayloadData As List(Of Dictionary(Of String, Object))
            Get
                If Me.Images Is Nothing Then Return Nothing
                Dim payload As New List(Of Dictionary(Of String, Object))
                For Each image In Me.Images
                    Call payload.Add(image.PayloadData)
                Next
                Return payload
            End Get
        End Property

        Public Sub AddImage(image As BskyUploadImageData)
            If Me.Images Is Nothing Then Me.Images = New List(Of Bdiu.BskyUploadPostData.BskyUploadImageData)
            Call Me.Images.Add(image)
        End Sub
        Public Function AddLabel(label As String) As Boolean
            If Me.Labels Is Nothing Then Me.Labels = New HashSet(Of String)
            Return Me.Labels.Add(label)
        End Function
        Public Function RemoveLabel(label As String) As Boolean
            If Me.Labels Is Nothing Then Return False
            Return Me.Labels.Remove(label)
        End Function
        Public Sub ClearLabelsAndUnionWith(lines() As String)
            If Me.Labels Is Nothing Then Me.Labels = New HashSet(Of String)
            Call Me.Labels.UnionWith(lines)
        End Sub

        Public Class BskyUploadImageData
            Public Property FilePath As String
            Public Property AltText As String
            Public Property Dimensions As ImageDimensions

            Public ReadOnly Property PayloadData As Dictionary(Of String, Object)
                Get
                    Dim payload As New Dictionary(Of String, Object)

                    If String.IsNullOrEmpty(Me.FilePath) Then
                    Else
                        Call payload.Add("path", Me.FilePath)
                    End If
                    If String.IsNullOrEmpty(Me.AltText) Then
                    Else
                        Call payload.Add("alt", Me.AltText)
                    End If
                    If Me.Dimensions IsNot Nothing Then
                        Call payload.Add("dimensions", Me.Dimensions.PayloadData)
                    End If
                    Return payload
                End Get
            End Property

            Public Sub New(filePath As String)
                Me.FilePath = filePath
            End Sub

            Public Sub LoadImageDimensions()
                Me.Dimensions = GetImageDimensions(Me.FilePath)
            End Sub

            Public Shared Function GetImageDimensions(filePath As String) As ImageDimensions
                Using stream As FileStream = File.OpenRead(filePath)

                    Dim info As SixLabors.ImageSharp.ImageInfo = SixLabors.ImageSharp.Image.Identify(stream)

                    If info IsNot Nothing Then
                        Return New ImageDimensions(width:=info.Width, height:=info.Height)
                    End If
                End Using

                Return Nothing
            End Function

            Public Class ImageDimensions
                Public Property Width As Nullable(Of System.UInt32)
                Public Property Height As Nullable(Of System.UInt32)

                Public ReadOnly Property PayloadData As Dictionary(Of String, System.UInt32)
                    Get
                        Dim payload As New Dictionary(Of String, System.UInt32)
                        If Me.Width.HasValue Then
                            payload.Add("width", Me.Width.Value)
                        End If
                        If Me.Height.HasValue Then
                            payload.Add("height", Me.Height.Value)
                        End If
                        Return payload
                    End Get
                End Property

                Public Sub New(width As UInt32, height As UInt32)
                    Me.Height = height
                    Me.Width = width
                End Sub
                Public Sub New(width As Int32, height As Int32)
                    Me.Height = CType(height, System.UInt32)
                    Me.Width = CType(width, System.UInt32)
                End Sub
                Public Sub New()
                End Sub
            End Class
        End Class
    End Class
End Namespace