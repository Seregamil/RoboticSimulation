using DomainLibrary;
using NetMQ;
using NetMQ.Monitoring;

namespace Platform.Interfaces;

public interface IBot
{
    void TransformAction(TransportDto transportDto);
    void SendDataFrame(FrameType frameType);

    void OnAcceptedHost(object? sender, NetMQMonitorSocketEventArgs e);
    void OnAcceptFailed(object? sender, NetMQMonitorErrorEventArgs e);
    void OnDisconnected(object? sender, NetMQMonitorSocketEventArgs e);
    void OnReceiveReady(object? sender, NetMQSocketEventArgs e);

    bool RegisterSensor(ISensor sensor);
    bool UnregisterSensor(long sensorId);
}