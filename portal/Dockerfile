FROM mcr.microsoft.com/dotnet/sdk:10.0 AS catalog
WORKDIR /src
COPY gov-cli/Gov.Cli/Gov.Cli.csproj gov-cli/Gov.Cli/
RUN dotnet restore gov-cli/Gov.Cli/Gov.Cli.csproj
COPY gov-cli/Gov.Cli/ gov-cli/Gov.Cli/
COPY sal/citizen-service.gov.yaml sal/
COPY portal/portal.gov.yaml portal/
RUN dotnet run --project gov-cli/Gov.Cli/Gov.Cli.csproj --no-restore -- catalog . > /services.json

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY portal/src/GovernmentPortal.Web/GovernmentPortal.Web.csproj portal/src/GovernmentPortal.Web/
COPY design-system/MKLU-Flag-Small.svg design-system/
COPY design-system/manticore.svg design-system/
RUN dotnet restore portal/src/GovernmentPortal.Web/GovernmentPortal.Web.csproj
COPY portal/src/GovernmentPortal.Web/ portal/src/GovernmentPortal.Web/
RUN dotnet publish portal/src/GovernmentPortal.Web/GovernmentPortal.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=catalog /services.json /app/catalog/services.json
COPY translations/ /app/translations/
USER 1001:1001
ENV ASPNETCORE_URLS=http://+:8080
ENV Catalog__Path=/app/catalog/services.json
ENV Translations__Path=/app/translations
EXPOSE 8080
ENTRYPOINT ["dotnet", "GovernmentPortal.Web.dll"]