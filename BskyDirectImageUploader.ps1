# =====================================================================
# PART 1: INITIALIZATION & SAFETY SIZE CHECKS
# =====================================================================
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

# 1. Capture dropped image paths or trigger file browser dialog
$ImagePaths = $args
$wshell = New-Object -ComObject Wscript.Shell

if (-not $ImagePaths) {
    Write-Host "No images dropped. Opening Windows File Browser..." -ForegroundColor Yellow
    $FileDialog = New-Object System.Windows.Forms.OpenFileDialog
    #$FileDialog.InitialDirectory = [Environment]::GetFolderPath("MyPictures")
    $FileDialog.Filter = "Bluesky Supported Images (*.png;*.jpg;*.jpeg;*.jfif;*.webp;*.avif)|*.png;*.jpg;*.jpeg;*.jfif;*.webp;*.avif|All Files (*.*)|*.*"
    $FileDialog.Multiselect = $true
    $FileDialog.RestoreDirectory = $true
    $FileDialog.Title = "Select 1 to 4 Images (Hold CTRL to select multiple)"
    
    $DialogResult = $FileDialog.ShowDialog((New-Object System.Windows.Forms.Form -Property @{TopMost = $true}))
    if ($DialogResult -eq [System.Windows.Forms.DialogResult]::OK) {
        $ImagePaths = @($FileDialog.FileNames)
    } else {
        Write-Host "Upload canceled by user." -ForegroundColor Red; Start-Sleep -Seconds 1; Exit
    }
}

if ($ImagePaths.Count -gt 4) {
    $wshell.Popup("Bluesky only allows up to 4 images per post!", 0, "Limit Exceeded", 48) | Out-Null; Exit
}

# 2. Strict File Size Verification (PDS threshold limit: 2MB per blob)
foreach ($Path in $ImagePaths) {
    $FileSize = (Get-Item $Path).Length
    if ($FileSize -gt 2000000) {
        $FileName = Split-Path $Path -Leaf
        $SizeInMB = [math]::Round($FileSize / 1MB, 2)
        $wshell.Popup("CRITICAL ERROR:`n`nThe file '$FileName' is too large ($SizeInMB MB)!`nBluesky servers strictly limit uncompressed blobs to a maximum of 2.00 MB.", 0, "Upload Aborted", 16) | Out-Null
        Exit
    }
}

# Define configuration directory and credentials storage files
$ConfigFolder = "$HOME\.config\bsky-ps"
$HandleFile = "$ConfigFolder\handle.txt"
$PasswordFile = "$ConfigFolder\password.enc"

if (Test-Path $HandleFile) {
    $ResetChoice = $wshell.Popup("Do you want to log out and enter new login credentials / App Password?", 0, "Bluesky Session Manager", 4 + 32)
    if ($ResetChoice -eq 6) {
        Write-Host "Clearing saved credentials..." -ForegroundColor Yellow
        if (Test-Path $HandleFile) { Remove-Item $HandleFile -Force }
        if (Test-Path $PasswordFile) { Remove-Item $PasswordFile -Force }
    }
}

if (-not (Test-Path $ConfigFolder)) { New-Item -ItemType Directory -Path $ConfigFolder | Out-Null }

# 4. SECURE CREDENTIAL CHECK & CONFIGURATION
if (-not (Test-Path $HandleFile) -or -not (Test-Path $PasswordFile)) {
    Write-Host "First-Time Login Setup initialized..." -ForegroundColor Yellow
    
    # Open a clean text input prompt for the handle
    $UserHandle = Read-Host "Enter your Bluesky handle (e.g., name.bsky.social)"
    if ([string]::IsNullOrWhiteSpace($UserHandle)) { Write-Host "Setup aborted."; Start-Sleep -Seconds 1; Exit }
    
    # Open a secure Windows password mask prompt (hides your characters with asterisks)
    $SecretPassword = Read-Host "Enter your Bluesky App Password (xxxx-xxxx-xxxx-xxxx)" -AsSecureString
    if ($null -eq $SecretPassword) { Write-Host "Setup aborted."; Start-Sleep -Seconds 1; Exit }
    
    # Convert the secure string back to plain text internally to save it encrypted via DPAPI
    $BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecretPassword)
    $AppPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)
    
    $UserHandle | Out-File -FilePath $HandleFile -Encoding utf8
    $AppPassword | ConvertTo-SecureString -AsPlainText -Force | ConvertFrom-SecureString | Out-File -FilePath $PasswordFile
    Write-Host "Credentials saved securely!" -ForegroundColor Green
}

