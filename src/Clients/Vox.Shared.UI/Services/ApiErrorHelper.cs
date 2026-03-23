using System.Text.Json;

namespace Vox.Shared.UI.Services;

internal static class ApiErrorHelper
{
    internal static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        string? content = null;
        try
        {
            content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
                return response.ReasonPhrase ?? $"HTTP {(int)response.StatusCode}";

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
                        if (item.TryGetProperty("errorMessage", out var emProp))
                        {
                            var msg = emProp.GetString();
                            if (!string.IsNullOrWhiteSpace(msg))
                                messages.Add(msg!);
                        }
                        else if (item.ValueKind == JsonValueKind.String)
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
                        var fieldMessages = ExtractFieldMessages(property.Value);
                        if (fieldMessages.Count > 0)
                            messages.Add($"{property.Name}: {string.Join(", ", fieldMessages)}");
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

    private static List<string> ExtractFieldMessages(JsonElement value)
    {
        var messages = new List<string>();

        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var v in value.EnumerateArray())
            {
                var msg = v.ValueKind == JsonValueKind.String ? v.GetString() : v.ToString();
                if (!string.IsNullOrWhiteSpace(msg))
                    messages.Add(msg!);
            }
        }
        else if (value.ValueKind == JsonValueKind.String)
        {
            var msg = value.GetString();
            if (!string.IsNullOrWhiteSpace(msg))
                messages.Add(msg!);
        }
        else
        {
            var msg = value.ToString();
            if (!string.IsNullOrWhiteSpace(msg))
                messages.Add(msg);
        }

        return messages;
    }
}
