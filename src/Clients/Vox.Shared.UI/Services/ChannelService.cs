using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vox.Shared.UI.Auth;

namespace Vox.Shared.UI.Services;

public sealed class ChannelService : IChannelService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly ITokenStorageService _tokenStorage;

    public string? ErrorMessage { get; private set; }

    public ChannelService(HttpClient http, ITokenStorageService tokenStorage)
    {
        _http = http;
        _tokenStorage = tokenStorage;
    }

    public async Task<IReadOnlyList<ChannelResponse>> GetServerChannelsAsync(Guid serverId)
    {
        ErrorMessage = null;
        try
        {
            using var request = await CreateAuthorizedRequestAsync(
                HttpMethod.Get, $"api/servers/{serverId}/channels");
            using var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = await ApiErrorHelper.ReadErrorAsync(response);
                return [];
            }

            return await response.Content.ReadFromJsonAsync<List<ChannelResponse>>(JsonOptions) ?? [];
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return [];
        }
    }

    public async Task<ChannelResponse?> GetChannelByIdAsync(Guid id)
    {
        ErrorMessage = null;
        try
        {
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, $"api/channels/{id}");
            using var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = await ApiErrorHelper.ReadErrorAsync(response);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ChannelResponse>(JsonOptions);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return null;
        }
    }

    public async Task<ChannelResponse?> CreateChannelAsync(Guid serverId, string name, string type)
    {
        ErrorMessage = null;
        try
        {
            using var request = await CreateAuthorizedRequestAsync(
                HttpMethod.Post, $"api/servers/{serverId}/channels");
            request.Content = JsonContent.Create(new CreateChannelRequest(name, type), options: JsonOptions);
            using var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = await ApiErrorHelper.ReadErrorAsync(response);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ChannelResponse>(JsonOptions);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return null;
        }
    }

    public async Task<ChannelResponse?> UpdateChannelAsync(Guid id, string name)
    {
        ErrorMessage = null;
        try
        {
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Put, $"api/channels/{id}");
            request.Content = JsonContent.Create(new UpdateChannelRequest(name), options: JsonOptions);
            using var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = await ApiErrorHelper.ReadErrorAsync(response);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ChannelResponse>(JsonOptions);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return null;
        }
    }

    public async Task<bool> DeleteChannelAsync(Guid id)
    {
        ErrorMessage = null;
        try
        {
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Delete, $"api/channels/{id}");
            using var response = await _http.SendAsync(request);

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
