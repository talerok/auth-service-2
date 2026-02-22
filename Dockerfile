FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Auth.sln", "./"]
COPY ["src/Auth.Api/Auth.Api.csproj", "src/Auth.Api/"]
COPY ["src/Auth.Application/Auth.Application.csproj", "src/Auth.Application/"]
COPY ["src/Auth.Domain/Auth.Domain.csproj", "src/Auth.Domain/"]
COPY ["src/Auth.Infrastructure/Auth.Infrastructure.csproj", "src/Auth.Infrastructure/"]
COPY ["src/Auth.Infrastructure.Integration/Auth.Infrastructure.Integration.csproj", "src/Auth.Infrastructure.Integration/"]
RUN dotnet restore "src/Auth.Api/Auth.Api.csproj"

COPY . .
RUN dotnet publish "src/Auth.Api/Auth.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .
EXPOSE 8080

ENTRYPOINT ["dotnet", "Auth.Api.dll"]
