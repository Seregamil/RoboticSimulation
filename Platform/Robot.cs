using System.Text.Json;
using AsyncIO;
using MessagePack;
using NetMQ;
using NetMQ.Monitoring;
using NetMQ.Sockets;
using Platform.Extensions;
using Platform.Handlers;
using Platform.Identifies;
using Platform.Interfaces;
using Platform.Models;
using Serilog;

namespace Platform;

public class Robot : IDisposable
{
    /// <summary>
    /// Pull socket variable
    /// </summary>
    private readonly NetMQSocket _socket;

    /// <summary>
    /// Robo identifier
    /// </summary>
    private readonly Identifier _identifier;

    /// <summary>
    /// Unique connected client identifier
    /// </summary>
    private Identifier? _connectedClientIdentifier;

    public delegate void ProducerConnected(AsyncSocket socket);
    public delegate void ProducesDisconnected();
    public delegate void KeyUp(string key);
    public delegate void KeyDown(string key);
    public delegate void JoystickUsed(Vector2 vector2);
    
    public event KeyUp? OnKeyUp;
    public event KeyDown? OnKeyDown;
    public event JoystickUsed? OnJoystickUsed;
    public event ProducerConnected? OnProducerConnected;
    public event ProducesDisconnected? OnProducerDisconnected;

    private readonly MoveHandler _moveHandler;

    /// <summary>
    /// Constructor of robot platform
    /// </summary>
    /// <param name="guid">Unique robot UUID</param>
    /// <param name="name">Robot name. Maby non-unique</param>
    /// <param name="socketPort">Port for starting Pull-socket listener</param>
    public Robot(Guid guid, 
        string name, 
        int socketPort)
    {
        _identifier = new RoboIdentifier(new IdentifierModel()
        {
            Id = guid,
            Name = name
        });
        
        _moveHandler = new MoveHandler();

        _moveHandler.OnKeyUp += key => OnKeyUp?.Invoke(key);
        _moveHandler.OnKeyDown += key => OnKeyDown?.Invoke(key);
        _moveHandler.OnVectorChanged += vector2 => OnJoystickUsed?.Invoke(vector2);
        
        _socket = new PairSocket($"@tcp://localhost:{socketPort}");
        
        var mqPoller = new NetMQPoller { _socket };
        var mqMonitor = new NetMQMonitor(_socket, $"inproc://localhost:{socketPort}", SocketEvents.All);
        mqMonitor.AttachToPoller(mqPoller);

        mqMonitor.Accepted += OnAcceptedHost;
        mqMonitor.Disconnected += OnDisconnected;

        mqPoller.RunAsync();
        
        Log.Verbose("<Platform::Constructor>: Robot {name}:{id} successfully deployed", GetName(), GetId());
    }

    /// <summary>
    /// Use this for get robot unique uuid
    /// </summary>
    /// <returns>Return UUID formatted value</returns>
    public Guid GetId() => _identifier.Id;

    /// <summary>
    /// Use this for getting connected client identifier model
    /// </summary>
    /// <returns></returns>
    public Identifier? GetConnectedClientIdentifier() => _connectedClientIdentifier;
    
    /// <summary>
    /// Use this for get robot name
    /// </summary>
    /// <returns>Return String value</returns>
    public string GetName() => _identifier.Name;
    
    /// <summary>
    /// Use for getting pressed keys by user. This list will be cleanup when producer disconnected
    /// </summary>
    /// <returns>IEnumerable values of pressed user keys</returns>
    public IEnumerable<string> GetPressedKeys() => _moveHandler.GetPressedKeys();

    /// <summary>
    /// Event called when producer connected to consumer and host successfully accepted
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnAcceptedHost(object? sender, NetMQMonitorSocketEventArgs e)
    {
        if (e.Socket == null)
            return;
        
        OnProducerConnected?.Invoke(e.Socket);

        Log.Verbose("<Platform::OnAcceptedHost>: Accepted host {host}:{port}",
            e.Socket.RemoteEndPoint.Address,
            e.Socket.RemoteEndPoint.Port);
            
        _socket.ReceiveReady += OnReceiveReady;
    }
    
