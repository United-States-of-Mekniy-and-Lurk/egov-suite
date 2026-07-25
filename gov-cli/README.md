# gov-cli

CLI tool for managing MKLU government portals and tools.

## Service manifest commands

```bash
gov validate service.yaml
gov plan service.yaml
gov apply service.yaml
gov catalog ../ > services.json
```

`validate` checks YAML structure and required fields.

`plan` and `apply` target Keycloak and require:

- `GOV_KEYCLOAK_URL`
- `GOV_KEYCLOAK_REALM`
- `GOV_KEYCLOAK_CLIENT_ID`
- `GOV_KEYCLOAK_CLIENT_SECRET`

Create a dedicated confidential Keycloak client for these commands, for example `gov-cli`. Enable
client authentication and service accounts, and disable browser flows. In the client's service
account role mappings, assign the `realm-management` client roles `manage-clients` and
`manage-realm`. Use that client's ID and generated credential as `GOV_KEYCLOAK_CLIENT_ID` and
`GOV_KEYCLOAK_CLIENT_SECRET`. Do not reuse an application Web client or commit the credential.

`catalog` recursively reads `*.gov.yaml` manifests and writes public portal metadata as JSON.
The generated file can be mounted into the government portal; the portal does not need access to
deployment credentials or the source repository.

Each client may declare API audiences. `gov apply` reconciles dedicated Keycloak audience mappers
without modifying unrelated protocol mappers:

```yaml
auth:
  clients:
    web:
      redirectUris:
        - https://service.example/signin-oidc
      scopes: [openid, profile, email]
      audiences: [service-api]
```
