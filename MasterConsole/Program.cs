using System.Text.Json;
using Platform;
using Platform.Models;
using Serilog;

const int port = 31253;

// Configure logger
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.Console()
    .CreateLogger();

// Configure platform
var manager = new Manager(Guid.NewGuid(), "#ebobot",  $"tcp://localhost:{port}");

manager.OnClientDisconnected += () => Log.Warning("Disconnected from Robot");

manager.OnClientConnectedToRobot += (socket) => 
    Log.Information("Connected to Robot {addr}:{p}", 
        socket.RemoteEndPoint.Address, socket.RemoteEndPoint.Port);

manager.OnMessageReceived += message => 
    Log.Verbose("Message received from {from} type {type} frames: {count}; workload: {w}",
        JsonSerializer.Serialize(message.IdentifierModel), 
        message.MessageType, 
        message.Messages.Count, 
        JsonSerializer.Serialize(message.Messages));

manager.OnMessageSendReady += () =>
{
    manager.Send(MessageType.Alert, 
        new AlertModel()
        {
            Message = "Help",
            Timestamp = DateTimeOffset.Now.ToUnixTimeSeconds()
        });

    var pressedList = Guid.NewGuid()
        .ToString("N")[..8]
        .ToUpper()
        .Select(x => x);
        
    var pressed = string.Join('|', pressedList);
        
    manager.Send(MessageType.Move, 
        new MoveModel()
        {
            Vector2 = new Vector2(0.2f, 0.1f),
            PressedKeys = pressed
        });
                
    Thread.Sleep(5000);
};