param(
    [Parameter(Mandatory = $true)]
    [string]$ArchivePath,
    [string]$RepositoryUrl = "https://github.com/viktor132607/MergeGame.git"
)

$ErrorActionPreference = "Stop"
$expectedHash = "f06f372d43e00bcb083ffcf1923b77f0c57840727522f2ae7cb4151070a1cab9"
$archive = (Resolve-Path -LiteralPath $ArchivePath).Path
$hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $archive).Hash.ToLowerInvariant()
if ($hash -ne $expectedHash) {
    throw "Incorrect ZIP: SHA256 $hash. Expected the verified 125-PNG archive."
}

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    throw "Git must be installed and authenticated with GitHub."
}

$work = Join-Path ([System.IO.Path]::GetTempPath()) ("MergeHarvest_Import_" + [guid]::NewGuid().ToString("N"))
$checkout = Join-Path $work "MergeGame"
$extracted = Join-Path $work "Extracted"
$archiveName = "MergeHarvest_125_Spawners_25_Categories.zip"

New-Item -ItemType Directory -Force -Path $work, $extracted | Out-Null
try {
    Expand-Archive -LiteralPath $archive -DestinationPath $extracted -Force
    $files = @(Get-ChildItem -LiteralPath $extracted -Recurse -File -Filter "*.png")
    if ($files.Count -ne 125) {
        throw "Archive must contain exactly 125 PNGs, found $($files.Count)."
    }

    $categories = @($files | ForEach-Object { $_.DirectoryName } | Sort-Object -Unique)
    if ($categories.Count -ne 25) {
        throw "Archive must contain exactly 25 category directories, found $($categories.Count)."
    }
    foreach ($category in $categories) {
        $categoryCount = @($files | Where-Object { $_.DirectoryName -eq $category }).Count
        if ($categoryCount -ne 5) {
            throw "Expected 5 levels per category, got $categoryCount in $category."
        }
    }

    foreach ($file in $files) {
        $stream = [System.IO.File]::OpenRead($file.FullName)
        try {
            $reader = New-Object System.IO.BinaryReader($stream)
            $head = $reader.ReadBytes(24)
            $sig = [byte[]]@(137,80,78,71,13,10,26,10)
            for ($i = 0; $i -lt 8; $i++) {
                if ($head[$i] -ne $sig[$i]) { throw "Invalid PNG: $($file.Name)" }
            }
            $width = ([int]$head[16] -shl 24) -bor ([int]$head[17] -shl 16) -bor ([int]$head[18] -shl 8) -bor [int]$head[19]
            $height = ([int]$head[20] -shl 24) -bor ([int]$head[21] -shl 16) -bor ([int]$head[22] -shl 8) -bor [int]$head[23]
            if ($width -ne 724 -or $height -ne 724) {
                throw "Unexpected image dimensions in $($file.Name): $($width)x$($height)."
            }
        } finally {
            $stream.Dispose()
        }
    }

    & git clone --depth 1 --branch main $RepositoryUrl $checkout
    if ($LASTEXITCODE -ne 0) { throw "Failed to clone GitHub repository." }

    $destination = Join-Path $checkout "Assets\Spawners\Collections"
    $archives = Join-Path $checkout "Assets\Spawners\Archives"
    New-Item -ItemType Directory -Force -Path $destination, $archives | Out-Null
    Copy-Item -LiteralPath $archive -Destination (Join-Path $archives $archiveName) -Force
    Copy-Item -Path (Join-Path $extracted "*") -Destination $destination -Recurse -Force

    & git -C $checkout add -- "Assets/Spawners/Collections" "Assets/Spawners/Archives"
    if ($LASTEXITCODE -ne 0) { throw "Git add failed." }
    & git -C $checkout diff --cached --quiet
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Already imported. No changes."
        exit 0
    }

    & git -C $checkout commit -m "Import original ZIP and all 125 extracted spawner PNGs"
    if ($LASTEXITCODE -ne 0) { throw "Git commit failed." }
    & git -C $checkout push origin main
    if ($LASTEXITCODE -ne 0) { throw "Git push failed. Check GitHub authentication and branch access." }

    Write-Host "SUCCESS: Original 125-PNG ZIP and extracted files committed to main."
} finally {
    if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force }
}
