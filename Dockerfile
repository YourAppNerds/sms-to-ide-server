# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the csproj and restore dependencies
COPY ["sms-to-ide-server.csproj", "./"]
RUN dotnet restore "sms-to-ide-server.csproj"

# Copy the remaining source code and build the application
COPY . .
RUN dotnet publish "sms-to-ide-server.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# Ensure we listen on port 8080 as requested
ENV ASPNETCORE_URLS=http://+:8080

# Create a directory for persistent data (so SQLite DB survives restarts)
RUN mkdir -p /app/data

# Copy the compiled application from the build stage
COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "sms-to-ide-server.dll"]
