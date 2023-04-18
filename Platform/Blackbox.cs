namespace Platform;

public class Blackbox
{
    private readonly Robot _robot;
    
    /// <summary>
    /// Log dateTime
    /// </summary>
    private DateTime DateTime { get; set; } = DateTime.Now;
    
    private Task? _blackBoxTask;
    private bool _collectorEnabled;
    
    public Blackbox(Robot robot)
    {
        _robot = robot;
        _blackBoxTask = null;

        StartCollecting();
    }

    ~Blackbox()
    {
        StopCollecting();
    }

    public void StartCollecting()
    {
        _collectorEnabled = true;
        _blackBoxTask = Task.Factory.StartNew(BlackboxCollectorTask);
    }

    public void StopCollecting()
    {
        _collectorEnabled = false;
    }
    
    private async Task BlackboxCollectorTask()
    {
        while (_collectorEnabled)
        {
            _robot.GetSensors()
                .Where(sensor => sensor.IsAvailable() && sensor.IsCollectable())
                .ToList()
                .ForEach(sensor =>
                {
                    // TODO: Save data into another storage (maby sqlite?)
                    // Console.WriteLine($"Robot {_robot.GetId()}:{_robot.GetName()}; Sensor: {sensor.GetInfo()}");
                });
  
            Thread.Sleep(900);
        }
    }
}