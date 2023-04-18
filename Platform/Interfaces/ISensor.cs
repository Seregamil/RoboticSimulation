namespace Platform.Interfaces;

public interface ISensor
{
    long GetId();
    string GetInfo();
    bool IsAvailable();
    bool IsCollectable();
}