[string]$HANDLE = (Get-Content -Path $HandleFile -Raw).Trim()
$EncryptedPassword = Get-Content -Path $PasswordFile
$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR((ConvertTo-SecureString $EncryptedPassword))
[string]$PASSWORD = ([System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)).Trim()

# =====================================================================
# PART 2: DIALOG USER FORMS (CAPTION WITH LIVE COUNTER, REPLY, LABELS)
# =====================================================================

# 5. MULTI-LINE CAPTION FORM (Resizing ENABLED + Live 300 Character Limit Counter)
$Form = New-Object System.Windows.Forms.Form
$Form.Text = "New Advanced Post"
$Form.Size = New-Object System.Drawing.Size(450,300) # Slightly taller to fit the counter
$Form.StartPosition = "CenterScreen"
$Form.TopMost = $true
$Form.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::Sizable
$Form.MaximizeBox = $true
$Form.MinimizeBox = $true

$Label = New-Object System.Windows.Forms.Label
$Label.Location = New-Object System.Drawing.Point(15,10)
$Label.Size = New-Object System.Drawing.Size(400,20)
$Label.Text = "Enter your Bluesky caption (Press Enter for new lines):"
$Label.Anchor = [System.Windows.Forms.AnchorStyles]::Top -bor [System.Windows.Forms.AnchorStyles]::Left -bor [System.Windows.Forms.AnchorStyles]::Right
$Form.Controls.Add($Label)

$TextBox = New-Object System.Windows.Forms.TextBox
$TextBox.Location = New-Object System.Drawing.Point(15,35)
$TextBox.Size = New-Object System.Drawing.Size(400,130)
$TextBox.Multiline = $true
$TextBox.ScrollBars = "Vertical"
$TextBox.AcceptsReturn = $true
$TextBox.Anchor = [System.Windows.Forms.AnchorStyles]::Top -bor [System.Windows.Forms.AnchorStyles]::Bottom -bor [System.Windows.Forms.AnchorStyles]::Left -bor [System.Windows.Forms.AnchorStyles]::Right
$Form.Controls.Add($TextBox)

# NEW: Live Character Counter Label
$CounterLabel = New-Object System.Windows.Forms.Label
$CounterLabel.Location = New-Object System.Drawing.Point(15,175)
$CounterLabel.Size = New-Object System.Drawing.Size(200,20)
$CounterLabel.Text = "Characters: 0 / 300"
$CounterLabel.ForeColor = [System.Drawing.Color]::DarkGreen
$CounterLabel.Anchor = [System.Windows.Forms.AnchorStyles]::Bottom -bor [System.Windows.Forms.AnchorStyles]::Left
$Form.Controls.Add($CounterLabel)

# The "Continue" Button
$Button = New-Object System.Windows.Forms.Button
$Button.Location = New-Object System.Drawing.Point(320,200)
$Button.Size = New-Object System.Drawing.Size(95,30)
$Button.Text = "Continue"
$Button.DialogResult = [System.Windows.Forms.DialogResult]::OK
$Button.Anchor = [System.Windows.Forms.AnchorStyles]::Bottom -bor [System.Windows.Forms.AnchorStyles]::Right
$Form.AcceptButton = $Button
$Form.Controls.Add($Button)

# The "Cancel" Button
$CancelButton = New-Object System.Windows.Forms.Button
$CancelButton.Location = New-Object System.Drawing.Point(215,200)
$CancelButton.Size = New-Object System.Drawing.Size(95,30)
$CancelButton.Text = "Cancel"
$CancelButton.DialogResult = [System.Windows.Forms.DialogResult]::Cancel
$CancelButton.Anchor = [System.Windows.Forms.AnchorStyles]::Bottom -bor [System.Windows.Forms.AnchorStyles]::Right
$Form.CancelButton = $CancelButton
$Form.Controls.Add($CancelButton)

# NEW: Live Text-Changed Event Handler for strict validation
$TextBox.Add_TextChanged({
    $CurrentLength = $TextBox.Text.Length
    $CounterLabel.Text = "Characters: $CurrentLength / 300"
    
    if ($CurrentLength -gt 300) {
        # Limit exceeded: Turn counter red and disable the Continue button
        $CounterLabel.ForeColor = [System.Drawing.Color]::Red
        $Button.Enabled = $false
    } else {
        # Safe length: Keep counter green and enable the Continue button
        $CounterLabel.ForeColor = [System.Drawing.Color]::DarkGreen
        $Button.Enabled = $true
    }
})

