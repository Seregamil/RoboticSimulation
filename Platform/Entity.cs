using NetMQ;
using NetMQ.Monitoring;
using Platform.Extensions;
using Platform.Identifies;
using Platform.Interfaces;
using Platform.Models;
using Serilog;

namespace Platform;

public abstract class Entity
{
    private readonly Identifier _identifier;
    protected Identifier? ConnectedIdentifier;
    
    protected readonly SocketExtension Socket;
    
    protected Entity(Guid guid, string name, string address)
    {
        _identifier = new Identifier(new IdentifierModel(guid, name));
        Socket = new SocketExtension(address);
        
        Socket.Run();
    }
    
    /// <summary>
    /// Use this for get robot unique uuid
    /// </summary>
    /// <returns>Return UUID formatted value</returns>
    protected Guid GetId() => _identifier.Id;
    
    /// <summary>
    /// Use this for get robot name
    /// </summary>
    /// <returns>Return String value</returns>
    protected string GetName() => _identifier.Name;
    
    /// <summary>
    /// Use this method for deserialize input message type from received message
    /// </summary>
    /// <param name="message">Bytes array with encoded message type</param>
    /// <returns></returns>
    protected MessageType? GetMessageType(byte[] message) => 
        EncoderExtension.DecodeMessage<MessageType>(message);

    /// <summary>
    /// Use this method for deserialize input identifier of client from received message
    /// Decoder: MessagePack
    /// </summary>
    /// <param name="message">Bytes array with encoded identifier</param>
    /// <returns></returns>
    protected IdentifierModel? GetMessageIdentifierModel(byte[] message) => 
        EncoderExtension.DecodeMessage<IdentifierModel>(message);

    /// <summary>
    /// Use this method for deserialize input identifier of client from received message
    /// Decoder: JsonSerializer
    /// </summary>
    /// <param name="message">Bytes array with encoded identifier</param>
    /// <returns></returns>
    public IdentifierModel? GetMessageIdentifierModel(string message) => 
        EncoderExtension.DecodeMessage<IdentifierModel>(message);

    /// <summary>
    /// Use this method for deserialize workload from received message
    /// </summary>
    /// <param name="messageType">Input message tyoe who deserialized by GetMessageType</param>
    /// <param name="message">Bytes array with encoded workload</param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    protected IMessage? GetMessageWorkloadModel(MessageType messageType, byte[] message)
    {
        return messageType switch
        {
                // MessageType.Sync => expr,
            MessageType.Move => EncoderExtension.DecodeMessage<MoveModel>(message),
                // MessageType.Event => expr,
                //MessageType.Sensor => MessagePackSerializer.Deserialize<SensorMessage>(message[4].Buffer),
            MessageType.Alert => EncoderExtension.DecodeMessage<AlertModel>(message),
            _ => throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null)
        };
    }
    
    /// <summary>
    /// Event called when producer connected to consumer and host successfully accepted
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected virtual void OnAcceptedHost(object? sender, NetMQMonitorSocketEventArgs e)
    { 
        if (e.Socket == null)
            return;
        
        Log.Verbose("<Platform::OnAcceptedHost>: Accepted host {host}:{port}",
            e.Socket.RemoteEndPoint.Address,
            e.Socket.RemoteEndPoint.Port);
        
        Socket.Handle.ReceiveReady += OnReceiveReady;
    }
    
    /// <summary>
    /// Event called when producer disconnected from consumer
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected virtual void OnDisconnected(object? sender, NetMQMonitorSocketEventArgs e)
    {
        Log.Verbose("<Platform::OnDisconnected>: Disconnected {id}", ConnectedIdentifier?.Id);
        Socket.Handle.ReceiveReady -= OnReceiveReady;

        ConnectedIdentifier = null;
    }

    /// <summary>
    /// Event who used for receiving messages from producer
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected virtual void OnReceiveReady(object? sender, NetMQSocketEventArgs e)
    {
        
    }

    public void Send<T>(MessageType type, T model)
    {
        var message = MessagesExtension.Configure(_identifier, type, model);
                
        Socket.Handle.TrySendMultipartBytes(message);
    }
}