using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NewPlay.ArcadeIdle
{
    public class Map : MonoBehaviour
    {
        [SerializeField, Tooltip("List of buttons representing map locations")]
        private List<Button> mapButtons;

        [SerializeField, Tooltip("Sprite shown when a location on the map is locked")]
        private Sprite lockedSprite;

        private List<Sprite> mapButtonSprites = new List<Sprite>(); // List to store original sprites for unlocked locations

        void Awake()
        {
            // Store the original sprites of the map buttons for later use
            foreach (var mapButton in mapButtons)
            {
                mapButtonSprites.Add(mapButton.image.sprite);
            }
        }

        void Start()
        {
            // Set up the onClick listeners for each map button
            for (int i = 0; i < mapButtons.Count; i++)
            {
                int index = i + 1; // Set the restaurant index based on the button's position in the list

                // Add listener to load the restaurant when a button is clicked
                mapButtons[i].onClick.AddListener(() =>
                {
                    gameObject.SetActive(false); // Hide the map UI
                    RestaurantManager.Instance.LoadRestaurant(index); // Load the corresponding restaurant scene
                });
            }
        }

        void OnEnable()
        {
            // Update the map button states based on the restaurant's unlocked status
            for (int i = 0; i < mapButtons.Count; i++)
            {
                int index = i + 1;
                bool isUnlocked = IsUnlocked(index); // Check if the restaurant is unlocked

                // Update button's interactability and sprite based on whether it's unlocked
                mapButtons[i].interactable = isUnlocked;
                mapButtons[i].image.sprite = isUnlocked ? mapButtonSprites[index - 1] : lockedSprite;
            }
        }

        // Checks if a specific restaurant is unlocked
        private bool IsUnlocked(int index)
        {
            if (index == 1) return true; // The first restaurant is always unlocked

            // Load the previous restaurant's data to check if the current restaurant is unlocked
            var previousRestaurantData = SaveSystem.LoadData<RestaurantData>($"Restaurant0{index - 1}");

            if (previousRestaurantData == null) return false; // If no data is found, the restaurant is locked

            // Return the unlocked status based on the previous restaurant's data
            return previousRestaurantData.IsUnlocked;
        }
    }
}
