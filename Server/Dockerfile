FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime

# Install required packages
RUN apt-get update && apt-get install -y \
    libssl3 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

# Copy all files from the build directory
COPY bin/Debug/net9.0/ ./
COPY ProudNet_libs/ ./libs/

# Copy all library files directly to the app directory
RUN ls -la /app/libs && \
    cp /app/libs/*.dll /app/ 2>/dev/null || true && \
    cp /app/libs/linux/*.so /app/ 2>/dev/null || true && \
    cp /app/libs/linux/Debug/*.so /app/ 2>/dev/null || true && \
    cp /app/libs/linux/Release/*.so /app/ 2>/dev/null || true && \
    cp /app/libs/x86_64-linux/Debug/*.so /app/ 2>/dev/null || true && \
    cp /app/libs/x86_64-linux/Release/*.so /app/ 2>/dev/null || true && \
    cp /app/libs/x86_64-linux/*.so /app/ 2>/dev/null || true && \
    ls -la /app && \
    echo "Contents of the app directory:"

# Open port
EXPOSE 33334

# Add troubleshooting command before running the actual app
CMD ls -la /app && echo "Starting application..." && dotnet TankServer.dll
