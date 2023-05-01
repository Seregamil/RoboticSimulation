using AsyncIO;
using NetMQ;
using NetMQ.Monitoring;
using Serilog;

namespace Platform;

public class Manager : Entity
{
    public event ClientConnected? OnClientConnectedToRobot;
    public event ClientDisconnected? OnClientDisconnected;
    
    public delegate void ClientConnected(AsyncSocket socket);
    public delegate void ClientDisconnected();
    
    public Manager(Guid guid, string name, string address) 
        : base(guid, name, address)
    {
        Socket.Monitor.Connected += OnClientConnected;
        Socket.Monitor.Disconnected += OnDisconnected;
        
        Log.Verbose("<Platform::Constructor>: Manager {name}:{id} successfully deployed", GetName(), GetId());
    }


    private void OnClientConnected(object? sender, NetMQMonitorSocketEventArgs e)
    {
        if(e.Socket is null)
            return;
        
        OnClientConnectedToRobot?.Invoke(e.Socket);
    }

    protected override void OnDisconnected(object? sender, NetMQMonitorSocketEventArgs e)
    {
        base.OnDisconnected(sender, e);
        
        OnClientDisconnected?.Invoke();
    }
}