# Build stage
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

# Copy everything and restore
COPY . . 
RUN dotnet restore

# Publish the project
RUN dotnet publish WasselniAPI.csproj -c Release -o /app/out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build /app/out .

# Expose port
EXPOSE 80

# Run the application
ENTRYPOINT ["dotnet", "WasselniAPI.dll"]
