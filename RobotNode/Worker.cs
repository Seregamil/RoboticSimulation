using System.Text.Json;
using DomainLibrary;
using MessagePack;
using NetMQ;
using NetMQ.Monitoring;
using NetMQ.Sockets;
using Platform;
using Platform.Sensors;

namespace RobotNode;

public class Worker : BackgroundService
{
    public Worker(ILogger<Worker> logger, 
        IConfiguration configuration)
    {
        var port = configuration.GetValue<int?>("Configuration:PullPort") ?? throw new Exception("Can't get PullPort");

        var robot = new Robot(Guid.NewGuid(), "#botyanya", port);

        robot.OnProducerConnected += socket =>
            logger.LogInformation("Connected producer {}", socket);

        robot.OnProducerDisconnected += () => logger.LogWarning("Producer disconnected");
        
        robot.OnKeyDown += name => logger.LogInformation("Key DOWN: {}", name);
        robot.OnKeyUp += name => logger.LogInformation("Key UP: {}", name);
        robot.OnJoystickUsed += vector => logger.LogInformation("JoyX: {}; JoyY: {}", vector.X, vector.Y);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
    }
}