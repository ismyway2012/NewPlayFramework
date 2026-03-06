using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace NewPlay.ArcadeIdle
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class EmployeeController : RoleController
    {
        [SerializeField, Tooltip("Base movement speed of the employee.")]
        private float baseSpeed = 2.5f;

        [SerializeField, Tooltip("Base capacity of the employee's stack.")]
        private int baseCapacity = 3;

        [SerializeField, Tooltip("Reference to the WobblingStack component for managing the employee's stack.")]
        private WobblingStack stack;

        [SerializeField, Tooltip("Target position for the employee's left hand during animations.")]
        private Transform leftHandTarget;

        [SerializeField, Tooltip("Target position for the employee's right hand during animations.")]
        private Transform rightHandTarget;

        private Animator animator; // Reference to the Animator component for character animation
        private NavMeshAgent agent; // Reference to the NavMeshAgent component for pathfinding

        private float IK_Weight; // Weight used to control the IK (Inverse Kinematics) blending
        private int capacity; // Current capacity for the employee (increased by upgrades)
        private StackType currentActivity; // The current activity (e.g., cleaning, refilling, etc.)

        void Awake()
        {
            // Get references to the Animator and NavMeshAgent components on the employee
            animator = GetComponent<Animator>();
            agent = GetComponent<NavMeshAgent>();
        }

        void Start()
        {
            // Subscribe to the upgrade event to update stats when upgrades are applied
            RestaurantManager.Instance.OnUpgrade += UpdateStats;

            UpdateStats(); // Initialize stats based on current upgrades
        }

        void Update()
        {
            // Set the "IsMoving" animator parameter based on the agent's movement speed
            animator.SetBool("IsMoving", agent.velocity.sqrMagnitude > 0.1f);

            // Handle the employee's current activity (cleaning, refilling, etc.)
            HandleActivity();
        }

        void UpdateStats()
        {
            // Update the employee's speed based on the speed upgrade level
            float speedLevel = RestaurantManager.Instance.GetUpgradeLevel(Upgrade.EmployeeSpeed);
            agent.speed = baseSpeed + (speedLevel * 0.1f);

            // Update the employee's capacity based on the capacity upgrade level
            int capacityLevel = RestaurantManager.Instance.GetUpgradeLevel(Upgrade.EmployeeCapacity);
            capacity = baseCapacity + capacityLevel;
        }

        void HandleActivity()
        {
            // If the employee is already performing an activity, don't start another
            if (currentActivity != StackType.None) return;

            // Randomly select an activity (cleaning, refilling food, or refilling packages)
            switch (Random.Range(0, 3))
            {
                case 0:
                    StartCoroutine(CleanTable()); // Start cleaning table activity
                    break;
                case 1:
                    StartCoroutine(RefillFood()); // Start refilling food activity
                    break;
                case 2:
                    StartCoroutine(RefillPackage()); // Start refilling package activity
                    break;
            }
        }

        IEnumerator CleanTable()
        {
            currentActivity = StackType.Trash; // Set the current activity to cleaning trash

            // Find valid trash piles that contain trash
            var validTrashPiles = RestaurantManager.Instance.TrashPiles.Where(x => x.Count > 0).ToList();

            // If there are no trash piles, reset activity and exit
            if (validTrashPiles.Count == 0)
            {
                currentActivity = StackType.None;
                yield break;
            }

            // Select a random trash pile to clean
            var trashPile = validTrashPiles[Random.Range(0, validTrashPiles.Count)];

            // Set the agent's destination to the trash pile
            agent.SetDestination(trashPile.transform.position);

            // Wait until the employee arrives at the trash pile
            while (!HasArrived())
            {
                // If the trash pile is emptied during movement, reset and exit
                if (trashPile.Count == 0)
                {
                    agent.SetDestination(transform.position); // Return to current position
                    currentActivity = StackType.None;
                    yield break;
                }

                yield return null; // Wait for the next frame
            }

            // Pick up trash from the trash pile while there's still trash and available capacity
            while (trashPile.Count > 0 && stack.Height < capacity)
            {
                trashPile.RemoveAndStackObject(stack); // Move trash to the stack

                yield return new WaitForSeconds(0.03f); // Wait before picking up more trash
            }

            yield return new WaitForSeconds(0.5f); // Wait before moving to the trash bin

            // Set destination to the trash bin to dispose of the trash
            var trashBin = RestaurantManager.Instance.TrashBin;
            agent.SetDestination(trashBin.transform.position);

            // Wait until the employee arrives at the trash bin
            yield return new WaitUntil(() => HasArrived());

            // Dispose of the trash in the trash bin
            while (stack.Count > 0)
            {
                trashBin.ThrowToBin(stack); // Throw items from the stack into the bin

                yield return new WaitForSeconds(0.03f); // Wait before disposing more trash
            }

            yield return new WaitForSeconds(0.5f); // Wait before finishing the activity

            currentActivity = StackType.None; // Reset the current activity
        }

        IEnumerator RefillFood()
        {
            currentActivity = StackType.Food; // Set current activity to refilling food

            // Find valid food stacks that are not full
            var validFoodStacks = RestaurantManager.Instance.FoodStacks.Where(x => x.gameObject.activeInHierarchy && !x.IsFull).ToList();
            if (validFoodStacks.Count == 0)
            {
                currentActivity = StackType.None;
                yield break;
            }

            // Select a random food stack to refill
            var foodStack = validFoodStacks[Random.Range(0, validFoodStacks.Count)];

            // Find valid food piles that have food
            var validFoodPiles = RestaurantManager.Instance.FoodPiles.Where(x => x.Count > 0).ToList();
            if (validFoodPiles.Count == 0)
            {
                currentActivity = StackType.None;
                yield break;
            }

            // Select a random food pile to gather food from
            var foodPile = validFoodPiles[Random.Range(0, validFoodPiles.Count)];

            // Set destination to the food pile to collect food
            agent.SetDestination(foodPile.transform.position);

            // Wait until the employee arrives at the food pile
            while (!HasArrived())
            {
                // If the food pile is emptied during movement, reset and exit
                if (foodPile.Count == 0)
                {
                    agent.SetDestination(transform.position); // Return to current position
                    currentActivity = StackType.None;
                    yield break;
                }

                yield return null; // Wait for the next frame
            }

            // Pick up food from the pile while there's food and available capacity
            while (foodPile.Count > 0 && stack.Height < capacity)
            {
                foodPile.RemoveAndStackObject(stack); // Add food to the stack

                yield return new WaitForSeconds(0.03f); // Wait before picking up more food
            }

            yield return new WaitForSeconds(0.5f); // Wait before moving to the food stack

            // Set destination to the food stack to refill it
            agent.SetDestination(foodStack.transform.position);

            // Wait until the employee arrives at the food stack
            yield return new WaitUntil(() => HasArrived());

            // Add food from the stack to the food stack
            while (stack.Count > 0)
            {
                if (!foodStack.IsFull)
                {
                    var food = stack.RemoveFromStack(); // Remove food from the stack
                    foodStack.AddToStack(food.gameObject); // Add food to the food stack
                }

                yield return new WaitForSeconds(0.03f); // Wait before refilling more food
            }

            yield return new WaitForSeconds(0.5f); // Wait before finishing the activity

            currentActivity = StackType.None; // Reset the current activity
        }

        IEnumerator RefillPackage()
        {
            currentActivity = StackType.Package; // Set the current activity to handling package refill

            // Check if the package stack exists, is active, and is not full
            var packageStack = RestaurantManager.Instance.PackageStack;
            if (packageStack == null || !packageStack.gameObject.activeInHierarchy || packageStack.IsFull)
            {
                currentActivity = StackType.None; // No activity to process if the package stack is unavailable or full
                yield break; // Exit the coroutine if the condition isn't met
            }

            // Check if the package pile exists, is active, and contains items
            var packagePile = RestaurantManager.Instance.PackagePile;
            if (packagePile == null || !packagePile.gameObject.activeInHierarchy || packagePile.Count == 0)
            {
                currentActivity = StackType.None; // No activity if the package pile is unavailable or empty
                yield break; // Exit the coroutine if the condition isn't met
            }

            agent.SetDestination(packagePile.transform.position);

            // Wait until the agent arrives at the package pile or the pile becomes empty
            while (!HasArrived())
            {
                if (packagePile.Count == 0) // If the package pile is empty, stop the task
                {
                    agent.SetDestination(transform.position); // Return to the starting position
                    currentActivity = StackType.None; // Reset the current activity
                    yield break;
                }

                yield return null;
            }

            // While the package pile still contains items and the stack is not full, move items from the pile to the stack
            while (packagePile.Count > 0 && stack.Height < capacity)
            {
                packagePile.RemoveAndStackObject(stack); // Move an item from the package pile to the stack

                yield return new WaitForSeconds(0.03f); // Delay to simulate the item moving
            }

            yield return new WaitForSeconds(0.5f); // Short delay before moving to the package stack

            agent.SetDestination(packageStack.transform.position);

            yield return new WaitUntil(() => HasArrived()); // Wait until the agent has arrived at the package stack

            // While the stack has items, move them to the package stack if it's not full
            while (stack.Count > 0)
            {
                if (!packageStack.IsFull) // Only add to the package stack if it's not full
                {
                    var food = stack.RemoveFromStack(); // Remove an item from the stack
                    packageStack.AddToStack(food.gameObject); // Add the item to the package stack
                }

                yield return new WaitForSeconds(0.03f); // Delay to simulate the item moving
            }

            yield return new WaitForSeconds(0.5f); // Short delay before finishing the activity

            currentActivity = StackType.None; // Reset the activity once the task is complete
        }

        private bool HasArrived()
        {
            // Check if the agent's path calculation is complete
            if (!agent.pathPending)
            {
                // Check if the agent has reached the stopping distance
                if (agent.remainingDistance <= agent.stoppingDistance)
                {
                    // Check if the agent no longer has a path to follow or if it has stopped moving
                    if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                    {
                        return true; // The agent has arrived at the destination
                    }
                }
            }

            return false; // The agent has not arrived yet
        }

        public void OnStep()
        {
            // Intentionally left blank.
            // This is an animation event triggered during the stickman run animation when the footstep hits the ground.
            // It's used by the player to trigger footstep sound effects, but left empty for employees.
        }

        /// <summary>
        /// Handles the IK for the employee's hands to adjust their position based on the stack's height.
        /// </summary>
        void OnAnimatorIK()
        {
            IK_Weight = Mathf.MoveTowards(IK_Weight, Mathf.Clamp01(stack.Height), Time.deltaTime * 3.5f);

            if (leftHandTarget != null)
            {
                // Set the IK position and rotation for the left hand
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, IK_Weight);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, IK_Weight);
                animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandTarget.position);
                animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandTarget.rotation);
            }

            if (rightHandTarget != null)
            {
                // Set the IK position and rotation for the right hand
                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, IK_Weight);
                animator.SetIKRotationWeight(AvatarIKGoal.RightHand, IK_Weight);
                animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandTarget.position);
                animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandTarget.rotation);
            }
        }
    }
}
