using System.Text.Json;
using Platform.Interfaces;

namespace Platform;

public class Sensor : ISensor
{
    public long Id { get; set; }
    public byte PinNo { get; set; }
    public string Name { get; set; }

    private bool _collectData;

    public bool CollectData
    {
        get => _collectData;
        set => _collectData = value;
    }

    public Sensor(long id, byte pinNo, string name, bool collectData = true)
    {
        Id = id;
        PinNo = pinNo;
        Name = name;
        CollectData = collectData;
    }

    public bool IsAvailable() => true;
    public bool IsCollectable() => _collectData;

    public string GetInfo()
    {
        return JsonSerializer.Serialize(this);
    }
}