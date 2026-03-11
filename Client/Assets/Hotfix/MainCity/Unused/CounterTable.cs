using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NewPlay.ArcadeIdle
{
    public class CounterTable : Workstation
    {
        [SerializeField, Tooltip("Base time interval for customer spawn in seconds.")]
        private float baseInterval = 1.5f;

        [SerializeField, Tooltip("Base price of food served at the counter.")]
        private int basePrice = 5;

        [SerializeField, Tooltip("Rate of price increase per profit upgrade.")]
        private float priceIncrementRate = 1.25f;

        [SerializeField, Tooltip("Base stack capacity of the food stack.")]
        private int baseStack = 30;

        [SerializeField, Tooltip("Point where customers spawn.")]
        private Transform spawnPoint;

        [SerializeField, Tooltip("Point where customers exit after being served.")]
        private Transform despawnPoint;

        [SerializeField, Tooltip("Waypoints defining the customer queue.")]
        private Waypoints queuePoints;

        [SerializeField, Tooltip("Prefab for customer objects.")]
        private CustomerController customerPrefab;

        [SerializeField, Tooltip("Stack containing the food available for serving.")]
        private ObjectStack foodStack;

        [SerializeField, Tooltip("Pile where earned money is stored.")]
        private MoneyPile moneyPile;

        // Properties required by Tutorial System
        public int FoodCount => foodStack.Count;
        public bool HasWorker => hasWorker;
        public int UnlockLevel => unlockLevel;
        public Vector3 FoodPoint => foodStack.transform.position;
        public Vector3 MoneyPoint => moneyPile.transform.position;

        private Queue<CustomerController> customers = new Queue<CustomerController>(); // Tracks customers in the queue.
        private CustomerController firstCustomer => customers.Peek(); // Reference to the first customer in the queue.
        private List<Seating> seatings; // List of available seating areas.

        private float spawnInterval; // Time interval between spawning customers.
        private float serveInterval; // Time interval between serving food.
        private int sellPrice; // Current price of food based on profit upgrades.
        private float spawnTimer; // Tracks elapsed time for spawning customers.
        private float serveTimer; // Tracks elapsed time for serving food.

        const int maxCustomers = 10; // Maximum number of customers allowed in the queue.

        void Start()
        {
            // Initializes seatings at the start.
            seatings = FindObjectsOfType<Seating>(true).ToList();
        }

        void Update()
        {
            // Handles customer spawning and food serving.
            HandleCustomerSpawn();
            HandleFoodServing();
        }

        /// <summary>
        /// Updates stats such as spawn interval, serve interval, and stack capacity.
        /// </summary>
        protected override void UpdateStats()
        {
            // Spawn interval decreases, serve interval decreases, and food stack capacity increases with unlock level.
            spawnInterval = (baseInterval * 3) - unlockLevel;
            serveInterval = baseInterval / unlockLevel;
            foodStack.MaxStack = baseStack + unlockLevel * 5;

            // Calculate food price based on profit upgrades.
            int profitLevel = RestaurantManager.Instance.GetUpgradeLevel(Upgrade.Profit);
            sellPrice = Mathf.RoundToInt(Mathf.Pow(priceIncrementRate, profitLevel) * basePrice);
        }

        /// <summary>
        /// Spawns a new customer at the spawn point if conditions allow.
        /// </summary>
        void HandleCustomerSpawn()
        {
            spawnTimer += Time.deltaTime;

            // Spawn a new customer if the timer exceeds the interval and the queue isn't full.
            if (spawnTimer >= spawnInterval && customers.Count < maxCustomers)
            {
                spawnTimer = 0f;

                // Instantiate a new customer and set their exit point.
                var newCustomer = Instantiate(customerPrefab, spawnPoint.position, spawnPoint.rotation);
                newCustomer.ExitPoint = despawnPoint.position;

                customers.Enqueue(newCustomer);

                // Assign the customer to the appropriate queue position.
                AssignQueuePoint(newCustomer, customers.Count - 1);
            }
        }

        /// <summary>
        /// Assigns a customer to a specific queue point.
        /// </summary>
        void AssignQueuePoint(CustomerController customer, int index)
        {
            Transform queuePoint = queuePoints.GetPoint(index);
            bool isFirst = index == 0;

            // Update the customer's position and status in the queue.
            customer.UpdateQueue(queuePoint, isFirst);
        }

        /// <summary>
        /// Handles food serving for the first customer in the queue.
        /// </summary>
        void HandleFoodServing()
        {
            // Do nothing if there are no customers or if the first customer doesn't have an order.
            if (customers.Count == 0 || !firstCustomer.HasOrder) return;

            serveTimer += Time.deltaTime;

            // Serve food if the timer exceeds the serve interval.
            if (serveTimer >= serveInterval)
            {
                serveTimer = 0f;

                // Check if a worker is available, food is in the stack, and the customer has pending orders.
                if (hasWorker && foodStack.Count > 0 && firstCustomer.OrderCount > 0)
                {
                    var food = foodStack.RemoveFromStack();
                    firstCustomer.FillOrder(food);
                    CollectPayment();
                }

                // If the order is complete, assign the customer to a seat if available.
                if (firstCustomer.OrderCount == 0 && CheckAvailableSeating(out Transform seat))
                {
                    var servedCustomer = customers.Dequeue();
                    servedCustomer.AssignSeat(seat);
                    UpdateQueuePositions();
                }
            }
        }

        /// <summary>
        /// Adds payment for the served food to the money pile.
        /// </summary>
        void CollectPayment()
        {
            for (int i = 0; i < sellPrice; i++)
            {
                moneyPile.AddMoney();
            }
        }

        /// <summary>
        /// Checks if there is available seating for a customer.
        /// </summary>
        private bool CheckAvailableSeating(out Transform seat)
        {
            // Prioritize semi-full seating if available.
            var semiFullSeating = seatings.Where(seating => seating.gameObject.activeInHierarchy && seating.IsSemiFull).FirstOrDefault();
            if (semiFullSeating != null)
            {
                seat = semiFullSeating.Occupy(firstCustomer);
                return true;
            }

            // Otherwise, look for completely empty seating.
            var emptySeatings = seatings.Where(seating => seating.gameObject.activeInHierarchy && seating.IsEmpty).ToList();
            if (emptySeatings.Count > 0)
            {
                int randomIndex = Random.Range(0, emptySeatings.Count);
                seat = emptySeatings[randomIndex].Occupy(firstCustomer);
                return true;
            }

            seat = null;
            return false;
        }

        /// <summary>
        /// Updates the queue positions of all customers after a customer is served.
        /// </summary>
        void UpdateQueuePositions()
        {
            int index = 0;
            foreach (var customer in customers)
            {
                AssignQueuePoint(customer, index);
                index++;
            }
        }
    }
}
