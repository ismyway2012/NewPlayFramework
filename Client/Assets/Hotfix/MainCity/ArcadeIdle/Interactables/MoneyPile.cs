using System.Collections;
using UnityEngine;
using DG.Tweening;

namespace NewPlay.ArcadeIdle
{
    public class MoneyPile : ObjectPile
    {
        [SerializeField, Tooltip("Maximum number of money objects allowed in the pile.")]
        private int maxPile = 120;

        [SerializeField, Range(1, 8), Tooltip("Multiplier for the collection rate based on the number of objects in the pile.")]
        private int collectMultiplier = 2;

        private int hiddenMoney; // The number of money objects that are hidden when the pile reaches the max.
        private bool isCollectingMoney; // Flag to indicate if the collection process is ongoing.
        private int collectRate => objects.Count > 8 ? collectMultiplier : 1; // Rate at which money is collected based on the pile size.

        protected override void Start()
        {
            // Start method is intentionally left blank to prevent altering the stack height for money objects.
        }

        /// <summary>
        /// Method to drop money from the pile and play collection animation.
        /// </summary>
        protected override void Drop()
        {
            if (!isCollectingMoney) return; // Don't drop money if the collection process is not active.

            var moneyObj = PoolManager.Instance.SpawnObject("Money"); // Spawn a money object from the pool.
            moneyObj.transform.position = objects.Peek().transform.position; // Position the money object at the top of the pile.

            // Animate the money object to move towards the player using DOTween.
            moneyObj.transform.DOJump(player.transform.position + Vector3.up * 2, 3f, 1, 0.5f)
                .OnComplete(() => PoolManager.Instance.ReturnObject(moneyObj)); // Return the money object to the pool once the animation completes.

            AudioManager.Instance.PlaySFX(AudioID.Money); // Play the money collection sound effect.
        }

        /// <summary>
        /// Starts the collection process when the player enters the trigger area.
        /// </summary>
        protected override void OnPlayerEnter()
        {
            StartCoroutine(CollectMoney()); // Start the coroutine to collect money from the pile.
        }

        /// <summary>
        /// Coroutine that handles collecting money from the pile and updating the player's money.
        /// </summary>
        IEnumerator CollectMoney()
        {
            isCollectingMoney = true; // Set the flag indicating that money is being collected.

            RestaurantManager.Instance.AdjustMoney(hiddenMoney); // Adjust the player's money by the amount of hidden money.
            hiddenMoney = 0; // Reset hidden money once it's been collected.

            // Collect money objects until the pile is empty or the player exits.
            while (player != null && objects.Count > 0)
            {
                for (int i = 0; i < collectRate; i++) // Collect money based on the current collect rate.
                {
                    if (objects.Count == 0) // Exit the loop if there are no more objects to collect.
                    {
                        isCollectingMoney = false;
                        break;
                    }

                    var removedMoney = objects.Pop(); // Remove the top money object from the pile.
                    PoolManager.Instance.ReturnObject(removedMoney); // Return the object to the pool.
                    RestaurantManager.Instance.AdjustMoney(1); // Increase the player's money by 1 for each collected object.
                }

                // If the collection rate is greater than 1, yield until the next frame, otherwise, wait briefly.
                if (collectRate > 1) yield return null;
                else yield return new WaitForSeconds(0.03f);
            }

            isCollectingMoney = false; // Reset the collecting flag once done.
        }

        /// <summary>
        /// Method to add money to the pile. If the pile has reached its maximum, the money is hidden.
        /// </summary>
        public void AddMoney()
        {
            if (objects.Count < maxPile) // If the pile hasn't reached its max size.
            {
                var moneyObj = PoolManager.Instance.SpawnObject("Money"); // Spawn a new money object.
                AddObject(moneyObj); // Add the new money object to the pile.
            }
            else
            {
                hiddenMoney++; // If the pile is full, increase the hidden money count.
            }

            if (!isCollectingMoney && player != null) // If the collection process is not active and the player is present.
                StartCoroutine(CollectMoney()); // Start the money collection coroutine.
        }
    }
}
