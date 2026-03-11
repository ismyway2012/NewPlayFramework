using MoreMountains.Tools;
using UnityEngine;

namespace NewPlay.ArcadeIdle
{
    public class Workstation : Unlockable
    {
        [SerializeField, Tooltip("The working spot where the worker performs tasks")]
        private WorkingSpot workingSpot;

        [SerializeField, Tooltip("The worker object assigned to this workstation")]
        public RoleController worker;

        // Required by Tutorial system
        public Vector3 WorkingPoint => workingSpot.transform.position;

        /// <summary>
        /// Checks if the workstation has a worker based on the unlock level.
        /// If the unlock level is greater than 1, the workstation always has a worker.
        /// Otherwise, it checks if the working spot has a worker.
        /// </summary>
        public virtual bool hasWorker => workingSpot.HasWorker || worker != null;

        /// <summary>
        /// Unlocks the workstation, enabling or disabling the worker and working spot.
        /// </summary>
        /// <param name="animate">If true, an animation is triggered when unlocking the workstation.</param>
        public override void Unlock(bool animate = true)
        {
            base.Unlock(animate);  // Call base class Unlock method for general unlocking functionality

            // Enable / Disable worker based on unlock level (worker becomes active only after level 1)
            //worker.SetActive(unlockLevel > 1);

            // Enable / Disable working spot based on unlock level (working spot is only active before level 1)
            //workingSpot.gameObject.SetActive(unlockLevel <= 1);
        }

        public void SetWorker(RoleController newWorker)
        {
            worker = newWorker;
            newWorker.GetComponent<MoreMountains.Tools.AIBrain>()?.TransitionToState("Working");
        }
    }
}
