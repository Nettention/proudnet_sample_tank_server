# Windows container Dockerfile
FROM mcr.microsoft.com/dotnet/runtime:9.0-nanoserver-ltsc2022

WORKDIR /app

# Copy all application files
COPY . .

# Expose port
EXPOSE 33334

# Run the application
ENTRYPOINT ["dotnet", "TankServer.dll"]
