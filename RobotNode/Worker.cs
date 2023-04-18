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
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;

    public Worker(ILogger<Worker> logger, 
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        var port = configuration.GetValue<int?>("Configuration:PullPort") ?? throw new Exception("Can't get PullPort");

        var robot = new Robot(Guid.NewGuid(), "#botyanya", port);

        robot.OnSensorRegistered += sensor => Console.WriteLine($"Sensor {sensor.GetId()} was registered");
        robot.OnSensorDisposed += sensor => Console.WriteLine($"Sensor {sensor.GetId()} was disposed");

        robot.OnKeyDown += name => Console.WriteLine($"Key DOWN: {name}");
        robot.OnKeyUp += name => Console.WriteLine($"Key UP: {name}");
        
        robot.RegisterSensor(new Temperature(12, 1, "TestSensor"));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
    }
}