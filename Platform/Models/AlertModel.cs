using System.Text.Json.Serialization;
using MessagePack;
using Platform.Interfaces;

namespace Platform.Models;

[MessagePackObject(true)]
public class AlertModel : IMessage
{
    [JsonPropertyName("timestamp")] 
    public long Timestamp { get; set; }

    [JsonPropertyName("msg")] 
    public string Message { get; set; } = string.Empty;
}