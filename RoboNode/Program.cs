using Platform;
using Serilog;

const int port = 31253;

var log = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

var robot = new Robot(Guid.NewGuid(), "#botyanya", port);

robot.OnProducerConnected += socket => log.Information($"Connected producer {socket}");
robot.OnProducerDisconnected += () => log.Warning("Producer disconnected");
        
robot.OnKeyDown += name => log.Debug($"Key DOWN: {name}");
robot.OnKeyUp += name => log.Debug($"Key UP: {name}");
robot.OnJoystickUsed += vector => log.Debug($"JoyX: {vector.X}; JoyY: {vector.Y}");