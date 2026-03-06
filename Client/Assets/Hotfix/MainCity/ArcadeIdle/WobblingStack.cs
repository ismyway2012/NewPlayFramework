using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;

namespace NewPlay.ArcadeIdle
{
    public class WobblingStack : MonoBehaviour
    {
        [SerializeField, Tooltip("The range for the wobble rate. X = base stack, Y = topmost items.")]
        private Vector2 rateRange = new Vector2(0.8f, 0.4f);

        [SerializeField, Tooltip("The factor that determines the amount of tilt for each item in the stack.")]
        private float bendFactor = 0.1f;

        [SerializeField, Tooltip("The tray GameObject that is shown when the stack is not empty.")]
        private GameObject tray;

        public StackType StackType { get; private set; }  // The current type of the stack (Food, Trash, or Package)
        public int Count => stack.Count;  // The current number of items in the stack
        public int Height => height;  // The height of the stack (number of items stacked)

        private List<Transform> stack = new List<Transform>();  // The list holding the stacked objects
        private int height;  // The current height of the stack (number of stacked items)

        // The offset between stacked items
        private float stackOffset => RestaurantManager.Instance.GetStackOffset(StackType);

        Vector2 movement;  // Used to store player input for wobble movement

        void Update()
        {
            // If the stack is empty, there's nothing to update
            if (stack.Count == 0) return;

            // Get the horizontal and vertical input for wobbling movement
            movement.x = SimpleInput.GetAxis("Horizontal");
            movement.y = SimpleInput.GetAxis("Vertical");

            // Update the position and rotation of the first item in the stack (base)
            stack[0].transform.position = transform.position;
            stack[0].transform.rotation = transform.rotation;

            // Loop through the remaining items in the stack
            for (int i = 1; i < stack.Count; i++)
            {
                // Calculate the wobble rate based on the item's position in the stack
                float rate = Mathf.Lerp(rateRange.x, rateRange.y, i / (float)stack.Count);

                // Smoothly move each item towards the position of the item below it
                stack[i].position = Vector3.Lerp(stack[i].position, stack[i - 1].position + (stack[i - 1].up * stackOffset), rate);

                // Smoothly rotate each item to align with the item below it
                stack[i].rotation = Quaternion.Lerp(stack[i].rotation, stack[i - 1].rotation, rate);

                // Apply bending / tilting based on player input
                if (movement != Vector2.zero)
                    stack[i].rotation *= Quaternion.Euler(-i * bendFactor * rate, 0, 0);
            }
        }

        /// <summary>
        /// Adds an item to the stack.
        /// </summary>
        /// <param name="child">The transform of the item to be added to the stack.</param>
        /// <param name="stackType">The type of the stack (Food, Trash, or Package).</param>
        public void AddToStack(Transform child, StackType stackType)
        {
            // If this is the first item, set the stack type and show the tray
            if (stack.Count == 0)
            {
                StackType = stackType;
                tray.SetActive(true);
            }

            height++;  // Increase the stack height
            Vector3 peakPoint = transform.position + Vector3.up * height * stackOffset;  // Calculate the peak point for the item to jump to

            // Animate the item to jump to the peak position
            child.DOJump(peakPoint, 5f, 1, 0.3f)
                .OnComplete(() => stack.Add(child));  // Add the item to the stack once the animation is complete
        }

        /// <summary>
        /// Removes an item from the stack.
        /// </summary>
        /// <returns>The transform of the item that was removed.</returns>
        public Transform RemoveFromStack()
        {
            // If the stack is empty, return null
            if (height == 0) return null;

            // Get the last item in the stack
            var lastChild = stack.LastOrDefault();
            lastChild.rotation = Quaternion.identity;  // Reset its rotation

            // Remove the last item from the stack and decrease the height
            stack.Remove(lastChild);
            height--;

            // If the stack is empty after removal, hide the tray
            if (stack.Count == 0)
            {
                StackType = StackType.None;
                tray.SetActive(false);
            }

            return lastChild;  // Return the removed item
        }
    }

    public enum StackType { None, Food, Trash, Package, Weapon }  // Enum to define the type of stack
}
