# Stage 1: build & publish
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ego.sln .
COPY src/Ego.Domain/Ego.Domain.csproj src/Ego.Domain/
COPY src/Ego.Application/Ego.Application.csproj src/Ego.Application/
COPY src/Ego.Infrastructure/Ego.Infrastructure.csproj src/Ego.Infrastructure/
COPY src/Ego.Api/Ego.Api.csproj src/Ego.Api/
COPY tests/Ego.Application.Tests/Ego.Application.Tests.csproj tests/Ego.Application.Tests/

RUN dotnet restore ego.sln

COPY . .

RUN dotnet publish src/Ego.Api/Ego.Api.csproj -c Release -o /app/publish --no-restore

# Stage 2: runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

USER 1001:1001

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Ego.Api.dll"]
