using UnityEngine;
using MoreMountains.Tools;
using System.Collections.Generic;
using NewPlay.ArcadeIdle;
using System.Linq;

public class AIDecisionEquipAlready : AIDecision
{
    List<WeaponStation> stations = new List<WeaponStation>();
    private SurvivorController survivor;

    public override void Initialization()
    {
        base.Initialization();
        survivor = GetComponent<SurvivorController>();
    }
    public override void OnEnterState()
    {
        base.OnEnterState();
        var found = FindObjectsByType<WeaponStation>(FindObjectsSortMode.None);
        stations.Clear();
        stations.AddRange(found);
    }

    public override bool Decide()
    {
        WeaponStation station = null;
        foreach (var item in stations)
        {
            if (item == null) continue;
            if (item.IsQueueFull()) continue;
            if (!survivor.IsEquipNeeded(item.ProductType)) continue;
            if (!item.IsSatisfied(survivor)) continue;
            if (station == null || item.GetQueueCount() < station.GetQueueCount())
            {
                station = item;
            }
        }
        if (station != null)
        {
            _brain.SetMemory("WeaponStation", station);
            return true;
        }
        return false;
    }
}
