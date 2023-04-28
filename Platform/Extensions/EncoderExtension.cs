using System.Text.Json;
using MessagePack;
using Serilog;

namespace Platform.Extensions;

public static class EncoderExtension
{
    /// <summary>
    /// Use this method for decode data by type from bytes array message
    /// </summary>
    /// <param name="message">Bytes array with encoded data</param>
    /// <typeparam name="T">Generic type to decode</typeparam>
    /// <returns></returns>
    public static T? DecodeMessage<T>(byte[] message)
    {
        try
        {
            var workload = MessagePackSerializer.Deserialize<T>(message);
            Log.Verbose("MessagePack: Deserialized: {w}", 
                JsonSerializer.Serialize(workload));
            return workload;
        }
        catch (MessagePackSerializationException e)
        {
            Log.Error("MessagePack: internal deserialization error; Return {d}; Error: {e}", 
                default, e.Message);
            return default;
        }
        catch (Exception e)
        {
            Log.Error("MessagePack: deserialization exception; Return {d}; Error: {e}", 
                default, e.Message);
            return default;
        }
    }

    /// <summary>
    /// Use this method for decode data by type from encoded JSON message
    /// </summary>
    /// <param name="message">JSON schema</param>
    /// <typeparam name="T">Generic type to decode</typeparam>
    /// <returns></returns>
    public static T? DecodeMessage<T>(string message) 
    {
        try
        {
            Log.Verbose("JsonSerializer: Deserialized: {w}", message);
            return JsonSerializer.Deserialize<T>(message);
        }
        catch (MessagePackSerializationException e)
        {
            Log.Error("JsonSerializer: internal deserialization error; Return {d}; Error: {e}", 
                default, e.Message);
            return default;
        }
        catch (Exception e)
        {
            Log.Error("JsonSerializer: deserialization exception; Return {d}; Error: {e}", 
                default, e.Message);
            return default;
        }
    }
}