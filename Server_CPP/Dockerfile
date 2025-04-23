FROM ubuntu:18.04

# 비대화형 설치 설정
ENV DEBIAN_FRONTEND=noninteractive
ENV TZ=Asia/Seoul

# 기본 도구 및 종속성 설치
RUN apt-get update && apt-get install -y \
    build-essential \
    cmake \
    git \
    libssl1.0-dev \
    libcurl4-gnutls-dev \
    tzdata \
    inotify-tools \
    unixodbc \
    unixodbc-dev \
    openssl \
    && rm -rf /var/lib/apt/lists/*

# 작업 디렉토리 설정
WORKDIR /app

# 필요한 디렉토리만 복사
# Server_CPP 디렉토리 복사
COPY tank_server/Server_CPP/src /app/src/
COPY tank_server/Server_CPP/include /app/include/
COPY tank_server/Server_CPP/CMakeLists.txt /app/CMakeLists.txt


# Common 디렉토리 복사 (필수 파일만)
COPY tank_server/Common/Vars.cpp /app/Common/Vars.cpp
COPY tank_server/Common/Vars.h /app/Common/Vars.h

# ProudNet 라이브러리 디렉토리 복사 (필수 파일만)
COPY ProudNet/include /app/ProudNet/include/
COPY ProudNet/lib/x86_x64-linux /app/ProudNet/lib/x86_x64-linux/
COPY ProudNet/util/PNLicenseAuth /app/ProudNet/util/
COPY tank_server/licensekey.txt /app/licensekey.txt

# 라이센스 인증 시도 (절대 경로 사용)
RUN chmod +x /app/ProudNet/util/PNLicenseAuth && \
    echo "시스템 정보:" && \
    uname -a && \
    echo "현재 디렉토리:" && \
    pwd && \
    ls -la /app/ProudNet/util/ && \
    /app/ProudNet/util/PNLicenseAuth /app/licensekey.txt || true

# 복사된 파일 확인
RUN echo "복사된 파일 구조:" && \
    ls -la /app && \
    echo "Common 디렉토리:" && \
    ls -la /app/Common/ || echo "Common 디렉토리 복사 실패" && \
    echo "ProudNet 디렉토리:" && \
    ls -la /app/ProudNet/ || echo "ProudNet 디렉토리 복사 실패" && \
    echo "ProudNet/lib 디렉토리:" && \
    ls -la /app/ProudNet/lib/ || echo "ProudNet/lib 디렉토리 없음" && \
    echo "ProudNet/lib/x86_x64-linux 디렉토리:" && \
    ls -la /app/ProudNet/lib/x86_x64-linux/ || echo "x86_x64-linux 디렉토리 없음"

# 포트 노출
EXPOSE 33334/tcp
EXPOSE 33335/tcp

# 빌드 스크립트 생성
RUN echo '#!/bin/bash \n\
echo "서버 빌드 시작..." \n\
\n\
# 필수 파일 체크 함수 \n\
check_file() { \n\
  if [ ! -f "$1" ]; then \n\
    echo "파일을 찾을 수 없습니다: $1" \n\
    echo "파일이 올바르게 복사되었는지 확인하세요." \n\
    return 1 \n\
  else \n\
    echo "파일 확인됨: $1" \n\
    return 0 \n\
  fi \n\
} \n\
\n\
# 필수 디렉토리 체크 함수 \n\
check_dir() { \n\
  if [ ! -d "$1" ]; then \n\
    echo "디렉토리를 찾을 수 없습니다: $1" \n\
    echo "디렉토리가 올바르게 복사되었는지 확인하세요." \n\
    return 1 \n\
  else \n\
    echo "디렉토리 확인됨: $1" \n\
    return 0 \n\
  fi \n\
} \n\
\n\
# 필수 파일 및 디렉토리 확인 \n\
if ! check_file "/app/Common/Vars.cpp" || ! check_file "/app/Common/Vars.h"; then \n\
  echo "Common 디렉토리 내용:" \n\
  ls -la /app/Common || true \n\
  echo "계속 실행하기 위해 오류를 무시하고 진행합니다." \n\
fi \n\
\n\
if ! check_dir "/app/ProudNet/lib/x86_x64-linux"; then \n\
  echo "ProudNet 디렉토리 구조:" \n\
  ls -la /app/ProudNet || true \n\
  ls -la /app/ProudNet/lib || true \n\
  echo "계속 실행하기 위해 오류를 무시하고 진행합니다." \n\
fi \n\
\n\
# ProudNet 라이센스 확인 \n\
LICENSE_VALID=false \n\
if [ -f "/app/ProudNet/util/PNLicenseAuth" ]; then \n\
  echo "ProudNet 라이센스 확인 중..." \n\
  echo "라이센스 파일 정보:" \n\
  ls -la /app/ProudNet/util/ \n\
  ls -la /app/licensekey.txt \n\
  LICENSE_OUTPUT=$(/app/ProudNet/util/PNLicenseAuth /app/licensekey.txt) \n\
  LICENSE_STATUS=$? \n\
  \n\
  # 라이센스 출력 결과 표시 \n\
  echo "$LICENSE_OUTPUT" \n\
  \n\
  # Validity 상태 확인 \n\
  if echo "$LICENSE_OUTPUT" | grep -q "Validity : OK"; then \n\
    LICENSE_VALID=true \n\
    echo -e "\n\033[32m[라이센스 확인 성공] ProudNet 라이센스가 유효합니다.\033[0m" \n\
    # 라이센스 만료일 추출 및 표시 \n\
    EXPIRY_DATE=$(echo "$LICENSE_OUTPUT" | grep "ExpiryDate" | awk -F ":" "{print \$2}" | xargs) \n\
    echo "라이센스 만료일: $EXPIRY_DATE" \n\
  else \n\
    echo -e "\n\033[31m[라이센스 확인 실패] ProudNet 라이센스가 유효하지 않습니다.\033[0m" \n\
    echo "라이센스 오류 코드: $LICENSE_STATUS" \n\
    echo "필요시 수동으로 라이센스 키를 등록하세요: /app/ProudNet/util/PNLicenseAuth /app/licensekey.txt" \n\
  fi \n\
else \n\
  echo "Warning: ProudNet 라이센스 확인 프로그램을 찾을 수 없습니다." \n\
fi \n\
\n\
# ODBC 설치 확인 \n\
echo "ODBC 드라이버 정보:" \n\
odbcinst -j || echo "ODBC 정보를 가져올 수 없습니다." \n\
\n\
# 빌드 및 실행 \n\
mkdir -p build \n\
cd build \n\
cmake .. || echo "CMake 실행 중 오류 발생, 그러나 계속 진행합니다." \n\
make || echo "빌드 중 오류 발생, 그러나 계속 진행합니다." \n\
echo "빌드 완료, 서버 시작 시도..." \n\
\n\
# 실행 파일이 존재하면 실행, 그렇지 않으면 메시지 출력 \n\
if [ -f "/app/build/TankServer" ]; then \n\
  if [ "$LICENSE_VALID" = true ]; then \n\
    echo "유효한 라이센스로 서버를 시작합니다." \n\
    /app/build/TankServer || echo "서버 실행 중 오류가 발생했습니다." \n\
  else \n\
    echo "라이센스가 유효하지 않아 서버를 시작하지 않습니다." \n\
    echo "서버를 실행하려면 유효한 라이센스가 필요합니다." \n\
  fi \n\
else \n\
  echo "서버 실행 파일을 찾을 수 없습니다. 빌드에 실패했습니다." \n\
fi \n\
\n\
# 컨테이너가 종료되지 않도록 유지 \n\
echo "컨테이너를 계속 실행 상태로 유지합니다. Ctrl+C로 중지할 수 있습니다." \n\
echo "문제를 디버깅하기 위해 다른 터미널에서 'docker exec -it 컨테이너ID /bin/bash'를 실행하세요." \n\
\n\
# 컨테이너 유지를 위한 무한 루프 \n\
while true; do \n\
  sleep 60 \n\
  echo "컨테이너 실행 중... (1분마다 메시지 출력)" \n\
done' > /app/start.sh

# 스크립트에 실행 권한 부여
RUN chmod +x /app/start.sh

# 서버 시작 (컨테이너가 계속 실행되도록 함)
CMD ["/bin/bash", "/app/start.sh"] 