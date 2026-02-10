using GameFrameX.Runtime;
using GameFrameX.Event.Runtime;

public enum GFEventType
{
    ApplicationQuit //游戏退出
}
public class GFEventArgs : GameEventArgs
{
    public static readonly string EventId = typeof(GFEventArgs).FullName;//.GetHashCode();
    public override string Id => EventId;
    public GFEventType EventType { get; private set; }
    public object UserData { get; private set; }
    public override void Clear()
    {
        UserData = null;
    }
    public static GFEventArgs Create(GFEventType eventType, object userDt = null)
    {
        var instance = ReferencePool.Acquire<GFEventArgs>();
        instance.EventType = eventType;
        instance.UserData = userDt;
        return instance;
    }
}
