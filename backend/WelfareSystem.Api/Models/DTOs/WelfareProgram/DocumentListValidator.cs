using System.Text.Json;

namespace WelfareSystem.Api.Models.DTOs.WelfareProgram;

public static class DocumentListValidator
{
    public static bool IsValid(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 20000) return false;
        try {
            using var json = JsonDocument.Parse(value);
            return json.RootElement.ValueKind == JsonValueKind.Array && json.RootElement.GetArrayLength() <= 100 &&
                json.RootElement.EnumerateArray().All(item => item.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(item.GetString()) && item.GetString()!.Length <= 200);
        } catch (JsonException) { return false; }
    }
}
