Option Compare Binary
Option Explicit On

#Const UseDefaultFacetExtractor = True

Namespace Bdiu
    Public Class BskyUploadPayload
        Public Property BskyMgmt As BskySessionManager
        Public Property BskyPost As BskyUploadPostData

        Public Event ProcessDataReceived(sender As Object, data As String, isErrorData As Boolean)

        Public Async Function PostToBsky(ct As System.Threading.CancellationToken) As Task

            Call Me.BskyMgmt.ResumeSessionOrLogInIfNotLoggedIn(ct)
            Dim agent = Me.BskyMgmt.BskyAgent

            ct.ThrowIfCancellationRequested()

            Dim buildPost As New idunno.Bluesky.PostBuilder(text:="DummyText", langs:=Me.BskyPost.Languages, createdAt:=Me.BskyPost.CreatedAt)

            If Not String.IsNullOrWhiteSpace(Me.BskyPost.ReplyUrl) Then
                RaiseEvent ProcessDataReceived(Me, "Resolving thread metadata for a native reply chain...", False)

                Dim strongRef = Await GetStrongRefFromUrl(Me.BskyMgmt, Me.BskyPost.ReplyUrl, ct)

                If Me.BskyPost.ReplyIsQuote Then
                    Call buildPost.Quote(strongRef)
                Else
                    Dim uploadResult = Await Me.BskyMgmt.ExecuteWithAutoSave(
                        Function() buildPost.ReplyTo(strongRef, agent, ct)
                        )
                End If
            End If

            ' Upload all images and videos
            Dim uploadedImagesList As List(Of idunno.Bluesky.Embed.EmbeddedImage) = Nothing
            Dim videoEmbed As idunno.Bluesky.Embed.EmbeddedVideo = Nothing
            Dim videoCount As Integer = 0

            If Me.BskyPost.Attachments IsNot Nothing Then
                For Each attach In Me.BskyPost.Attachments
                    If attach.IsAnimated Then
                        videoCount += 1
                    End If
                Next
                If videoCount > 1 Then
                    Throw New ArgumentException($"There are {videoCount} video / animation attachments. Only 1 is allowed.")
                End If
            End If

            If Me.BskyPost.Attachments IsNot Nothing Then
                For Each attach In Me.BskyPost.Attachments

                    If attach.IsAnimated Then

                        RaiseEvent ProcessDataReceived(Me, $"Uploading: {attach.FileName}", False)

                        ' Upload video or animated image as EmbeddedVideo blob

                        If attach.MimeType.First.Equals("v"c) OrElse attach.MimeType.First.Equals("V"c) Then
                            ' File is actual video. Do the upload in chunks.
                            videoEmbed = Await Me.UploadChunkedVideoAsync(attach, ct)
                        Else
                            ' File is animated image. Upload must be done all at once.
                            videoEmbed = Await Me.UploadVideoAsync(attach, ct)
                        End If
                    Else
                        If uploadedImagesList Is Nothing Then
                            uploadedImagesList = New List(Of idunno.Bluesky.Embed.EmbeddedImage)
                        End If

                        RaiseEvent ProcessDataReceived(Me, $"Uploading raw bytes for: {attach.FileName}", False)

                        ' Upload static image as EmbeddedImage blob
                        Dim uploadResult = Await Me.BskyMgmt.ExecuteWithAutoSave(
                            Function() agent.UploadBlob(attach.FilePath, attach.MimeType, cancellationToken:=ct)
                            )

                        If Not uploadResult.Succeeded Then
                            Throw New BlueskyAttachmentUploadException($"Upload of ""{attach.FileName}"" failed. {uploadResult.AtErrorDetail.Error}: {uploadResult.AtErrorDetail.Message}", uploadResult.StatusCode)
                        End If

                        ' Create embed image object (Besides the uploaded image, stores the AltText and Dimensions)
                        Dim imageEmbed As idunno.Bluesky.Embed.EmbeddedImage
                        Dim altText As String = If(attach.AltText, String.Empty)

                        If attach.Dimensions IsNot Nothing AndAlso attach.Dimensions.Width.HasValue AndAlso attach.Dimensions.Height.HasValue Then
                            Dim ratio As New idunno.Bluesky.Embed.AspectRatio(CInt(attach.Dimensions.Width.Value), CInt(attach.Dimensions.Height.Value))
                            imageEmbed = New idunno.Bluesky.Embed.EmbeddedImage(uploadResult.Result, altText, ratio)
                        Else
                            imageEmbed = New idunno.Bluesky.Embed.EmbeddedImage(uploadResult.Result, altText)
                        End If
                        Call uploadedImagesList.Add(imageEmbed)
                    End If
                Next
            End If

            ct.ThrowIfCancellationRequested()

            ' The library usually validates if the 10 image limit is exceeded and throws an error
            ' Bluesky itself technically allows 20 however
            ' This circumvents the limit validation with an inherited class
            Dim imageGalleryEmbeds As idunno.Bluesky.Embed.EmbeddedGallery = Nothing
            If uploadedImagesList IsNot Nothing AndAlso uploadedImagesList.Count > 0 Then

                RaiseEvent ProcessDataReceived(Me, "Embedding images", False)

                ' Create the EmbeddedGallery instance and add the first image
                ' This is the only image that will be validated before sending the data to Bluesky
                Dim firstGalleryImageLst As New List(Of idunno.Bluesky.Embed.Gallery.GalleryImage)
                Call firstGalleryImageLst.Add(New idunno.Bluesky.Embed.Gallery.GalleryImage(uploadedImagesList(0)))
                imageGalleryEmbeds = New idunno.Bluesky.Embed.EmbeddedGallery(firstGalleryImageLst)

                ' Convert the EmbeddedImage List to a GalleryImage List
                Dim uploadedGalleryImagesList As New List(Of idunno.Bluesky.Embed.Gallery.GalleryImage)
                For Each imageEmbed In uploadedImagesList
                    Dim galleryImageEmbed = New idunno.Bluesky.Embed.Gallery.GalleryImage(imageEmbed)
                    Call uploadedGalleryImagesList.Add(galleryImageEmbed)
                Next

                Dim galleryType As Type = GetType(idunno.Bluesky.Embed.EmbeddedGallery)

                ' Get the internal Private GalleryImage List in the EmbeddedGallery
                Dim itemsField As System.Reflection.FieldInfo = galleryType.GetField("_items", System.Reflection.BindingFlags.NonPublic Or System.Reflection.BindingFlags.Instance)

                ' Set the internal List directly to avoid validation
                Call itemsField?.SetValue(imageGalleryEmbeds, uploadedGalleryImagesList)

                If imageGalleryEmbeds.Count = uploadedImagesList.Count Then
                    RaiseEvent ProcessDataReceived(Me, "Images embedded", False)
                Else
                    RaiseEvent ProcessDataReceived(Me, "Images embedding failed", True)
                    Throw New BlueskyException("Failed setting the gallery images in the EmbeddedGallery")
                End If
            End If

            ct.ThrowIfCancellationRequested()

            ' Add the embedded images to the post builder
            If uploadedImagesList IsNot Nothing AndAlso uploadedImagesList.Count > 0 Then
                Call buildPost.EmbedRecord(imageGalleryEmbeds)
            End If

            ' Add the embedded video to the post builder
            If videoEmbed IsNot Nothing Then
                Call buildPost.Add(videoEmbed)
            End If

            ' Set the actual post text
            If Me.BskyPost.Text IsNot Nothing AndAlso Me.BskyPost.Text.Length <> 0 Then
                Call buildPost.WithText(Me.BskyPost.Text)

