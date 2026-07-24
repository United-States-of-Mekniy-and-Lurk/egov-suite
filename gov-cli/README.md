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

`catalog` recursively reads `*.gov.yaml` manifests and writes public portal metadata as JSON.
The generated file can be mounted into the government portal; the portal does not need access to
deployment credentials or the source repository.
