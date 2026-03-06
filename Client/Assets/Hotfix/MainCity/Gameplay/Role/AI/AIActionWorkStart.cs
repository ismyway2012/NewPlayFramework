using UnityEngine;
using MoreMountains.Tools;

public class AIActionWorkStart : AIAction
{
    private Animator m_Animator;
    protected override void Awake()
    {
        base.Awake();
        m_Animator = GetComponent<Animator>();
    }
    public override void PerformAction()
    {
    }

    public override void OnEnterState()
    {
        m_Animator.SetBool("Working", true);
    }
}
