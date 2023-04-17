namespace Platform.Interfaces;

public interface ISensor
{
    string GetInfo();
    bool IsAvailable();
}