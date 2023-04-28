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
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.LiteDB($"blackbox-{DateTimeOffset.Now.ToUnixTimeSeconds()}.db", "blackbox")
    .WriteTo.Console()
    .CreateLogger();

// Configure platform
var robot = new Robot(Guid.NewGuid(), "#botyanya", port);

robot.OnProducerConnected += socket => Log.Debug($"Connected producer {socket}");
robot.OnProducerDisconnected += () => Log.Debug("Producer disconnected");
        
robot.OnKeyDown += s => Log.Verbose("Key DOWN: {s}", s);
robot.OnKeyUp += s => Log.Verbose("Key UP: {s}", s);
robot.OnJoystickUsed += v => Log.Verbose("JoyX: {x}; JoyY: {y}", v.X, v.Y);