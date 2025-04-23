# Docker build preparation script

# Create necessary directories
$ProudNetLibsDir = ".\ProudNet_libs"
$ProudNetLinuxLibsDir = "$ProudNetLibsDir\linux"
New-Item -ItemType Directory -Force -Path $ProudNetLibsDir
New-Item -ItemType Directory -Force -Path $ProudNetLinuxLibsDir

# Copy Windows native DLL files
Copy-Item "..\..\ProudNet\lib\DotNet\*.dll" -Destination $ProudNetLibsDir
Copy-Item "..\..\ProudNet\lib\DotNet\x64\*.dll" -Destination $ProudNetLibsDir

# Copy all DLL files from current build output directory
Copy-Item ".\bin\Debug\net9.0\*.dll" -Destination $ProudNetLibsDir

# Find and copy Linux library files
$linuxPaths = @(
    "..\..\ProudNet\lib\DotNet\x86_64-linux\Release",
    "..\..\ProudNet\lib\DotNet\x86_64-linux\Debug",
    "..\..\ProudNet\lib\DotNet\x86_64-linux",
    "..\..\ProudNet\lib\x86_64-linux\Release",
    "..\..\ProudNet\lib\x86_64-linux\Debug",
    "..\..\ProudNet\lib\x86_64-linux"
)

$foundLinuxLibs = $false

foreach ($path in $linuxPaths) {
    if (Test-Path $path) {
        Write-Host "Found Linux libraries at: $path"
        
        # Copy DLL and SO files
        if (Test-Path "$path\*.dll") {
            Copy-Item "$path\*.dll" -Destination $ProudNetLinuxLibsDir
            $foundLinuxLibs = $true
        }
        
        if (Test-Path "$path\*.so") {
            Copy-Item "$path\*.so" -Destination $ProudNetLinuxLibsDir
            $foundLinuxLibs = $true
        }
    }
}

# Search more locations - Find and copy all .so files in ProudNet
Get-ChildItem -Path "..\..\ProudNet" -Filter "*.so" -Recurse | ForEach-Object {
    Write-Host "Additional SO file found: $($_.FullName)"
    Copy-Item $_.FullName -Destination $ProudNetLinuxLibsDir
    $foundLinuxLibs = $true
}

# Display warnings
if (-not $foundLinuxLibs) {
    Write-Host "WARNING: Could not find Linux libraries. This may cause issues with Docker build."
    Write-Host "Please check if ProudNet supports Linux."
} else {
    # List copied Linux library files
    Write-Host "Copied Linux library files:"
    Get-ChildItem -Path $ProudNetLinuxLibsDir | ForEach-Object {
        Write-Host "  - $($_.Name)"
    }
}

Write-Host "Docker build preparation complete! Now run the following commands:"
Write-Host "docker build -t tank-server ."
Write-Host "docker run -it --rm -p 33334:33334 tank-server" 