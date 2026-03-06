using UnityEngine;
using DG.Tweening;

namespace NewPlay.ArcadeIdle
{
    public class MovingObject : MonoBehaviour
    {
        // The original position of the object, used to reset its movement.
        private Vector3 originalPosition;

        void Start()
        {
            originalPosition = transform.position;
            Move();
        }

        /// <summary>
        /// Moves the object along the Z-axis using a looping animation.
        /// Resets the position before starting the movement.
        /// </summary>
        void Move()
        {
            transform.position = originalPosition; // Reset position to the original.

            transform.DOLocalMoveZ(0.7f, 0.5f) // Move the object 0.7 units along the Z-axis over 0.5 seconds.
                .SetEase(Ease.Linear) // Use a linear easing for consistent speed.
                .OnComplete(() => Move()); // Recursively call Move to repeat the animation.
        }
    }
}
