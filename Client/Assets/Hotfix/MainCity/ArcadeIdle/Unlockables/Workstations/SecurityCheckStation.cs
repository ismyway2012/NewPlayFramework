using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NewPlay.ArcadeIdle
{
    public class SecurityCheckStation : Workstation
    {
        [SerializeField, Tooltip("The base time interval (in seconds) between car spawns.")]
        private float baseInterval = 1.5f;

        [SerializeField, Tooltip("The base price for an order at the drive-thru.")]
        private int basePrice = 15;

        [SerializeField, Tooltip("The rate at which the price increases with each upgrade.")]
        private float priceIncrementRate = 1.25f;

        [SerializeField, Tooltip("The base stack capacity for the packages that can be served.")]
        private int baseStack = 30;

        [SerializeField, Tooltip("The transform representing the spawn point for incoming cars.")]
        private Transform spawnPoint;

        [SerializeField, Tooltip("The transform representing the despawn point for cars that have been served.")]
        private Transform despawnPoint;

        [SerializeField, Tooltip("A reference to the waypoints that define the queue path for the cars.")]
        private Waypoints queuePoints;

        [SerializeField, Tooltip("Array of car prefabs that can be spawned in the drive-thru.")]
        private SurvivorController[] carPrefabs;

        [SerializeField, Tooltip("The stack of packages to be served to cars.")]
        private ObjectStack packageStack;

        [SerializeField, Tooltip("The money pile that will receive the payment from customers.")]
        private MoneyPile moneyPile;

        private Queue<SurvivorController> survivorQueue = new Queue<SurvivorController>();  // Queue to manage cars waiting to be served
        public SurvivorController firstSurvivor => survivorQueue.Peek();  // Accessor to get the first car in the queue

        private float spawnInterval;  // Time interval between car spawns (adjusted by unlock level)
        private float serveInterval;  // Time interval between serving cars (adjusted by unlock level)
        private int sellPrice;  // Final sell price after upgrades
        private float spawnTimer;  // Timer for managing car spawn intervals
        private float serveTimer;  // Timer for managing serving intervals
        private bool isFinishingService;  // Flag to indicate when service is being finished for a car

        const int maxCars = 10;  // Maximum number of cars that can wait in the queue at a time

        void Update()
        {
            HandleCarSpawn();  // Manage spawning of new cars
            //HandlePackageServing();  // Handle serving packages to the cars in the queue
        }

        /// <summary>
        /// Updates the workstation stats based on its current unlock level.
        /// Adjusts the spawn and serve intervals, package stack capacity, and sell price.
        /// </summary>
        protected override void UpdateStats()
        {
            // Calculate the spawn interval and serve interval based on the unlock level
            spawnInterval = (baseInterval * 3) - unlockLevel;
            serveInterval = baseInterval / unlockLevel;

            // Update the package stack capacity based on the unlock level
            packageStack.MaxStack = baseStack + unlockLevel * 5;

            // Get the current profit upgrade level and adjust the sell price accordingly
            int profitLevel = RestaurantManager.Instance.GetUpgradeLevel(Upgrade.Profit);
            sellPrice = Mathf.RoundToInt(Mathf.Pow(priceIncrementRate, profitLevel) * basePrice);
        }

        /// <summary>
        /// Manages the spawning of new cars at the drive-thru.
        /// Spawns a new car at the spawn point if the interval has passed and the queue isn't full.
        /// </summary>
        void HandleCarSpawn()
        {
            spawnTimer += Time.deltaTime;  // Increment spawn timer

            // Spawn a new car if the spawn timer has elapsed and the queue isn't full
            if (spawnTimer >= spawnInterval && survivorQueue.Count < maxCars)
            {
                spawnTimer = 0f;  // Reset the spawn timer

                int rand = Random.Range(0, carPrefabs.Length);  // Randomly select a car prefab
                var newCar = Instantiate(carPrefabs[rand], spawnPoint.position, spawnPoint.rotation);  // Spawn the new car
                newCar.ExitPoint = despawnPoint.position;
                survivorQueue.Enqueue(newCar);  // Add the new car to the queue
                
                //newCar.Init(queuePoints, despawnPoint, cars.Count - 1);  // Initialize the car with the queue waypoints and despawn point

                // Assign the customer to the appropriate queue position.
                AssignQueuePoint(newCar, survivorQueue.Count - 1);
            }
        }

        public bool IsQueueFull()
        {
            return survivorQueue.Count >= maxCars;
        }

        public int GetQueueCount()
        {
            return survivorQueue.Count;
        }

        public Transform GetQueuePoint()
        {
            int index = survivorQueue.Count == 0 ? 0 : survivorQueue.Count;
            Transform queuePoint = queuePoints.GetPoint(index);
            return queuePoint;
        }

        /// <summary>
        /// Assigns a customer to a specific queue point.
        /// </summary>
        void AssignQueuePoint(SurvivorController customer, int index)
        {
            Transform queuePoint = queuePoints.GetPoint(index);
            bool isFirst = index == 0;
            // Update the customer's position and status in the queue.
            customer.UpdateQueue(queuePoint);

            if (isFirst)
            {
                customer.PlaceOrder();
            }
        }

        public void AddSurvivorQueue(SurvivorController customer)
        {
            if (survivorQueue.Contains(customer))
            {
                return;
            }
            survivorQueue.Enqueue(customer);  // Add the new car to the queue
            customer.ExitPoint = despawnPoint.position;
            AssignQueuePoint(customer, survivorQueue.Count - 1);
        }

        /// <summary>
        /// Handles serving packages to the cars in the queue.
        /// Serves packages to the first car in the queue if the car has an order and the worker is available.
        /// </summary>
        public void HandlePackageServing()
        {
            // Exit early if there are no cars or the first car has no order
            if (survivorQueue.Count == 0 || !firstSurvivor.HasOrder) return;

            serveTimer += Time.deltaTime;  // Increment serve timer

            // Serve the package if the serve timer has elapsed
            if (serveTimer >= serveInterval)
            {
                serveTimer = 0f;  // Reset the serve timer

                // Check if there is a worker, a package in the stack, and the first car has an order
                if (hasWorker && packageStack.Count > 0 && firstSurvivor.OrderCount > 0)
                {
                    var package = packageStack.RemoveFromStack();  // Get a package from the stack
                    firstSurvivor.FillOrder(package);  // Fill the order for the first car

                    CollectPayment();  // Collect payment for the order
                }

                // If the first car's order is complete, start finishing the service
                if (firstSurvivor.OrderCount == 0 && !isFinishingService)
                    StartCoroutine(FinishServing());
            }
        }

        /// <summary>
        /// Collects the payment for the order from the customer.
        /// Adds the appropriate amount of money to the money pile based on the sell price.
        /// </summary>
        void CollectPayment()
        {
            // Add money to the money pile for each unit of the sell price
            for (int i = 0; i < sellPrice; i++)
            {
                moneyPile.AddMoney();
            }
        }

        /// <summary>
        /// Finishes serving the current car and moves it out of the queue.
        /// Updates the queue for the remaining cars and resets timers.
        /// </summary>
        /// <returns>Returns a coroutine that delays the process by 0.5 seconds before finishing.</returns>
        IEnumerator FinishServing()
        {
            isFinishingService = true;  // Flag that service is finishing

            yield return new WaitForSeconds(0.5f);  // Wait for 0.5 seconds


            var servedCar = firstSurvivor;  // Dequeue the served car
            var time = servedCar.Eat(1);

            yield return new WaitForSeconds(time);  // Wait for 0.5 seconds

            servedCar.OrderInfo.HideInfo();

            while (!servedCar.IsEquipAlready())
            {
                yield return null;  // Wait until the survivor is ready to equip
            }
            survivorQueue.Dequeue();  // Remove the served car from the queue

            //servedCar.ToTraining();  // Make the survivor go to training
            servedCar.SurvivorState = SurvivorState.Checked;

            // Update the queue for the remaining cars
            UpdateQueuePositions();

            serveTimer = 0f;  // Reset the serve timer

            isFinishingService = false;  // Reset the finishing service flag
        }

        /// <summary>
        /// Updates the queue positions of all customers after a customer is served.
        /// </summary>
        public void UpdateQueuePositions()
        {
            int index = 0;
            foreach (var customer in survivorQueue)
            {
                AssignQueuePoint(customer, index);
                index++;
            }
        }
    }
}
