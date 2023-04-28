using MessagePack;
using Platform.Identifies;

namespace Platform.Extensions;

public static class MessagesExtension
{
    /// <summary>
    /// Use this method for configuring IEnumerable byte array message for sending to socket
    /// </summary>
    /// <param name="model">Socket serialization</param>
    /// <param name="identifyModel"></param>
    /// <param name="type">MessageType conversion</param>
    /// <typeparam name="T">Workload data</typeparam>
    /// <returns></returns>
    public static IEnumerable<byte[]> Configure<T>(Identifier identifyModel, MessageType type, T model)
    {
        var bytesList = new List<byte[]>
        {
            identifyModel.Serialize(),
            Array.Empty<byte>(),
            new [] { (byte) type },
            Array.Empty<byte>(),
            MessagePackSerializer.Serialize(model)
        };
        
        return bytesList;
    }
}