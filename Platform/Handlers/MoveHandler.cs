using Platform.Interfaces;
using Platform.Models;

namespace Platform.Handlers;

public class MoveHandler : IHandler
{
    private MoveModel _model;
    
    /// <summary>
    /// List of all pressed keys
    /// </summary>
    private readonly List<string> _pressedKeyList;

    public delegate void KeyUp(string key);
    public delegate void KeyDown(string key);
    public delegate void VectorChanged(Vector2 vector2);
    
    public event VectorChanged? OnVectorChanged;
    public event KeyDown? OnKeyUp;
    public event KeyUp? OnKeyDown;

    public MoveHandler()
    {
        _model = new MoveModel();
        _pressedKeyList = new List<string>();
    }

    /// <summary>
    /// Use this for get all pressed keys in this moment
    /// </summary>
    /// <returns></returns>
    public IEnumerable<string> GetPressedKeys() => _pressedKeyList;
    
    /// <summary>
    /// Clear all data when client disconnected from platform
    /// </summary>
    public void Clear()
    {
        // event all keys unpressed
        _pressedKeyList.ForEach(x => OnKeyDown?.Invoke(x));
        
        // clear all keys from mem
        _pressedKeyList.Clear();
    }
    
    /// <summary>
    /// Method for decompose message and trigger all actions who has in workload model
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public bool Update(IMessage model)
    {
        _model = (MoveModel) model;
        
        // Key event registration
        var nonRegisteredKeysStrings = _model.PressedKeys
            .Split('|')
            .ToList();
        
        // Register key press action
        nonRegisteredKeysStrings.Except(_pressedKeyList)
            .ToList()
            .ForEach(x =>
            {
                _pressedKeyList.Add(x);
                OnKeyDown?.Invoke(x);
            });
        
        // Regsiter key up action
        _pressedKeyList.Except(nonRegisteredKeysStrings)
            .ToList()
            .ForEach(x =>
            {
                _pressedKeyList.Remove(x);
                OnKeyUp?.Invoke(x);
            });
        
        OnVectorChanged?.Invoke(_model.Vector2);

        return true;
    }
}