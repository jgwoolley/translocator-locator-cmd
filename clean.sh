docker run -w /app -v "$(pwd)":/app mcr.microsoft.com/dotnet/sdk:8.0 dotnet restore ZZCakeBuild/CakeBuild.csproj
docker run -w /app -v "$(pwd)":/app mcr.microsoft.com/dotnet/sdk:8.0 dotnet clean /app/ZZCakeBuild/CakeBuild.csproj
