using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gov.Cli.Core;

namespace Gov.Cli.Keycloak;

public sealed class KeycloakAdapter(HttpClient httpClient, KeycloakOptions options) : IPlatformAdapter
{
    private const string ServiceAttribute = "gov.service";

    public async Task<CurrentState> GetCurrentStateAsync(string service, CancellationToken cancellationToken = default)
    {
        await AuthenticateAsync(cancellationToken);

        var clientModels = await GetAllClientsAsync(cancellationToken);
        var clients = clientModels
            .Where(c => c.ClientId is not null)
            .Select(c => ToCurrentClient(service, c))
            .Where(c => c is not null)
            .Cast<CurrentClient>()
            .ToArray();

        var roleModels = await GetJsonAsync<List<KeycloakRole>>(RolesUrl(), cancellationToken) ?? [];
        var roles = roleModels
            .Select(r => DecodeRoleName(service, r.Name))
            .Where(static r => r is not null)
            .Cast<string>()
            .ToArray();

        return new CurrentState(clients, roles);
    }

    public async Task CreateClientAsync(string service, ClientCreate client, CancellationToken cancellationToken = default)
    {
        await AuthenticateAsync(cancellationToken);

        var body = new KeycloakClient
        {
            ClientId = EncodeClientId(service, client.LogicalName),
            Name = EncodeClientId(service, client.LogicalName),
            Enabled = true,
            PublicClient = false,
            Protocol = "openid-connect",
            RedirectUris = client.RedirectUris.ToList(),
            DefaultClientScopes = client.Scopes.ToList(),
            Attributes = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [ServiceAttribute] = service,
            },
        };

