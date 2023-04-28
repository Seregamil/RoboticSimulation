using System.Text.Json;
using AsyncIO;
using NetMQ;
using NetMQ.Monitoring;
using Platform.Handlers;
using Platform.Identifies;
using Platform.Models;
using Serilog;

namespace Platform;

public class Robot : Entity
{
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
    /// <param name="address">address Pull-socket listener</param>
    public Robot(Guid guid, string name, string address) 
        : base(guid, name, address)
    {
        _moveHandler = new MoveHandler();

        _moveHandler.OnKeyUp += key => OnKeyUp?.Invoke(key);
        _moveHandler.OnKeyDown += key => OnKeyDown?.Invoke(key);
        _moveHandler.OnVectorChanged += vector2 => OnJoystickUsed?.Invoke(vector2);
        
        Socket.Monitor.Accepted += OnAcceptedHost;
        Socket.Monitor.Disconnected += OnDisconnected;
        
        Log.Verbose("<Platform::Constructor>: Robot {name}:{id} successfully deployed", GetName(), GetId());
    }

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
    protected override void OnAcceptedHost(object? sender, NetMQMonitorSocketEventArgs e)
    {
        base.OnAcceptedHost(sender, e);
        if (e.Socket == null)
            return;

        OnProducerConnected?.Invoke(e.Socket);
    }

    /// <summary>
    /// Event called when producer disconnected from consumer
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected override void OnDisconnected(object? sender, NetMQMonitorSocketEventArgs e)
    {
        base.OnDisconnected(sender, e);
        
        _moveHandler.Clear();
        OnProducerDisconnected?.Invoke();
    }

    /// <summary>
    /// Event who used for receiving messages from producer
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected override void OnReceiveReady(object? sender, NetMQSocketEventArgs e)
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
        ConnectedIdentifier = new ClientIdentifier(identifier);        
        
        // 4 will be empty, 5+ - workload
        if(message.Count is < 3 and < 5)
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
}