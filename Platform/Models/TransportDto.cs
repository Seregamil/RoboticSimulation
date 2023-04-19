using MessagePack;

namespace Platform.Models;

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
    
    public void GyroscopeToJoystickConversion() {
        const float joyZeroEmulation = 511.5f; 

        var x = joyZeroEmulation * Vector2.X + (Vector2.X > 0 ? Vector2.X : -Vector2.X);
        var y = joyZeroEmulation * Vector2.Y + (Vector2.Y > 0 ? Vector2.Y : -Vector2.Y);

        Vector2.X = x;
        Vector2.Y = y;
    }
}