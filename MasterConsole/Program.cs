using System.Text.Json;
using Platform;
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

manager.OnMessageReceivedFromRobot += message => 
    Log.Verbose("Message received from {from} type {type} frames: {count}; workload: {w}",
        JsonSerializer.Serialize(message.IdentifierModel), 
        message.MessageType, 
        message.Messages.Count, 
        JsonSerializer.Serialize(message.Messages));