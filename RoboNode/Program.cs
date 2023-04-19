using Platform;

const int port = 31253;

var robot = new Robot(Guid.NewGuid(), "#botyanya", port);

robot.OnProducerConnected += socket => Console.WriteLine($"Connected producer {socket}");
robot.OnProducerDisconnected += () => Console.WriteLine("Producer disconnected");
        
robot.OnKeyDown += name => Console.WriteLine($"Key DOWN: {name}");
robot.OnKeyUp += name => Console.WriteLine($"Key UP: {name}");
robot.OnJoystickUsed += vector => Console.WriteLine($"JoyX: {vector.X}; JoyY: {vector.Y}");