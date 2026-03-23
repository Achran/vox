using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Vox.Shared.UI.Auth;

namespace Vox.Shared.UI.Services;

public sealed class MessageService : IMessageService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private readonly ITokenStorageService _tokenStorage;

    public string? ErrorMessage { get; private set; }

    public MessageService(HttpClient http, ITokenStorageService tokenStorage)
    {
        _http = http;
        _tokenStorage = tokenStorage;
    }

    public async Task<IReadOnlyList<MessageResponse>> GetChannelMessagesAsync(
        Guid channelId, int pageSize = 50, DateTime? before = null)
    {
        ErrorMessage = null;
        try
        {
            var url = $"api/channels/{channelId}/messages?pageSize={pageSize}";
            if (before.HasValue)
            {
                url += $"&before={before.Value:O}";
            }

            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, url);
            using var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = await ApiErrorHelper.ReadErrorAsync(response);
                return [];
            }

            return await response.Content.ReadFromJsonAsync<List<MessageResponse>>(JsonOptions) ?? [];
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return [];
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
