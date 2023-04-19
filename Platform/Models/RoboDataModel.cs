using MessagePack;

namespace Platform.Models;

[MessagePackObject(true)]
public class RoboDataModel
{
    public Vector2 GyroscopeData { get; set; }
    public Guid TestData { get; set; }
    public string Text { get; set; }
}