FROM ubuntu:24.04

# Non-interactive installation setup
ENV DEBIAN_FRONTEND=noninteractive
ENV TZ=Asia/Seoul

# Install basic tools and dependencies
RUN apt-get update && apt-get install -y \
    build-essential \
    cmake \
    git \
    libssl-dev \
    libssl3 \
    libcurl4-openssl-dev \
    tzdata \
    inotify-tools \
    unixodbc \
    unixodbc-dev \
    openssl \
    pkg-config \
    && rm -rf /var/lib/apt/lists/*

# Set working directory
WORKDIR /app

# Copy only necessary directories
# Copy Server_CPP directory
COPY tank_server/Server_CPP/src /app/src/
COPY tank_server/Server_CPP/include /app/include/
COPY tank_server/Server_CPP/CMakeLists.txt /app/CMakeLists.txt


# Copy Common directory (essential files only)
COPY tank_server/Common/Vars.cpp /app/Common/Vars.cpp
COPY tank_server/Common/Vars.h /app/Common/Vars.h

# Copy ProudNet library directory (essential files only)
COPY ProudNet/include /app/ProudNet/include/
COPY ProudNet/lib/x86_x64-linux /app/ProudNet/lib/x86_x64-linux/
COPY ProudNet/util/PNLicenseAuth /app/ProudNet/util/

# Copy license key file only if it contains a valid key
RUN if [ -f "tank_server/licensekey.txt" ] && \
    [ "$(cat tank_server/licensekey.txt)" != "PUT_YOUR_PROUDNET_LICENSE_KEY_HERE" ]; then \
    cp tank_server/licensekey.txt /app/; \
fi

# Try license authentication (using absolute path)
RUN chmod +x /app/ProudNet/util/PNLicenseAuth && \
    echo "System information:" && \
    uname -a && \
    echo "Current directory:" && \
    pwd && \
    ls -la /app/ProudNet/util/ && \
    if [ -f "/app/licensekey.txt" ]; then \
        /app/ProudNet/util/PNLicenseAuth /app/licensekey.txt || true; \
    else \
        echo "License key file not found. Build will continue."; \
    fi

# Check copied files
RUN echo "Copied file structure:" && \
    ls -la /app && \
    echo "Common directory:" && \
    ls -la /app/Common/ || echo "Failed to copy Common directory" && \
    echo "ProudNet directory:" && \
    ls -la /app/ProudNet/ || echo "Failed to copy ProudNet directory" && \
    echo "ProudNet/lib directory:" && \
    ls -la /app/ProudNet/lib/ || echo "ProudNet/lib directory not found" && \
    echo "ProudNet/lib/x86_x64-linux directory:" && \
    ls -la /app/ProudNet/lib/x86_x64-linux/ || echo "x86_x64-linux directory not found"

# Expose ports
EXPOSE 33334/tcp
EXPOSE 33335/tcp

# Build script generation
RUN echo '#!/bin/bash \n\
echo "Server build starting..." \n\
\n\
# Required file check function \n\
check_file() { \n\
  if [ ! -f "$1" ]; then \n\
    echo "File not found: $1" \n\
    echo "Please check if the file was copied correctly." \n\
    return 1 \n\
  else \n\
    echo "File found: $1" \n\
    return 0 \n\
  fi \n\
} \n\
\n\
# Required directory check function \n\
check_dir() { \n\
  if [ ! -d "$1" ]; then \n\
    echo "Directory not found: $1" \n\
    echo "Please check if the directory was copied correctly." \n\
    return 1 \n\
  else \n\
    echo "Directory found: $1" \n\
    return 0 \n\
  fi \n\
} \n\
\n\
# Required file and directory check \n\
if ! check_file "/app/Common/Vars.cpp" || ! check_file "/app/Common/Vars.h"; then \n\
  echo "Common directory contents:" \n\
  ls -la /app/Common || true \n\
  echo "Continuing execution despite error." \n\
fi \n\
\n\
if ! check_dir "/app/ProudNet/lib/x86_x64-linux"; then \n\
  echo "ProudNet directory structure:" \n\
  ls -la /app/ProudNet || true \n\
  ls -la /app/ProudNet/lib || true \n\
  echo "Continuing execution despite error." \n\
fi \n\
\n\
# ProudNet license check \n\
LICENSE_VALID=false \n\
if [ -f "/app/ProudNet/util/PNLicenseAuth" ]; then \n\
  echo "ProudNet license check in progress..." \n\
  echo "License file information:" \n\
  ls -la /app/ProudNet/util/ \n\
  if [ -f "/app/licensekey.txt" ]; then \n\
    ls -la /app/licensekey.txt \n\
    LICENSE_OUTPUT=$(/app/ProudNet/util/PNLicenseAuth /app/licensekey.txt) \n\
  LICENSE_STATUS=$? \n\
  \n\
  # License output result display \n\
  echo "$LICENSE_OUTPUT" \n\
  \n\
  # Validity status check \n\
  if echo "$LICENSE_OUTPUT" | grep -q "Validity : OK"; then \n\
    LICENSE_VALID=true \n\
    echo -e "\n\033[32m[License check success] ProudNet license is valid.\033[0m" \n\
    # License expiry date extraction and display \n\
    EXPIRY_DATE=$(echo "$LICENSE_OUTPUT" | grep "ExpiryDate" | awk -F ":" "{print \$2}" | xargs) \n\
    echo "License expiry date: $EXPIRY_DATE" \n\
  else \n\
    echo -e "\n\033[31m[License check failed] ProudNet license is invalid.\033[0m" \n\
    echo "License error code: $LICENSE_STATUS" \n\
    echo "If needed, manually register the license key: /app/ProudNet/util/PNLicenseAuth /app/licensekey.txt" \n\
  fi \n\
  else \n\
    echo "License key file not found: /app/licensekey.txt" \n\
    echo "Building without ProudNet license." \n\
  fi \n\
else \n\
  echo "Warning: Could not find ProudNet license check program." \n\
fi \n\
\n\
# ODBC installation check \n\
echo "ODBC driver information:" \n\
odbcinst -j || echo "Could not get ODBC information." \n\
\n\
# Build and run \n\
mkdir -p build \n\
cd build \n\
cmake .. || echo "CMake execution error, but will continue." \n\
make || echo "Build error, but will continue." \n\
echo "Build completed, attempting to start server..." \n\
\n\
# If executable exists, run, otherwise print message \n\
if [ -f "/app/build/TankServer" ]; then \n\
  if [ "$LICENSE_VALID" = true ]; then \n\
    echo "Starting server with valid license." \n\
  else \n\
    echo "Starting server without license (50 CCU limitation mode)." \n\
  fi \n\
  /app/build/TankServer || echo "Server execution error occurred." \n\
else \n\
  echo "Server executable not found. Build failed." \n\
fi \n\
\n\
# Keep container from exiting \n\
echo "Keeping container running. Ctrl+C to stop." \n\
echo "To debug, run 'docker exec -it containerID /bin/bash' in another terminal." \n\
\n\
# Infinite loop for container retention \n\
while true; do \n\
  sleep 60 \n\
  echo "Container running... (1 minute message)" \n\
done' > /app/start.sh

# Grant execution permission to script
RUN chmod +x /app/start.sh

# Start server (keep container running)
CMD ["/bin/bash", "/app/start.sh"] 