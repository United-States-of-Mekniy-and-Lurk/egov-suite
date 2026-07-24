# Government portal

Database-free public entrypoint for government information and services. Public pages do not require
an account. Authentication is role-free and only unlocks the personal overview.

## Service catalog

Applications describe themselves in `*.gov.yaml` manifests under the optional `portal` section.
They are the only managed source of service metadata. For local development, generate the ignored
runtime artifact with:

```sh
dotnet run --project gov-cli/Gov.Cli/Gov.Cli.csproj -- catalog . > portal/src/GovernmentPortal.Web/catalog/services.json
```

Do not edit or commit `services.json`. The Docker build performs generation itself. In Kubernetes, the
same output can be supplied as a generated ConfigMap at `Catalog__Path`.

## Authentication

Configure `Oidc__Authority`, `Oidc__ClientId`, and `Oidc__ClientSecret`. The Keycloak client needs the
standard authorization-code redirect URI `/signin-oidc`; it does not require any roles.

## Kubernetes and Argo CD

The chart is in `portal/helm/government-portal`. The repository container workflow publishes
`ghcr.io/united-states-of-mekniy-and-lurk/egov-suite-portal`, and the Helm release workflow pins its
SHA tag after a successful build.

Before applying `portal/argocd/application.yaml`, provision the Secret referenced by the Argo values:

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: government-portal-secret
  namespace: default
type: Opaque
stringData:
  Oidc__ClientSecret: replace-through-your-secret-manager
```

Configure the Keycloak client redirect URI as `https://portal.gov.mklu.org/signin-oidc` and its
post-logout redirect URI as `https://portal.gov.mklu.org/`. The chart intentionally defaults to one
replica until shared ASP.NET data-protection keys are configured.

## Overview modules

Private overview blades implement `IPortalModule`. Modules should call purpose-built summary endpoints
owned by their services and degrade independently. They must not move domain operations or records into
the portal.

CMS-backed public and private content can later implement a separate content provider. A database and
authoring UI are intentionally excluded until publishing workflow, moderation, localization, and audit
requirements are defined.
