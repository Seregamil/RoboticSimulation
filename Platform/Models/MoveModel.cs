using System.Text.Json.Serialization;
using MessagePack;
using Platform.Interfaces;

namespace Platform.Models;

[MessagePackObject(true)]
public class MoveModel : IMessage
{
    /// <summary>
    ///         Vector for moving.
    ///                | y (0; 1)
    ///                |
    ///                |
    /// (-1;0) x_______.________x (1;0)
    ///                |
    ///                |
    ///                |y (0; -1)
    /// </summary>
    [JsonPropertyName("vec2")]
    public Vector2 Vector2 { get; set; } = new (0.0f, 0.0f);

    /// <summary>
    /// Pressed keys array by delimiter '|'
    /// </summary>
    [JsonPropertyName("keys")]
    public string PressedKeys { get; set; } = string.Empty;
}