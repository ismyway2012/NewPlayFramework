using UnityEngine;

namespace NewPlay.ArcadeIdle
{
    public class FoodMachine : Unlockable
    {
        [SerializeField, Tooltip("Base time interval for food production in seconds.")]
        private float baseInterval = 1.5f;

        [SerializeField, Tooltip("Base capacity for food storage.")]
        private int baseCapacity = 6;

        [SerializeField, Tooltip("Reference to the pile where produced food is stored.")]
        private ObjectPile foodPile;

        private float productionInterval; // Adjusted production interval based on the unlock level.
        private int productionCapacity;  // Adjusted capacity based on the unlock level.
        private float productionTimer;   // Tracks elapsed time for food production.

        /// <summary>
        /// Updates the production interval and capacity based on the current unlock level.
        /// </summary>
        protected override void UpdateStats()
        {
            // Reduce the production interval and increase capacity as the unlock level increases.
            productionInterval = baseInterval / unlockLevel;
            productionCapacity = baseCapacity * unlockLevel;
        }

        void Update()
        {
            // If the food pile has reached the current capacity, stop producing.
            if (foodPile.Count >= productionCapacity) return;

            // Increment the production timer by the time passed since the last frame.
            productionTimer += Time.deltaTime;

            // Check if the production interval has been met.
            if (productionTimer >= productionInterval)
            {
                productionTimer = 0f; // Reset the timer.

                // Spawn a food object from the pool and add it to the food pile.
                var food = PoolManager.Instance.SpawnObject("Food");
                foodPile.AddObject(food);
            }
        }
    }
}
