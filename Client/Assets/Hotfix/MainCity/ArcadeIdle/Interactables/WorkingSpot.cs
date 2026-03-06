using UnityEngine;
using UnityEngine.UI;

namespace NewPlay.ArcadeIdle
{
    public class WorkingSpot : Interactable
    {
        [SerializeField, Tooltip("Reference to the UI Image that indicates the working spot's status")]
        private Image indicatorImage;

        // Property that checks if there is a worker (i.e., if the player is assigned to the spot)
        public bool HasWorker => player != null;// || stayRoles.Exists(e => e != null && e.gameObject.CompareTag("Worker"));

        // This method is called when the player enters the working spot
        protected override void OnPlayerEnter()
        {
            indicatorImage.color = Color.green; // Change the indicator color to green when a worker (player) enters
        }

        // This method is called when the player exits the working spot
        protected override void OnPlayerExit()
        {
            indicatorImage.color = Color.red; // Change the indicator color to red when no worker (player) is present
        }
    }
}
