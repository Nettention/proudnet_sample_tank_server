# Docker 재설정 및 정리 스크립트

Write-Host "Stopping Docker service..." -ForegroundColor Cyan
Stop-Service docker

Write-Host "Cleaning up Docker data..." -ForegroundColor Cyan
# Docker 이미지, 컨테이너, 볼륨 정리
docker system prune -af

# 디스크 공간 확보
Write-Host "Removing Docker data directory..." -ForegroundColor Cyan
Remove-Item "$env:ProgramData\Docker\windowsfilter" -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "Starting Docker service..." -ForegroundColor Cyan
Start-Service docker

Write-Host "Docker reset complete. Please ensure Docker is running in Windows container mode." -ForegroundColor Green
Write-Host "You can verify by running: docker version" -ForegroundColor Yellow

# Windows 컨테이너 모드 확인
$dockerInfo = docker version --format '{{.Server.Os}}'
if ($dockerInfo -eq "windows") {
    Write-Host "Docker is running in Windows container mode." -ForegroundColor Green
} else {
    Write-Host "Docker is NOT running in Windows container mode. Please switch to Windows containers." -ForegroundColor Red
    Write-Host "Right-click Docker Desktop icon > Switch to Windows containers..." -ForegroundColor Yellow
}

# 기본 이미지 가져오기 테스트
Write-Host "Testing base image pull..." -ForegroundColor Cyan
docker pull mcr.microsoft.com/dotnet/runtime:9.0-nanoserver-ltsc2022

Write-Host "Reset complete. Now try building your Windows container again." -ForegroundColor Green 