    /// <summary>
    /// Event called when producer disconnected from consumer
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnDisconnected(object? sender, NetMQMonitorSocketEventArgs e)
    {
        Log.Verbose($"<Platform::OnDisconnected>: Disconnected {_connectedClientIdentifier?.Id}");
        
        _socket.ReceiveReady -= OnReceiveReady;
        _moveHandler.Clear();
        
        OnProducerDisconnected?.Invoke();
        _connectedClientIdentifier = null;
    }

    /// <summary>
    /// Event who used for receiving messages from producer
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnReceiveReady(object? sender, NetMQSocketEventArgs e)
    {
        var message = new List<byte[]>();
        var messageStatus = e.Socket.TryReceiveMultipartBytes(ref message);
        if (!messageStatus)
            return;
                
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
        
        Log.Debug("Received multipart bytes message: {msg}", JsonSerializer.Serialize(message));
        
        // so, simple isAlive message will be have next format: 
        // 0 IdentifierModel
        // 1 EmptyFrame
        // 2 MessageType.Healthckeck
        if (message.Count < 3)
        {
            Log.Error("Not full message. Frames: {received}/3; Aborting", message.Count);
            return;
        }

        // Check protocol. Second frame should me empty
        if (message[1].Length > 0)
        {
            Log.Error("Not correct protocol. 2 frame should me empty. Frame len: {len}", message[1].Length);
            return;
        }

        var identifier = GetMessageIdentifierModel(message[0]);
        var messageType = GetMessageType(message[2]);

        if (identifier is null)
        {
            Log.Warning("Identifier is {val}. Aborting;", identifier);
            return;
        }
        
        if (messageType is null)
        {
            Log.Warning("Message type is {val}. Aborting;", messageType);
            return;
        }
        
        Log.Verbose("Message from client {id}; Type: {type}; ", 
            identifier.Id, 
            messageType);

        // declare connected identifier
        _connectedClientIdentifier = new ClientIdentifier(identifier);

        // 4 will be empty, 5+ - workload
        if(message.Count < 3 && message.Count < 5)
            return;

        if (message[3].Length > 0)
        {
            Log.Error("Not correct protocol. 4 frame should me empty. Frame len: {len}", message[3].Length);
            return;
        }
        
        for (var i = 4; i != message.Count; i++)
        {
            var workload = GetMessageWorkloadModel(messageType.Value, message[i]);
            if (workload is null)
            {
                Log.Warning("Workload in frame {f}/{s} is null!", i, message.Count);
                continue;
            }

            _ = messageType switch
            {
                MessageType.Move => _moveHandler.Update((MoveModel) workload),
                MessageType.Alert => true,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
    
    public void Send<T>(T model)
    {
        var serializedData = MessagePackSerializer.Serialize(model);
        var json = MessagePackSerializer.ConvertToJson(serializedData);
        
        if(!_socket.TrySendFrame(serializedData))
            Log.Error($"<Platform::Send>: Can't send {json}");
        else
            Log.Debug($"<Platform::Send>: Sended {json}");
    }

    /// <summary>
    /// Use this method for deserialize input message type from received message
    /// </summary>
    /// <param name="message">Bytes array with encoded message type</param>
    /// <returns></returns>
    private MessageType? GetMessageType(byte[] message) => 
        EncoderExtension.DecodeMessage<MessageType>(message);

    /// <summary>
    /// Use this method for deserialize input identifier of client from received message
    /// Decoder: MessagePack
    /// </summary>
    /// <param name="message">Bytes array with encoded identifier</param>
    /// <returns></returns>
    private IdentifierModel? GetMessageIdentifierModel(byte[] message) => 
        EncoderExtension.DecodeMessage<IdentifierModel>(message);

    /// <summary>
    /// Use this method for deserialize input identifier of client from received message
    /// Decoder: JsonSerializer
    /// </summary>
    /// <param name="message">Bytes array with encoded identifier</param>
    /// <returns></returns>
    private IdentifierModel? GetMessageIdentifierModel(string message) => 
        EncoderExtension.DecodeMessage<IdentifierModel>(message);

    /// <summary>
    /// Use this method for deserialize workload from received message
    /// </summary>
    /// <param name="messageType">Input message tyoe who deserialized by GetMessageType</param>
    /// <param name="message">Bytes array with encoded workload</param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private IMessage? GetMessageWorkloadModel(MessageType messageType, byte[] message)
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
    
    public void Dispose()
    {
        _socket.Dispose();
    }
}