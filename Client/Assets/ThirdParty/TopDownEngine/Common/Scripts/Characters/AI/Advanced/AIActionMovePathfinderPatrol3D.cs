using MoreMountains.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TextCore.Text;

namespace MoreMountains.TopDownEngine
{
	/// <summary>
	/// This Action will make the Character patrol along the defined path (see the MMPath inspector for that) until it hits a wall or a hole while following a path.
	/// </summary>
	[AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Move Patrol 3D with Pathfinder")]
	//[RequireComponent(typeof(MMPath))]
	//[RequireComponent(typeof(Character))]
	//[RequireComponent(typeof(CharacterMovement))]
	public class AIActionMovePathfinderPatrol3D : AIAction
	{
        [Header("Patrol")]
        public float MiniPatrolRadius = 2f;
        public float PatrolRadius = 5f;
        /// the minimum duration (in seconds) before we update the target's position again
		[Tooltip("the minimum duration (in seconds) before we update the target's position again")]
        public float MinimumDelayBeforeUpdatingTarget = 0.3f;
        /// whether or not to clear the target when exiting the state running this action, stopping the movement
        [Tooltip("whether or not to clear the target when exiting the state running this action, stopping the movement")]
        public bool ClearTargetOnExit = false;

        protected Character _character;
        protected CharacterMovement _characterMovement;
        protected CharacterPathfinder3D _characterPathfinder3D;
        protected float _lastSetNewDestinationAt = -Single.MaxValue;
        protected Health _health;
        protected Vector3 _targetPosition;

        /// <summary>
        /// On init we grab our CharacterMovement ability
        /// </summary>
        public override void Initialization()
        {
            if (!ShouldInitialize) return;
            base.Initialization();
            _character = this.gameObject.GetComponentInParent<Character>();
            _characterMovement = _character?.FindAbility<CharacterMovement>();
            _characterPathfinder3D = _character?.FindAbility<CharacterPathfinder3D>();
            _health = _character.CharacterHealth;
            if (_characterPathfinder3D == null)
            {
                Debug.LogWarning(this.name + " : the AIActionMovePathfinderPatrol3D AI Action requires the CharacterPathfinder3D ability");
            }
        }

        protected virtual void InitializePatrol()
        {
            var old = transform.position;
            _targetPosition = this.transform.position + UnityEngine.Random.insideUnitSphere * UnityEngine.Random.Range(MiniPatrolRadius, PatrolRadius);
            _targetPosition.y = old.y;
            _targetPosition = _characterPathfinder3D.FindClosestPositionOnNavmesh(_targetPosition);

            if (_characterPathfinder3D.AgentPath != null)
            {
                _lastSetNewDestinationAt = Time.time;
                _characterPathfinder3D.SetNewDestination(_targetPosition);
            }
        }

        /// <summary>
        /// On PerformAction we move
        /// </summary>
        public override void PerformAction()
        {
            Move();
        }

        /// <summary>
        /// Moves the character towards the target if needed
        /// </summary>
        protected virtual void Move()
        {
            if (_characterPathfinder3D.NextWaypointIndex == -1)
            {
                InitializePatrol();
                return;
            }
            if (Time.time - _lastSetNewDestinationAt < MinimumDelayBeforeUpdatingTarget)
            {
                return;
            }

            _lastSetNewDestinationAt = Time.time;
            _characterPathfinder3D.SetNewDestination(_targetPosition);
        }

        public override void OnEnterState()
        {
            base.OnEnterState();
            InitializePatrol();
        }

        /// <summary>
        /// On exit state we stop our movement
        /// </summary>
        public override void OnExitState()
        {
            base.OnExitState();

            if (ClearTargetOnExit)
            {
                _characterPathfinder3D?.CleanTarget();
                _characterPathfinder3D?.StopPathfinding();
            }
            _characterPathfinder3D?.SetNewDestination(null);
            _characterMovement?.SetHorizontalMovement(0f);
            _characterMovement?.SetVerticalMovement(0f);
        }

        /// <summary>
        /// When reviving we make sure our directions are properly setup
        /// </summary>
        protected virtual void OnRevive()
		{            
			//InitializePatrol();
		}

		/// <summary>
		/// On enable we start listening for OnRevive events
		/// </summary>
		protected virtual void OnEnable()
		{
			if (_health == null)
			{
				_health = (_character != null) ? _character.CharacterHealth : this.gameObject.GetComponent<Health>();
			}

			if (_health != null)
			{
				_health.OnRevive += OnRevive;
			}
		}

		/// <summary>
		/// On disable we stop listening for OnRevive events
		/// </summary>
		protected virtual void OnDisable()
		{
			if (_health != null)
			{
				_health.OnRevive -= OnRevive;
			}
		}
	}
}