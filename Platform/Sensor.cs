using System.Text.Json;
using System.Text.Json.Serialization;
using Platform.Interfaces;

namespace Platform;

public class Sensor : ISensor
{
    [JsonPropertyName("id")]
    public long Id { get; set; }
    
    [JsonPropertyName("pin")]
    public byte PinNo { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; }
    
    [JsonPropertyName("info")]
    public string Info { get; set; }

    private bool _collectData;

    [JsonIgnore]
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
        Info = Guid.Empty.ToString();
    }

    public virtual bool IsAvailable() => true;
    public virtual bool IsCollectable() => _collectData;

    /// <summary>
    /// Collect all information from sensor
    /// </summary>
    /// <returns></returns>
    public virtual string GetInfo()
    {
        return JsonSerializer.Serialize(this);
    }

    public virtual long GetId() => Id;
}