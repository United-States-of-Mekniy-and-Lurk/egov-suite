# SAL

## Keycloak audience

The web application forwards the access token issued to the `citizen-admin`
OIDC client when it calls the Citizen Service API. That token must contain
`citizen-service` in its `aud` claim, matching `config.jwtAudience` in the
Helm values.

In Keycloak, create an Audience protocol mapper with:

- Included Client Audience: `citizen-service`, if that Keycloak client exists;
	otherwise set Included Custom Audience to `citizen-service`
- Add to access token: enabled
- Add to ID token: disabled

Attach the mapper directly to `citizen-admin`, or put it in a client scope
that is assigned to `citizen-admin` as a default client scope. After changing
the mapper, sign out and sign in again so Keycloak issues a new access token.

To verify the configuration, decode the access token and confirm that its
audience includes the API:

```json
{
	"aud": ["account", "citizen-service"]
}
```

Do not change the API audience to `account`: that audience identifies
Keycloak's account service and does not establish that a token was issued for
the Citizen Service API.