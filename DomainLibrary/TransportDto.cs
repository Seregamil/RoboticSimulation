using MessagePack;

namespace DomainLibrary;

[MessagePackObject(keyAsPropertyName: true)]
public class TransportDto
{
    public Vector2 Vector2 { get; set; }
    public string PressedKeys { get; set; }

    public TransportDto(Vector2 vector2, string pressedKeys)
    {
        Vector2 = vector2;
        PressedKeys = pressedKeys;
    }

    public byte[] Serialize()
    {
        var serialized = MessagePackSerializer.Serialize(this);
        return serialized;
    }
}