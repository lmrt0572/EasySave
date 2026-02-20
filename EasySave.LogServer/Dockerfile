# ===== BUILD STAGE =====
# Use the .NET SDK image to compile the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["EasySave.LogServer/EasySave.LogServer.csproj", "EasySave.LogServer/"]
RUN dotnet restore "EasySave.LogServer/EasySave.LogServer.csproj"

# Copy the rest of the source code and build
COPY . .
WORKDIR "/src/EasySave.LogServer"
RUN dotnet build "EasySave.LogServer.csproj" -c Release -o /app/build

# ===== PUBLISH STAGE =====
# Create the final production-ready binaries
FROM build AS publish
RUN dotnet publish "EasySave.LogServer.csproj" -c Release -o /app/publish

# ===== FINAL RUNTIME STAGE =====
# Use a lightweight runtime image for the final container
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# ===== VOLUME SETUP =====
# Ensure the log directory exists and has correct permissions
RUN mkdir -p /app/logs && chmod 777 /app/logs

EXPOSE 8080
ENTRYPOINT ["dotnet", "EasySave.LogServer.dll"]