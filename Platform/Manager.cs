using AsyncIO;
using NetMQ;
using NetMQ.Monitoring;
using Platform.Extensions;
using Platform.Identifies;
using Platform.Models;
using Serilog;

namespace Platform;

public class Manager : Entity
{
    private readonly ClientIdentifier _identifier;
    public event ClientConnected? OnClientConnectedToRobot;
    public event ClientDisconnected? OnClientDisconnected;
    public event ClientMessageReceived? OnMessageReceivedFromRobot;
    
    public delegate void ClientConnected(AsyncSocket socket);
    public delegate void ClientDisconnected();
    public delegate void ClientMessageReceived(MessageModel messageModel);
    
    
    public Manager(Guid guid, string name, string address) 
        : base(guid, name, address)
    {
        _identifier = new ClientIdentifier(new IdentifierModel
        {
            Id = guid,
            Name = name
        });
        
        OnMessageReceived += OnMessageSuccessfullyReceivedFromRobot;
        
        Socket.Monitor.Connected += OnClientConnected;
        Socket.Monitor.Disconnected += OnDisconnected;
        
        Log.Verbose("<Platform::Constructor>: Manager {name}:{id} successfully deployed", GetName(), GetId());
    }

    private void OnMessageSuccessfullyReceivedFromRobot(MessageModel messageModel)
    {
        OnMessageReceivedFromRobot?.Invoke(messageModel);
    }

    private void OnClientConnected(object? sender, NetMQMonitorSocketEventArgs e)
    {
        if(e.Socket is null)
            return;
        
        OnClientConnectedToRobot?.Invoke(e.Socket);
        
        Socket.Handle.SendReady += HandleOnSendReady;
    }

    protected override void OnDisconnected(object? sender, NetMQMonitorSocketEventArgs e)
    {
        base.OnDisconnected(sender, e);
        
        OnClientDisconnected?.Invoke();
    }

    private void HandleOnSendReady(object? sender, NetMQSocketEventArgs e)
    {
        var message = MessagesExtension.Configure(_identifier, MessageType.Alert, 
            new AlertModel()
            {
                Message = "Help",
                Timestamp = DateTimeOffset.Now.ToUnixTimeSeconds()
            });
        
        Socket.Handle.TrySendMultipartBytes(message);

        var pressedList = Guid.NewGuid()
            .ToString("N")[..8]
            .ToUpper()
            .Select(x => x);
        
        var pressed = string.Join('|', pressedList);
        
        message = MessagesExtension.Configure(_identifier, MessageType.Move, 
            new MoveModel()
            {
                Vector2 = new Vector2(0.2f, 0.1f),
                PressedKeys = pressed
            });
                
        Socket.Handle.TrySendMultipartBytes(message);
        
        Thread.Sleep(5000);
    }
}