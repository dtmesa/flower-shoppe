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

# No id here - the API derives it from type+color+size (e.g. "RYM") and rejects a second item
# with the same combo, so every row below must be a distinct Type/Color/Size combination.
# $item.id is filled in after creation, once the auto-generated tag is known.
$items = @(
    @{ type = "Rooted Plant"; color = "Yellow/White"; size = "Medium"; price = 24.99; qty = 8;  desc = "Fragrant classic yellow-and-white plumeria, easy to grow."; photo = $true }
    @{ type = "Cutting"; color = "Red"; size = "Small"; price = 12.50; qty = 15; desc = "Deep red blooms with a spicy-sweet fragrance."; photo = $true }
    @{ type = "Rooted Plant"; color = "Yellow/White"; size = "Large"; price = 34.00; qty = 3;  desc = "Bold golden-yellow flowers on a vigorous grower."; photo = $true }
    @{ type = "Rooted Plant"; color = "Pink"; size = "Medium"; price = 28.75; qty = 0;  desc = "Swirled pink and white petals, currently sold out."; photo = $true }
    @{ type = "Cutting"; color = "Yellow/White"; size = "Small"; price = 9.99;  qty = 20; desc = "The classic backyard plumeria - reliable bloomer."; photo = $true }
    @{ type = "Rooted Plant"; color = "Pink"; size = "Large"; price = 45.00; qty = 5;  desc = "Rich rose-pink blooms, a favorite for leis."; photo = $true }
    @{ type = "Rooted Plant"; color = "Red"; size = "Medium"; price = 32.50; qty = 6;  desc = "Vivid true-red flowers that hold their color well."; photo = $true }
    @{ type = "Cutting"; color = "Pink"; size = "Small"; price = 6.00;  qty = 25; desc = "Compact dwarf variety, soft pink blossoms."; photo = $true }
    @{ type = "Cutting"; color = "Red"; size = "Large"; price = 18.25; qty = 4;  desc = "Ruffled deep red petals with a velvety texture."; photo = $true }
    @{ type = "Rooted Plant"; color = "Yellow/White"; size = "Small"; price = 15.00; qty = 10; desc = "Compact grower, no photo yet - fresh cutting just arrived."; photo = $false }
)

$tempDir = Join-Path $env:TEMP "plumeria-seed-images"
New-Item -ItemType Directory -Force -Path $tempDir | Out-Null

foreach ($item in $items) {
    $body = @{
        type = $item.type
        color = $item.color
        size = $item.size
        price = $item.price
        quantityTotal = $item.qty
        description = $item.desc
    } | ConvertTo-Json

    $created = Invoke-RestMethod -Uri "$apiBase/api/inventory" -Method Post -ContentType "application/json" -Headers $headers -Body $body
    $item.id = $created.id
    Write-Output "Created: $($created.id) ($($item.type) / $($item.color) / $($item.size))"

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
# email-only, phone-only, and both, since a request now requires at least one. Referenced by array
# index rather than a fixed ID string, since the actual ID is only known after creation. Priya's
# request bundles two different items under one submission, exercising the grouped-cart shape.
$pickupRequests = @(
    @{ name = "Jane Customer"; phone = $null; email = "jane@example.com"; notes = "Can I pick up this weekend?"; items = @(@{ itemId = $items[0].id; qty = 2 }) }
    @{ name = "Mike Rivera"; phone = "(555) 014-2000"; email = $null; notes = ""; items = @(@{ itemId = $items[2].id; qty = 1 }) }
    @{ name = "Priya Shah"; phone = "(555) 019-8000"; email = "priya.shah@example.com"; notes = "Need them for a wedding on the 14th."; items = @(@{ itemId = $items[5].id; qty = 3 }, @{ itemId = $items[6].id; qty = 2 }) }
)

foreach ($req in $pickupRequests) {
    $body = @{
        customerName = $req.name
        customerPhone = $req.phone
        customerEmail = $req.email
        notes = $req.notes
        # @(...) forces an array even when there's only one line item - PowerShell's pipeline
        # otherwise unwraps a single ForEach-Object result to a bare object, which would serialize
        # as a JSON object instead of a one-element array and fail model binding on the backend.
        items = @($req.items | ForEach-Object { @{ inventoryItemId = $_.itemId; quantityRequested = $_.qty } })
    } | ConvertTo-Json -Depth 5
    Invoke-RestMethod -Uri "$apiBase/api/reservations" -Method Post -ContentType "application/json" -Body $body | Out-Null
    Write-Output "Pickup request created for $($req.name) ($($req.items.Count) item(s))"
}

Write-Output ""
Write-Output "Done. $($items.Count) inventory items and $($pickupRequests.Count) pickup requests seeded."
