using System.Collections.Generic;
using UnityEngine;

namespace NewPlay.ArcadeIdle
{
    /// <summary>
    /// This is an abstract base class for interactable objects. It provides the functionality for detecting player interaction
    /// when the player enters or exits a trigger zone. Derived classes can implement custom behavior when the player interacts.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public abstract class Interactable : MonoBehaviour
    {

        protected List<RoleController> stayRoles = new List<RoleController>();
        protected PlayerController player { get; private set; }

        /// <summary>
        /// Called when a player enters the trigger zone of the interactable object.
        /// </summary>
        /// <param name="other">The collider of the object that entered the trigger zone.</param>
        protected virtual void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player")) // Checks if the object entering is the player.
            {
                player = other.GetComponent<PlayerController>(); // Retrieves the PlayerController component from the player.
                if (player != null) OnPlayerEnter(); // If a player is detected, triggers the OnPlayerEnter method.
            }
            var role = other.GetComponent<RoleController>();
            if (role != null)
            {
                stayRoles.Add(role);
            }
        }

        /// <summary>
        /// Called when the player exits the trigger zone of the interactable object.
        /// </summary>
        /// <param name="other">The collider of the object that exited the trigger zone.</param>
        protected virtual void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player")) // Checks if the object exiting is the player.
            {
                OnPlayerExit(); // Calls the OnPlayerExit method to handle player exit.
                player = null; // Nullifies the player reference when the player exits the trigger.
            }
            var role = other.GetComponent<RoleController>();
            if (role != null)
            {
                stayRoles.Remove(role);
            }
        }

        protected virtual void OnTriggerStay(Collider other)
        {

        }


        /// <summary>
        /// Virtual method that can be overridden in derived classes to define behavior when the player enters the interactable object.
        /// </summary>
        protected virtual void OnPlayerEnter() { }

        /// <summary>
        /// Virtual method that can be overridden in derived classes to define behavior when the player exits the interactable object.
        /// </summary>
        protected virtual void OnPlayerExit() { }
    }
}
