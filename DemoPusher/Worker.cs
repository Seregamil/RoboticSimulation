using MessagePack;
using NetMQ;
using NetMQ.Monitoring;
using NetMQ.Sockets;
using Platform;
using Platform.Models;

namespace DemoPusher;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    
    private readonly NetMQSocket _socket;
    private readonly NetMQPoller _mqPoller;

    private const bool NeedGyroscopeToJoystickConversion = true;

    public Worker(ILogger<Worker> logger, 
        IConfiguration configuration)
    {
        _logger = logger;
        
        var pushAddress = configuration.GetValue<string?>("Configuration:PushAddress") ?? throw new Exception("Can't get PushAddress");
        _socket = new PairSocket(pushAddress);
        _mqPoller = new NetMQPoller{ _socket };
        
        var mqMonitor = new NetMQMonitor(_socket, $"inproc://{pushAddress}", SocketEvents.All);
        mqMonitor.AttachToPoller(_mqPoller);
        
        mqMonitor.Connected += (_, args) =>
        {
            _logger.LogInformation("Successfully connected to {socket}", args.Address);
            _socket.SendReady += PushSocketOnSendReady;
            _socket.ReceiveReady += (sender, eventArgs) =>
            {
                while (true)
                {
                    if (!eventArgs.Socket.TryReceiveFrameBytes(out var data))
                        return;

                    _logger.LogInformation($"Received: {MessagePackSerializer.ConvertToJson(data)}");
                }
            };
        };
        
        mqMonitor.Disconnected += (_, args) =>
        {
            _logger.LogCritical("Disconnected from {socket}", args.Address);
            _socket.SendReady -= PushSocketOnSendReady;
        };
        
        mqMonitor.AcceptFailed += (_, args) =>
        {
            _logger.LogCritical("Cant accept {}; Err: {}", args.Address, args.ErrorCode);
        };

        mqMonitor.ConnectRetried += (_, args) =>
        {
            _logger.LogWarning("Connection retried: {}", args.Address);
        };
    }

    private void PushSocketOnSendReady(object? sender, NetMQSocketEventArgs e)
    {
        var random = new Random();
        var data = new InputControllerModel(new Vector2(random.NextSingle(), random.NextSingle()), "Q|E|R");
        
        if(NeedGyroscopeToJoystickConversion)
            data.GyroscopeToJoystickConversion(); 
        
        var serialized = data.Serialize();
        var json = MessagePackSerializer.ConvertToJson(serialized);
                    
        if (_socket.TrySendFrame(serialized))
        {
            // _logger.LogInformation("Sended {message}", json);
        }
        else
        {
            // _logger.LogCritical("Cant send {message}", json);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _mqPoller.RunAsync();
    }
}