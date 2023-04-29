using AsyncIO;
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
        
        OnMessageReceived += OnMessageSuccessfullyReceived;
        
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
    /// Called when message successfully received and deserialized
    /// </summary>
    /// <param name="messageModel"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private void OnMessageSuccessfullyReceived(MessageModel messageModel)
    {
        ConnectedIdentifier = new ClientIdentifier(messageModel.IdentifierModel);        

        if(messageModel.Messages.Count == 0)
            return;
        
        messageModel.Messages.ForEach(message =>
        {
            _ = messageModel.MessageType switch
            {
                MessageType.Move => _moveHandler.Update((MoveModel) message),
                MessageType.Alert => true,
                _ => throw new ArgumentOutOfRangeException()
            };
        });
    }
}