$Form.Add_Shown({$TextBox.Focus()})
$Result = $Form.ShowDialog()

if ($Result -ne [System.Windows.Forms.DialogResult]::OK) { 
    Write-Host "Upload canceled by user at caption stage." -ForegroundColor Red; Start-Sleep -Seconds 1; Exit 
}
$PostText = $TextBox.Text
if ($PostText -eq $null) { $PostText = "" }


# 5b. THREAD REPLY FORM (Resizing DISABLED - Fixed Size)
$ReplyForm = New-Object System.Windows.Forms.Form
$ReplyForm.Text = "Reply Configuration"
$ReplyForm.Size = New-Object System.Drawing.Size(450,200)
$ReplyForm.StartPosition = "CenterScreen"
$ReplyForm.TopMost = $true
$ReplyForm.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::FixedDialog
$ReplyForm.MaximizeBox = $false
$ReplyForm.MinimizeBox = $false

$ReplyLabel = New-Object System.Windows.Forms.Label
$ReplyLabel.Location = New-Object System.Drawing.Point(15,10)
$ReplyLabel.Size = New-Object System.Drawing.Size(400,40)
$ReplyLabel.Text = "If this is a reply, paste the full Bluesky post URL below:`n(Leave blank and click Continue for a normal post)"
$ReplyForm.Controls.Add($ReplyLabel)

$ReplyTextBox = New-Object System.Windows.Forms.TextBox
$ReplyTextBox.Location = New-Object System.Drawing.Point(15,55)
$ReplyTextBox.Size = New-Object System.Drawing.Size(400,25)
$ReplyForm.Controls.Add($ReplyTextBox)

$ReplyBtn = New-Object System.Windows.Forms.Button
$ReplyBtn.Location = New-Object System.Drawing.Point(320,105)
$ReplyBtn.Size = New-Object System.Drawing.Size(95,30)
$ReplyBtn.Text = "Continue"
$ReplyBtn.DialogResult = [System.Windows.Forms.DialogResult]::OK
$ReplyForm.AcceptButton = $ReplyBtn
$ReplyForm.Controls.Add($ReplyBtn)

$ReplyCancelBtn = New-Object System.Windows.Forms.Button
$ReplyCancelBtn.Location = New-Object System.Drawing.Point(215,105)
$ReplyCancelBtn.Size = New-Object System.Drawing.Size(95,30)
$ReplyCancelBtn.Text = "Cancel"
$ReplyCancelBtn.DialogResult = [System.Windows.Forms.DialogResult]::Cancel
$ReplyForm.CancelButton = $ReplyCancelBtn
$ReplyForm.Controls.Add($ReplyCancelBtn)

$ReplyForm.Add_Shown({$ReplyTextBox.Focus()})
$ReplyResult = $ReplyForm.ShowDialog()

if ($ReplyResult -ne [System.Windows.Forms.DialogResult]::OK) {
    Write-Host "Upload canceled by user at reply stage." -ForegroundColor Red; Start-Sleep -Seconds 1; Exit
}
$ReplyURL = $ReplyTextBox.Text
if ([string]::IsNullOrWhiteSpace($ReplyURL)) { $ReplyURL = "NONE" }


# 5c. MULTIPLE CONTENT WARNING FORM (Resizing DISABLED - Fixed Size)
$LabelForm = New-Object System.Windows.Forms.Form
$LabelForm.Text = "Content Warning Configuration"
$LabelForm.Size = New-Object System.Drawing.Size(450,240)
$LabelForm.StartPosition = "CenterScreen"
$LabelForm.TopMost = $true
$LabelForm.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::FixedDialog
$LabelForm.MaximizeBox = $false
$LabelForm.MinimizeBox = $false

$MenuLabel = New-Object System.Windows.Forms.Label
$MenuLabel.Location = New-Object System.Drawing.Point(15,10)
$MenuLabel.Size = New-Object System.Drawing.Size(400,80)
$MenuLabel.Text = "Choose Content Warning Labels (Separate with commas, e.g. 1,2):`n`n0 = None`n1 = Suggestive (Sexual)  |  2 = Nudity`n3 = Graphic Media (Violence)  |  4 = Porn (Explicit)"
$LabelForm.Controls.Add($MenuLabel)

