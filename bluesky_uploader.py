import time
import sys
import os
import json
import re
from atproto import Client, models, client_utils

def resolve_web_url_to_at_uri(client, url):
    """Converts a standard bsky.app web URL into a native database AT-URI."""
    match = re.search(r'/profile/([^/]+)/post/([^/]+)', url)
    if not match:
        raise ValueError("Invalid Bluesky URL format. Make sure it contains /profile/.../post/...")
    
    user_identifier = match.group(1)
    post_id = match.group(2)
    
    resolved_did = user_identifier
    if not user_identifier.startswith("did:"):
        profile = client.get_profile(actor=user_identifier)
        resolved_did = profile.did
        
    return f"at://{resolved_did}/app.bsky.feed.post/{post_id}"

def build_rich_text_facets(text_content):
    """
    Parses plain text to automatically detect hashtags (#tag) and URLs (http/https).
    Returns a TextBuilder object which maps text byte ranges to clickable system facets.
    """
    builder = client_utils.TextBuilder()
    
    pattern = re.compile(r'(https?://[^\s]+)|(#[a-zA-Z0-9_\u00c0-\u00ff]+)')
    last_idx = 0
    
    for match in pattern.finditer(text_content):
        start, end = match.span()
        if start > last_idx:
            builder.text(text_content[last_idx:start])
            
        matched_text = match.group(0)
        
        if matched_text.startswith('#'):
            tag_value = matched_text[1:]
            builder.tag(matched_text, tag_value)
        else:
            builder.link(matched_text, matched_text)
            
        last_idx = end
        
    if last_idx < len(text_content):
        builder.text(text_content[last_idx:])
        
    return builder

def main():
    # Verify that the JSON bridge payload path was provided
    if len(sys.argv) < 2:
        print("[ERROR] Missing JSON bridge payload path.", file=sys.stderr)
        sys.exit(1)
        
    json_path = sys.argv[1]
    
    if not os.path.exists(json_path):
        print(f"[ERROR] Bridge payload file not found: {json_path}", file=sys.stderr)
        sys.exit(1)

    with open(json_path, "r", encoding="utf-8-sig") as f:
        data = json.load(f)

    print("Read payload", file=sys.stdout)

    HANDLE: str = data.get("handle")
    APP_PASSWORD: str = data.get("password")
    post_text: str | None = data.get("text")
    # Ensure post_text is a valid string, even if an empty value was sent
    if post_text is None:
        post_text = ""
    reply_url: str | None = data.get("reply_url")
    label_list: list = data.get("labels", [])
    images_list: list[dict] = data.get("images", [])

    client = Client()
    try:
        client.login(HANDLE, APP_PASSWORD)
    except Exception as e:
        print(f"[CRITICAL ERROR] Login failed: {e}", file=sys.stderr)
        sys.exit(1)

    print("Parsing text caption for interactive hashtags and web links...")
    rich_text = build_rich_text_facets(post_text)

    # Upload raw uncompressed image blobs
    images_embed = []
    if isinstance(images_list, dict):
        images_list = [images_list]

    for img_item in images_list:
        path: str = img_item.get("path")
        filename: str = os.path.basename(path)
        alt_text: str | None = img_item.get("alt", "")

        # Ensure alt_text is a valid string, even if an empty value was sent
        if alt_text is None:
            alt_text = ""

        # Read provided image dimensions
        width: int | None = None
        height: int | None = None
        try:
            dimensions = img_item["dimensions"]
            try:
                width = dimensions["width"]
            except KeyError:
                print(f"[WARNING] No image width provided for: {filename}")
            try:
                height = dimensions["height"]
            except KeyError:
                print(f"[WARNING] No image height provided for: {filename}")
        except KeyError:
            print(f"[WARNING] No image dimensions provided for: {filename}")

        if width is None:
            width = 1000
            print(f"Using fallback value {width} for width")

        if height is None:
            height = 1000
            print(f"Using fallback value {height} for height")

        print(f"Uploading raw bytes for: {filename}")
        try:
            with open(path, "rb") as f:
                img_data = f.read()
            upload = client.upload_blob(img_data)

            images_embed.append(models.AppBskyEmbedGallery.Image(alt=alt_text, image=upload.blob, aspectRatio=models.AppBskyEmbedDefs.AspectRatio(width=width, height=height) ))
        except Exception as e:
            print(f"[CRITICAL ERROR] Image upload failed for {path}: {e}", file=sys.stderr)
            sys.exit(1)

    print(f"Embedding images")
    try:
        embed = models.AppBskyEmbedGallery.Main(items=images_embed) if images_embed else None

    except Exception as e:
        print(f"[CRITICAL ERROR] Image embed failed: {e}", file=sys.stderr)
        sys.exit(1)

    # Handle Thread-Reply mapping
    reply_to = None
    if reply_url and reply_url.lower() != "none":
        print("Resolving thread metadata for a native reply chain...")
        try:
            at_uri = resolve_web_url_to_at_uri(client, reply_url)
            thread = client.get_post_thread(at_uri)
            parent_uri = thread.thread.post.uri
            parent_cid = thread.thread.post.cid
            
            root_uri = thread.thread.post.record.reply.root.uri if hasattr(thread.thread.post.record, 'reply') and thread.thread.post.record.reply else parent_uri
            root_cid = thread.thread.post.record.reply.root.cid if hasattr(thread.thread.post.record, 'reply') and thread.thread.post.record.reply else parent_cid
            
            reply_to = models.AppBskyFeedPost.ReplyRef(
                parent=models.ComAtprotoRepoStrongRef.Main(cid=parent_cid, uri=parent_uri),
                root=models.ComAtprotoRepoStrongRef.Main(cid=root_cid, uri=root_uri)
            )
        except Exception as e:
            print(f"\n[CRITICAL ERROR] Failed to resolve reply thread: {e}", file=sys.stderr)
            sys.exit(1)

    # Build Content Warning System Labels
    labels = None
    if label_list and len(label_list) > 0:
        print(f"Applying Content Warning System Labels: {label_list}")
        self_labels_array = [models.ComAtprotoLabelDefs.SelfLabel(val=token) for token in label_list]
        labels = models.ComAtprotoLabelDefs.SelfLabels(values=self_labels_array)

    # Fire the post record payload
    try:
        post_record = models.AppBskyFeedPost.Record(
            text=rich_text.build_text(),            
            facets=rich_text.build_facets(),        
            embed=embed,
            reply=reply_to,
            labels=labels,
            created_at=client.get_current_time_iso()
        )
        
        client.com.atproto.repo.create_record(
            models.ComAtprotoRepoCreateRecord.Data(
                repo=client.me.did,
                collection=models.ids.AppBskyFeedPost,
                record=post_record
            )
        )
        print("Successfully posted to Bluesky!")
    except Exception as e:
        print(f"\n[CRITICAL SERVER REJECTION] The Bluesky PDS network rejected this post payload: {e}", file=sys.stderr)
        sys.exit(1)

if __name__ == "__main__":
    main()
