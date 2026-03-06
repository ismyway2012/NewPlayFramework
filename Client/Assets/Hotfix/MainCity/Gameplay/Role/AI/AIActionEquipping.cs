using MoreMountains.Tools;
using NewPlay.ArcadeIdle;
using UnityEngine;
using UnityEngine.AI;

public class AIActionEquipping : AIAction
{
    private SurvivorController m_Survivor;
    private Animator m_Animator;

    public override void Initialization()
    {
        base.Initialization();
        m_Survivor = GetComponent<SurvivorController>();
        m_Animator = GetComponent<Animator>();
    }

    public override void PerformAction()
    {
    }

    public override void OnEnterState()
    {
        var station = _brain.GetMemory<WeaponStation>("WeaponStation");
        m_Animator.SetBool("Working", true);
    }
}
