using DomainLibrary;
using MessagePack;
using NetMQ;
using NetMQ.Monitoring;
using NetMQ.Sockets;

namespace RobotNode;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;

    private readonly PullSocket _pullSocket;
    private readonly NetMQPoller _mqPoller;
    private readonly NetMQMonitor _mqMonitor;

    private readonly int _pullingPort;

    public Worker(ILogger<Worker> logger, 
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        _pullingPort = configuration.GetValue<int?>("Configuration:PullPort") ?? throw new Exception("Can't get PullPort");
        _pullSocket = new PullSocket($"@tcp://localhost:{_pullingPort}");
        
        _mqPoller = new NetMQPoller { _pullSocket };
        _mqMonitor = new NetMQMonitor(_pullSocket, $"inproc://localhost:{_pullingPort}", SocketEvents.All);
        _mqMonitor.AttachToPoller(_mqPoller);
        
        _mqMonitor.Accepted += (sender, args) =>
        {
            _logger.LogInformation("Successfully accepted {socket}", args.Address);
            _pullSocket.ReceiveReady += PullSocketOnReceiveReady;
        };
        
        _mqMonitor.Disconnected += (sender, args) =>
        {
            _logger.LogCritical("Disconnected {socket}", args.Address);
            _pullSocket.ReceiveReady -= PullSocketOnReceiveReady;
        };
    }

    private void PullSocketOnReceiveReady(object? sender, NetMQSocketEventArgs e)
    {
        var receiveBytes = e.Socket.ReceiveFrameBytes();
        var model = MessagePackSerializer.ConvertToJson(receiveBytes);
                    
        _logger.LogInformation("{len} bytes: {model}", receiveBytes.Length, model);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _mqPoller.RunAsync();
    }
}