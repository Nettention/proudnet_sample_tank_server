# Simple Windows Container Build Script
# Uses ASCII characters only to avoid encoding issues

# Check current directory
$currentDir = Get-Location
Write-Host "Current directory: $currentDir" -ForegroundColor Cyan

# 1. Check Windows container mode
Write-Host "Checking Docker mode..." -ForegroundColor Cyan
$dockerInfo = docker version --format '{{.Server.Os}}' 2>$null
if ($dockerInfo -ne "windows") {
    Write-Host "WARNING: Docker is not in Windows container mode!" -ForegroundColor Red
    Write-Host "Please select 'Switch to Windows containers...' from Docker Desktop tray icon" -ForegroundColor Yellow
    $continue = Read-Host "Continue anyway? (y/n)"
    if ($continue -ne "y") {
        exit
    }
}

# 2. Clean Docker system
Write-Host "Cleaning Docker system..." -ForegroundColor Cyan
docker system prune -f

# 3. Create Dockerfile
Write-Host "Creating Dockerfile..." -ForegroundColor Cyan
@"
# Windows container Dockerfile
FROM mcr.microsoft.com/dotnet/runtime:9.0-nanoserver-ltsc2022

WORKDIR /app

# Copy all application files
COPY . .

# Expose port
EXPOSE 33334

# Run the application
ENTRYPOINT ["dotnet", "TankServer.dll"]
"@ | Out-File -FilePath "Dockerfile.win" -Encoding ascii

# 4. Prepare build directory
$buildDir = ".\win_build"
if (Test-Path $buildDir) {
    Remove-Item -Path $buildDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $buildDir | Out-Null

# 5. Copy necessary files
Write-Host "Copying files..." -ForegroundColor Cyan
Copy-Item ".\bin\Debug\net9.0\*" -Destination $buildDir -Recurse
Copy-Item ".\ProudNet_libs\*.dll" -Destination $buildDir
Copy-Item "Dockerfile.win" -Destination "$buildDir\Dockerfile"

# 6. Build Docker image
Write-Host "Building Docker image..." -ForegroundColor Cyan
Set-Location $buildDir
docker build -t tank-server-win .

# 7. Return to original directory
Set-Location $currentDir

# 8. Check result
$imageExists = docker images tank-server-win -q
if ($imageExists) {
    Write-Host "Build SUCCESS! Run container with:" -ForegroundColor Green
    Write-Host "docker run -it --rm -p 33334:33334 tank-server-win" -ForegroundColor Yellow
    
    # Run container?
    $runContainer = Read-Host "Run container now? (y/n)"
    if ($runContainer -eq "y") {
        docker run -it --rm -p 33334:33334 tank-server-win
    }
} else {
    Write-Host "Build FAILED. Check error messages above." -ForegroundColor Red
} 