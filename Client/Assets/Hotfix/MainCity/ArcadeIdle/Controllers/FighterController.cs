using DG.Tweening;
using NewPlay.ArcadeIdle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.Android.Gradle.Manifest;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace NewPlay.ArcadeIdle
{
    public enum FighterState
    {
        Out = 0,
        Idle,
        Patrol,
        Chase,
        Attack,
        Dead
    }

    public enum CampType
    {
        Friendly,
        Enemy,
        Neutral,
    }

    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class FighterController : FighterBaseController
    {

        [SerializeField]
        private Transform[] attackSlots;
        [SerializeField]
        private GameObject[] effects;

        [SerializeField]
        private int maxHP = 1000;

        [SerializeField]
        private WeaponData data;

        [SerializeField]
        private DropListData dropData;

        private AIBrain brain;
        private Animator animator;
        private NavMeshAgent agent;

        public FighterState State => brain.state;

        void Awake()
        {
            Health = new AIHealth(maxHP);
            animator = GetComponent<Animator>();
            agent = GetComponent<NavMeshAgent>();

        }

        public void Relive()
        {
            Health.ResetHealth();
            agent.enabled = true;
            agent.isStopped = false;
            if (brain != null)
            {
                brain.Reset();
            }
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            var perception = new AIPerception(this, 20f);

            var movement = new AIMovement(agent);
            var combat = new AICombat(5f, 1f, 100f);

            brain = new AIBrain(
                this,
                perception,
                movement,
                combat,
                Health
            );
            Health.OnDead -= HandleDead;
            Health.OnDead += HandleDead;
        }

        // Update is called once per frame
        void Update()
        {
            brain.Tick(Time.deltaTime);

            if (brain.state != FighterState.Dead)
            {
                // Updates the customer's movement state based on the NavMeshAgent's velocity.
                animator.SetBool("IsMoving", agent.velocity.sqrMagnitude > 0.1f);
            }
        }


        void HandleDead()
        {
            //if (brain.state == FighterState.Dead) return;

            // 1️⃣ 停止 AI & 移动
            agent.isStopped = true;
            agent.enabled = false;
            Drop();
            // // 2️⃣ 播放死亡动画
            // if (animator != null)
            // {
            //     animator.SetTrigger("Dead");
            // }
            // else
            {
                // 没动画兜底
                Recycle();
            }
        }

        void Drop()
        {
            if (dropData == null || dropData.maxDropNum < 1 || dropData.dropOjbects == null || dropData.dropOjbects.Count == 0)
            {
                return;
            }
            int dropNum = Random.Range(0, dropData.maxDropNum + 1);
            if (dropNum == 0)
            {
                return;
            }

            Dictionary<int, ObjectPile> objectStacks = new Dictionary<int, ObjectPile>();
            int num = 0;
            while (num < dropNum)
            {
                for (int i = 0; i < dropData.dropOjbects.Count; i++)
                {
                    var dropItemData = dropData.dropOjbects[i];
                    if (!objectStacks.TryGetValue(i, out var stack))
                    {
                        var dropPile = PoolManager.Instance.SpawnObject(dropItemData.dropPilePrefab);
                        var dropPos = transform.position + Random.insideUnitSphere * dropData.dropRange;
                        dropPos.y = transform.position.y;
                        dropPile.transform.position = dropPos;
                        stack = dropPile.GetComponent<ObjectPile>();
                    }

                    var dropItemGo = PoolManager.Instance.SpawnObject(dropItemData.dropOjbectPrefab);
                    stack.AddObject(dropItemGo);
                    var startPos = transform.position + new Vector3(0, 1.5f, 0);
                    dropItemGo.transform.position = startPos;
                    dropItemGo.transform.DOJump(stack.PeakPoint, 5f, 1, 0.5f);

                    num++;
                    if (num >= dropNum)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Animation Event 调用
        /// </summary>
        public void OnDeathEnd()
        {
            Recycle();
        }

        private void Recycle()
        {
            //回收对象
            PoolManager.Instance.ReturnObject(gameObject);
        }


        public void PlayEffect(string effectName)
        {
            foreach (var effect in effects)
            {
                if (effect.name == effectName)
                {
                    effect.SetActive(true);
                }
            }
        }

        public void PlaySound(string soundName)
        {

        }

        public void Attack(string args)
        {
            var parameters = args.Split('|');
            int.TryParse(parameters[0], out int slotIndex);
            var attackSlot = GetAttackSlot(slotIndex);

            var effect = data.castingEffect;
            if (string.IsNullOrEmpty(effect))
            {
                return;
            }
            //Debug.LogError($"AttackSpot >>> skill {data.skill}: effect={effect}");
            var go = SpawnEffect(effect);
            if (go == null)
            {
                Debug.LogError("Effect not found: " + effect);
                return;
            }
            var tf = go.transform;
            tf.SetParent(transform.parent, false);
            tf.position = attackSlot.position;
            var dir = brain.CurrentTarget.transform.position - attackSlot.position;
            var time = dir.magnitude / Mathf.Max(1, data.flySpeed);
            tf.forward = dir.normalized;
            go.SetActive(true);
            tf.DOKill();
            tf.DOMove(brain.CurrentTarget.transform.position, time).OnComplete(() =>
            {
                if (go != null)
                {
                    UnspawnEffect(go);
                    ShowHitEffect(brain.CurrentTarget.transform.position, data.hitEffect);
                }
            });
        }

        private void UnspawnEffect(GameObject go)
        {
            if (go == null)
            {
                return;
            }
            PoolManager.Instance.ReturnObject(go);
        }

        private GameObject SpawnEffect(string effect)
        {
            if (string.IsNullOrEmpty(effect))
            {
                return null;
            }
            return PoolManager.Instance.SpawnObject(effect);
        }

        private Transform GetAttackSlot(int index)
        {
            if (attackSlots == null || attackSlots.Length == 0)
            {
                return transform;
            }
            var spot = attackSlots[index % attackSlots.Length];
            if (spot == null)
            {
                return transform;
            }
            return spot;
        }


        private void ShowHitEffect(Vector3 target, string hitEffect)
        {
            if (string.IsNullOrEmpty(hitEffect))
            {
                return;
            }
            if (gameObject == null || !gameObject.activeInHierarchy)
            {
                return;
            }
            //Debug.LogError($"PlayOnHitEffect >>> skill {data.skill}: effect={data.hitEffect}");
            var go = SpawnEffect(hitEffect);
            if (go == null)
            {
                return;
            }
            go.transform.SetParent(transform.parent, false);
            go.transform.position = target;
            go.SetActive(true);
            //go.PlayParticles();
            StartCoroutine(DoHideHitEffect(go));
        }

        private IEnumerator DoHideHitEffect(GameObject go)
        {
            yield return new WaitForSeconds(2f);
            UnspawnEffect(go);
        }

        [System.Serializable]
        public class WeaponData
        {
            public int skill;
            public string castingEffect;
            public string hitEffect;
            public float flySpeed;
            public int damage;
        }


        [System.Serializable]
        public class DropListData
        {
            [Tooltip("最大掉落数量")]
            public int maxDropNum;

            [Tooltip("掉落列表")]
            public List<DropItemData> dropOjbects;

            public float dropRange = 5f;
        }

        [System.Serializable]
        public class DropItemData
        {
            [Tooltip("掉落权重")]
            public int dropWeight;
            public string dropPilePrefab;

            [Tooltip("掉落道具模型")]
            public string dropOjbectPrefab;
        }

    }
    public sealed class AICombat
    {
        private float cooldown;
        private float timer;

        public float AttackRange { get; }
        public float Damage { get; }

        public AICombat(float attackRange, float cooldown, float damage)
        {
            AttackRange = attackRange;
            this.cooldown = cooldown;
            Damage = damage;
        }

        public void Tick(float delta)
        {
            timer += delta;
        }

        public bool CanAttack() => timer >= cooldown;

        public void Attack(FighterBaseController target)
        {
            timer = 0f;

            var hp = target.Health;
            if (hp != null)
            {
                target.TakeDamage(Damage);
            }
        }
    }


    public sealed class AIBrain
    {
        public FighterState state { get; private set; }

        private readonly AIPerception perception;
        private readonly AIMovement movement;
        private readonly AICombat combat;
        private readonly AIHealth health;
        private readonly FighterBaseController self;

        private Vector3 patrolPoint;
        private Vector3 doorPoint;

        public FighterBaseController CurrentTarget => perception?.CurrentTarget;

        public AIBrain(
            FighterBaseController self,
            AIPerception perception,
            AIMovement movement,
            AICombat combat,
            AIHealth health)
        {
            this.self = self;
            this.perception = perception;
            this.movement = movement;
            this.combat = combat;
            this.health = health;

            health.OnDead += OnDead;

            if (!self.IsInBattleZone())
            {
                doorPoint = self.transform.position;
                doorPoint.z = 80;
                state = FighterState.Out;
            }
            else
            {
                state = FighterState.Patrol;
            }
            GeneratePatrolPoint();
        }

        public void Reset()
        {
            state = FighterState.Patrol;
            GeneratePatrolPoint();
        }

        public void Tick(float delta)
        {
            perception.Tick();
            combat.Tick(delta);

            switch (state)
            {
                case FighterState.Patrol:
                    UpdatePatrol();
                    break;
                case FighterState.Chase:
                    UpdateChase();
                    break;
                case FighterState.Attack:
                    UpdateAttack();
                    break;
                case FighterState.Out:
                    UpdateOut();
                    break;
            }
        }

        void UpdateOut()
        {
            movement.MoveTo(doorPoint);

            if (movement.IsArrived(0.1f) && self.IsInBattleZone())
            {
                movement.SetAreaMask(self.BattleAreaMask);
                Reset();
            }
        }

        void UpdatePatrol()
        {
            if (perception.CurrentTarget != null)
            {
                state = FighterState.Chase;
                return;
            }

            movement.MoveTo(patrolPoint);

            if (movement.IsArrived(0.1f))
                GeneratePatrolPoint();
        }

        void UpdateChase()
        {
            var target = perception.CurrentTarget;
            if (target == null)
            {
                state = FighterState.Patrol;
                return;
            }

            float dist = Vector3.Distance(self.transform.position, target.transform.position);

            if (dist <= combat.AttackRange)
            {
                movement.Stop();
                state = FighterState.Attack;
            }
            else
            {
                movement.MoveTo(target.transform.position);
            }
        }

        void UpdateAttack()
        {
            var target = perception.CurrentTarget;
            if (target == null)
            {
                state = FighterState.Patrol;
                return;
            }

            float dist = Vector3.Distance(self.transform.position, target.transform.position);
            if (dist > combat.AttackRange)
            {
                state = FighterState.Chase;
                return;
            }

            if (combat.CanAttack())
            {
                combat.Attack(target);
            }
        }

        void GeneratePatrolPoint()
        {
            patrolPoint = self.transform.position + Random.insideUnitSphere * 6f;
            patrolPoint.y = self.transform.position.y;
        }

        private void OnDead()
        {
            state = FighterState.Dead;
            movement.Stop();
        }

    }


    public sealed class AIMovement
    {
        public readonly NavMeshAgent agent;

        public AIMovement(NavMeshAgent agent)
        {
            this.agent = agent;
        }

        public void SetAreaMask(int mask)
        {
            agent.areaMask = mask;
        }

        public void MoveTo(Vector3 pos)
        {
            if (!agent.enabled) return;
            agent.isStopped = false;
            agent.SetDestination(pos);
        }

        public void Stop()
        {
            if (!agent.enabled) return;
            agent.isStopped = true;
        }

        public bool IsArrived(float stopDistance)
        {
            if (agent.pathPending) return false;
            return agent.remainingDistance <= stopDistance;
        }
    }

    public sealed class AIPerception
    {
        private readonly FighterBaseController self;
        private readonly float viewRadius;

        public FighterBaseController CurrentTarget { get; private set; }

        public AIPerception(FighterBaseController self, float viewRadius)
        {
            this.self = self;
            this.viewRadius = viewRadius;
        }

        private LayerMask enemyLayer
        {
            get
            {
                return self.CampType switch
                {
                    CampType.Friendly => (LayerMask)LayerMask.GetMask("Enemy", "Neutral"),
                    CampType.Enemy => (LayerMask)LayerMask.GetMask("Friendly", "Player", "Neutral"),
                    CampType.Neutral => (LayerMask)LayerMask.GetMask("Friendly", "Player", "Enemy"),
                    _ => (LayerMask)(-1 & ~(1 << self.gameObject.layer)),//All except self
                };
            }
        }

        public void Tick()
        {
            Collider[] hits = Physics.OverlapSphere(
                self.transform.position,
                viewRadius,
                enemyLayer
            );

            float minDist = float.MaxValue;
            FighterController nearest = null;

            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                var enemy = hit.GetComponent<FighterController>();
                if (enemy == null || enemy.CampType == self.CampType || enemy.State == FighterState.Dead)
                {
                    continue;
                }
                float dist = Vector3.SqrMagnitude(
                    hit.transform.position - self.transform.position
                );

                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = enemy;
                }
            }

            CurrentTarget = nearest;
        }
    }

    public sealed class AIHealth
    {
        public float MaxHP { get; }
        public float CurrentHP { get; private set; }

        public bool IsDead => CurrentHP <= 0;

        public event System.Action OnDead;
        public event Action<float> OnDamaged;

        public AIHealth(float maxHp)
        {
            MaxHP = maxHp;
            CurrentHP = maxHp;
        }

        public void TakeDamage(float damage)
        {
            if (IsDead) return;

            CurrentHP -= damage;
            OnDamaged?.Invoke(damage);

            if (CurrentHP <= 0)
            {
                CurrentHP = 0;
                OnDead?.Invoke();
            }
        }

        public void ResetHealth()
        {
            CurrentHP = MaxHP;
        }
    }



}
