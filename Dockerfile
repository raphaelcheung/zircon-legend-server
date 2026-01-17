# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Zircon Server.sln", "./"]
COPY ["Server/Server.csproj", "Server/"]
COPY ["Library/Library/Library.csproj", "Library/Library/"]

# Restore dependencies
RUN dotnet restore "Zircon Server.sln"

# Copy all source code
COPY . .

# Build and publish
RUN dotnet publish "Server/Server.csproj" -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /zircon

# Copy published output
COPY --from=build /app/publish .

# Create necessary directories
RUN mkdir -p datas Map

# Expose ports: Game=7000, UserCount=3000, WebUI=7080
EXPOSE 7000 3000 7080

# Set entry point
ENTRYPOINT ["./Server"]
