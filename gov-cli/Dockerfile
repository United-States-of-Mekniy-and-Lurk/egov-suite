FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY gov-cli.slnx ./
COPY Gov.Cli/Gov.Cli.csproj Gov.Cli/
COPY Gov.Cli.Tests/Gov.Cli.Tests.csproj Gov.Cli.Tests/

RUN dotnet restore Gov.Cli/Gov.Cli.csproj

COPY . .
RUN dotnet publish Gov.Cli/Gov.Cli.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

RUN addgroup --system --gid 1001 app && adduser --system --uid 1001 --ingroup app app

COPY --from=build /app/publish .

USER app
ENTRYPOINT ["dotnet", "Gov.Cli.dll"]
