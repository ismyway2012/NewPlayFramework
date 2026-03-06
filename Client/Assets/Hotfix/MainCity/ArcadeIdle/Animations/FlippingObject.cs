using UnityEngine;
using DG.Tweening;

namespace NewPlay.ArcadeIdle
{
    public class FlippingObject : MonoBehaviour
    {
        void Start()
        {
            Flip();
        }

        /// <summary>
        /// Handles the flipping animation of the object. 
        /// Includes a random delay, a jump animation, and a 180-degree rotation.
        /// Automatically loops upon completion.
        /// </summary>
        void Flip()
        {
            float delay = Random.Range(1f, 3f); // Random delay between flips.
            var sequence = DOTween.Sequence();

            sequence.SetDelay(delay);
            sequence.Append(transform.DOJump(transform.position, 1f, 1, 0.5f)); // Makes the object jump with animation.
            sequence.Join(transform.DOLocalRotate(new Vector3(0, 0, 180), 0.5f, RotateMode.LocalAxisAdd)); // Rotates the object 180 degrees locally.
            sequence.OnComplete(() => Flip()); // Recursively calls Flip to repeat the animation.
        }
    }
}
