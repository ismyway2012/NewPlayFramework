using UnityEngine;
using MoreMountains.Tools;
using UnityEngine.AI;
using NewPlay.ArcadeIdle;

public class AIDecisionArrivedSecurityCheck : AIDecision
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
    public override bool Decide()
    {
        // Check if the NavMeshAgent has reached its destination
        if (!m_NavMeshAgent.pathPending)
        {
            if (m_NavMeshAgent.remainingDistance <= m_NavMeshAgent.stoppingDistance)
            {
                if (!m_NavMeshAgent.hasPath || m_NavMeshAgent.velocity.sqrMagnitude == 0f)
                {
                    return true;
                }
            }
        }
        return m_NavMeshAgent.isStopped;
    }
}
