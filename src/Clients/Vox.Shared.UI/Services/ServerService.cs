using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vox.Shared.UI.Auth;

namespace Vox.Shared.UI.Services;

public sealed class ServerService : IServerService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly ITokenStorageService _tokenStorage;

    public string? ErrorMessage { get; private set; }

    public ServerService(HttpClient http, ITokenStorageService tokenStorage)
    {
        _http = http;
        _tokenStorage = tokenStorage;
    }

    public async Task<IReadOnlyList<ServerResponse>> GetUserServersAsync()
    {
        ErrorMessage = null;
        try
        {
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, "api/servers");
            using var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = await ApiErrorHelper.ReadErrorAsync(response);
                return [];
            }

            return await response.Content.ReadFromJsonAsync<List<ServerResponse>>(JsonOptions) ?? [];
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return [];
        }
    }

    public async Task<ServerResponse?> GetServerByIdAsync(Guid id)
    {
        ErrorMessage = null;
        try
        {
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, $"api/servers/{id}");
            var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = await ApiErrorHelper.ReadErrorAsync(response);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ServerResponse>(JsonOptions);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return null;
        }
    }

    public async Task<ServerResponse?> CreateServerAsync(string name, string? description)
    {
        ErrorMessage = null;
        try
        {
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Post, "api/servers");
            request.Content = JsonContent.Create(new CreateServerRequest(name, description), options: JsonOptions);
            var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = await ApiErrorHelper.ReadErrorAsync(response);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ServerResponse>(JsonOptions);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return null;
        }
    }

    public async Task<ServerResponse?> UpdateServerAsync(Guid id, string name, string? description)
    {
        ErrorMessage = null;
        try
        {
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Put, $"api/servers/{id}");
            request.Content = JsonContent.Create(new UpdateServerRequest(name, description), options: JsonOptions);
            var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = await ApiErrorHelper.ReadErrorAsync(response);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ServerResponse>(JsonOptions);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return null;
        }
    }

    public async Task<bool> DeleteServerAsync(Guid id)
    {
        ErrorMessage = null;
        try
        {
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Delete, $"api/servers/{id}");
            var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = await ApiErrorHelper.ReadErrorAsync(response);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return false;
        }
    }

    private async Task<HttpRequestMessage> CreateAuthorizedRequestAsync(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        var accessToken = await _tokenStorage.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(accessToken))
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }
}
