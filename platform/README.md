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
- `Egov.Platform.Forms` provides localized legacy form models, immutable
  version/draft contracts, and configurable Form.io schema conversion.

Consumer services own their EF Core contexts, database tables, migrations,
workflow states, and role names. Shared persistence will only be introduced
after a second registry demonstrates a stable storage contract.

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