$LabelTextBox = New-Object System.Windows.Forms.TextBox
$LabelTextBox.Location = New-Object System.Drawing.Point(15,100)
$LabelTextBox.Size = New-Object System.Drawing.Size(400,25)
$LabelTextBox.Text = "0"
$LabelForm.Controls.Add($LabelTextBox)

$LabelBtn = New-Object System.Windows.Forms.Button
$LabelBtn.Location = New-Object System.Drawing.Point(320,150)
$LabelBtn.Size = New-Object System.Drawing.Size(95,30)
$LabelBtn.Text = "Continue"
$LabelBtn.DialogResult = [System.Windows.Forms.DialogResult]::OK
$LabelForm.AcceptButton = $LabelBtn
$LabelForm.Controls.Add($LabelBtn)

$LabelCancelBtn = New-Object System.Windows.Forms.Button
$LabelCancelBtn.Location = New-Object System.Drawing.Point(215,150)
$LabelCancelBtn.Size = New-Object System.Drawing.Size(95,30)
$LabelCancelBtn.Text = "Cancel"
$LabelCancelBtn.DialogResult = [System.Windows.Forms.DialogResult]::Cancel
$LabelForm.CancelButton = $LabelCancelBtn
$LabelForm.Controls.Add($LabelCancelBtn)

$LabelForm.Add_Shown({$LabelTextBox.Focus(); $LabelTextBox.SelectAll()})
$LabelResult = $LabelForm.ShowDialog()

if ($LabelResult -ne [System.Windows.Forms.DialogResult]::OK) {
    Write-Host "Upload canceled by user at warning label stage." -ForegroundColor Red; Start-Sleep -Seconds 1; Exit
}

$LabelInput = $LabelTextBox.Text
if ([string]::IsNullOrWhiteSpace($LabelInput)) { $LabelInput = "0" }

$ResolvedTokens = @()
foreach ($Choice in $LabelInput.Split(",")) {
    $Token = switch ($Choice.Trim()) {
        "1" { "sexual" }
        "2" { "nudity" }
        "3" { "graphic-media" }
        "4" { "porn" }
        Default { $null }
    }
    if ($Token) { $ResolvedTokens += $Token }
}

# =====================================================================
# PART 3: ADVANCED ALT-TEXT FORMS, REVIEW CHECK & PAYLOAD SUBMIT
# =====================================================================

# 6. Loop through each attached image file to collect dedicated accessible multi-line descriptions
$ImagesArray = @()
$ImgCounter = 1

