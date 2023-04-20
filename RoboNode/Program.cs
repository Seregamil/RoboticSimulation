using Platform;
using Serilog;

const int port = 31253;
const int numberOfDbToKeep = 2;

// Find all databases with logs
var files = new DirectoryInfo(Directory.GetCurrentDirectory())
    .GetFiles("blackbox*.db")
    .OrderByDescending(f => f.LastWriteTime)
    .ToList();

// Remove count of db who we are need save
files.RemoveRange(0, numberOfDbToKeep);
files.ForEach(x => x.Delete());

// Configure logger
var log = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.LiteDB($"blackbox-{DateTimeOffset.Now.ToUnixTimeSeconds()}.db", "blackbox")
    .WriteTo.Console()
    .CreateLogger();

// Configure platform
var robot = new Robot(Guid.NewGuid(), "#botyanya", port, log);

robot.OnProducerConnected += socket => log.Information($"Connected producer {socket}");
robot.OnProducerDisconnected += () => log.Warning("Producer disconnected");
        
robot.OnKeyDown += name => log.Information($"Key DOWN: {name}");
robot.OnKeyUp += name => log.Information($"Key UP: {name}");
robot.OnJoystickUsed += vector => log.Information($"JoyX: {vector.X}; JoyY: {vector.Y}");