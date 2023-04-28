using System.Text.Json.Serialization;
using MessagePack;

namespace Platform.Models;

[MessagePackObject(true)]
public class IdentifierModel
{
    /// <summary>
    /// Unique identifier uuid
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Unique identifier name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}