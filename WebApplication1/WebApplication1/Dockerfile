# Use the official .NET SDK image for building the app
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.sln .
COPY WebApplication1/*.csproj ./WebApplication1/
RUN dotnet restore

# Copy the entire source and build
COPY . .
WORKDIR /app/WebApplication1
RUN dotnet publish -c Release -o out

# Use the ASP.NET runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build /app/WebApplication1/out .

# Expose the port your app runs on
EXPOSE 80

# Set the entrypoint
ENTRYPOINT ["dotnet", "WebApplication1.dll"]
