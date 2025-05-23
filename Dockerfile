# Use the .NET 8.0 SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution and project files
COPY WebApplication1/WebApplication1.sln .
COPY WebApplication1/WebApplication1/WasselniAPI.csproj ./WebApplication1/

# Restore dependencies from the solution directory
RUN dotnet restore WebApplication1.sln

# Copy the entire source and build
COPY . .
WORKDIR /app/WebApplication1/WebApplication1
RUN dotnet publish -c Release -o /app/publish

# Use the .NET 8.0 ASP.NET runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WasselniAPI.dll"]