foreach ($Path in $ImagePaths) {
    $FileName = Split-Path $Path -Leaf
    $AltForm = New-Object System.Windows.Forms.Form
    $AltForm.Text = "Accessibility Setup"
    $AltForm.Size = New-Object System.Drawing.Size(450,280)
    $AltForm.StartPosition = "CenterScreen"
    $AltForm.TopMost = $true
    $AltForm.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::Sizable
    $AltForm.MaximizeBox = $true
    $AltForm.MinimizeBox = $true

    $AltLabel = New-Object System.Windows.Forms.Label
    $AltLabel.Location = New-Object System.Drawing.Point(15,10)
    $AltLabel.Size = New-Object System.Drawing.Size(400,20)
    $AltLabel.Text = "Alt Text (Image Description) for image $ImgCounter/$($ImagePaths.Count) (Blank for no Alt Text):"
    $AltLabel.Anchor = [System.Windows.Forms.AnchorStyles]::Top -bor [System.Windows.Forms.AnchorStyles]::Left -bor [System.Windows.Forms.AnchorStyles]::Right
    $AltForm.Controls.Add($AltLabel)
    
    $FileLabel = New-Object System.Windows.Forms.Label
    $FileLabel.Location = New-Object System.Drawing.Point(15,30)
    $FileLabel.Size = New-Object System.Drawing.Size(400,20)
    $FileLabel.Text = "File: $FileName"
    $FileLabel.Font = New-Object System.Drawing.Font($FileLabel.Font, [System.Drawing.FontStyle]::Bold)
    $FileLabel.Anchor = [System.Windows.Forms.AnchorStyles]::Top -bor [System.Windows.Forms.AnchorStyles]::Left -bor [System.Windows.Forms.AnchorStyles]::Right
    $AltForm.Controls.Add($FileLabel)

    $AltTextBox = New-Object System.Windows.Forms.TextBox
    $AltTextBox.Location = New-Object System.Drawing.Point(15,55)
    $AltTextBox.Size = New-Object System.Drawing.Size(400,110)
    $AltTextBox.Multiline = $true
    $AltTextBox.ScrollBars = "Vertical"
    $AltTextBox.AcceptsReturn = $true
    $AltTextBox.Anchor = [System.Windows.Forms.AnchorStyles]::Top -bor [System.Windows.Forms.AnchorStyles]::Bottom -bor [System.Windows.Forms.AnchorStyles]::Left -bor [System.Windows.Forms.AnchorStyles]::Right
    $AltForm.Controls.Add($AltTextBox)

    $AltButton = New-Object System.Windows.Forms.Button
    $AltButton.Location = New-Object System.Drawing.Point(320,180)
    $AltButton.Size = New-Object System.Drawing.Size(95,30)
    $AltButton.Text = "Continue"
    $AltButton.DialogResult = [System.Windows.Forms.DialogResult]::OK
    $AltButton.Anchor = [System.Windows.Forms.AnchorStyles]::Bottom -bor [System.Windows.Forms.AnchorStyles]::Right
    $AltForm.AcceptButton = $AltButton
    $AltForm.Controls.Add($AltButton)

    $AltCancelButton = New-Object System.Windows.Forms.Button
    $AltCancelButton.Location = New-Object System.Drawing.Point(215,180)
    $AltCancelButton.Size = New-Object System.Drawing.Size(95,30)
    $AltCancelButton.Text = "Cancel"
    $AltCancelButton.DialogResult = [System.Windows.Forms.DialogResult]::Cancel
    $AltCancelButton.Anchor = [System.Windows.Forms.AnchorStyles]::Bottom -bor [System.Windows.Forms.AnchorStyles]::Right
    $AltForm.CancelButton = $AltCancelButton
    $AltForm.Controls.Add($AltCancelButton)

    $AltForm.Add_Shown({$AltTextBox.Focus()})
    if ($AltForm.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) {
        Write-Host "Upload canceled by user at alt-text stage." -ForegroundColor Red; Start-Sleep -Seconds 1; Exit
    }
    
    $ImagesArray += [PSCustomObject]@{ path = $Path; alt = $AltTextBox.Text }
    $ImgCounter++
}

# 6b. Final Graphical Review checkpoint verification window
$LabelSummary = if ($ResolvedTokens.Count -gt 0) { $ResolvedTokens -join ", " } else { "None" }
$ReplySummary = if ($ReplyURL -eq "NONE") { "Standalone Post" } else { "Thread Reply" }

$SummaryMessage = "REVIEW YOUR POST BEFORE SENDING:`n`n" +
                  "----------------------------------------`n" +
                  "Caption:`n$PostText`n" +
                  "----------------------------------------`n" +
                  "Type: $ReplySummary`n" +
                  "Content Warnings: $LabelSummary`n" +
                  "Images Attached: $($ImagePaths.Count)`n`n" +
                  "Are you sure you want to publish this post uncompressed?"

if ($wshell.Popup($SummaryMessage, 0, "Final Confirmation", 4 + 32) -ne 6) {
    Write-Host "Upload aborted by user at final check." -ForegroundColor Red; Start-Sleep -Seconds 1; Exit
}

# Build and write a flattened json bridge data payload file
$Payload = [PSCustomObject]@{
    handle = $HANDLE; password = $PASSWORD; text = $PostText; reply_url = $ReplyURL; labels = $ResolvedTokens; images = $ImagesArray
}
$JsonPath = "$PSScriptRoot\transfer_payload.json"
$Payload | ConvertTo-Json -Depth 4 | Out-File -FilePath $JsonPath -Encoding utf8

# 7. Execute the local Python pipeline engine
Write-Host "Handing over payload to Python Engine..." -ForegroundColor Cyan
$PythonScriptPath = "$PSScriptRoot\bluesky_uploader.py"
python "$PythonScriptPath" "$JsonPath"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Python Execution Failed. The post was not published."
    if (Test-Path $JsonPath) { Remove-Item $JsonPath -Force }
    Read-Host "Press Enter to exit..."; Exit
}

if (Test-Path $JsonPath) { Remove-Item $JsonPath -Force }
Write-Host "Process completed successfully!" -ForegroundColor Green
Start-Sleep -Seconds 2
