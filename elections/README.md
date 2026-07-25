# Election Service

Election Service provides a public Razor Pages portal and a protected ASP.NET Core API for publishing, administering, and participating in elections. The solution targets .NET 10 and uses PostgreSQL through Entity Framework Core.

## Architecture

- `ElectionService.Domain` contains election entities and state rules.
- `ElectionService.Application` contains use cases and application contracts.
- `ElectionService.Infrastructure` contains persistence, integration clients, and credential hashing.
- `ElectionService.Api` exposes the API, validates Keycloak JWTs, and applies migrations at startup.
- `ElectionService.Web` is the public and administrative portal using OIDC and the internal API.
- `Egov.Platform.Identity` supplies shared identity integration from `platform/`.

## Privacy invariant

Participation records and ballot records are stored separately. This schema separation reduces direct linkage, but it does not by itself provide cryptographic unlinkability. Operational logging must not capture invitation URLs or ballot bodies. Infrastructure access should separate participation data and ballot data where possible, including database access, backups, diagnostics, and support workflows.

Public records expose the official ballot definition, aggregate turnout, and aggregate party-list or referendum results only. The service never publishes individual anonymous ballot rows or participation credentials.

## Public records and historical elections

The Web portal supports English and Czech through a culture selector. Finalized and archived elections publish an official record at `GET /public/elections/{identifier}/record`, including the electorate when known, participating voters, valid and invalid ballot totals, turnout, the ballot definition, and aggregate results. Elections that have not been finalized do not expose this record.

Administrators can import an aggregate historical election with `POST /admin/historical-elections` or the portal's historical import page. An import snapshots party, candidate, or referendum labels and requires a source reference. Historical records are created directly as immutable archives; they do not create synthetic anonymous ballots or participation records, and historical parties do not need a current Organization Registry identifier.

## Local configuration

Set configuration with environment variables or local user secrets. The API requires `ConnectionStrings__DefaultConnection`, `Jwt__Authority`, `Jwt__Audience`, `Voting__ActiveKeyVersion`, at least one matching `Voting__CredentialHashKeys__<version>` value of 32 or more characters, `PersonRegistry__BaseUrl`, `CitizenRegistry__BaseUrl`, and `OrganizationRegistry__BaseUrl`. The Web requires `ElectionApi__BaseUrl`, `Oidc__Authority`, `Oidc__ClientId`, `Oidc__ClientSecret`, and `Oidc__PublicBaseUrl`; `DataProtection__KeysPath` is recommended when sessions must survive restarts.

Each election freezes the active credential-hash key version when it is created. Rotate keys by adding a new version and changing `Voting__ActiveKeyVersion`; retain every older key while elections or invitations created with it must remain usable.

Do not commit credentials, invitation URLs, or production connection strings.

## Build and test

From the repository root with the .NET 10 SDK:

```sh
dotnet restore elections/ElectionService.slnx
dotnet build elections/ElectionService.slnx --no-restore
dotnet test elections/ElectionService.slnx --no-build
```

Container builds use the repository root as their context because both applications reference `platform/Egov.Platform.Identity`.

## Database migrations

The API applies pending migrations on startup. To apply them explicitly from the repository root:

```sh
ConnectionStrings__DefaultConnection='Host=localhost;Database=elections;Username=postgres;Password=postgres' \
  dotnet ef database update \
  --project elections/src/ElectionService.Infrastructure \
  --startup-project elections/src/ElectionService.Api
```

## Deployment

The Helm chart is in `helm/election-service`. Its disposable-development defaults can provision PostgreSQL. Production should disable bundled PostgreSQL and set `secret.existingSecret` to a Secret containing `ConnectionStrings__DefaultConnection`, `Voting__CredentialHashKeys__v1` (plus retained and active future versions), and `Oidc__ClientSecret`. Set `config.votingActiveKeyVersion` to the active version. Disposable deployments can retain keys in the `secret.credentialHashKeys` map. The Web data-protection PVC must use storage that remains available across rollouts.

Validate the chart with:

```sh
helm lint elections/helm/election-service
helm template election-service elections/helm/election-service
```

The Argo CD application in `argocd/application.yaml` configures the production hosts and automated synchronization.
