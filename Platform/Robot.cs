using AsyncIO;
using MessagePack;
using NetMQ;
using NetMQ.Monitoring;
using NetMQ.Sockets;
using Platform.Models;
using Serilog.Core;

namespace Platform;

public class Robot
{
    /// <summary>
    /// Unique robot UUID
    /// </summary>
    private readonly Guid _robotGuid;
    
    /// <summary>
    /// Robot name
    /// </summary>
    private readonly string _robotName;
    
    /// <summary>
    /// Reference variable for logger
    /// </summary>
    private readonly Logger? _logger;

    /// <summary>
    /// Pull socket variable
    /// </summary>
    private readonly NetMQSocket _socket;
    
    public delegate void KeyUp(string keyName);
    public delegate void KeyDown(string keyName);
    public delegate void JoystickUsed(Vector2 vector2);
    public delegate void ProducerConnected(AsyncSocket socket);
    public delegate void ProducesDisconnected();
    
    public event KeyUp? OnKeyUp;
    public event KeyDown? OnKeyDown;
    public event JoystickUsed? OnJoystickUsed;
    public event ProducerConnected? OnProducerConnected;
    public event ProducesDisconnected? OnProducerDisconnected;
    
    
    private readonly List<string> _pressedKeyList = new ();

    /// <summary>
    /// Constructor of robot platform
    /// </summary>
    /// <param name="guid">Unique robot UUID</param>
    /// <param name="name">Robot name. Maby non-unique</param>
    /// <param name="pullPort">Port for starting Pull-socket listener</param>
    /// <param name="logger">Logger reference. Optional parameter Maby null.</param>
    public Robot(Guid guid, 
        string name, 
        int pullPort, 
        Logger? logger = null)
    {
        if (logger is not null)
            _logger = logger;
        
        _robotGuid = guid;
        _robotName = name;
        
        _socket = new PairSocket($"@tcp://localhost:{pullPort}");
        
        var mqPoller = new NetMQPoller { _socket };
        var mqMonitor = new NetMQMonitor(_socket, $"inproc://localhost:{pullPort}", SocketEvents.All);
        mqMonitor.AttachToPoller(mqPoller);

        mqMonitor.Accepted += OnAcceptedHost;
        mqMonitor.Disconnected += OnDisconnected;

        mqPoller.RunAsync();
        
        _logger?.Information($"<Platform::Constructor>: Robot {GetName()}:{GetId()} successfully deployed");
    }

    /// <summary>
    /// Use this for get robot unique uuid
    /// </summary>
    /// <returns>Return UUID formatted value</returns>
    public Guid GetId() => _robotGuid;
    
    /// <summary>
    /// Use this for get robot name
    /// </summary>
    /// <returns>Return String value</returns>
    public string GetName() => _robotName;
    
    /// <summary>
    /// Use for getting pressed keys by user. This list will be cleanup when producer disconnected
    /// </summary>
    /// <returns>IEnumerable values of pressed user keys</returns>
    public IEnumerable<string> GetPressedKeys() => _pressedKeyList;

    /// <summary>
    /// Event called when producer connected to consumer and host successfully accepted
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnAcceptedHost(object? sender, NetMQMonitorSocketEventArgs e)
    {
        if (e.Socket != null) 
            OnProducerConnected?.Invoke(e.Socket);
        
        _logger?.Debug($"<Platform::OnAcceptedHost>: Accepted host! {e.Address}");
        _socket.ReceiveReady += OnReceiveReady;
    }

    /// <summary>
    /// Event called when producer disconnected from consumer
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnDisconnected(object? sender, NetMQMonitorSocketEventArgs e)
    {
        _socket.ReceiveReady -= OnReceiveReady;
        _pressedKeyList.Clear();

        _logger?.Debug($"<Platform::OnDisconnected>: Disconnected host! {e.Address}");
        OnProducerDisconnected?.Invoke();
    }

    /// <summary>
    /// Event who used for receiving messages from producer
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnReceiveReady(object? sender, NetMQSocketEventArgs e)
    {
        _logger?.Debug($"<Platform::OnReceiveReady>: Started listener");
        
        var messageStatus = e.Socket.TryReceiveFrameBytes(out var receiveBytes);
        if (!messageStatus)
        {
            _logger?.Error("<Platform::OnReceiveReady>: Can't receive message.");
            return;
        }

        if (receiveBytes is null)
        {
            _logger?.Error("<Platform::OnReceiveReady>: Received null bytes.");
            return;
        }

        var json = MessagePackSerializer.ConvertToJson(receiveBytes);
        
        InputControllerModel model;
        
        try
        {
            model = MessagePackSerializer.Deserialize<InputControllerModel>(receiveBytes);
            _logger?.Debug($"<Platform::OnReceiveReady>: Successfully received bytes array. Len: {receiveBytes.Length}; Message: {json}");
        }
        catch (MessagePackSerializationException err)
        {
            _logger?.Fatal($"<Platform::OnReceiveReady>: Message deserialization error! Message: {json}; Bytes: {receiveBytes.Length}; Error: {err.Message}");
            return;
        }

        // Key event registration
        var nonRegisteredKeysStrings = model.PressedKeys
            .Split('|')
            .ToList();
        
        // Register key press action
        nonRegisteredKeysStrings.Except(_pressedKeyList)
            .ToList()
            .ForEach(x =>
            {
                _pressedKeyList.Add(x);
                OnKeyDown?.Invoke(x);
            });
        
        // Regsiter key up action
        _pressedKeyList.Except(nonRegisteredKeysStrings)
            .ToList()
            .ForEach(x =>
            {
                _pressedKeyList.Remove(x);
                OnKeyUp?.Invoke(x);
            });
        
        OnJoystickUsed?.Invoke(model.Vector2);
    }

    public void Send<T>(T model)
    {
        var serializedData = MessagePackSerializer.Serialize(model);
        var json = MessagePackSerializer.ConvertToJson(serializedData);
        
        if(!_socket.TrySendFrame(serializedData))
            _logger?.Error($"<Platform::Send>: Can't send {json}");
        else
            _logger?.Debug($"<Platform::Send>: Sended {json}");
    }
}