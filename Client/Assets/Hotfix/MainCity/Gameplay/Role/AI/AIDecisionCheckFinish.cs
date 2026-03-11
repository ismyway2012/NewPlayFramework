using UnityEngine;
using MoreMountains.Tools;
using System.Collections.Generic;
using NewPlay.ArcadeIdle;
using System.Linq;

public class AIDecisionCheckFinish : AIDecision
{
    SecurityCheckStation station;
    private SurvivorController survivor;

    public override void Initialization()
    {
        base.Initialization();

    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        survivor = GetComponent<SurvivorController>();
        station = _brain.GetMemory<SecurityCheckStation>("SecurityCheckStation");
    }

    public override bool Decide()
    {
        if (survivor != null && survivor.IsCheckFinishied)
        {
            return true;
        }
        return false;
    }
}
