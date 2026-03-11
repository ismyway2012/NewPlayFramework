using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace NewPlay.ArcadeIdle
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class CustomerController : RoleController
    {
        [SerializeField, Tooltip("Max number of orders a customer can place")]
        private int maxOrder = 5;

        [SerializeField, Tooltip("Reference to the customer's stack for carrying items")]
        private WobblingStack stack;

        [SerializeField, Tooltip("Target transform for the left hand IK")]
        private Transform leftHandTarget;

        [SerializeField, Tooltip("Target transform for the right hand IK")]
        private Transform rightHandTarget;

        public Vector3 ExitPoint { get; set; } // The exit point where the customer will leave after eating
        public bool HasOrder { get; private set; } // Whether the customer has placed an order
        public int OrderCount { get; private set; } // The number of items in the customer's order
        public bool ReadyToEat { get; private set; } // Whether the customer is ready to eat

        private OrderInfo orderInfo => RestaurantManager.Instance.FoodOrderInfo; // Access to the food order information

        private Animator animator; // Reference to the customer's animator component
        private NavMeshAgent agent; // Reference to the customer's NavMeshAgent for navigation

        private LayerMask entranceLayer; // Layer mask to detect entrance doors

        private float IK_Weight; // Weight for controlling the IK of the customer's hands

        void Awake()
        {
            // Initializes the customer controller with references to the animator and nav mesh agent.
            animator = GetComponent<Animator>();
            agent = GetComponent<NavMeshAgent>();

            // Sets up the entrance layer mask.
            entranceLayer = 1 << LayerMask.NameToLayer("Entrance");
        }

        void Start()
        {

        }

        void Update()
        {
            // Updates the customer's movement state based on the NavMeshAgent's velocity.
            animator.SetBool("IsMoving", agent.velocity.sqrMagnitude > 0.1f);
        }

        /// <summary>
        /// Updates the customer's queue position and optionally places an order.
        /// </summary>
        /// <param name="queuePoint">The queue point the customer should move to.</param>
        /// <param name="isFirst">Whether this customer is the first in the queue to place an order.</param>
        public void UpdateQueue(Transform queuePoint, bool isFirst)
        {
            agent.SetDestination(queuePoint.position); // Move to the queue point

            if (isFirst) StartCoroutine(PlaceOrder()); // If first in line, place an order
        }

        /// <summary>
        /// Fills the customer's order by adding food to their stack and updating the order count.
        /// </summary>
        /// <param name="food">The food item being delivered to the customer.</param>
        public void FillOrder(Transform food)
        {
            OrderCount--; // Decrease the order count
            stack.AddToStack(food, StackType.Food); // Add food to the customer's stack

            orderInfo.ShowInfo(transform, OrderCount); // Update the order info display
        }

        /// <summary>
        /// Assigns a seat to the customer and starts the walking animation to the seat.
        /// </summary>
        /// <param name="seat">The seat the customer will walk to.</param>
        public void AssignSeat(Transform seat)
        {
            orderInfo.HideInfo(); // Hide the order info as the customer begins searching for a seat

            StartCoroutine(WalkToSeat(seat)); // Start the walk to the seat
        }

        /// <summary>
        /// Triggers the eating animation for the customer.
        /// </summary>
        public void TriggerEat()
        {
            animator.SetTrigger("Eat");
        }

        /// <summary>
        /// Handles the process of finishing eating and moving the customer to the exit.
        /// </summary>
        public void FinishEating()
        {
            agent.SetDestination(ExitPoint); // Set the destination to the exit
            agent.areaMask |= (1 << NavMesh.GetAreaFromName("Battle Zone")); // Allow the agent to walk through entrance areas
            animator.SetTrigger("Leave"); // Trigger the leaving animation

            StartCoroutine(WalkToExit()); // Start the walk to the exit
        }

        /// <summary>
        /// Places an order by waiting until the customer has arrived, then randomly choosing an order count.
        /// </summary>
        IEnumerator PlaceOrder()
        {
            yield return new WaitUntil(() => HasArrived()); // Wait until the customer arrives

            OrderCount = Random.Range(1, maxOrder + 1); // Randomly set the order count
            HasOrder = true; // Mark the customer as having placed an order

            orderInfo.ShowInfo(transform, OrderCount); // Update the order info display
        }

        /// <summary>
        /// Makes the customer walk to their assigned seat.
        /// </summary>
        /// <param name="seat">The seat the customer is walking to.</param>
        IEnumerator WalkToSeat(Transform seat)
        {
            yield return new WaitForSeconds(0.3f); // Wait for the last food item to land

            agent.SetDestination(seat.position); // Set destination to the seat

            yield return new WaitUntil(() => HasArrived()); // Wait until the customer has arrived

            // Align the customer's rotation to match the seat's rotation
            while (Vector3.Angle(transform.forward, seat.forward) > 0.1f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, seat.rotation, Time.deltaTime * 270f);
                yield return null;
            }

            var seating = seat.GetComponentInParent<Seating>();
            // Place the food items on the table
            while (stack.Count > 0)
            {
                seating.AddFoodOnTable(stack.RemoveFromStack());
                yield return new WaitForSeconds(0.05f); // Wait between placing items
            }

            ReadyToEat = true; // Mark the customer as ready to eat

            animator.SetTrigger("Sit"); // Trigger the sitting animation
        }

        /// <summary>
        /// Makes the customer walk to the exit and destroys the customer object once they leave.
        /// </summary>
        IEnumerator WalkToExit()
        {
            yield return new WaitUntil(() => HasArrived()); // Wait until the customer arrives at the exit

            agent.areaMask &= ~(1 << NavMesh.GetAreaFromName("Camp Area"));// Restore the agent's area mask

            //Destroy(gameObject); // Destroy the customer object when they leave
            this.enabled = false; // Disable the customer controller
            var fighter = GetComponentInChildren<FighterController>();
            if (fighter)
            {
                fighter.enabled = true;
            }
        }

        /// <summary>
        /// Checks if the customer has arrived at their destination.
        /// </summary>
        /// <returns>True if the customer has arrived, false otherwise.</returns>
        private bool HasArrived()
        {
            // Check if the NavMeshAgent has reached its destination
            if (!agent.pathPending)
            {
                if (agent.remainingDistance <= agent.stoppingDistance)
                {
                    if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                    {
                        return true;
                    }
                }
            }

            return false; // Customer has not arrived yet
        }

        /// <summary>
        /// Handles the IK for the customer's hands to adjust their position based on the stack's height.
        /// </summary>
        void OnAnimatorIK()
        {
            IK_Weight = Mathf.MoveTowards(IK_Weight, Mathf.Clamp01(stack.Height), Time.deltaTime * 3.5f); // Smoothly adjust the IK weight

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
