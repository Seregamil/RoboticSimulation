using AsyncIO;
using DomainLibrary;
using MessagePack;
using NetMQ;
using NetMQ.Monitoring;
using NetMQ.Sockets;
using Platform.Interfaces;

namespace Platform;

public class Robot
{
    private readonly Guid _robotGuid;
    private readonly string _robotName;

    private readonly PullSocket _pullSocket;
    
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

    public Robot(Guid guid, string name, int pullPort)
    {
        _robotGuid = guid;
        _robotName = name;
        
        _pullSocket = new PullSocket($"@tcp://localhost:{pullPort}");
        
        var mqPoller = new NetMQPoller { _pullSocket };
        var mqMonitor = new NetMQMonitor(_pullSocket, $"inproc://localhost:{pullPort}", SocketEvents.All);
        mqMonitor.AttachToPoller(mqPoller);

        mqMonitor.Accepted += OnAcceptedHost;
        mqMonitor.Disconnected += OnDisconnected;

        _ = Task.Factory.StartNew(() => mqPoller.RunAsync());
    }

    public Guid GetId() => _robotGuid;
    public string GetName() => _robotName;
    public IEnumerable<string> GetPressedKeys() => _pressedKeyList;

    private void TranslateMessageToActions(TransportDto transportDto)
    {
        // Key event registration
        var nonRegisteredKeysStrings = transportDto.PressedKeys
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
        
        OnJoystickUsed?.Invoke(transportDto.Vector2);
    }

    private void OnAcceptedHost(object? sender, NetMQMonitorSocketEventArgs e)
    {
        if (e.Socket != null) 
            OnProducerConnected?.Invoke(e.Socket);
        
        _pullSocket.ReceiveReady += OnReceiveReady;
    }

    private void OnDisconnected(object? sender, NetMQMonitorSocketEventArgs e)
    {
        _pullSocket.ReceiveReady -= OnReceiveReady;
        _pressedKeyList.Clear();

        OnProducerDisconnected?.Invoke();
    }

    private void OnReceiveReady(object? sender, NetMQSocketEventArgs e)
    {
        // get received bytes
        var receiveBytes = e.Socket.ReceiveFrameBytes();
        
        TransportDto model;
        
        // try deserialize DTO model by messagepack
        try
        {
            model = MessagePackSerializer.Deserialize<TransportDto>(receiveBytes);
        }
        catch (MessagePackSerializationException err)
        {
            var json = MessagePackSerializer.ConvertToJson(receiveBytes);
            Console.WriteLine($"[{DateTime.Now}] Received message: {json}; Deserialization error: {err.Message}");
            return;
        }

        TranslateMessageToActions(model);
    }
}