using DomainLibrary;
using MessagePack;
using NetMQ;
using NetMQ.Monitoring;
using NetMQ.Sockets;
using Platform;

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

        _ = new Robot(Guid.NewGuid(), "#botyanya", port);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
    }
}