using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace NewPlay.ArcadeIdle
{
    public class PackingTable : Workstation
    {
        [SerializeField, Tooltip("Time interval for packing operations")]
        private float baseInterval = 1.5f;

        [SerializeField, Tooltip("Base stack size for the food stack")]
        private int baseStack = 30;

        [SerializeField, Tooltip("Transform for the packaging area")]
        private Transform packageBox;

        [SerializeField, Tooltip("Stack of food items to be packed")]
        private ObjectStack foodStack;

        [SerializeField, Tooltip("Pile where packed packages will be stored")]
        private ObjectPile packagePile;

        [SerializeField, Tooltip("Maximum number of food items per package")]
        private int maxFoodInPackage = 4;  // Maximum number of food items per package

        private float packingInterval;  // Adjusted time interval for packing
        private int packingCapacity;  // Capacity of food stack after upgrades
        private float packingTimer;  // Timer to control packing intervals
        private int packingProgress;  // Track the progress of packing food items


        /// <summary>
        /// Updates the workstation stats based on the current unlock level.
        /// Adjusts the packing interval and packing capacity.
        /// </summary>
        protected override void UpdateStats()
        {
            // Calculate the packing interval based on the unlock level
            packingInterval = baseInterval / unlockLevel;

            // Update the packing capacity of the food stack based on the unlock level
            int maxStack = baseStack + unlockLevel * 5;
            packingCapacity = foodStack.MaxStack = maxStack;
        }

        void Update()
        {
            HandlePacking();
        }

        /// <summary>
        /// Handles the process of packing food items into packages.
        /// Moves food items from the food stack to the package box with animation.
        /// </summary>
        void HandlePacking()
        {
            packingTimer += Time.deltaTime;  // Increment the packing timer

            // Check if it's time to pack a food item and if there's space in the package pile
            if (packingTimer >= packingInterval && packagePile.Count < packingCapacity)
            {
                packingTimer = 0f;  // Reset the packing timer

                // Ensure there's a worker and food to pack
                if (hasWorker && foodStack.Count > 0)
                {
                    // Remove a food item from the stack
                    Transform food = foodStack.RemoveFromStack();

                    // Animate the food item jumping to the appropriate position in the package box
                    food.DOJump(packageBox.GetChild(packingProgress).position, 5f, 1, 0.3f)
                        .OnComplete(() =>
                        {
                            packingProgress++;  // Increment the packing progress

                            // Return the food object to the pool
                            PoolManager.Instance.ReturnObject(food.gameObject);

                            // Activate the packed food item in the package box
                            packageBox.GetChild(packingProgress - 1).gameObject.SetActive(true);

                            // Check if the package is full, and reset the progress if needed
                            if (packingProgress == maxFoodInPackage)
                            {
                                packingProgress = 0;  // Reset the progress

                                // Start the process of finishing the packing
                                StartCoroutine(FinishPacking());
                            }
                        });
                }
            }
        }

        /// <summary>
        /// Finalizes the packing process.
        /// Creates a new package and adds it to the package pile, then resets the package box.
        /// </summary>
        /// <returns>Returns a coroutine that waits for the animation to finish before finalizing the packing.</returns>
        IEnumerator FinishPacking()
        {
            yield return new WaitForSeconds(0.1f);  // Short delay before finishing packing

            // Hide the package box to prepare for the next package
            packageBox.gameObject.SetActive(false);

            // Deactivate all children in the package box
            foreach (Transform child in packageBox)
            {
                child.gameObject.SetActive(false);
            }

            // Spawn a new package from the pool and position it at the package box
            var package = PoolManager.Instance.SpawnObject("Package");
            package.transform.position = packageBox.position;

            // Animate the package to the package pile and wait for the animation to complete
            yield return package.transform.DOJump(packagePile.PeakPoint, 5f, 1, 0.5f).WaitForCompletion();

            // Add the packed package to the package pile
            packagePile.AddObject(package);

            // Reactivate the package box for future packing
            packageBox.gameObject.SetActive(true);
        }
    }
}
