# Build and run Docker container script

# 1. Run the preparation script first
Write-Host "Running preparation script..."
./prepare_docker.ps1

# 2. Check for all required files
$requiredFiles = @(
    "./bin/Debug/net9.0/TankServer.dll",
    "./bin/Debug/net9.0/TankServer.deps.json",
    "./bin/Debug/net9.0/TankServer.runtimeconfig.json",
    "./ProudNet_libs"
)

$allFilesExist = $true
foreach ($file in $requiredFiles) {
    if (-not (Test-Path $file)) {
        Write-Host "ERROR: Required file/directory not found: $file" -ForegroundColor Red
        $allFilesExist = $false
    }
}

if (-not $allFilesExist) {
    Write-Host "Missing required files. Please check error messages above." -ForegroundColor Red
    exit 1
}

# 3. Create a temporary directory for Docker context
$tempDir = ".\docker_build_context"
New-Item -ItemType Directory -Force -Path $tempDir | Out-Null
New-Item -ItemType Directory -Force -Path "$tempDir\bin\Debug\net9.0" | Out-Null

# 4. Copy all files to the temporary context
Write-Host "Copying files to Docker build context..."
Copy-Item ".\bin\Debug\net9.0\*" -Destination "$tempDir\bin\Debug\net9.0\" -Recurse
Copy-Item ".\ProudNet_libs\" -Destination "$tempDir\" -Recurse
Copy-Item ".\Dockerfile" -Destination "$tempDir\"

# 5. Build Docker image from the temporary context
Write-Host "Building Docker image from context: $tempDir"
Set-Location $tempDir
docker build -t tank-server .

# 6. Cleanup
Set-Location ..
Remove-Item -Path $tempDir -Recurse -Force

# 7. Run Docker container
Write-Host "Build complete! Run the container with: docker run -it --rm -p 33334:33334 tank-server"
$runContainer = Read-Host "Do you want to run the container now? (y/n)"
if ($runContainer -eq "y") {
    Write-Host "Running container..."
    docker run -it --rm -p 33334:33334 tank-server
} 