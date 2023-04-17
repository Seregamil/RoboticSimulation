namespace Platform;

public class Blackbox
{
    /// <summary>
    /// Unique robot Id
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Unique robot name
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Log dateTime
    /// </summary>
    private DateTime DateTime { get; set; } = DateTime.Now;
    
    /// <summary>
    /// Pool of infos about all registered sensors
    /// </summary>
    private List<Sensor> Sensors { get; set; } = new ();

    private Task? _blackBoxTask;
    private bool _collectorEnabled;
    
    public Blackbox(Guid robotId, string robotName)
    {
        Id = robotId;
        Name = robotName;
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
            foreach (var sensor in Sensors)
            {
                if(!sensor.IsAvailable() || !sensor.IsCollectable())
                    continue;
            
                // TODO: Save data into another storage (maby sqlite?)
            }   
            Thread.Sleep(900);
        }
    }

    /// <summary>
    /// Registrations sensor for getting events from him
    /// </summary>
    /// <param name="sensor"></param>
    public void RegisterSensor(Sensor sensor)
    {
        Sensors.Add(sensor);
    }

    /// <summary>
    /// Remove sensor for blackbox
    /// </summary>
    /// <param name="sensorId"></param>
    public void RemoveSensor(long sensorId)
    {
        var sensor = Sensors.FirstOrDefault(x => x.Id == sensorId);
        if(sensor == null)
            return;
        
        Sensors.Remove(sensor);
    }
}