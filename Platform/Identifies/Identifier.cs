using System.Text.Json;
using MessagePack;
using Platform.Interfaces;
using Platform.Models;

namespace Platform.Identifies;

public class Identifier : IIdentifier
{
    private readonly IdentifierModel _identifier;
    
    public Identifier(IdentifierModel model)
    {
        _identifier = model;
    }

    public Guid Id => _identifier.Id;
    public string Name => _identifier.Name;
    
    /// <summary>
    /// Serialization method inherit from Interface
    /// </summary>
    /// <returns></returns>
    public byte[] Serialize() => 
        MessagePackSerializer.Serialize(_identifier);

    /// <summary>
    /// Serialization to JSON schema inherit from Interface
    /// </summary>
    /// <returns></returns>
    public string SerializeJson() =>
        JsonSerializer.Serialize(_identifier);
}