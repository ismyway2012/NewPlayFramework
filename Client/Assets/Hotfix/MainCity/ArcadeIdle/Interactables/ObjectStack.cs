using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

namespace NewPlay.ArcadeIdle
{
    public class ObjectStack : Interactable
    {
        [SerializeField, Tooltip("The type of stack (e.g., food, package, etc.)")]
        private StackType stackType;

        [SerializeField, Tooltip("Time interval between each stack operation")]
        private float stackInterval = 0.05f;

        public StackType StackType => stackType; // Read-only properties for stack information
        public int MaxStack { get; set; } // The maximum number of objects the stack can hold
        public int Count => objects.Count; // The current number of objects in the stack
        public bool IsFull => Count >= MaxStack; // Check if the stack is full

        private Stack<GameObject> objects = new Stack<GameObject>(); // The stack of objects
        private float stackOffset; // The vertical offset between stacked objects
        private float stackTimer; // Timer to track stack operation intervals

        void Start()
        {
            // Get the stack offset based on the stack type from the RestaurantManager
            stackOffset = RestaurantManager.Instance.GetStackOffset(stackType);
        }

        void Update()
        {
            // Increment the stackTimer with the time passed since last frame
            stackTimer += Time.deltaTime;

            // If the interval time has passed, attempt to stack objects
            if (stackTimer >= stackInterval)
            {
                stackTimer = 0f; // Reset the stack timer

                // Check if the player and their stack are valid
                if (player == null) return;
                if (player.Stack.StackType != stackType) return; // Ensure player is using the correct stack type
                if (player.Stack.Count == 0) return; // Ensure there are objects in the player's stack

                // If the stack is full, don't add more objects
                if (objects.Count >= MaxStack) return;

                // Get an object from the player's stack and add it to the object stack
                var objToStack = player.Stack.RemoveFromStack();
                if (objToStack == null) return;

                // Add the object to the stack and play a sound effect
                AddToStack(objToStack.gameObject);
                AudioManager.Instance.PlaySFX(AudioID.Pop); // Play object stacking sound
            }
        }

        // Adds a game object to the stack with a jump animation
        public void AddToStack(GameObject obj)
        {
            objects.Push(obj); // Push the object onto the stack

            // Calculate the target position for the new stack object based on the count
            var heightOffset = new Vector3(0f, (Count - 1) * stackOffset, 0f);
            Vector3 targetPos = transform.position + heightOffset;

            // Animate the object to the stack position with a jump
            obj.transform.DOJump(targetPos, 5f, 1, 0.3f);
        }

        // Removes an object from the stack and returns its transform
        public Transform RemoveFromStack()
        {
            // Pop the top object from the stack and kill any ongoing animations
            Transform removed = objects.Pop().transform;
            DOTween.Kill(removed);

            return removed;
        }
    }
}
