using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace NewPlay.ArcadeIdle
{
    public class CarController : RoleController
    {
        [SerializeField, Range(5f, 10f), Tooltip("Speed at which the car moves.")]
        private float movingSpeed = 5.0f;

        [SerializeField, Range(15f, 45f), Tooltip("Speed at which the car turns.")]
        private float turningSpeed = 15.0f;

        [SerializeField, Tooltip("Maximum number of orders the car can carry.")]
        private int maxOrder = 5;

        // Indicates if the car currently has an order.
        public bool HasOrder { get; private set; }

        // Current count of orders the car is carrying.
        public int OrderCount { get; private set; }

        // List of waypoints for the car to follow.
        private List<Vector3> waypoints;

        // The exit point where the car leaves.
        private Transform exitPoint;

        // The car's current position in the queue.
        private int queueNumber;

        // The index of the current waypoint the car is moving towards.
        private int currentWaypointIndex;

        // Reference to the order information display from the restaurant manager.
        private OrderInfo orderInfo => RestaurantManager.Instance.PackageOrderInfo;

        /// <summary>
        /// Initializes the car with waypoints, exit point, and queue number.
        /// </summary>
        /// <param name="waypoints">The waypoints the car will follow.</param>
        /// <param name="exitPoint">The exit point where the car will leave.</param>
        /// <param name="queueNumber">The car's initial position in the queue.</param>
        public void Init(Waypoints waypoints, Transform exitPoint, int queueNumber)
        {
            this.waypoints = waypoints.GetPoints();
            this.exitPoint = exitPoint;
            this.queueNumber = queueNumber;
            currentWaypointIndex = this.waypoints.Count - 1;

            MoveToNextWayPoint();
        }

        /// <summary>
        /// Updates the car's queue position and moves it to the next waypoint.
        /// </summary>
        public void UpdateQueue()
        {
            queueNumber--;
            currentWaypointIndex = queueNumber;
            MoveToNextWayPoint();
        }

        /// <summary>
        /// Moves the car to the next waypoint in the path.
        /// </summary>
        private void MoveToNextWayPoint()
        {
            Vector3 targetPoint = waypoints[currentWaypointIndex];

            System.Action onComplete = () =>
            {
                if (currentWaypointIndex > queueNumber)
                {
                    currentWaypointIndex--;
                    MoveToNextWayPoint();
                }
                else if (queueNumber == 0)
                {
                    PlaceOrder();
                }
            };

            Move(targetPoint, onComplete);
        }

        /// <summary>
        /// Moves the car to a target point with animation.
        /// </summary>
        /// <param name="targetPoint">The position the car should move to.</param>
        /// <param name="onComplete">Callback to invoke when movement is complete.</param>
        private void Move(Vector3 targetPoint, System.Action onComplete)
        {
            // Check if the transform is already being animated (car is moving);
            // if so, exit early to avoid overlapping animations.
            if (DOTween.IsTweening(transform)) return;

            // Calculate the distance between the car position and the target point.
            float distance = Vector3.Distance(transform.position, targetPoint);

            // Calculate the target rotation to face the direction of movement.
            Vector3 direction = targetPoint - transform.position;
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // Calculate the duration of movement and rotation based on the speed and distance.
            float movingDuration = distance / movingSpeed;
            float turningDuration = distance / turningSpeed;

            // Create a new DOTween sequence to chain movement and rotation animations.
            var sequence = DOTween.Sequence();
            sequence.Append(transform.DOMove(targetPoint, movingDuration).SetEase(Ease.Linear));
            sequence.Join(transform.DORotateQuaternion(targetRotation, turningDuration).SetEase(Ease.Linear));

            // Assign the provided callback to be invoked upon completion of the sequence.
            sequence.OnComplete(() => onComplete?.Invoke());
        }

        /// <summary>
        /// Assigns a random number of orders to the car and displays order info.
        /// </summary>
        private void PlaceOrder()
        {
            OrderCount = Random.Range(1, maxOrder);
            HasOrder = true;
            orderInfo.ShowInfo(transform, OrderCount);
        }

        /// <summary>
        /// Processes the fulfillment of one order, updates the order count, and manages the package's animation and return to the object pool.
        /// </summary>
        /// <param name="package">The package being delivered to the car.</param>
        public void FillOrder(Transform package)
        {
            // Animate the package to jump above the car, simulating a delivery action.
            package.DOJump(transform.position + Vector3.up * 3, 5f, 1, 0.5f)
                // Once the animation is complete, return the package to the object pool.
                .OnComplete(() => PoolManager.Instance.ReturnObject(package.gameObject));

            // Reduce the current order count since one order has been fulfilled.
            OrderCount--;

            // If all orders are fulfilled, hide the order info display.
            if (OrderCount == 0)
            {
                orderInfo.HideInfo();
            }
            // Otherwise, update the order info display to show the remaining count.
            else
            {
                orderInfo.ShowInfo(transform, OrderCount);
            }
        }

        /// <summary>
        /// Moves the car to the exit point and destroys it upon completion.
        /// </summary>
        public void Leave()
        {
            Move(exitPoint.position, () => Destroy(gameObject));
        }
    }
}
