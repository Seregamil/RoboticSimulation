using DomainLibrary;
using MessagePack;
using NetMQ;
using NetMQ.Monitoring;
using NetMQ.Sockets;
using Platform.Interfaces;

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
        
        _blackbox = new Blackbox(_robotGuid, _robotName);
    }

    public void TransformAction(TransportDto transportDto)
    {
        throw new NotImplementedException();
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
        var receiveBytes = e.Socket.ReceiveFrameBytes();
        var json = MessagePackSerializer.ConvertToJson(receiveBytes);

        TransportDto model;
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
    }

    public void Dispose()
    {
        _pollerTask.Dispose();
        _pullSocket.Dispose();
        _mqMonitor.Dispose();
        _mqPoller.Dispose();
    }
}