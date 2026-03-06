using UnityEngine;
using MoreMountains.Tools;
using UnityEngine.AI;
using NewPlay.ArcadeIdle;

public class AIActionToSecurityCheck : AIAction
{
    private Animator m_Animator;
    private NavMeshAgent m_NavMeshAgent;
    private SurvivorController m_Survivor;
    public override void Initialization()
    {
        base.Initialization();
        m_Survivor = GetComponent<SurvivorController>();
        m_Animator = GetComponent<Animator>();
        m_NavMeshAgent = GetComponent<NavMeshAgent>();
    }

    public override void PerformAction()
    {
    }

    public override void OnEnterState()
    {
        m_Animator.SetBool("IsMoving", true);
        var station = _brain.GetMemory<SecurityCheckStation>("SecurityCheckStation");
        var target = station.GetQueuePoint();
        _brain.SetMemory("Target", target);
        station.AddSurvivorQueue(m_Survivor);
    }
}
