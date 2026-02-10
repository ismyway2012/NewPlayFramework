using GameFrameX.Event.Runtime;
using GameFrameX.Runtime;

public enum GameplayEventType
{
    GameOver
}
public class GameplayEventArgs : GameEventArgs
{
    public static readonly string EventId = typeof(GameplayEventArgs).FullName;//.GetHashCode();
    public override string Id => EventId;
    public GameplayEventType EventType { get; private set; }
    public RefParams Params { get; private set; }
    public override void Clear()
    {
        if (Params != null)
            ReferencePool.Release(Params);
    }
    public static GameplayEventArgs Create(GameplayEventType eventType, RefParams eventData = null)
    {
        var instance = ReferencePool.Acquire<GameplayEventArgs>();
        instance.EventType = eventType;
        instance.Params = eventData;
        return instance;
    }
}
