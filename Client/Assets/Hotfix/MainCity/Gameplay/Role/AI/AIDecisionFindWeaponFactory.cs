using UnityEngine;
using MoreMountains.Tools;
using System.Collections.Generic;
using NewPlay.ArcadeIdle;
using System.Linq;

public class AIDecisionFindWeaponFactory : AIDecision
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
        WeaponStation checkStation = null;
        foreach (var item in stations)
        {
            if (item == null) continue;
            if (item.IsQueueFull()) continue;
            if (!survivor.IsRequireEquipment(item.ProductType)) continue;
            if (checkStation == null || item.GetQueueCount() < checkStation.GetQueueCount())
            {
                checkStation = item;
            }
        }
        if (checkStation != null)
        {
            _brain.SetMemory("WeaponStation", checkStation);
            return true;
        }
        return false;
    }

}
