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
        m_Station = _brain.GetMemory<WeaponStation>("WeaponStation");
        var target = m_Station.GetQueuePoint();
        m_Agent.destination = target.position;
        _brain.SetMemory("Target", target);
        m_Station.AddSurvivorQueue(m_Survivor);
    }
}
