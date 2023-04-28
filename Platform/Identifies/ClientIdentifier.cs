using Platform.Models;

namespace Platform.Identifies;

/// <summary>
/// Client identifier model
/// </summary>
public class ClientIdentifier : Identifier
{
    public ClientIdentifier(IdentifierModel model) :
        base(model)
    {
    }
}