using System.Collections;
using UnityEngine;
using DG.Tweening;

namespace NewPlay.ArcadeIdle
{
    public class TravelingObject : MonoBehaviour
    {
        [SerializeField, Tooltip("The Waypoints component defining the path for the object to travel.")]
        private Waypoints waypoints;

        // The MeshRenderer component of the object, used to toggle visibility.
        private MeshRenderer meshRenderer;

        // The initial position of the object, used for resetting its position.
        private Vector3 originalPosition;

        /// <summary>
        /// Handles the initial setup of the object, including visibility delay and starting travel.
        /// </summary>
        IEnumerator Start()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            originalPosition = transform.position;

            meshRenderer.enabled = false;

            float delay = transform.GetSiblingIndex(); // Delay based on sibling index in hierarchy.
            yield return new WaitForSeconds(delay);

            meshRenderer.enabled = true;

            Travel();
        }

        /// <summary>
        /// Moves the object along a predefined path and loops the motion.
        /// Also adds a rotation effect midway through the animation.
        /// </summary>
        void Travel()
        {
            transform.DOPath(waypoints.GetPoints().ToArray(), 5f) // Moves the object along the waypoints over 5 seconds.
                .SetEase(Ease.Linear) // Linear easing for consistent speed.
                .OnComplete(() =>
                {
                    transform.position = originalPosition; // Resets to the starting position after completion.
                    Travel(); // Repeats the motion.
                });

            transform.DOLocalRotate(new Vector3(360, 0, 0), 0.3f, RotateMode.LocalAxisAdd) // Adds a rotation effect.
                .SetDelay(2.5f); // Starts the rotation halfway through the path animation.
        }
    }
}
