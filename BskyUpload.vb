Option Compare Binary
Option Explicit On

#Const UseDefaultFacetExtractor = True

Namespace Bdiu
    Public Class BskyUploadPayload
        Public Property BskyHandle As String
        Public Property BskyPasswordEncrypted As Byte()
        Public Property BskyPost As BskyUploadPostData

        Public Event ProcessDataReceived(sender As Object, data As String, isErrorData As Boolean)

        Public Async Function PostToBsky(ct As System.Threading.CancellationToken) As Task

            Dim agent As New idunno.Bluesky.BlueskyAgent

            ' Login
            Dim loginResult = Await agent.Login(
                        identifier:=Me.BskyHandle,
                        password:=System.Text.Encoding.UTF8.GetString(System.Security.Cryptography.ProtectedData.Unprotect(Me.BskyPasswordEncrypted, Nothing, System.Security.Cryptography.DataProtectionScope.CurrentUser)),
                        cancellationToken:=ct)
            If Not loginResult.Succeeded Then
                Throw New BlueskyLoginException($"Bluesky login failed. Please check credentials. {loginResult.AtErrorDetail.Error}: {loginResult.AtErrorDetail.Message}", loginResult.StatusCode)
            End If

            ct.ThrowIfCancellationRequested()

            Dim buildPost As New idunno.Bluesky.PostBuilder(text:="DummyText", langs:=Me.BskyPost.Languages, createdAt:=Me.BskyPost.CreatedAt)

            If Not String.IsNullOrWhiteSpace(Me.BskyPost.ReplyUrl) Then
                RaiseEvent ProcessDataReceived(Me, "Resolving thread metadata for a native reply chain...", False)

                Dim strongRef = Await GetStrongRefFromUrl(agent, Me.BskyPost.ReplyUrl, ct)

                If Me.BskyPost.ReplyIsQuote Then
                    Call buildPost.Quote(strongRef)
                Else
                    Await buildPost.ReplyTo(strongRef, agent, ct)
                End If
            End If

            ' Upload all images
            Dim uploadedImagesList As List(Of idunno.Bluesky.Embed.EmbeddedImage) = Nothing

            If Me.BskyPost.Images IsNot Nothing Then

                uploadedImagesList = New List(Of idunno.Bluesky.Embed.EmbeddedImage)

                For Each image In Me.BskyPost.Images

                    RaiseEvent ProcessDataReceived(Me, $"Uploading raw bytes for: {image.FileName}", False)

                    ' Upload image as blob
                    Dim uploadResult = Await agent.UploadBlob(image.FilePath, image.MimeType, cancellationToken:=ct)

                    If Not uploadResult.Succeeded Then
                        Throw New BlueskyImageUploadException($"Upload of ""{image.FileName}"" failed. {uploadResult.AtErrorDetail.Error}: {uploadResult.AtErrorDetail.Message}", uploadResult.StatusCode)
                    End If

                    ' Create embed image object (Besides the uploaded image, stores the AltText and Dimensions)
                    Dim imageEmbed As idunno.Bluesky.Embed.EmbeddedImage
                    Dim altText As String = If(image.AltText, String.Empty)

                    If image.Dimensions IsNot Nothing AndAlso image.Dimensions.Width.HasValue AndAlso image.Dimensions.Height.HasValue Then
                        Dim ratio As New idunno.Bluesky.Embed.AspectRatio(CInt(image.Dimensions.Width.Value), CInt(image.Dimensions.Height.Value))
                        imageEmbed = New idunno.Bluesky.Embed.EmbeddedImage(uploadResult.Result, altText, ratio)
                    Else
                        imageEmbed = New idunno.Bluesky.Embed.EmbeddedImage(uploadResult.Result, altText)
                    End If
                    Call uploadedImagesList.Add(imageEmbed)
                Next
            End If

            RaiseEvent ProcessDataReceived(Me, "Embedding images", False)
            ct.ThrowIfCancellationRequested()

            ' The library usually validates if the 10 image limit is exceeded and throws an error
            ' Bluesky itself technically allows 20 however
            ' This circumvents the limit validation with an inherited class
            Dim imageGalleryEmbeds As EmbeddedGalleryEdit = Nothing
            If uploadedImagesList IsNot Nothing AndAlso uploadedImagesList.Count > 0 Then

                ' Create the EmbeddedGallery instance and add the first image
                ' This is the only image that will be validated before sending the data to Bluesky
                Dim firstGalleryImageLst As New List(Of idunno.Bluesky.Embed.Gallery.GalleryImage)
                Call firstGalleryImageLst.Add(New idunno.Bluesky.Embed.Gallery.GalleryImage(uploadedImagesList(0)))
                imageGalleryEmbeds = New EmbeddedGalleryEdit(firstGalleryImageLst)

                ' Add all other images without validation
                For i As Byte = 1 To CByte(uploadedImagesList.Count - 1)
                    Dim imageGalleryEmbed As New idunno.Bluesky.Embed.Gallery.GalleryImage(uploadedImagesList(i))

                    Call imageGalleryEmbeds.AddWithoutValidate(imageGalleryEmbed)
                Next
            End If

            ct.ThrowIfCancellationRequested()

            ' Add the embedded images to the post builder
            If uploadedImagesList IsNot Nothing AndAlso uploadedImagesList.Count > 0 Then
                Call buildPost.EmbedRecord(imageGalleryEmbeds)
            End If

            ' Set the actual post text
            If Me.BskyPost.Text IsNot Nothing AndAlso Me.BskyPost.Text.Length <> 0 Then
                Call buildPost.WithText(Me.BskyPost.Text)

