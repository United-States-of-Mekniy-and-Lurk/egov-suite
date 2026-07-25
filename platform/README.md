# E-government platform modules

Reusable capabilities shared by registry and workflow services live here. Each
project is independently packable and must not reference a service project.

## Modules

- `Egov.Platform.Documents` provides renderer-neutral official document models
  and a configurable PDFsharp renderer. Host images must provide Noto Sans and
  Noto Serif fonts.
- `Egov.Platform.Identity` provides current-actor and person lookup contracts,
  plus Keycloak realm/client role normalization. Service-specific authorization
  policies and downstream HTTP adapters remain in each service.
- `Egov.Platform.Localization` provides consistent request-culture selection,
  JSON translation loading, and the shared `mklu.culture` preference cookie.
  The cookie is scoped to `.mklu.org` only for requests on that domain; local
  development automatically uses a host-only cookie.
- `Egov.Platform.Forms` provides localized legacy form models, immutable
  version/draft contracts, and configurable Form.io schema conversion.

Consumer services own their EF Core contexts, database tables, migrations,
workflow states, and role names. Shared persistence will only be introduced
after a second registry demonstrates a stable storage contract.

## Localization

Web applications register shared culture behavior with
`AddMkluRequestLocalization(configuration)`. Applications using JSON translation
files additionally call `AddMkluJsonLocalization(translationsPath)`. Culture
handlers write preferences through `MkluCultureCookie` rather than constructing
cookies themselves.

Defaults can be overridden under the `Localization` configuration section:

```json
{
  "Localization": {
    "DefaultCulture": "en",
    "FallbackCulture": "en",
    "SupportedCultures": ["en", "cs"],
    "CookieName": "mklu.culture",
    "CookieDomain": ".mklu.org",
    "CookieLifetimeDays": 365
  }
}
```

The shared cookie is essential, HTTP-only, `SameSite=Lax`, and secure on the
shared domain. The legacy host-only `.AspNetCore.Culture` cookie remains a
lower-priority reader during migration.

## Local development

The modules currently target .NET 10, matching the services. Build all modules
with:

```sh
dotnet build platform/Egov.Platform.slnx
```

Citizen Service Docker builds use the repository root as their context:

```sh
docker build -f sal/Dockerfile.api .
docker build -f sal/Dockerfile.web .
```