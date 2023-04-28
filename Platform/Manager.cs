using Platform.Identifies;
using Platform.Models;

namespace Platform;

public class Manager
{
    private readonly ClientIdentifier _identifier;
    
    public Manager(Guid guid, string name)
    {
        _identifier = new ClientIdentifier(new IdentifierModel
        {
            Id = guid,
            Name = name
        });
    }
}