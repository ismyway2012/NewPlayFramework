using UnityEngine;

namespace NewPlay.ArcadeIdle
{
    public class Activator : Interactable
    {
        [SerializeField, Tooltip("The GameObject to be activated/deactivated.")]
        private GameObject linkedObject;

        /// <summary>
        /// Called when the player enters the trigger area. It activates the linked object.
        /// </summary>
        protected override void OnPlayerEnter()
        {
            linkedObject.SetActive(true); // Activates the linked object when the player enters the trigger area.
        }

        /// <summary>
        /// Called when the player exits the trigger area. It deactivates the linked object.
        /// </summary>
        protected override void OnPlayerExit()
        {
            linkedObject.SetActive(false); // Deactivates the linked object when the player exits the trigger area.
        }
    }
}
