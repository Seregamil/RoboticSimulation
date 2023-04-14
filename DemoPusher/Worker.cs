using DomainLibrary;
using MessagePack;
using NetMQ;
using NetMQ.Monitoring;
using NetMQ.Sockets;

namespace DemoPusher;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    
    private readonly PushSocket _pushSocket;
    private readonly NetMQPoller _mqPoller;
    private readonly NetMQMonitor _mqMonitor;

    private const bool NeedGyroscopeToJoystickConversion = true;

    public Worker(ILogger<Worker> logger, 
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        
        var pushAddress = configuration.GetValue<string?>("Configuration:PushAddress") ?? throw new Exception("Can't get PushAddress");
        _pushSocket = new PushSocket(pushAddress);
        _mqPoller = new NetMQPoller
        {
            _pushSocket 
        };
        
        _mqMonitor = new NetMQMonitor(_pushSocket, $"inproc://{pushAddress}", SocketEvents.All);
        _mqMonitor.AttachToPoller(_mqPoller);
        
        _mqMonitor.Connected += (sender, args) =>
        {
            _logger.LogInformation("Successfully connected to {socket}", args.Address);
            _pushSocket.SendReady += PushSocketOnSendReady;
        };
        
        _mqMonitor.Disconnected += (sender, args) =>
        {
            _logger.LogCritical("Disconnected from {socket}", args.Address);
            _pushSocket.SendReady -= PushSocketOnSendReady;
        };
    }

    private void PushSocketOnSendReady(object? sender, NetMQSocketEventArgs e)
    {
        var random = new Random();
        var data = new TransportDto(new Vector2(random.NextSingle(), random.NextSingle()), "Q|E|R");
        
        if(NeedGyroscopeToJoystickConversion)
            data.GyroscopeToJoystickConversion(); 
        
        var serialized = data.Serialize();
        var json = MessagePackSerializer.ConvertToJson(serialized);
                    
        if (_pushSocket.TrySendFrame(serialized))
        {
            _logger.LogInformation("Sended {message}", json);
        }
        else
        {
            _logger.LogCritical("Cant send {message}", json);
        }
                    
        Thread.Sleep(1000);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _mqPoller.RunAsync();
    }
}