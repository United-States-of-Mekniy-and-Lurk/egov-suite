# gov-cli

CLI tool for managing MKLU government portals and tools.

## Service manifest commands

```bash
gov validate service.yaml
gov plan service.yaml
gov apply service.yaml
```

`validate` checks YAML structure and required fields.

`plan` and `apply` target Keycloak and require:

- `GOV_KEYCLOAK_URL`
- `GOV_KEYCLOAK_REALM`
- `GOV_KEYCLOAK_CLIENT_ID`
- `GOV_KEYCLOAK_CLIENT_SECRET`