#If UseDefaultFacetExtractor Then
                ' Pass your agent's handle resolution routine directly into the constructor
                Dim extractor As idunno.Bluesky.RichText.IFacetExtractor = New idunno.Bluesky.RichText.DefaultFacetExtractor(
                    Async Function(handle, cancellationToken)
                        Dim resolveResult = Await agent.ResolveHandle(handle, cancellationToken)
                        Return resolveResult.Value
                    End Function
                )
#Else
                Dim extractor = New LocalFacetExtractor(agent)
#End If
                Await buildPost.ExtractFacets(extractor, ct)

#If UseDefaultFacetExtractor Then
                ' Strip off the inner metadata hash marks
                Call RemoveHashSymbolFromTagFacets(buildPost.Facets)
#End If
            Else
                ' Write Dummy text data to avoid validation errors
                ' Will be later set to an empty string and will therefore not show up in the actual post
                Call buildPost.WithText("DummyText")
            End If

            Dim finalPost As New idunno.Bluesky.Post(buildPost)

            ' Apply labels
            ' Only add them to the finalPost object instead of buildPost.
            ' Otherwise it would fail when creating the finalPost object out of the buildPost.
            ' The validator checks if there are images or videos are present and fails if not.
            ' However it fails to take gallery images into account.
            ' Putting this here skips the validation.
            If Me.BskyPost.Labels IsNot Nothing AndAlso Me.BskyPost.Labels.Count > 0 Then
                RaiseEvent ProcessDataReceived(Me, $"Applying Content Warning Labels: {String.Join(" & ", Me.BskyPost.Labels.ToArray)}", False)

                Dim selfLbls As New idunno.AtProto.Labels.SelfLabels
                For Each label In Me.BskyPost.Labels
                    Dim selfLbl As New idunno.AtProto.Labels.SelfLabel(label)
                    Call selfLbls.AddLabel(selfLbl)
                Next
                ' Set Labels via reflection
                ' Otherwise custom labels would be removed
                Try
                    Dim fields As System.Reflection.FieldInfo() = finalPost.GetType().GetFields(
            System.Reflection.BindingFlags.Instance Or
            System.Reflection.BindingFlags.NonPublic Or
            System.Reflection.BindingFlags.Public
        )
                    For Each f In fields
                        If f.Name.Equals("labels", StringComparison.OrdinalIgnoreCase) OrElse f.Name.Contains("<Labels>", StringComparison.OrdinalIgnoreCase) Then
                            f.SetValue(finalPost, selfLbls)
                            Exit For
                        End If
                    Next
                Catch ex As Exception
                    ' Fallback strategy in case field metadata names differ across SDK versions
                    Dim backupField = finalPost.GetType().GetField("<Labels>k__BackingField", System.Reflection.BindingFlags.Instance Or System.Reflection.BindingFlags.NonPublic)
                    backupField?.SetValue(finalPost, selfLbls)
                End Try
            End If

            ' --- REFLECTION FIX FOR PURE IMAGE POSTS ---
            ' If the text is empty, we force the underlying private field to contain 
            ' a literal empty string "". This bypasses the SDK's validation exceptions 
            ' and ensures the server receives the required "text" key without rendering a blank line.
            If Me.BskyPost.Text Is Nothing OrElse Me.BskyPost.Text.Length = 0 Then
                Try
                    ' Retrieve all instance fields (public, private, and internal) from the Post object
                    Dim fields As System.Reflection.FieldInfo() = finalPost.GetType().GetFields(
            System.Reflection.BindingFlags.Instance Or
            System.Reflection.BindingFlags.NonPublic Or
            System.Reflection.BindingFlags.Public
        )

                    ' Loop through fields to find either the direct "text" field or the compiler-generated backing field
                    For Each f In fields
                        If f.Name.Equals("text", StringComparison.OrdinalIgnoreCase) OrElse f.Name.Contains("<Text>", StringComparison.OrdinalIgnoreCase) Then
                            ' Directly write the empty string into memory, bypassing property setters and constructor guards
                            f.SetValue(finalPost, "")
                            Exit For
                        End If
                    Next
                Catch ex As Exception
                    ' Fallback strategy in case field metadata names differ across SDK versions
                    Dim backupField = finalPost.GetType().GetField("<Text>k__BackingField", System.Reflection.BindingFlags.Instance Or System.Reflection.BindingFlags.NonPublic)
                    backupField?.SetValue(finalPost, "")
                End Try
            End If

            RaiseEvent ProcessDataReceived(Me, $"Sending the post data to Bluesky", False)
            ct.ThrowIfCancellationRequested()

            ' Actually create the Bluesky Post
            Dim postResult = Await agent.Post(finalPost, cancellationToken:=ct, extractFacets:=False)
            If Not postResult.Succeeded Then
                Throw New BlueskyPostCreateException($"Creating Bluesky post failed. {postResult.AtErrorDetail.Error}: {postResult.AtErrorDetail.Message}", postResult.StatusCode)
            End If

        End Function


