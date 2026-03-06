using UnityEngine;
using DG.Tweening;

namespace NewPlay.ArcadeIdle
{
    public class TrashBin : Interactable
    {
        [SerializeField, Tooltip("The time interval between each object throw")]
        private float throwInterval;

        [SerializeField, Tooltip("The offset direction in which the object will be thrown into the trash bin")]
        private Vector3 throwOffset = new Vector3(0f, 0.1f, 0.5f);

        private float throwCooldown; // Tracks the cooldown for throwing objects

        void Update()
        {
            if (player == null) return; // Exit if no player is assigned

            // Reduce the throwCooldown by the time passed since last frame
            throwCooldown -= Time.deltaTime;

            // If the cooldown is complete, throw an object into the bin
            if (throwCooldown <= 0)
            {
                throwCooldown = throwInterval; // Reset the cooldown timer

                // Get an object from the player's stack
                var thrownObj = player.Stack.RemoveFromStack();
                if (thrownObj == null) return; // Exit if there's no object to throw

                // Play the trash sound effect
                AudioManager.Instance.PlaySFX(AudioID.Trash);

                // Animate the object being thrown into the trash bin
                thrownObj.DOJump(transform.TransformPoint(throwOffset), 5f, 1, 0.5f)
                    .OnComplete(() =>
                    {
                        // Once the object is "thrown", return it to the pool and play a bin sound effect
                        PoolManager.Instance.ReturnObject(thrownObj.gameObject);
                        AudioManager.Instance.PlaySFX(AudioID.Bin);
                    });
            }
        }

        // Method to throw an object from a given WobblingStack into the trash bin
        public void ThrowToBin(WobblingStack stack)
        {
            // Remove an object from the given stack
            var thrownObj = stack.RemoveFromStack();

            // Animate the object being thrown into the trash bin
            thrownObj.DOJump(transform.position + Vector3.up, 5f, 1, 0.5f)
                .OnComplete(() =>
                {
                    // Once the object is "thrown", return it to the pool
                    PoolManager.Instance.ReturnObject(thrownObj.gameObject);
                });
        }
    }
}
