using System.Text.Json;

namespace RusalProject.Services.Agent.Core;

public static class AgentSerialization
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static Dictionary<string, object> ToDictionary(JsonElement element)
    {
        if (element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return new Dictionary<string, object>();

        var deserialized = JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText(), JsonOptions);
        return deserialized ?? new Dictionary<string, object>();
    }
}
