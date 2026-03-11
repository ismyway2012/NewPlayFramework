using UnityEngine;
using MoreMountains.Tools;
using UnityEngine.AI;
using NewPlay.ArcadeIdle;

public class AIActionSecurityCheck : AIAction
{
    private Animator m_Animator;
    private NavMeshAgent m_NavMeshAgent;
    private SurvivorController m_Survivor;
    private SecurityCheckStation station;
    public override void Initialization()
    {
        base.Initialization();
        m_Survivor = GetComponent<SurvivorController>();
        m_Animator = GetComponent<Animator>();
        m_NavMeshAgent = GetComponent<NavMeshAgent>();
    }

    public override void PerformAction()
    {
        station.HandlePackageServing();
    }

    public override void OnEnterState()
    {
        base.OnEnterState();
        station = _brain.GetMemory<SecurityCheckStation>("SecurityCheckStation");
        //m_Animator.SetBool("IsMoving", true);
        //var target = station.GetQueuePoint();
        //_brain.SetMemory("Target", target);
        //station.AddSurvivorQueue(m_Survivor);
    }
}