#If UseDefaultFacetExtractor Then
        Public Shared Sub RemoveHashSymbolFromTagFacets(facets As IReadOnlyCollection(Of idunno.Bluesky.RichText.Facet))
            If facets Is Nothing Then
                Return
            End If
            ' Loop through And strip off the inner metadata hash marks
            For Each facet In facets
                For Each feature In facet.Features

                    ' Targets only the Tag features while leaving @mentions and links as is
                    If TypeOf feature Is idunno.Bluesky.RichText.TagFacetFeature Then
                        Dim tagFeature = DirectCast(feature, idunno.Bluesky.RichText.TagFacetFeature)

                        ' Clean up the lookup value without breaking byte character ranges
                        If tagFeature.Tag IsNot Nothing AndAlso tagFeature.Tag.StartsWith("#"c) Then

                            ' Trim leading hash symbols that were kept in by the DefaultFacetExtractor
                            Dim tagTrimmed = tagFeature.Tag.TrimStart("#"c)

                            Call UpdateTagFacetFeatureValue(tagFeature, tagTrimmed)
                        End If
                    End If
                Next
            Next
        End Sub
        Public Shared Sub UpdateTagFacetFeatureValue(tagFeature As idunno.Bluesky.RichText.TagFacetFeature, tagValue As String)
            ' Set updated tag data through Reflection
            Try
                Dim fields As System.Reflection.FieldInfo() = tagFeature.GetType().GetFields(
        System.Reflection.BindingFlags.Instance Or
        System.Reflection.BindingFlags.NonPublic Or
        System.Reflection.BindingFlags.Public
    )
                For Each f In fields
                    If f.Name.Equals("tag", StringComparison.OrdinalIgnoreCase) OrElse f.Name.Contains("<Tag>", StringComparison.OrdinalIgnoreCase) Then
                        f.SetValue(tagFeature, tagValue)
                        Exit For
                    End If
                Next
            Catch ex As Exception
                ' Fallback strategy in case field metadata names differ across SDK versions
                Dim backupField = tagFeature.GetType().GetField("<Tag>k__BackingField", System.Reflection.BindingFlags.Instance Or System.Reflection.BindingFlags.NonPublic)
                backupField?.SetValue(tagFeature, tagValue)
            End Try
        End Sub