        _ = await PostJsonAsync(ClientsBaseUrl(), body, cancellationToken, allowConflict: true);
    }

    public async Task UpdateClientAsync(string service, ClientUpdate client, CancellationToken cancellationToken = default)
    {
        await AuthenticateAsync(cancellationToken);

        var body = new KeycloakClient
        {
            Id = client.KeycloakId,
            ClientId = EncodeClientId(service, client.LogicalName),
            Name = EncodeClientId(service, client.LogicalName),
            Enabled = true,
            PublicClient = false,
            Protocol = "openid-connect",
            RedirectUris = client.RedirectUris.ToList(),
            DefaultClientScopes = client.Scopes.ToList(),
            Attributes = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [ServiceAttribute] = service,
            },
        };

        await PutJsonAsync($"{ClientsBaseUrl()}/{client.KeycloakId}", body, cancellationToken);
    }

    public async Task DeleteClientAsync(ClientDelete client, CancellationToken cancellationToken = default)
    {
        await AuthenticateAsync(cancellationToken);

        var response = await httpClient.DeleteAsync($"{ClientsBaseUrl()}/{client.KeycloakId}", cancellationToken);
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            throw await CreateApiException(response, "delete client");
        }
    }

    public async Task CreateRoleAsync(string service, RoleCreate role, CancellationToken cancellationToken = default)
    {
        await AuthenticateAsync(cancellationToken);

        var body = new KeycloakRole
        {
            Name = EncodeRoleName(service, role.Name),
            Description = $"Managed by gov manifest for service '{service}'",
        };

        _ = await PostJsonAsync(RolesUrl(), body, cancellationToken, allowConflict: true);
    }

    public async Task DeleteRoleAsync(string service, RoleDelete role, CancellationToken cancellationToken = default)
    {
        await AuthenticateAsync(cancellationToken);

        var encodedName = Uri.EscapeDataString(EncodeRoleName(service, role.Name));
        var response = await httpClient.DeleteAsync($"{RolesUrl()}/{encodedName}", cancellationToken);
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            throw await CreateApiException(response, "delete role");
        }
    }

    private CurrentClient? ToCurrentClient(string service, KeycloakClient client)
    {
        var logicalName = DecodeClientId(service, client.ClientId!);
        if (logicalName is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(client.Id))
        {
            throw new InvalidOperationException($"Keycloak client '{client.ClientId}' did not include an id.");
        }

        return new CurrentClient(
            logicalName,
            client.Id,
            (client.RedirectUris ?? []).Order(StringComparer.Ordinal).ToArray(),
            (client.DefaultClientScopes ?? []).Order(StringComparer.Ordinal).ToArray());
    }

    private async Task AuthenticateAsync(CancellationToken cancellationToken)
    {
        if (httpClient.DefaultRequestHeaders.Authorization is not null)
        {
            return;
        }

        var tokenEndpoint = $"{options.BaseUrl}/realms/{Uri.EscapeDataString(options.Realm)}/protocol/openid-connect/token";
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = options.ClientId,
            ["client_secret"] = options.ClientSecret,
        };

        var response = await httpClient.PostAsync(tokenEndpoint, new FormUrlEncodedContent(form), cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw await CreateApiException(response, "authenticate");
        }

        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken);
        if (string.IsNullOrWhiteSpace(payload?.AccessToken))
        {
            throw new InvalidOperationException("Keycloak token response did not include an access_token.");
        }

        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload.AccessToken);
    }

    private string ClientsBaseUrl() => $"{options.BaseUrl}/admin/realms/{Uri.EscapeDataString(options.Realm)}/clients";

    private string ListClientsUrl(int first, int max) => $"{ClientsBaseUrl()}?first={first}&max={max}";

    private string RolesUrl() => $"{options.BaseUrl}/admin/realms/{Uri.EscapeDataString(options.Realm)}/roles";

    private async Task<T?> GetJsonAsync<T>(string url, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw await CreateApiException(response, "fetch state");
        }

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
    }

    private async Task<CreateResult> PostJsonAsync<T>(string url, T body, CancellationToken cancellationToken, bool allowConflict = false)
    {
        var response = await httpClient.PostAsJsonAsync(url, body, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return CreateResult.Created;
        }

        if (allowConflict && response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            return CreateResult.Conflict;
        }

        throw await CreateApiException(response, "create resource");
    }

    private async Task<List<KeycloakClient>> GetAllClientsAsync(CancellationToken cancellationToken)
    {
        const int pageSize = 200;
        var offset = 0;
        var clients = new List<KeycloakClient>();

        while (true)
        {
            var page = await GetJsonAsync<List<KeycloakClient>>(ListClientsUrl(offset, pageSize), cancellationToken) ?? [];
            if (page.Count == 0)
            {
                break;
            }

            clients.AddRange(page);
            if (page.Count < pageSize)
            {
                break;
            }

            offset += pageSize;
        }

        return clients;
    }

    private async Task PutJsonAsync<T>(string url, T body, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
        };

        var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw await CreateApiException(response, "update resource");
        }
    }

    private static async Task<Exception> CreateApiException(HttpResponseMessage response, string operation)
    {
        var body = await response.Content.ReadAsStringAsync();
        return new InvalidOperationException($"Keycloak API failed to {operation}. Status={(int)response.StatusCode}. Body={body}");
    }

    private static string EncodeClientId(string service, string logicalClientName) => $"{service}-{logicalClientName}";

    private static string? DecodeClientId(string service, string clientId)
    {
        var prefix = $"{service}-";
        if (!clientId.StartsWith(prefix, StringComparison.Ordinal))
        {
            return null;
        }

        return clientId[prefix.Length..];
    }

    private static string EncodeRoleName(string service, string roleName) => $"{service}:{roleName}";

    private static string? DecodeRoleName(string service, string? roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return null;
        }

        var prefix = $"{service}:";
        if (!roleName.StartsWith(prefix, StringComparison.Ordinal))
        {
            return null;
        }

        return roleName[prefix.Length..];
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }
    }

    private sealed class KeycloakClient
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("clientId")]
        public string? ClientId { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; init; }

        [JsonPropertyName("publicClient")]
        public bool PublicClient { get; init; }

        [JsonPropertyName("protocol")]
        public string? Protocol { get; init; }

        [JsonPropertyName("redirectUris")]
        public List<string>? RedirectUris { get; init; }

        [JsonPropertyName("defaultClientScopes")]
        public List<string>? DefaultClientScopes { get; init; }

        [JsonPropertyName("attributes")]
        public Dictionary<string, string>? Attributes { get; init; }
    }

    private sealed class KeycloakRole
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; init; }
    }

    private enum CreateResult
    {
        Created,
        Conflict,
    }
}
