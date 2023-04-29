using Platform.Interfaces;

namespace Platform.Models;

public class MessageModel
{
    public IdentifierModel IdentifierModel { get; set; } 
    public MessageType MessageType { get; set; }
    public List<IMessage> Messages { get; set; } = new ();
}