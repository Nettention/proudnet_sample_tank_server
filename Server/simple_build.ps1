# 간소화된 Windows 컨테이너 빌드 스크립트

# 현재 디렉토리 확인
$currentDir = Get-Location
Write-Host "Current directory: $currentDir" -ForegroundColor Cyan

# 1. Windows 컨테이너 모드 확인
$dockerInfo = docker version --format '{{.Server.Os}}' 2>$null
if ($dockerInfo -ne "windows") {
    Write-Host "경고: Docker가 Windows 컨테이너 모드가 아닙니다!" -ForegroundColor Red
    Write-Host "Docker Desktop 트레이 아이콘 > Switch to Windows containers... 를 선택하세요" -ForegroundColor Yellow
    $continue = Read-Host "계속 진행하시겠습니까? (y/n)"
    if ($continue -ne "y") {
        exit
    }
}

# 2. Docker 정리
Write-Host "Docker 정리 중..." -ForegroundColor Cyan
docker system prune -f

# 3. 기본 이미지 가져오기
Write-Host "기본 Windows 이미지 가져오는 중..." -ForegroundColor Cyan
docker pull mcr.microsoft.com/dotnet/runtime:9.0-nanoserver-ltsc2022

# 4. 간단한 Dockerfile 생성
$dockerfile = @"
FROM mcr.microsoft.com/dotnet/runtime:9.0-nanoserver-ltsc2022
WORKDIR /app
COPY . .
EXPOSE 33334
ENTRYPOINT ["dotnet", "TankServer.dll"]
"@

$dockerfile | Out-File -FilePath "Dockerfile.simple" -Encoding utf8

# 5. 빌드 디렉토리 준비
$buildDir = ".\simple_build"
New-Item -ItemType Directory -Force -Path $buildDir | Out-Null

# 6. 필요한 파일 복사
Write-Host "필요한 파일 복사 중..." -ForegroundColor Cyan
Copy-Item ".\bin\Debug\net9.0\*" -Destination $buildDir -Recurse
Copy-Item ".\ProudNet_libs\*.dll" -Destination $buildDir
Copy-Item ".\Dockerfile.simple" -Destination "$buildDir\Dockerfile"

# 7. Docker 이미지 빌드
Write-Host "Docker 이미지 빌드 중..." -ForegroundColor Cyan
Set-Location $buildDir
docker build -t tank-server-windows-simple .

# 8. 원래 디렉토리로 돌아가기
Set-Location $currentDir

# 9. 결과 확인
$imageExists = docker images tank-server-windows-simple -q
if ($imageExists) {
    Write-Host "빌드 성공! 다음 명령으로 컨테이너를 실행하세요:" -ForegroundColor Green
    Write-Host "docker run -it --rm -p 33334:33334 tank-server-windows-simple" -ForegroundColor Yellow
    
    # 컨테이너 실행 여부
    $runContainer = Read-Host "컨테이너를 지금 실행하시겠습니까? (y/n)"
    if ($runContainer -eq "y") {
        docker run -it --rm -p 33334:33334 tank-server-windows-simple
    }
} else {
    Write-Host "빌드 실패. 오류 메시지를 확인하세요." -ForegroundColor Red
} 