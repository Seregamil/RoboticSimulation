using NetMQ;
using NetMQ.Monitoring;
using NetMQ.Sockets;

namespace Platform.Extensions;

public class SocketExtension
{
    private readonly NetMQSocket _socket;
    private readonly NetMQPoller _poller;
    private readonly NetMQMonitor _monitor;

    public SocketExtension(string address)
    {
        _socket = new PairSocket(address);  
        _poller = new NetMQPoller { _socket };

        var inprocAddress = address.Replace("tcp", "inproc");
        _monitor = new NetMQMonitor(_socket, inprocAddress.Replace("@", ""), SocketEvents.All);
        _monitor.AttachToPoller(_poller);
    }

    public NetMQSocket Handle => _socket;
    public NetMQMonitor Monitor => _monitor;

    public void Run()
    {
        _poller.RunAsync();
    }
}