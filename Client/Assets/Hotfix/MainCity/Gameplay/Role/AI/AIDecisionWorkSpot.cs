using UnityEngine;
using MoreMountains.Tools;
using System.Collections.Generic;
using System.Linq;

public class AIDecisionWorkSpot : AIDecision
{
    private Dictionary<int, Collider> _workSpotColliders = new Dictionary<int, Collider>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public override void Initialization()
    {
        base.Initialization();
        _workSpotColliders.Clear();
    }

    public override bool Decide()
    {
        return _workSpotColliders.Values.Sum(e => e != null ? 1 : 0) > 0;
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("WorkSpot"))
        {
            _workSpotColliders[other.GetInstanceID()] = other;
        }
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("WorkSpot"))
        {
            _workSpotColliders.Remove(other.GetInstanceID());
        }
    }

    protected virtual void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("WorkSpot"))
        {
            _workSpotColliders[other.GetInstanceID()] = other;
        }
    }
}
