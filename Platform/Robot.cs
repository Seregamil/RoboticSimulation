using DomainLibrary;
using MessagePack;
using NetMQ;
using NetMQ.Monitoring;
using NetMQ.Sockets;
using Platform.Interfaces;
using Platform.Sensors;

namespace Platform;

public class Robot : IBot, IDisposable
{
    private readonly Blackbox _blackbox;

    private readonly Guid _robotGuid;
    private readonly string _robotName;

    private readonly PullSocket _pullSocket;
    private readonly NetMQMonitor _mqMonitor;
    private readonly NetMQPoller _mqPoller;
    private readonly Task _pollerTask;

    public delegate void SensorRegistered(ISensor sensor);
    public delegate void SensorDisposed(ISensor sensor);
    public delegate void KeyUp(string keyName);
    public delegate void KeyDown(string keyName);
    
    public event SensorRegistered OnSensorRegistered;
    public event SensorDisposed OnSensorDisposed;
    public event KeyUp OnKeyUp;
    public event KeyDown OnKeyDown;
    
    
    private readonly List<ISensor> _sensors = new ();
    private readonly List<string> _pressedKeyList = new ();

    public Robot(Guid guid, string name, int pullPort)
    {
        _robotGuid = guid;
        _robotName = name;
        
        _pullSocket = new PullSocket($"@tcp://localhost:{pullPort}");
        
        _mqPoller = new NetMQPoller { _pullSocket };
        _mqMonitor = new NetMQMonitor(_pullSocket, $"inproc://localhost:{pullPort}", SocketEvents.All);
        _mqMonitor.AttachToPoller(_mqPoller);

        _mqMonitor.Accepted += OnAcceptedHost;
        _mqMonitor.Disconnected += OnDisconnected;
        _mqMonitor.AcceptFailed += OnAcceptFailed;

        _pollerTask = Task.Factory.StartNew(() => _mqPoller.RunAsync());
        
        _blackbox = new Blackbox(this);
    }

    public List<ISensor> GetSensors() => _sensors;
    public Guid GetId() => _robotGuid;
    public string GetName() => _robotName;
    
    public void TransformAction(TransportDto transportDto)
    {
        // Key event registration
        var nonRegisteredKeysStrings = transportDto.PressedKeys
            .Split('|')
            .ToList();
        
        var nonRegisteredKeys = nonRegisteredKeysStrings.Except(_pressedKeyList).ToList();
        var nonUnregisteredKeys = _pressedKeyList.Except(nonRegisteredKeysStrings).ToList(); 
        
        nonRegisteredKeys.ForEach(x =>
        {
            _pressedKeyList.Add(x);
            OnKeyDown.Invoke(x);
        });
        
        nonUnregisteredKeys.ForEach(x =>
        {
            _pressedKeyList.Remove(x);
            OnKeyUp.Invoke(x);
        });
    }

    public void SendDataFrame(FrameType frameType)
    {
        throw new NotImplementedException();
    }

    public void OnAcceptedHost(object? sender, NetMQMonitorSocketEventArgs e)
    {
        _pullSocket.ReceiveReady += OnReceiveReady;
    }

    public void OnAcceptFailed(object? sender, NetMQMonitorErrorEventArgs e)
    {
        throw new NotImplementedException();
    }

    public void OnDisconnected(object? sender, NetMQMonitorSocketEventArgs e)
    {
        _pullSocket.ReceiveReady -= OnReceiveReady;
    }

    public void OnReceiveReady(object? sender, NetMQSocketEventArgs e)
    {
        // get received bytes
        var receiveBytes = e.Socket.ReceiveFrameBytes();
        
        // TODO: Remove. Its for debug
        var json = MessagePackSerializer.ConvertToJson(receiveBytes);

        TransportDto? model = null;
        
        // try deserialize DTO model by messagepack
        try
        {
            model = MessagePackSerializer.Deserialize<TransportDto>(receiveBytes);
            Console.WriteLine("Message {0} bytes: {1}", json, receiveBytes.Length);
        }
        catch (MessagePackSerializationException err)
        {
            Console.WriteLine("Deserialization error; message: {0} len: {1} error: {2}", json, 
                receiveBytes.Length,
                err.Message);
        }
        
        // Check for model is null
        if(model is null)
            return;
        
        TransformAction(model);
    }

    public bool RegisterSensor(ISensor sensor)
    {
        _sensors.Add(sensor);
        
        OnSensorRegistered.Invoke(sensor);
        return true;
    }

    public bool UnregisterSensor(long sensorId)
    {
        var sensor = _sensors.FirstOrDefault(x => x.GetId() == sensorId);
        if (sensor is null)
            return false;

        _sensors.Remove(sensor);
        
        OnSensorDisposed.Invoke(sensor);
        return true;
    }

    public void Dispose()
    {
        _pollerTask.Dispose();
        _pullSocket.Dispose();
        _mqMonitor.Dispose();
        _mqPoller.Dispose();
    }
}