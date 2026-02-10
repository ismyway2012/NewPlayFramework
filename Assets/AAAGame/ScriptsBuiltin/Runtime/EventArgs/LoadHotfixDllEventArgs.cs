using GameFrameX.Event.Runtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadHotfixDllEventArgs : GameEventArgs
{
    public readonly static string EventId = typeof(LoadHotfixDllEventArgs).FullName;//.GetHashCode();
    public override string Id => EventId;
    public string DllName { get; private set; }
    public System.Reflection.Assembly Assembly { get; private set; }
    public object UserData { get; private set; }
    public LoadHotfixDllEventArgs Fill(string dllName, System.Reflection.Assembly dll, object userdata)
    {
        this.DllName = dllName;
        this.Assembly = dll;
        this.UserData = userdata;
        return this;
    }
    public override void Clear()
    {
        this.DllName = default;
        this.Assembly = null;
        this.UserData = null;
    }
}
