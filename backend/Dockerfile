# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Install Docker CLI
RUN apt-get update && \
    apt-get install -y curl && \
    curl -fsSL https://get.docker.com | sh && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=build /app/publish .

# Expose port
EXPOSE 5159

# Run migrations and start app
ENTRYPOINT ["dotnet", "RusalProject.dll"]
