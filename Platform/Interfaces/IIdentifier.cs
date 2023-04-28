namespace Platform.Interfaces;

public interface IIdentifier
{
    byte[] Serialize();
    string SerializeJson();
}