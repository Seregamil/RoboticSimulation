namespace Platform.Interfaces;

public interface IHandler
{
    void Clear();
    bool Update(IMessage model);
}