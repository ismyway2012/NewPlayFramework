using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NewPlay.ArcadeIdle
{
    public class Seating : Unlockable
    {
        [SerializeField, Tooltip("Base time it takes for a customer to finish eating")]
        private float baseEatTime = 5f;

        [SerializeField, Tooltip("Base chance of receiving a tip from customers")]
        private float baseTipChance = 0.4f;

        [SerializeField, Tooltip("List of seats available for customers")]
        private List<Transform> seats;

        [SerializeField, Tooltip("Transform representing the table top where food is placed")]
        private Transform tableTop;

        [SerializeField, Tooltip("Pile where trash is collected after eating")]
        private ObjectPile trashPile;

        [SerializeField, Tooltip("Pile where tips are added after customers eat")]
        private MoneyPile tipMoneyPile;

        /// <summary>
        /// Returns true if the seating is semi-full (some customers are seated, but not all seats are occupied).
        /// </summary>
        public bool IsSemiFull => trashPile.Count == 0 && customers.Count > 0 && customers.Count < seats.Count;

        /// <summary>
        /// Returns true if the seating is empty (no customers and no trash).
        /// </summary>
        public bool IsEmpty => trashPile.Count == 0 && customers.Count == 0;

        private List<CustomerController> customers = new List<CustomerController>();  // List of customers seated at the table
        private Stack<Transform> foods = new Stack<Transform>();  // Stack of food items placed on the table

        private float foodStackOffset;  // Offset for stacking food items on the table
        private float eatTime;  // Time it takes for customers to finish eating
        private float tipChance;  // Chance that customers will leave a tip
        private int tipLevel;  // Level of the profit upgrade that affects tip amounts

        void Start()
        {
            // Get the food stack offset from the restaurant manager
            foodStackOffset = RestaurantManager.Instance.GetStackOffset(StackType.Food);
        }

        /// <summary>
        /// Updates seating stats based on unlock level.
        /// </summary>
        protected override void UpdateStats()
        {
            base.UpdateStats();

            eatTime = (baseEatTime - (unlockLevel - 1)) * seats.Count;  // Adjust eat time based on unlock level and number of seats
            tipChance = baseTipChance + ((unlockLevel - 1) * 0.1f);  // Adjust tip chance based on unlock level
            tipLevel = RestaurantManager.Instance.GetUpgradeLevel(Upgrade.Profit);  // Get the current upgrade level for profit (affects tips)
        }

        /// <summary>
        /// Occupies a seat for a customer and starts the eating process if all seats are filled.
        /// </summary>
        /// <param name="customer">The customer occupying the seat</param>
        /// <returns>The seat assigned to the customer</returns>
        public Transform Occupy(CustomerController customer)
        {
            customers.Add(customer);

            // Start eating process if all seats are occupied
            if (customers.Count >= seats.Count)
                StartCoroutine(BeginEating());

            return seats[customers.Count - 1];
        }

        /// <summary>
        /// Adds food to the table and adjusts the position of the food stack.
        /// </summary>
        /// <param name="food">The food item to be added</param>
        public void AddFoodOnTable(Transform food)
        {
            foods.Push(food);  // Add food to the stack
            Vector3 heightOffset = new Vector3(0f, (foods.Count - 1) * foodStackOffset, 0f);  // Calculate height offset for stacking
            food.transform.position = tableTop.position + heightOffset;  // Set food position on the table
            food.transform.rotation = Quaternion.identity;  // Reset food rotation to default
        }

        /// <summary>
        /// Handles the tipping process by randomly giving tips to customers.
        /// </summary>
        void LeaveTip()
        {
            if (Random.value < tipChance)  // Check if tip is given based on chance
            {
                int tipAmount = Random.Range(2, 5 + tipLevel);  // Randomly decide tip amount based on tip level
                for (int i = 0; i < tipAmount; i++)
                {
                    tipMoneyPile.AddMoney();  // Add money to the tip pile
                }
            }
        }

        /// <summary>
        /// Begins the eating process once customers are ready and all seats are filled.
        /// </summary>
        IEnumerator BeginEating()
        {
            yield return new WaitUntil(() => customers.All(customer => customer.ReadyToEat));  // Wait until all customers are ready to eat

            foreach (var customer in customers)
            {
                customer.TriggerEat();  // Trigger eating action for each customer
                yield return new WaitForSeconds(Random.Range(0.1f, 0.3f));  // Random delay between each customer eating
            }

            float interval = eatTime / foods.Count;  // Calculate time interval per food item
            int trashCount = 0;

            // Process eating and trash generation
            while (foods.Count > 0)
            {
                yield return new WaitForSeconds(interval);

                PoolManager.Instance.ReturnObject(foods.Pop().gameObject);  // Return food object to pool
                trashCount++;
                LeaveTip();  // Leave a tip for the customer
            }

            // Spawn trash objects for each customer after eating
            while (trashCount > 0)
            {
                var trash = PoolManager.Instance.SpawnObject("Trash");  // Spawn trash object
                trashPile.AddObject(trash);  // Add trash to the trash pile
                trashCount--;

                yield return new WaitForSeconds(0.05f);  // Small delay between trash spawning
            }

            // Finish eating for each customer and clear the list
            foreach (var customer in customers)
            {
                customer.FinishEating();
                yield return new WaitForSeconds(Random.Range(1, 4) * 0.5f);  // Random delay between customers finishing
            }

            customers.Clear();  // Clear customer list after eating process
        }
    }
}
