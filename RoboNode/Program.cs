using Platform;
using Serilog;

const int port = 31253;

var log = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

var robot = new Robot(Guid.NewGuid(), "#botyanya", port, log);

robot.OnProducerConnected += socket => log.Information($"Connected producer {socket}");
robot.OnProducerDisconnected += () => log.Warning("Producer disconnected");
        
robot.OnKeyDown += name => log.Information($"Key DOWN: {name}");
robot.OnKeyUp += name => log.Information($"Key UP: {name}");
robot.OnJoystickUsed += vector => log.Information($"JoyX: {vector.X}; JoyY: {vector.Y}");

Console.Read();