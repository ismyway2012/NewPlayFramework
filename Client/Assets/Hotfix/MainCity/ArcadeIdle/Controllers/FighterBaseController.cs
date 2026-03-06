using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace NewPlay.ArcadeIdle
{
    public class FighterBaseController : RoleController
    {
        protected int battleZoneAreaIndex = -1;
        protected int battleZoneAreaMask = NavMesh.AllAreas;

        public CampType CampType = CampType.Enemy;

        public AIHealth Health { get; protected set; }

        protected HpBar m_HpBar;
        protected HpBar HpBar
        {
            get
            {
                if (m_HpBar == null)
                {
                    m_HpBar = RestaurantManager.Instance.CreateHpBar();
                    m_HpBar.Target = transform;
                }
                return m_HpBar;
            }
        }

        public virtual void TakeDamage(float damage)
        {
            Health?.TakeDamage(damage);
            HpBar.SetHp(transform, Health.CurrentHP, Health.MaxHP);
        }

        /// <summary>
        /// Cache the Battle Zone NavMesh area index
        /// </summary>
        protected void CacheBattleZoneArea()
        {
            if (battleZoneAreaIndex >= 0)
            {
                return;
            }
            battleZoneAreaIndex = NavMesh.GetAreaFromName("Battle Zone");
            if (battleZoneAreaIndex >= 0)
            {
                battleZoneAreaMask = 1 << battleZoneAreaIndex;
            }
            else
            {
                battleZoneAreaMask = NavMesh.AllAreas;
                Debug.LogWarning($"PlayerController: NavMesh area Battle Zone not found. Falling back to AllAreas.");
            }
        }

        public int BattleAreaMask
        {
            get 
            {
                CacheBattleZoneArea();
                return battleZoneAreaMask;
            }
        }

        /// <summary>
        /// Check if the player is currently in the Battle Zone
        /// </summary>
        public bool IsInBattleZone()
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 1f, BattleAreaMask))
            {
                return (hit.mask & battleZoneAreaMask) != 0;
            }
            return false;
        }
    }
}
