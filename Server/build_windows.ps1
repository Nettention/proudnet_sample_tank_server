# Build and run Docker container script for Windows containers

# 1. Run the preparation script first
Write-Host "Running preparation script..."
./prepare_docker.ps1

# 2. Create a temporary directory for Docker context
$tempDir = ".\docker_build_context"
New-Item -ItemType Directory -Force -Path $tempDir | Out-Null
New-Item -ItemType Directory -Force -Path "$tempDir\bin\Debug\net9.0" | Out-Null

# 3. Copy all files to the temporary context
Write-Host "Copying files to Docker build context..."
Copy-Item ".\bin\Debug\net9.0\*" -Destination "$tempDir\bin\Debug\net9.0\" -Recurse
Copy-Item ".\ProudNet_libs\" -Destination "$tempDir\" -Recurse
Copy-Item ".\Dockerfile.windows" -Destination "$tempDir\Dockerfile" -Force

# 4. Build Docker image from the temporary context
Write-Host "Building Windows container image from context: $tempDir"
Set-Location $tempDir
docker build -t tank-server-windows .

# 5. Cleanup
Set-Location ..
Remove-Item -Path $tempDir -Recurse -Force

# 6. Run Docker container
Write-Host "Build complete! Run the Windows container with: docker run -it --rm -p 33334:33334 tank-server-windows"
Write-Host "Note: You must switch Docker to Windows container mode first:"
Write-Host "Right-click Docker Desktop icon > Switch to Windows containers..." 