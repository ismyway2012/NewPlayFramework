using UnityEngine;
using MoreMountains.Tools;
using System.Collections.Generic;
using NewPlay.ArcadeIdle;
using System.Linq;

public class AIDecisionFindSecurityStation : AIDecision
{
    List<SecurityCheckStation> securityCheckStations = new List<SecurityCheckStation>();
    private SurvivorController survivor;

    public override void Initialization()
    {
        base.Initialization();
        survivor = GetComponent<SurvivorController>();

    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        var found = FindObjectsByType<SecurityCheckStation>(FindObjectsSortMode.None);
        securityCheckStations.Clear();
        securityCheckStations.AddRange(found);
    }

    public override bool Decide()
    {
        SecurityCheckStation checkStation = null;
        foreach (var item in securityCheckStations)
        {
            if (item == null) continue;
            if (item.IsQueueFull()) continue;
            if (checkStation == null || item.GetQueueCount() < checkStation.GetQueueCount())
            {
                checkStation = item;
            }
        }
        if (checkStation != null)
        {
            _brain.SetMemory("SecurityCheckStation", checkStation);
            return true;
        }
        return false;
    }
}
