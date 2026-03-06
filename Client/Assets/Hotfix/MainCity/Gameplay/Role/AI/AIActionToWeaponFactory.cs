using UnityEngine;
using MoreMountains.Tools;
using UnityEngine.AI;
using NewPlay.ArcadeIdle;

public class AIActionToWeaponFactory : AIAction
{
    private Animator m_Animator;
    private NavMeshAgent m_Agent;
    private WeaponStation m_Station;
    private SurvivorController m_Survivor;
    public override void Initialization()
    {
        base.Initialization();
        m_Survivor = GetComponent<SurvivorController>();
        m_Animator = GetComponent<Animator>();
        m_Agent = GetComponent<NavMeshAgent>();
    }

    public override void PerformAction()
    {
    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        m_Animator.SetBool("IsMoving", true);
        var target = m_Station.GetQueuePoint();
        _brain.SetMemory("Target", target);
        var station = _brain.GetMemory<WeaponStation>("WeaponStation");
        station.AddSurvivorQueue(m_Survivor);
    }
}
