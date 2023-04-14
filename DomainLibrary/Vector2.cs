using MessagePack;

namespace DomainLibrary;

[MessagePackObject(keyAsPropertyName: true)]
public class Vector2
{
    public float X { get; set; }
    public float Y { get; set; }
    
    public Vector2(float x, float y)
    {
        X = x;
        Y = y;
    }
}