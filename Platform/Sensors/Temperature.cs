using System.Text.Json;
using Platform.Interfaces;

namespace Platform.Sensors;

public class Temperature : Sensor
{
    public Temperature(long id, byte pinNo, string name, bool collectData = true) 
        : base(id, pinNo, name, collectData)
    {
    }

    public override string GetInfo()
    {
        Info = "36Celsium";
        return JsonSerializer.Serialize(this);
    }
}