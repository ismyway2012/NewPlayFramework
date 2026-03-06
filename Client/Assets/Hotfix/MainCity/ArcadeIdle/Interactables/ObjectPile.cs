using System.Collections.Generic;
using UnityEngine;

namespace NewPlay.ArcadeIdle
{
    public class ObjectPile : Interactable
    {
        [SerializeField, Tooltip("The type of stack (e.g., food, trash, etc.).")]
        private StackType stackType;

        [SerializeField, Tooltip("Number of objects in each row.")]
        private int length = 2;

        [SerializeField, Tooltip("Number of objects in each column.")]
        private int width = 2;

        [SerializeField, Tooltip("Spacing between objects in the pile.")]
        private Vector3 spacing = new Vector3(0.5f, 0.1f, 0.5f);

        [SerializeField, Tooltip("Time interval between dropping objects.")]
        private float dropInterval = 0.03f;

        public StackType StackType => stackType; // Get the stack type of the pile.
        public int Count => objects.Count; // Get the number of objects in the pile.
        public Vector3 PeakPoint => transform.position + new Vector3(0, spacing.y * Count, 0); // The point at the top of the pile.

        protected Stack<GameObject> objects = new Stack<GameObject>(); // Stack to hold the objects in the pile.
        private Vector3 pileCenter; // The center point of the pile used for positioning objects.

        private float dropTimer; // Timer to track the drop interval.

        void Awake()
        {
            // Initializes the center of the pile based on the length and width of the pile.
            pileCenter = new Vector3((length - 1) * spacing.x / 2f, 0f, (width - 1) * spacing.z / 2f);
        }

        protected virtual void Start()
        {
            spacing.y = RestaurantManager.Instance.GetStackOffset(stackType); // Set vertical spacing based on the stack type.
        }

        void Update()
        {
            if (player == null || objects.Count == 0) return; // Don't update if the player is not set or there are no objects in the pile.

            dropTimer += Time.deltaTime;

            if (dropTimer >= dropInterval)
            {
                dropTimer = 0f;
                Drop(); // Drop an object from the pile.
            }
        }

        /// <summary>
        /// Drops an object from the pile to the player's stack if possible.
        /// </summary>
        protected virtual void Drop()
        {
            // Check if the player's stack allows adding this object.
            if (player.Stack.StackType == StackType.None || player.Stack.StackType == stackType)
            {
                if (player.Stack.Height < player.Capacity)
                {
                    var removedObj = objects.Pop(); // Remove the top object from the pile.
                    player.Stack.AddToStack(removedObj.transform, stackType); // Add the object to the player's stack.

                    PlayObjectSound(); // Play a sound based on the object type.
                }
            }
        }

        /// <summary>
        /// Adds an object to the pile and arranges its position within the pile.
        /// </summary>
        public void AddObject(GameObject obj)
        {
            objects.Push(obj); // Push the new object onto the stack.
            ArrangeAddedObject(); // Arrange the newly added object in the pile.
        }

        /// <summary>
        /// Removes an object from the pile and adds it to another stack.
        /// </summary>
        public void RemoveAndStackObject(WobblingStack stack)
        {
            var removedObj = objects.Pop(); // Remove the top object from the pile.
            stack.AddToStack(removedObj.transform, stackType); // Add it to the given stack.
        }

        /// <summary>
        /// Arranges the newly added object within the pile based on its index.
        /// </summary>
        private void ArrangeAddedObject()
        {
            int lastIndex = objects.Count - 1; // Get the index of the last added object.

            int row = (lastIndex / length) % width; // Determine the row based on the index.
            int column = lastIndex % length; // Determine the column based on the index.

            // Calculate the position of the object within the pile.
            float xPos = column * spacing.x - pileCenter.x;
            float yPos = Mathf.FloorToInt(lastIndex / (length * width)) * spacing.y;
            float zPos = row * spacing.z - pileCenter.z;

            var latestObjectPushed = objects.Peek(); // Get the most recently added object.
            latestObjectPushed.transform.position = transform.position + new Vector3(xPos, yPos, zPos); // Set its position.
        }

        /// <summary>
        /// Plays a sound based on the stack type.
        /// </summary>
        private void PlayObjectSound()
        {
            // Play specific sounds based on the stack type (food, trash, etc.).
            switch (stackType)
            {
                case StackType.Food:
                case StackType.Package:
                    AudioManager.Instance.PlaySFX(AudioID.Pop); // Play the "Pop" sound for food and package stacks.
                    break;

                case StackType.Trash:
                    AudioManager.Instance.PlaySFX(AudioID.Trash); // Play the "Trash" sound for trash stacks.
                    break;

                case StackType.None:
                default:
                    break;
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            // Draw wireframe cubes to represent the objects' positions in the pile.

            pileCenter = new Vector3((length - 1) * spacing.x / 2f, 0f, (width - 1) * spacing.z / 2f);

            Gizmos.color = Color.yellow;

            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    Vector3 position = transform.position + new Vector3(i * spacing.x - pileCenter.x, spacing.y / 2f, j * spacing.z - pileCenter.z);
                    Gizmos.DrawWireCube(position, spacing);
                }
            }
        }
#endif
    }
}
