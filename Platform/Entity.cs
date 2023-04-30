using System.Text.Json;
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
    
    public delegate void MessageReceived(MessageModel messageModel);
    public event MessageReceived? OnMessageReceived;
    
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
    private void OnReceiveReady(object? sender, NetMQSocketEventArgs e)
    {
        var messagesArray = new List<byte[]>();
        var messageStatus = e.Socket.TryReceiveMultipartBytes(ref messagesArray);
        if (!messageStatus)
            return;

        var message = DeconstructMessage(messagesArray);
        if(message is null)
            return;
        
        OnMessageReceived?.Invoke(message);
    }

    private MessageModel? DeconstructMessage(List<byte[]> message)
    {
        /*
         * 0 IdentifierModel
         * 1 Empty frame
         * 2 MessageType
         * 3 Empty frame
         * 4 Workload
         * 5 Workload
         * ...
         * n Workload
         */
        var outputModel = new MessageModel();
        
        Log.Debug("Received multipart bytes message: {msg}", JsonSerializer.Serialize(message));
        
        // so, simple isAlive message will be have next format: 
        // 0 IdentifierModel
        // 1 EmptyFrame
        // 2 MessageType.Healthckeck
        if (message.Count < 3)
        {
            Log.Error("Not full message. Frames: {received}/3; Aborting", message.Count);
            return null;
        }

        // Check protocol. Second frame should me empty
        if (message[1].Length > 0)
        {
            Log.Error("Not correct protocol. 2 frame should me empty. Frame len: {len}", message[1].Length);
            return null;
        }

        var identifier = GetMessageIdentifierModel(message[0]);
        var messageType = GetMessageType(message[2]);

        if (identifier is null)
        {
            Log.Warning("Identifier is {val}. Aborting;", identifier);
            return null;
        }
        
        if (messageType is null)
        {
            Log.Warning("Message type is {val}. Aborting;", messageType);
            return null;
        }

        outputModel.IdentifierModel = identifier;
        outputModel.MessageType = messageType.Value;
        
        Log.Verbose("Message from client {id}; Type: {type}; ", 
            identifier.Id, 
            messageType);

        // declare connected identifier
        // ConnectedIdentifier = new ClientIdentifier(identifier);        
        
        // 4 will be empty, 5+ - workload
        if(message.Count is < 3 and < 5)
            return outputModel;

        if (message[3].Length > 0)
        {
            Log.Error("Not correct protocol. 4 frame should me empty. Frame len: {len}", message[3].Length);
            return outputModel;
        }

        var workloads = new List<IMessage>();
        for (var i = 4; i != message.Count; i++)
        {
            var workload = GetMessageWorkloadModel(messageType.Value, message[i]);
            if (workload is null)
            {
                Log.Warning("Workload in frame {f}/{s} is null!", i, message.Count);
                continue;
            }
            
            workloads.Add(workload);
        }

        outputModel.Messages = workloads;
        return outputModel;
    }

    public void Send<T>(MessageType type, T model)
    {
        var message = MessagesExtension.Configure(_identifier, type, model);
                
        Socket.Handle.TrySendMultipartBytes(message);
    }
}