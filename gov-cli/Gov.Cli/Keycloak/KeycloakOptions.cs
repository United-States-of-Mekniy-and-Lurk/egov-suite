namespace Gov.Cli.Keycloak;

public sealed record KeycloakOptions(string BaseUrl, string Realm, string ClientId, string ClientSecret)
{
    public static (KeycloakOptions? Options, List<string> Errors) FromEnvironment()
    {
        var errors = new List<string>();

        var baseUrl = Environment.GetEnvironmentVariable("GOV_KEYCLOAK_URL");
        var realm = Environment.GetEnvironmentVariable("GOV_KEYCLOAK_REALM");
        var clientId = Environment.GetEnvironmentVariable("GOV_KEYCLOAK_CLIENT_ID");
        var clientSecret = Environment.GetEnvironmentVariable("GOV_KEYCLOAK_CLIENT_SECRET");

        if (string.IsNullOrWhiteSpace(baseUrl)) errors.Add("Environment variable GOV_KEYCLOAK_URL is required.");
        if (string.IsNullOrWhiteSpace(realm)) errors.Add("Environment variable GOV_KEYCLOAK_REALM is required.");
        if (string.IsNullOrWhiteSpace(clientId)) errors.Add("Environment variable GOV_KEYCLOAK_CLIENT_ID is required.");
        if (string.IsNullOrWhiteSpace(clientSecret)) errors.Add("Environment variable GOV_KEYCLOAK_CLIENT_SECRET is required.");

        if (errors.Count > 0)
        {
            return (null, errors);
        }

        return (new KeycloakOptions(baseUrl!.TrimEnd('/'), realm!, clientId!, clientSecret!), errors);
    }
}
