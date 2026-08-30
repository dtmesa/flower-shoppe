# One-off script to populate the running backend with sample inventory + reservations for exploring the UI.
# Usage: powershell -File seed-test-data.ps1  (backend must already be running on http://localhost:8080)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$apiBase = "http://localhost:8080"

$login = Invoke-RestMethod -Uri "$apiBase/api/auth/login" -Method Post -ContentType "application/json" -Body '{"username":"admin","password":"admin"}'
$token = $login.token
$headers = @{ Authorization = "Bearer $token" }
Write-Output "Logged in as $($login.username)"

function New-SolidColorPng {
    param([string]$HexColor, [string]$OutPath)
    $color = [System.Drawing.ColorTranslator]::FromHtml($HexColor)
    $bmp = New-Object System.Drawing.Bitmap(300, 300)
    $graphics = [System.Drawing.Graphics]::FromImage($bmp)
    $brush = New-Object System.Drawing.SolidBrush($color)
    $graphics.FillRectangle($brush, 0, 0, 300, 300)
    $graphics.Dispose()
    $bmp.Save($OutPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

$colorSwatch = @{
    "Red"          = "#C1503F"
    "Pink"         = "#E8A0BF"
    "Yellow/White" = "#F4D35E"
}

# id mimics the physical ID tag attached to each plant.
$items = @(
    @{ id = "PLM-0001"; type = "Rooted Plant"; color = "Yellow/White"; size = "Medium"; price = 24.99; qty = 8;  desc = "Fragrant classic yellow-and-white plumeria, easy to grow."; photo = $true }
    @{ id = "PLM-0002"; type = "Cutting"; color = "Red"; size = "Small"; price = 12.50; qty = 15; desc = "Deep red blooms with a spicy-sweet fragrance."; photo = $true }
    @{ id = "PLM-0003"; type = "Rooted Plant"; color = "Yellow/White"; size = "Large"; price = 34.00; qty = 3;  desc = "Bold golden-yellow flowers on a vigorous grower."; photo = $true }
    @{ id = "PLM-0004"; type = "Rooted Plant"; color = "Pink"; size = "Medium"; price = 28.75; qty = 0;  desc = "Swirled pink and white petals, currently sold out."; photo = $true }
    @{ id = "PLM-0005"; type = "Cutting"; color = "Yellow/White"; size = "Small"; price = 9.99;  qty = 20; desc = "The classic backyard plumeria - reliable bloomer."; photo = $true }
    @{ id = "PLM-0006"; type = "Rooted Plant"; color = "Pink"; size = "Large"; price = 45.00; qty = 5;  desc = "Rich rose-pink blooms, a favorite for leis."; photo = $true }
    @{ id = "PLM-0007"; type = "Rooted Plant"; color = "Red"; size = "Medium"; price = 32.50; qty = 6;  desc = "Vivid true-red flowers that hold their color well."; photo = $true }
    @{ id = "PLM-0008"; type = "Cutting"; color = "Pink"; size = "Small"; price = 6.00;  qty = 25; desc = "Compact dwarf variety, soft pink blossoms."; photo = $true }
    @{ id = "PLM-0009"; type = "Cutting"; color = "Red"; size = "Large"; price = 18.25; qty = 4;  desc = "Ruffled deep red petals with a velvety texture."; photo = $true }
    @{ id = "PLM-0010"; type = "Rooted Plant"; color = "Yellow/White"; size = "Small"; price = 15.00; qty = 10; desc = "Compact grower, no photo yet - fresh cutting just arrived."; photo = $false }
)

$tempDir = Join-Path $env:TEMP "plumeria-seed-images"
New-Item -ItemType Directory -Force -Path $tempDir | Out-Null

foreach ($item in $items) {
    $body = @{
        id = $item.id
        type = $item.type
        color = $item.color
        size = $item.size
        price = $item.price
        quantityAvailable = $item.qty
        description = $item.desc
    } | ConvertTo-Json

    $created = Invoke-RestMethod -Uri "$apiBase/api/inventory" -Method Post -ContentType "application/json" -Headers $headers -Body $body
    Write-Output "Created: $($created.id)"

    if ($item.photo) {
        $pngPath = Join-Path $tempDir "$($created.id).png"
        New-SolidColorPng -HexColor $colorSwatch[$item.color] -OutPath $pngPath

        Add-Type -AssemblyName System.Net.Http
        $client = New-Object System.Net.Http.HttpClient
        $client.DefaultRequestHeaders.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $token)
        $fileBytes = [System.IO.File]::ReadAllBytes($pngPath)
        $content = New-Object System.Net.Http.MultipartFormDataContent
        $byteContent = New-Object System.Net.Http.ByteArrayContent(,$fileBytes)
        $byteContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse("image/png")
        $content.Add($byteContent, "file", "$($item.id).png")
        $response = $client.PostAsync("$apiBase/api/inventory/$($created.id)/images", $content).Result
        if (-not $response.IsSuccessStatusCode) {
            Write-Warning "Image upload failed for $($item.id): $($response.StatusCode)"
        }
    }
}

Remove-Item -Recurse -Force $tempDir -ErrorAction SilentlyContinue

# A few sample pickup requests so the admin Reservations tab has content too - covering
# email-only, phone-only, and both, since a reservation now requires at least one.
$reservations = @(
    @{ itemId = "PLM-0001"; name = "Jane Customer"; phone = $null; email = "jane@example.com"; qty = 2; notes = "Can I pick up this weekend?" }
    @{ itemId = "PLM-0003"; name = "Mike Rivera"; phone = "555-0142"; email = $null; qty = 1; notes = "" }
    @{ itemId = "PLM-0006"; name = "Priya Shah"; phone = "555-0198"; email = "priya.shah@example.com"; qty = 3; notes = "Need them for a wedding on the 14th." }
)

foreach ($res in $reservations) {
    $body = @{
        inventoryItemId = $res.itemId
        customerName = $res.name
        customerPhone = $res.phone
        customerEmail = $res.email
        quantityRequested = $res.qty
        notes = $res.notes
    } | ConvertTo-Json
    Invoke-RestMethod -Uri "$apiBase/api/reservations" -Method Post -ContentType "application/json" -Body $body | Out-Null
    Write-Output "Reservation created for item $($res.itemId) ($($res.name))"
}

Write-Output ""
Write-Output "Done. $($items.Count) inventory items and $($reservations.Count) pickup requests seeded."