#If UseDefaultFacetExtractor Then
                ' Pass your agent's handle resolution routine directly into the constructor
                Dim extractor As idunno.Bluesky.RichText.IFacetExtractor = New idunno.Bluesky.RichText.DefaultFacetExtractor(
                    Async Function(handle, cancellationToken)
                        Dim resolveResult = Await Me.BskyMgmt.ExecuteWithAutoSave(
                            Function() agent.ResolveHandle(handle, cancellationToken)
                            )
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
            Dim postResult = Await Me.BskyMgmt.ExecuteWithAutoSave(
                        Function() agent.Post(finalPost, cancellationToken:=ct, extractFacets:=False)
                        )
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
            Implements idunno.Bluesky.RichText.IFacetExtractor

            Public Property Agent As idunno.Bluesky.BlueskyAgent

            Sub New(agent As idunno.Bluesky.BlueskyAgent)
                Me.Agent = agent
            End Sub

            Public Function ExtractFacets(text As String, Optional cancellationToken As System.Threading.CancellationToken = Nothing) As Task(Of IList(Of idunno.Bluesky.RichText.Facet)) Implements idunno.Bluesky.RichText.IFacetExtractor.ExtractFacets
                ' REGEX PATTERNS FOR ALL RICH TEXT COMPONENTS
                Dim mentionRegex As New System.Text.RegularExpressions.Regex("(?<=\s|^)@([a-zA-Z0-9.-]+)", System.Text.RegularExpressions.RegexOptions.Compiled)
                Dim urlRegex As New System.Text.RegularExpressions.Regex("(?<=\s|^)https?://[^\s]+", System.Text.RegularExpressions.RegexOptions.Compiled)
                Dim tagRegex As New System.Text.RegularExpressions.Regex("(?<=\s|^)#\w+", System.Text.RegularExpressions.RegexOptions.Compiled)

                Dim facetsList As New List(Of idunno.Bluesky.RichText.Facet)()

                ' --- A. PROCESS MENTIONS (@username) ---
                For Each match As System.Text.RegularExpressions.Match In mentionRegex.Matches(text)
                    Dim byteStart As Integer = System.Text.Encoding.UTF8.GetByteCount(text.AsSpan(0, match.Index))
                    Dim byteEnd As Integer = byteStart + System.Text.Encoding.UTF8.GetByteCount(match.Value)

                    Dim handleValue As String = match.Groups(1).Value

                    Try
                        ' RESOLUTION FIX: Call the agent to resolve the text handle to a valid DID over the network
                        ' (Note: The response type might expose a .Value or directly cast to the Did object wrapper)
                        Dim resolveResult = Agent.ResolveHandle(handleValue, cancellationToken)

                        Call resolveResult.Wait(cancellationToken)

                        ' Pass the resolved Did type directly into the feature constructor
                        Dim mentionFeature As New idunno.Bluesky.RichText.MentionFacetFeature(resolveResult.Result)

                        Call facetsList.Add(New idunno.Bluesky.RichText.Facet(New idunno.Bluesky.RichText.ByteSlice(byteStart, byteEnd), New List(Of idunno.Bluesky.RichText.FacetFeature) From {mentionFeature}))

                    Catch ex As Exception
                        ' Ignore invalid handles
                    End Try
                Next

                ' --- B. PROCESS LINKS (https://...) ---
                For Each match As System.Text.RegularExpressions.Match In urlRegex.Matches(text)
                    Dim byteStart As Integer = System.Text.Encoding.UTF8.GetByteCount(text.AsSpan(0, match.Index))
                    Dim byteEnd As Integer = byteStart + System.Text.Encoding.UTF8.GetByteCount(match.Value)

                    Dim urlValue As String = match.Value

                    ' FIX: Convert the string URL into a proper System.Uri object
                    Dim uriObject As New Uri(urlValue)
                    Dim linkFeature As New idunno.Bluesky.RichText.LinkFacetFeature(uriObject)

                    Call facetsList.Add(New idunno.Bluesky.RichText.Facet(New idunno.Bluesky.RichText.ByteSlice(byteStart, byteEnd), New List(Of idunno.Bluesky.RichText.FacetFeature) From {linkFeature}))
                Next

                ' --- C. PROCESS HASHTAGS (#tag) ---
                For Each match As System.Text.RegularExpressions.Match In tagRegex.Matches(text)
                    Dim byteStart As Integer = System.Text.Encoding.UTF8.GetByteCount(text.AsSpan(0, match.Index))
                    Dim byteEnd As Integer = byteStart + System.Text.Encoding.UTF8.GetByteCount(match.Value)

                    ' CRITICAL STEP: Strip the hash prefix so the hidden indexing tag is purely alphanumeric ("dotnet")
                    Dim cleanTagMetadata As String = match.Value.TrimStart("#"c)
                    Dim tagFeature As New idunno.Bluesky.RichText.TagFacetFeature(cleanTagMetadata)

                    Call facetsList.Add(New idunno.Bluesky.RichText.Facet(New idunno.Bluesky.RichText.ByteSlice(byteStart, byteEnd), New List(Of idunno.Bluesky.RichText.FacetFeature) From {tagFeature}))
                Next

                Return Task.FromResult(DirectCast(facetsList, IList(Of idunno.Bluesky.RichText.Facet)))
            End Function
        End Class
#End If

        Public Shared Async Function GetStrongRefFromUrl(bskyMgmt As BskySessionManager, postUrl As String, ct As System.Threading.CancellationToken) As Task(Of idunno.AtProto.Repo.StrongReference)
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
            Dim threadResponse = Await bskyMgmt.ExecuteWithAutoSave(
                        Function() bskyMgmt.BskyAgent.GetPostThread(atUri, depth:=0, cancellationToken:=ct)
                        )

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

        Async Function UploadCaptions(attach As BskyDirectImageUploader.Bdiu.BskyUploadPostData.BskyUploadAttachmentData, ct As System.Threading.CancellationToken) As Task(Of idunno.Bluesky.Embed.Caption)
            Dim captionsAsBytes() As Byte = System.IO.File.ReadAllBytes(attach.CaptionFilePath)

            Dim agent = Me.BskyMgmt.BskyAgent

            Dim captionResult = Await agent.UploadCaptions(captionsAsBytes:=captionsAsBytes, captionLanguage:=attach.CaptionLanguage, cancellationToken:=ct)

            If Not captionResult.Succeeded Then
                Throw New BlueskyAttachmentUploadException($"Caption upload for ""{attach.FileName}"" failed. {captionResult.AtErrorDetail.Error}: {captionResult.AtErrorDetail.Message}", captionResult.StatusCode)
            End If

            Return captionResult.Result
        End Function

        ''' <summary>
        ''' Package the finalized video Blob
        ''' </summary>
        Public Shared Function PackageVideoBlob(
               attach As BskyDirectImageUploader.Bdiu.BskyUploadPostData.BskyUploadAttachmentData,
               videoBlob As idunno.AtProto.Blob,
               captions As ICollection(Of idunno.Bluesky.Embed.Caption)
            ) As idunno.Bluesky.Embed.EmbeddedVideo

            Dim aspectRatio As idunno.Bluesky.Embed.AspectRatio = Nothing
            If attach.Dimensions IsNot Nothing AndAlso attach.Dimensions.Width.HasValue AndAlso attach.Dimensions.Height.HasValue Then
                aspectRatio = New idunno.Bluesky.Embed.AspectRatio(width:=CInt(attach.Dimensions.Width.Value), height:=CInt(attach.Dimensions.Height.Value))
            End If

            Dim videoEmbed As idunno.Bluesky.Embed.EmbeddedVideo

            If attach.PresentAsGif Then
                videoEmbed = New idunno.Bluesky.Embed.EmbeddedVideo(
                    video:=videoBlob, altText:=attach.AltText, aspectRatio:=aspectRatio, captions:=captions, presentation:=idunno.Bluesky.Embed.VideoPresentationKnownValues.Gif)
            Else
                videoEmbed = New idunno.Bluesky.Embed.EmbeddedVideo(
                    video:=videoBlob, altText:=attach.AltText, aspectRatio:=aspectRatio, captions:=captions)
            End If

            Return videoEmbed
        End Function

        ''' <summary>
        ''' Uploads a video attachment all at once
        ''' Will also work for animated images e.g. GIF
        ''' </summary>
        Async Function UploadVideoAsync(attach As BskyDirectImageUploader.Bdiu.BskyUploadPostData.BskyUploadAttachmentData, ct As System.Threading.CancellationToken) As Task(Of idunno.Bluesky.Embed.EmbeddedVideo)
            Dim captions As List(Of idunno.Bluesky.Embed.Caption)

            If String.IsNullOrWhiteSpace(attach.CaptionFilePath) Then
                ' No captions provided
                captions = Nothing
            Else
                captions = New List(Of idunno.Bluesky.Embed.Caption) From {
                    Await Me.UploadCaptions(attach, ct)
                }
            End If

            Dim agent = Me.BskyMgmt.BskyAgent

            Dim media As Byte() = System.IO.File.ReadAllBytes(attach.FilePath)

            ' Upload the whole video / animated image at once
            Dim uploadResult = Await agent.UploadVideo(
                  fileName:=attach.FileName,
                     media:=media,
                  mimeType:=attach.MimeType,
         cancellationToken:=ct)

            If Not uploadResult.Succeeded Then
                Throw New BlueskyAttachmentUploadException($"Failed single upload for file ""{attach.FileName}"". {uploadResult.AtErrorDetail.Error}: {uploadResult.AtErrorDetail.Message}", uploadResult.StatusCode)
            End If

            Return PackageVideoBlob(attach:=attach, videoBlob:=uploadResult.Result.Blob, captions:=captions)
        End Function

        ''' <summary>
        ''' Uploads a video attachment in chunks
        ''' Will not work for animated images e.g. GIF
        ''' </summary>
        Async Function UploadChunkedVideoAsync(attach As BskyDirectImageUploader.Bdiu.BskyUploadPostData.BskyUploadAttachmentData, ct As System.Threading.CancellationToken) As Task(Of idunno.Bluesky.Embed.EmbeddedVideo)
            Dim captions As List(Of idunno.Bluesky.Embed.Caption)

            If String.IsNullOrWhiteSpace(attach.CaptionFilePath) Then
                ' No captions provided
                captions = Nothing
            Else
                captions = New List(Of idunno.Bluesky.Embed.Caption) From {
                    Await Me.UploadCaptions(attach, ct)
                }
            End If

            Dim uploadMimeType As String = Nothing
            uploadMimeType = attach.MimeType

            Dim videoWidth As Nullable(Of Int32)
            Dim videoHeight As Nullable(Of Int32)
            If attach.Dimensions IsNot Nothing Then
                videoWidth = CType(attach.Dimensions.Width, Nullable(Of Int32))
                videoHeight = CType(attach.Dimensions.Height, Nullable(Of Int32))
            End If

            ' Get the exact size of the complete file before splitting it into parts
            Dim fileInfo As New System.IO.FileInfo(attach.FilePath)

            Dim agent = Me.BskyMgmt.BskyAgent

            Dim startResult = Await agent.StartUpload(size:=fileInfo.Length, mimeType:=uploadMimeType, width:=videoWidth, height:=videoHeight, cancellationToken:=ct)

            If Not startResult.Succeeded Then
                Throw New BlueskyAttachmentUploadException($"Starting upload of ""{attach.FileName}"" failed. {startResult.AtErrorDetail.Error}: {startResult.AtErrorDetail.Message}", startResult.StatusCode)
            End If

            ' The upload identifier used for subsequent parts
            Dim sessionJobId As String = startResult.Result.JobId

            ' Stream and upload sequential parts using the JobId
            Dim maxChunkSize As Integer = 5 * 1024 * 1024 ' 5 MB chunks
            Dim buffer(maxChunkSize - 1) As Byte
            Dim partIndex As Integer = 1 ' Start index is required to be 1 by idunno.BlueSky

            Try
                Using fileStream As System.IO.FileStream = System.IO.File.OpenRead(attach.FilePath)
                    Dim bytesRead As Integer = fileStream.Read(buffer, 0, buffer.Length)

                    While bytesRead > 0
                        Dim chunkBytes(bytesRead - 1) As Byte
                        Call System.Buffer.BlockCopy(buffer, 0, chunkBytes, 0, bytesRead)

                        RaiseEvent ProcessDataReceived(Me, $"Uploading part {partIndex} ({bytesRead} bytes)...", False)

                        ' Pass the sessionJobId to authenticate this chunk sequence
                        Dim partResult = Await agent.UploadPart(
                                 jobId:=sessionJobId,
                                  part:=partIndex,
                                 bytes:=chunkBytes,
                     cancellationToken:=ct)

                        If Not partResult.Succeeded Then
                            Throw New BlueskyAttachmentUploadException($"Failed at part {partIndex} for file ""{attach.FileName}"". {partResult.AtErrorDetail.Error}: {partResult.AtErrorDetail.Message}", partResult.StatusCode)
                            Await agent.AbortUpload(sessionJobId, ct)
                        End If

                        partIndex += 1
                        bytesRead = fileStream.Read(buffer, 0, buffer.Length)
                    End While
                End Using

                ' Finalize the chunk layout
                Dim finishResult = Await agent.FinishUpload(
                           jobId:=sessionJobId,
               cancellationToken:=ct)

                If Not finishResult.Succeeded Then
                    Throw New BlueskyAttachmentUploadException($"Failed to complete multipart session for file ""{attach.FileName}"". {finishResult.AtErrorDetail.Error}: {finishResult.AtErrorDetail.Message}", finishResult.StatusCode)
                End If

                ' Poll for Transcode Status
                ' The processing lifecycle continues on the same JobId tracking identity
                Dim videoBlob As idunno.AtProto.Blob = Nothing
                Dim isProcessing As Boolean = True

                RaiseEvent ProcessDataReceived(Me, "Waiting for server-side transcoding processing...", False)
                While isProcessing
                    Await Task.Delay(5000, ct)

                    Dim statusResult = Await agent.GetJobStatus(sessionJobId, ct)

                    If statusResult.Succeeded Then
                        Dim state = statusResult.Result.State
                        If state = idunno.Bluesky.Video.JobState.Completed Then
                            videoBlob = statusResult.Result.Blob
                            isProcessing = False
                        ElseIf state = idunno.Bluesky.Video.JobState.Failed Then
                            Throw New BlueskyAttachmentUploadException($"Transcode processing failed on Bluesky infrastructure for file ""{attach.FileName}"". {statusResult.AtErrorDetail.Error}: {statusResult.AtErrorDetail.Message}", statusResult.StatusCode)
                        End If
                    Else
                        Throw New BlueskyAttachmentUploadException($"Error contacting status monitoring service for file ""{attach.FileName}"". {statusResult.AtErrorDetail.Error}: {statusResult.AtErrorDetail.Message}", statusResult.StatusCode)
                    End If
                End While

                Return PackageVideoBlob(attach:=attach, videoBlob:=videoBlob, captions:=captions)
            Catch ex As Exception
                ' Safely await an offloaded asynchronous task without violating compiler constraints
                Call Task.Run(Async Function() Await agent.AbortUpload(sessionJobId, System.Threading.CancellationToken.None)).Wait(System.Threading.CancellationToken.None)
                Throw
            End Try
        End Function

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

        Public Class BlueskyAttachmentUploadException
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

        Public Sub New(bskyMgmt As BskySessionManager, bskyPost As BskyUploadPostData)
            Me.BskyMgmt = bskyMgmt
            Me.BskyPost = bskyPost
        End Sub

    End Class

    Public Class BskyUploadPostData
        Public Property Text As String
        Public Property ReplyUrl As String
        Public Property ReplyIsQuote As Boolean = False
        Public Property Labels As HashSet(Of String)
        Public Property Attachments As List(Of BskyUploadAttachmentData)
        Public Property Languages As HashSet(Of String)
        Public Property CreatedAt As Nullable(Of DateTimeOffset)
        Public Function GetMaxModifiedAttachmentDate() As Nullable(Of DateTimeOffset)
            If Me.Attachments Is Nothing Then
                Return Nothing
            End If
            Dim maxModDate As Nullable(Of DateTimeOffset) = Nothing
            For Each attach In Me.Attachments
                If (maxModDate Is Nothing AndAlso attach.DateTimeModified.HasValue) _
             OrElse attach.DateTimeModified.Value.CompareTo(maxModDate.Value) > 0 Then
                    ' Current image has a larger modified date than the previous
                    maxModDate = attach.DateTimeModified
                End If
            Next
            Return maxModDate
        End Function
        Public Function GetMaxCreatedAttachmentDate() As Nullable(Of DateTimeOffset)
            If Me.Attachments Is Nothing Then
                Return Nothing
            End If
            Dim maxCrtDate As Nullable(Of DateTimeOffset) = Nothing
            For Each attach In Me.Attachments
                If (maxCrtDate Is Nothing AndAlso attach.DateTimeCreated.HasValue) _
             OrElse attach.DateTimeCreated.Value.CompareTo(maxCrtDate.Value) > 0 Then
                    ' Current image has a larger modified date than the previous
                    maxCrtDate = attach.DateTimeCreated
                End If
            Next
            Return maxCrtDate
        End Function
        Public Sub AddAttachment(image As BskyUploadAttachmentData)
            If Me.Attachments Is Nothing Then Me.Attachments = New List(Of Bdiu.BskyUploadPostData.BskyUploadAttachmentData)
            Call Me.Attachments.Add(image)
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
        Public Class BskyUploadAttachmentData
            Public Property FilePath As String
            Public Property AltText As String
            Public Property Dimensions As ImageDimensions
            Public Property MimeType As String
            ''' <summary>
            ''' Is animated image or video
            ''' </summary>
            Public Property IsAnimated As Boolean
            ''' <summary>
            ''' The final <see cref="idunno.Bluesky.Embed.EmbeddedVideo"/> will be shown as "GIF"
            ''' </summary>
            Public Property PresentAsGif As Boolean
            ''' <summary>
            ''' Path of a Bluesky supported .vtt caption file (is ignored for static image posts)
            ''' </summary>
            Public Property CaptionFilePath As String
            Public Property CaptionLanguage As String
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

            Public Sub LoadAttachmentData()
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

                ' Map MimeTypes for videos
                ' Magick.Net returns an empty string when reading the MimeType for videos
                Select Case formatInfo.Format
                    Case ImageMagick.MagickFormat.Mp4
                        Me.MimeType = "video/mp4"
                        Me.IsAnimated = True
                    Case ImageMagick.MagickFormat.M4v
                        Me.MimeType = "video/x-m4v"
                        Me.IsAnimated = True
                    Case ImageMagick.MagickFormat.WebM
                        Me.MimeType = "video/webm"
                        Me.IsAnimated = True
                    Case ImageMagick.MagickFormat.Avi
                        Me.MimeType = "video/x-msvideo"
                        Me.IsAnimated = True
                    Case ImageMagick.MagickFormat.Mov
                        Me.MimeType = "video/quicktime"
                        Me.IsAnimated = True
                    Case ImageMagick.MagickFormat.Mpg, ImageMagick.MagickFormat.Mpeg
                        Me.MimeType = "video/mpeg"
                        Me.IsAnimated = True
                    Case Else
                        ' Check if the image is animated
                        Try
                            Select Case formatInfo.Format
                                Case ImageMagick.MagickFormat.Png

                                    ' Check for APNG animation control chunk
                                    Me.IsAnimated = IsPngAnimated(fileInfo.FullName)

                                Case Else
                                    Dim frames As IEnumerable(Of ImageMagick.IMagickImageInfo) = ImageMagick.MagickImageInfo.ReadCollection(fileInfo)

                                    ' Counts as animated if there is more than one frame
                                    If frames.Count() > 1 Then
                                        Me.IsAnimated = True
                                    End If
                            End Select

                            If Me.IsAnimated Then
                                Me.PresentAsGif = True
                            End If

                        Catch ex As Exception
                            ' Ignore errors
                        End Try
                End Select
            End Sub

            ''' <summary>
            ''' Fast evaluation to determine if a PNG is animated by checking its headers.
            ''' Counts as animated if an acTL chunk is found.
            ''' </summary>
            Public Shared Function IsPngAnimated(filePath As String) As Boolean
                Try
                    Using stream As New System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read)
                        ' Target binary indicators converted to integers for fast matching
                        Dim acTlMarker As Integer = &H6163544C ' "acTL" in Hex
                        Dim idatMarker As Integer = &H49444154 ' "IDAT" in Hex

                        ' Sliding 4-byte window buffer
                        Dim rollingBuffer As Long = 0
                        Dim currentByte As Integer = 0

                        ' Read byte by byte. The acTL chunk always resides near the file header.
                        While True
                            currentByte = stream.ReadByte()
                            If currentByte = -1 Then Exit While ' End of file reached

                            ' Shift old bytes left and append the newest byte
                            rollingBuffer = ((rollingBuffer And &HFFFFFF) << 8) Or CByte(currentByte)

                            ' Check if the window matches the Animation Control chunk
                            If rollingBuffer = acTlMarker Then
                                Return True ' Confirmed APNG
                            End If

                            ' Optimization: If "IDAT" is hit, we passed the valid header window 
                            ' where acTL is allowed to reside. Abort immediately.
                            If rollingBuffer = idatMarker Then
                                Return False ' Standard Static PNG
                            End If
                        End While
                    End Using

                Catch ex As Exception
                    ' Handle or log stream errors safely here
                End Try

                Return False
            End Function

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