#Else
        Public Class LocalFacetExtractor
            Implements IFacetExtractor

            Public Property Agent As BlueskyAgent

            Sub New(agent As BlueskyAgent)
                Me.Agent = agent
            End Sub

            Public Function ExtractFacets(text As String, Optional cancellationToken As CancellationToken = Nothing) As Task(Of IList(Of Facet)) Implements IFacetExtractor.ExtractFacets
                ' REGEX PATTERNS FOR ALL RICH TEXT COMPONENTS
                Dim mentionRegex As New Regex("(?<=\s|^)@([a-zA-Z0-9.-]+)", RegexOptions.Compiled)
                Dim urlRegex As New Regex("(?<=\s|^)https?://[^\s]+", RegexOptions.Compiled)
                Dim tagRegex As New Regex("(?<=\s|^)#\w+", RegexOptions.Compiled)

                Dim facetsList As New List(Of Facet)()

                ' --- A. PROCESS MENTIONS (@username) ---
                For Each match As Match In mentionRegex.Matches(text)
                    Dim byteStart As Integer = Encoding.UTF8.GetByteCount(text.AsSpan(0, match.Index))
                    Dim byteEnd As Integer = byteStart + Encoding.UTF8.GetByteCount(match.Value)

                    Dim handleValue As String = match.Groups(1).Value

                    Try
                        ' RESOLUTION FIX: Call the agent to resolve the text handle to a valid DID over the network
                        ' (Note: The response type might expose a .Value or directly cast to the Did object wrapper)
                        Dim resolveResult = Agent.ResolveHandle(handleValue, cancellationToken)

                        Call resolveResult.Wait(cancellationToken)

                        ' Pass the resolved Did type directly into the feature constructor
                        Dim mentionFeature As New MentionFacetFeature(resolveResult.Result)

                        Call facetsList.Add(New Facet(New ByteSlice(byteStart, byteEnd), New List(Of FacetFeature) From {mentionFeature}))

                    Catch ex As Exception
                        ' Ignore invalid handles
                    End Try
                Next

                ' --- B. PROCESS LINKS (https://...) ---
                For Each match As Match In urlRegex.Matches(text)
                    Dim byteStart As Integer = Encoding.UTF8.GetByteCount(text.AsSpan(0, match.Index))
                    Dim byteEnd As Integer = byteStart + Encoding.UTF8.GetByteCount(match.Value)

                    Dim urlValue As String = match.Value

                    ' FIX: Convert the string URL into a proper System.Uri object
                    Dim uriObject As New Uri(urlValue)
                    Dim linkFeature As New LinkFacetFeature(uriObject)

                    Call facetsList.Add(New Facet(New ByteSlice(byteStart, byteEnd), New List(Of FacetFeature) From {linkFeature}))
                Next

                ' --- C. PROCESS HASHTAGS (#tag) ---
                For Each match As Match In tagRegex.Matches(text)
                    Dim byteStart As Integer = Encoding.UTF8.GetByteCount(text.AsSpan(0, match.Index))
                    Dim byteEnd As Integer = byteStart + Encoding.UTF8.GetByteCount(match.Value)

                    ' CRITICAL STEP: Strip the hash prefix so the hidden indexing tag is purely alphanumeric ("dotnet")
                    Dim cleanTagMetadata As String = match.Value.TrimStart("#"c)
                    Dim tagFeature As New TagFacetFeature(cleanTagMetadata)

                    Call facetsList.Add(New Facet(New ByteSlice(byteStart, byteEnd), New List(Of FacetFeature) From {tagFeature}))
                Next

                Return Task.FromResult(DirectCast(facetsList, IList(Of Facet)))
            End Function
        End Class
