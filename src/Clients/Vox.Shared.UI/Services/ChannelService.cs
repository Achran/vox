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
            var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = await ReadErrorAsync(response);
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
            var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = await ReadErrorAsync(response);
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
            var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = await ReadErrorAsync(response);
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
            var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = await ReadErrorAsync(response);
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
            var response = await _http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = await ReadErrorAsync(response);
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

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        string? content = null;
        try
        {
            content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
                throw new JsonException("Empty content");

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            if (root.TryGetProperty("error", out var errProp) && errProp.ValueKind == JsonValueKind.String)
            {
                var error = errProp.GetString();
                if (!string.IsNullOrWhiteSpace(error))
                    return error!;
            }

            if (root.TryGetProperty("errors", out var errorsProp))
            {
                var messages = new List<string>();

                if (errorsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in errorsProp.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            var msg = item.GetString();
                            if (!string.IsNullOrWhiteSpace(msg))
                                messages.Add(msg!);
                        }
                        else
                        {
                            var msg = item.ToString();
                            if (!string.IsNullOrWhiteSpace(msg))
                                messages.Add(msg);
                        }
                    }
                }
                else if (errorsProp.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in errorsProp.EnumerateObject())
                    {
                        var key = property.Name;
                        var value = property.Value;
                        var fieldMessages = new List<string>();

                        if (value.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var v in value.EnumerateArray())
                            {
                                if (v.ValueKind == JsonValueKind.String)
                                {
                                    var msg = v.GetString();
                                    if (!string.IsNullOrWhiteSpace(msg))
                                        fieldMessages.Add(msg!);
                                }
                                else
                                {
                                    var msg = v.ToString();
                                    if (!string.IsNullOrWhiteSpace(msg))
                                        fieldMessages.Add(msg);
                                }
                            }
                        }
                        else if (value.ValueKind == JsonValueKind.String)
                        {
                            var msg = value.GetString();
                            if (!string.IsNullOrWhiteSpace(msg))
                                fieldMessages.Add(msg!);
                        }
                        else
                        {
                            var msg = value.ToString();
                            if (!string.IsNullOrWhiteSpace(msg))
                                fieldMessages.Add(msg);
                        }

                        if (fieldMessages.Count > 0)
                        {
                            messages.Add($"{key}: {string.Join(", ", fieldMessages)}");
                        }
                    }
                }

                if (messages.Count > 0)
                    return string.Join("; ", messages);
            }
        }
        catch
        {
            // Ignore parsing errors and fall back below.
        }

        if (!string.IsNullOrWhiteSpace(content))
            return content!;
        return response.ReasonPhrase ?? $"HTTP {(int)response.StatusCode}";
    }
}
