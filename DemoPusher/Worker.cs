using NetMQ;
using NetMQ.Monitoring;
using NetMQ.Sockets;
using Platform;
using Platform.Extensions;
using Platform.Identifies;
using Platform.Models;
using Serilog;

namespace DemoPusher;

public class Worker : BackgroundService
{
    private readonly NetMQSocket _socket;
    private readonly NetMQPoller _mqPoller;

    private Identifier _identifier;
    
    private const bool NeedGyroscopeToJoystickConversion = true;

    public Worker(IConfiguration configuration)
    {
        var model = new IdentifierModel()
        {
            Id = Guid.NewGuid(),
            Name = "#zaebot"
        };

        _identifier = new ClientIdentifier(model);
        
        var pushAddress = configuration.GetValue<string?>("Configuration:PushAddress") ?? throw new Exception("Can't get PushAddress");
        _socket = new PairSocket(pushAddress);
        _mqPoller = new NetMQPoller{ _socket };
        
        var mqMonitor = new NetMQMonitor(_socket, $"inproc://{pushAddress}", SocketEvents.All);
        mqMonitor.AttachToPoller(_mqPoller);
        
        mqMonitor.Connected += (_, args) =>
        {
            Log.Information("Successfully connected to {socket}", args.Address);

            _socket.SendReady += PushSocketOnSendReady;     
            _socket.ReceiveReady += (sender, eventArgs) =>
            {

            };
        };

        mqMonitor.Accepted += (_, args) =>
        {
            Log.Information("Accepted {host}", args.Address);
        };
        
        mqMonitor.Disconnected += (_, args) =>
        {
            Log.Error("Disconnected from {socket}", args.Address);
            _socket.SendReady -= PushSocketOnSendReady;
        };
        
        mqMonitor.AcceptFailed += (_, args) =>
        {
            Log.Error("Cant accept {}; Err: {}", args.Address, args.ErrorCode);
        };

        mqMonitor.ConnectRetried += (_, args) =>
        {
            Log.Warning("Connection retried: {}", args.Address);
        };
    }

    private void PushSocketOnSendReady(object? sender, NetMQSocketEventArgs e)
    {
        var message = MessagesExtension.Configure(_identifier, MessageType.Alert, 
            new AlertModel()
                {
                    Message = "Help",
                    Timestamp = DateTimeOffset.Now.ToUnixTimeSeconds()
                });
        
        _socket.TrySendMultipartBytes(message);

        var pressedList = Guid.NewGuid()
            .ToString("N")[..8]
            .ToUpper()
            .Select(x => x);
        
        var pressed = string.Join('|', pressedList);
        
        message = MessagesExtension.Configure(_identifier, MessageType.Move, 
            new MoveModel()
                {
                    Vector2 = new Vector2(0.2f, 0.1f),
                    PressedKeys = pressed
                });
                
        _socket.TrySendMultipartBytes(message);
        
        Thread.Sleep(5000);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _mqPoller.RunAsync();
    }
}