#End If

        Public Shared Async Function GetStrongRefFromUrl(agent As idunno.Bluesky.BlueskyAgent, postUrl As String, ct As System.Threading.CancellationToken) As Task(Of idunno.AtProto.Repo.StrongReference)
            ' 1. Parse the web URL to extract the handle/DID and the record key (rkey)
            ' Format: https://bsky.app/profile/{actor}/post/{rkey}
            Dim uri As New Uri(postUrl)

            ' Using RemoveEmptyEntries strips the leading empty element, ensuring predictable indices
            Dim segments As String() = uri.AbsolutePath.Split("/"c, StringSplitOptions.RemoveEmptyEntries)

            ' Index 0 = "profile"
            ' Index 1 = {actor} (e.g., "username.bsky.social")
            ' Index 2 = "post"
            ' Index 3 = {rkey} (e.g., "3k7qmjev5lr2s")
            If segments.Length < 4 OrElse segments(0) <> "profile" OrElse segments(2) <> "post" Then
                Dim ex = New ArgumentException("Invalid Bluesky URL format. Make sure it contains /profile/.../post/...")
                Throw New BlueskyException(ex.Message, ex)
            End If

            Dim actor As String = segments(1) ' Example: "username.bsky.social"
            Dim rkey As String = segments(3)  ' Example: "3k7qmjev5lr2s"

            ' 2. Construct the unvalidated target AT-URI string 
            Dim atUri As String = $"at://{actor}/app.bsky.feed.post/{rkey}"

            ' 3. Call the API to get the fully hydrated Post View (which contains the verified CID)
            ' We use GetPostThread with a depth of 0 to fetch only this target post efficiently
            Dim threadResponse = Await agent.GetPostThread(atUri, depth:=0, cancellationToken:=ct)


            If threadResponse IsNot Nothing AndAlso threadResponse.Succeeded Then
                ' Get the base thread object from the response
                Dim baseThread = threadResponse.Result.Thread

                ' 4. Verify if the thread node is a regular, visible post (ThreadViewPost)
                If TypeOf baseThread Is idunno.Bluesky.Feed.ThreadViewPost Then
                    ' Safely cast to ThreadViewPost to unlock the .Post property

                    Dim viewPost As idunno.Bluesky.Feed.ThreadViewPost = DirectCast(baseThread, idunno.Bluesky.Feed.ThreadViewPost)

                    Dim targetPost As idunno.Bluesky.Feed.PostView = viewPost.Post

                    ' Return the finalized StrongReference containing the verified Uri and Cid
                    Return New idunno.AtProto.Repo.StrongReference(targetPost.Uri, targetPost.Cid)

                Else
                    If TypeOf baseThread Is idunno.Bluesky.Feed.NotFoundPost Then
                        Throw New BlueskyException("Post to reply to could not be loaded (post is not found).")
                    ElseIf TypeOf baseThread Is idunno.Bluesky.Feed.BlockedPost Then
                        Throw New BlueskyException("Post to reply to could not be loaded (post is blocked).")
                    Else
                        Throw New BlueskyException("Post to reply to could not be loaded.")
                    End If
                End If
            Else
                Throw New BlueskyException($"Post to reply to not found. {threadResponse.AtErrorDetail.Error}: {threadResponse.AtErrorDetail.Message}", threadResponse.StatusCode)
            End If
            Return Nothing
        End Function

        Public Class EmbeddedGalleryEdit
                Inherits idunno.Bluesky.Embed.EmbeddedGallery

            Public Sub New(original As idunno.Bluesky.Embed.EmbeddedGallery)
                MyBase.New(original)
            End Sub

            Public Sub New(items As ICollection(Of idunno.Bluesky.Embed.EmbeddedImage))
                MyBase.New(items)
            End Sub

            Public Sub New(items As ICollection(Of idunno.Bluesky.Embed.Gallery.GalleryImage))
                MyBase.New(items)
            End Sub

            ''' <summary>
            ''' Allows adding gallery images without validation
            ''' </summary>
            ''' <param name="item"></param>
            Public Sub AddWithoutValidate(item As idunno.Bluesky.Embed.Gallery.GalleryImage)
                Me.Items.Add(item)
            End Sub
        End Class

            Public Class BlueskyException
                Inherits Exception

            Public Property StatusCode As System.Net.HttpStatusCode

            Public Sub New(message As String)
                    MyBase.New(message)
                End Sub

            Public Sub New(message As String, statusCode As System.Net.HttpStatusCode)
                MyBase.New($"{message} (HTTP Status: {statusCode})")
                Me.StatusCode = statusCode
            End Sub

            Public Sub New(message As String, innerException As Exception)
                    MyBase.New(message, innerException)
                End Sub
            End Class

            Public Class BlueskyPostCreateException
                Inherits BlueskyException

                Public Sub New(message As String)
                    MyBase.New(message)
                End Sub

            Public Sub New(message As String, statusCode As System.Net.HttpStatusCode)
                MyBase.New($"{message} (HTTP Status: {statusCode})")
                Me.StatusCode = statusCode
            End Sub

            Public Sub New(message As String, innerException As Exception)
                    MyBase.New(message, innerException)
                End Sub
            End Class

            Public Class BlueskyImageUploadException
                Inherits BlueskyException

                Public Sub New(message As String)
                    MyBase.New(message)
                End Sub

            Public Sub New(message As String, statusCode As System.Net.HttpStatusCode)
                MyBase.New($"{message} (HTTP Status: {statusCode})")
                Me.StatusCode = statusCode
            End Sub

            Public Sub New(message As String, innerException As Exception)
                    MyBase.New(message, innerException)
                End Sub
            End Class

            Public Class BlueskyLoginException
                Inherits BlueskyException

                Public Sub New(message As String)
                    MyBase.New(message)
                End Sub

            Public Sub New(message As String, statusCode As System.Net.HttpStatusCode)
                MyBase.New($"{message} (HTTP Status: {statusCode})")
                Me.StatusCode = statusCode
            End Sub

            Public Sub New(message As String, innerException As Exception)
                MyBase.New(message, innerException)
            End Sub
        End Class

        Public Sub New(bskyHandle As String, bskyPasswordEncrypted As Byte(), bskyPost As BskyUploadPostData)
            Me.BskyHandle = bskyHandle
            Me.BskyPasswordEncrypted = bskyPasswordEncrypted
            Me.BskyPost = bskyPost
        End Sub

    End Class

    Public Class BskyUploadPostData
        Public Property Text As String
        Public Property ReplyUrl As String
        Public Property ReplyIsQuote As Boolean = False
        Public Property Labels As HashSet(Of String)
        Public Property Images As List(Of BskyUploadImageData)
        Public Property Languages As HashSet(Of String)
        Public Property CreatedAt As Nullable(Of DateTimeOffset)
        Public Function GetMaxModifiedAttachmentDate() As Nullable(Of DateTimeOffset)
            If Me.Images Is Nothing Then
                Return Nothing
            End If
            Dim maxModDate As Nullable(Of DateTimeOffset) = Nothing
            For Each attach In Me.Images
                If (maxModDate Is Nothing AndAlso attach.DateTimeModified.HasValue) _
             OrElse attach.DateTimeModified.Value.CompareTo(maxModDate.Value) > 0 Then
                    ' Current image has a larger modified date than the previous
                    maxModDate = attach.DateTimeModified
                End If
            Next
            Return maxModDate
        End Function
        Public Function GetMaxCreatedAttachmentDate() As Nullable(Of DateTimeOffset)
            If Me.Images Is Nothing Then
                Return Nothing
            End If
            Dim maxCrtDate As Nullable(Of DateTimeOffset) = Nothing
            For Each attach In Me.Images
                If (maxCrtDate Is Nothing AndAlso attach.DateTimeCreated.HasValue) _
             OrElse attach.DateTimeCreated.Value.CompareTo(maxCrtDate.Value) > 0 Then
                    ' Current image has a larger modified date than the previous
                    maxCrtDate = attach.DateTimeCreated
                End If
            Next
            Return maxCrtDate
        End Function
        Public Sub AddImage(image As BskyUploadImageData)
            If Me.Images Is Nothing Then Me.Images = New List(Of Bdiu.BskyUploadPostData.BskyUploadImageData)
            Call Me.Images.Add(image)
        End Sub
        Public Function AddLabel(label As String) As Boolean
            If Me.Labels Is Nothing Then Me.Labels = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            Return Me.Labels.Add(label)
        End Function
        Public Function RemoveLabel(label As String) As Boolean
            If Me.Labels Is Nothing Then Return False
            Return Me.Labels.Remove(label)
        End Function
        Public Sub ClearLabelsAndUnionWith(lines() As String)
            If Me.Labels Is Nothing Then
                Me.Labels = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            Else
                Call Me.Labels.Clear()
            End If
            Call Me.Labels.UnionWith(lines)
        End Sub
        Public Function AddLanguage(bcp47Language As String) As Boolean
            If Me.Languages Is Nothing Then Me.Languages = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            Return Me.Languages.Add(bcp47Language)
        End Function
        Public Sub ClearLanguages()
            If Me.Languages IsNot Nothing Then
                Call Me.Languages.Clear()
            End If
        End Sub
        Public Sub ClearLanguagesAndUnionWith(bcp47Languages() As String)
            If Me.Languages Is Nothing Then
                Me.Languages = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            Else
                Call Me.Languages.Clear()
            End If
            Call Me.Languages.UnionWith(bcp47Languages)
        End Sub
        Public Class BskyUploadImageData
            Public Property FilePath As String
            Public Property AltText As String
            Public Property Dimensions As ImageDimensions
            Public Property MimeType As String
            Public Property DateTimeModified As Nullable(Of DateTimeOffset)
            Public Property DateTimeCreated As Nullable(Of DateTimeOffset)

            Public ReadOnly Property FileName As String
                Get
                    Return System.IO.Path.GetFileName(Me.FilePath)
                End Get
            End Property
            Public Sub New(filePath As String)
                Me.FilePath = filePath
            End Sub

            Public Sub LoadImageData()
                Dim info As New ImageMagick.MagickImageInfo(Me.FilePath)

                If Me.Dimensions Is Nothing Then
                    Me.Dimensions = New ImageDimensions(width:=info.Width, height:=info.Height)
                Else
                    Me.Dimensions.Width = CType(info.Width, System.UInt32)
                    Me.Dimensions.Height = CType(info.Height, System.UInt32)
                End If
                Dim format As ImageMagick.MagickFormat = info.Format

                Dim formatInfo = ImageMagick.MagickFormatInfo.Create(info.Format)

                Me.MimeType = formatInfo.MimeType

                Dim fileInfo As New System.IO.FileInfo(Me.FilePath)

                Me.DateTimeModified = New DateTimeOffset(fileInfo.LastWriteTimeUtc)
                Me.DateTimeCreated = New DateTimeOffset(fileInfo.CreationTime)
            End Sub

            Public Class ImageDimensions
                Public Property Width As Nullable(Of System.UInt32)
                Public Property Height As Nullable(Of System.UInt32)

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
    Public Class LanguageItem
        Public ReadOnly Property Bcp47Tag As String
        Public ReadOnly Property DisplayName As String

        Public Sub New(tag As String, name As String)
            Me.Bcp47Tag = tag
            Me.DisplayName = name
        End Sub

        Public Shared Function GetLanguageList() As List(Of LanguageItem)
            ' 1. Fetch all cultures installed on the Windows machine
            Dim cultures() As System.Globalization.CultureInfo = System.Globalization.CultureInfo.GetCultures(System.Globalization.CultureTypes.AllCultures)
            Dim languageList As New List(Of LanguageItem)()
            Dim friendlyName As String
            Dim lngDescChange = System.Globalization.CultureInfo.CurrentUICulture.Name.Equals("en-GB", StringComparison.OrdinalIgnoreCase)

            For Each culture As System.Globalization.CultureInfo In cultures
                ' Filter out the empty InvariantCulture tag
                If Not String.IsNullOrEmpty(culture.Name) Then

                    ' Create a user-friendly display name, e.g., "English (United States) [en-US]"
                    friendlyName = $"{culture.EnglishName} [{culture.Name}]"

                    If lngDescChange Then
                        Select Case True
                            Case culture.Name.Equals("en-US", StringComparison.OrdinalIgnoreCase)
                                friendlyName = $"English (Simplified) [{culture.Name}]"
                            Case culture.Name.Equals("en-GB", StringComparison.OrdinalIgnoreCase)
                                friendlyName = $"English (Traditional) [{culture.Name}]"
#If False Then
                        Case culture.Name.Equals("en", StringComparison.OrdinalIgnoreCase)
                        Case culture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase)
                            Continue For
#End If
                        End Select

                        Call languageList.Add(New LanguageItem(culture.Name, friendlyName))
                    End If
                End If
            Next

            ' 2. Sort the languages alphabetically by their friendly display name
            Return languageList.OrderBy(Function(x) x.DisplayName).ToList()
        End Function

    End Class
End Namespace