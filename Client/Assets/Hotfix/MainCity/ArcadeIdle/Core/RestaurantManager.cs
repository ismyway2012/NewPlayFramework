using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

namespace NewPlay.ArcadeIdle
{
    public class RestaurantManager : MonoBehaviour
    {
        public static RestaurantManager Instance { get; private set; }

        [SerializeField, Tooltip("Base cost of upgrading the restaurant.")]
        private int baseUpgradePrice = 250;

        [SerializeField, Range(1.01f, 1.99f), Tooltip("The growth factor applied to upgrade prices with each upgrade.")]
        private float upgradeGrowthFactor = 1.5f;

        [SerializeField, Tooltip("Base cost for unlocking items or features.")]
        private int baseUnlockPrice = 75;

        [SerializeField, Range(1.01f, 1.99f), Tooltip("The growth factor applied to unlock prices with each unlock.")]
        private float unlockGrowthFactor = 1.1f;

        [SerializeField, Tooltip("Starting money for the restaurant.")]
        private long startingMoney = 1000;

        [Header("Stack Offset")]
        [SerializeField, Tooltip("Offset distance for food items in the stack.")]
        private float foodOffset = 0.35f;

        [SerializeField, Tooltip("Offset distance for trash items in the stack.")]
        private float trashOffset = 0.18f;

        [SerializeField, Tooltip("Offset distance for package items in the stack.")]
        private float packageOffset = 0.3f;

        [Header("Employee")]
        [SerializeField, Tooltip("The point where employees will spawn.")]
        private Transform employeePoint;

        [SerializeField, Tooltip("Prefab for the employee.")]
        private EmployeeController employeePrefab;

        [SerializeField, Tooltip("Radius within which employees will spawn.")]
        private float employeeSpawnRadius = 3f;

        [Header("User Interface")]
        [SerializeField, Tooltip("Text field displaying the current money.")]
        private TMP_Text moneyDisplay;

        [SerializeField]
        private HpBar hpbar;

        [SerializeField, Tooltip("Order info display for food orders.")]
        private OrderInfo foodOrderInfo;

        [SerializeField, Tooltip("Order info display for package orders.")]
        private OrderInfo packageOrderInfo;

        [SerializeField, Tooltip("Screen fader for transitions between scenes.")]
        private ScreenFader screenFader;

        [Header("Effects")]
        [SerializeField, Tooltip("Particle effect to play when unlocking something.")]
        private ParticleSystem unlockParticle;

        [SerializeField, Tooltip("Background music to play in the restaurant.")]
        private AudioClip backgroundMusic;

        [Header("Unlockable")]
        [SerializeField, Tooltip("The buyer object responsible for unlocking features.")]
        private UnlockableBuyer unlockableBuyer;

        [SerializeField, Tooltip("List of unlockables that can be bought.")]
        private List<Unlockable> unlockables;

        #region Reference Properties
        public OrderInfo FoodOrderInfo => foodOrderInfo;
        public OrderInfo PackageOrderInfo => packageOrderInfo;

        public HpBar HpBar => hpbar;

        public HpBar CreateHpBar()
        {
            var hpBarInstance = Instantiate(hpbar, hpbar.transform.parent, false);
            return hpBarInstance;
        }

        public List<ObjectPile> TrashPiles { get; private set; } = new List<ObjectPile>();
        public TrashBin TrashBin { get; private set; }

        public List<ObjectPile> FoodPiles { get; private set; } = new List<ObjectPile>();
        public List<ObjectStack> FoodStacks { get; private set; } = new List<ObjectStack>();

        public ObjectPile PackagePile { get; private set; }
        public ObjectStack PackageStack { get; private set; }
        #endregion

        public event System.Action OnUpgrade;
        public event System.Action<float> OnUnlock;

        public int PaidAmount
        {
            get => data.PaidAmount;
            set => data.PaidAmount = value;
        }

        private int unlockCount
        {
            get => data.UnlockCount;
            set => data.UnlockCount = value;
        }

        private RestaurantData data;

        private string restaurantID;

        void Awake()
        {
            // Ensures that there is only one instance of RestaurantManager throughout the game.
            Instance = this;

            // Gets the name of the current scene to use as a unique restaurant ID.
            restaurantID = SceneManager.GetActiveScene().name;

            // Loads saved data for the current restaurant (if any exists).
            data = SaveSystem.LoadData<RestaurantData>(restaurantID);

            // If no data is found, initialize with default values (starting money and empty data).
            if (data == null) data = new RestaurantData(restaurantID, startingMoney);

            // Adjust the money with 0 adjustment to update the UI only.
            AdjustMoney(0);

            // Spawn the number of employees based on the saved data.
            for (int i = 0; i < data.EmployeeAmount; i++) SpawnEmployee();
        }

        void Start()
        {
            // Fade out the screen at the start of the scene.
            screenFader.FadeOut();

            // Find all ObjectPile instances in the scene and categorize them based on StackType.
            var objectPiles = FindObjectsOfType<ObjectPile>(true);

            // Loop through all ObjectPile instances and add them to the appropriate list (TrashPiles, FoodPiles, or assign PackagePile).
            foreach (var pile in objectPiles)
            {
                if (pile.StackType == StackType.Trash) TrashPiles.Add(pile);
                else if (pile.StackType == StackType.Food) FoodPiles.Add(pile);
                else if (pile.StackType == StackType.Package) PackagePile = pile;
            }

            // Find the TrashBin object in the scene and assign it to TrashBin.
            TrashBin = FindObjectOfType<TrashBin>(true);

            // Find all ObjectStack instances in the scene and categorize them.
            var objectStacks = FindObjectsOfType<ObjectStack>(true);

            // Loop through all ObjectStack instances and assign them to the corresponding list or object (FoodStacks or PackageStack).
            foreach (var stack in objectStacks)
            {
                if (stack.StackType == StackType.Food) FoodStacks.Add(stack);
                else if (stack.StackType == StackType.Package) PackageStack = stack;
            }

            // Initialize unlocked Unlockables based on the data (UnlockCount).
            for (int i = 0; i < unlockCount; i++)
            {
                unlockables[i].Unlock(false);
            }

            // Update the UnlockableBuyer UI to reflect the current unlockable state.
            UpdateUnlockableBuyer();

            // Play background music once the scene has loaded.
            AudioManager.Instance.PlayBGM(backgroundMusic);
        }

        void SpawnEmployee()
        {
            // Generate a random position within a circular radius around the employee spawn point.
            var randomCircle = Random.insideUnitCircle * employeeSpawnRadius;
            var randomPos = employeePoint.position + new Vector3(randomCircle.x, 0, randomCircle.y);

            // Instantiate a new employee at the calculated random position and the predefined rotation.
            Instantiate(employeePrefab, randomPos, employeePoint.rotation);
        }

        void Update()
        {
            // DEBUG: Please remove on build!
            if (Input.GetKeyDown(KeyCode.G))
            {
                AdjustMoney(10000);
                SaveSystem.SaveData<RestaurantData>(data, restaurantID);
                Debug.Log("Added 10,000 Money for debugging purposes. Please remove on build!");
            }
        }

        /// <summary>
        /// Returns the appropriate offset for a specific stack type.
        /// The offset determines the position adjustment (height) for the stack of objects (e.g., food, trash, or packages).
        /// </summary>
        /// <param name="stackType">The type of the stack (Food, Trash, Package, or None).</param>
        /// <returns>A float value representing the stack offset for the given stack type.</returns>
        public float GetStackOffset(StackType stackType) => stackType switch
        {
            StackType.Food => foodOffset,
            StackType.Trash => trashOffset,
            StackType.Package => packageOffset,
            StackType.Weapon => foodOffset,
            StackType.None => 0f,
            _ => 0f
        };

        /// <summary>
        /// Purchases and unlocks the next available unlockable item.
        /// The method triggers the unlocking process, plays visual and audio effects, and updates the unlockable count.
        /// </summary>
        public void BuyUnlockable()
        {
            // Unlock the next unlockable item
            unlockables[unlockCount].Unlock();

            // Play the unlock particle effect at the unlockable's position
            unlockParticle.transform.position = unlockables[unlockCount].transform.position;
            unlockParticle.Play();

            // Play the magical sound effect for the unlock
            AudioManager.Instance.PlaySFX(AudioID.Magical);

            // Increment the unlock count and reset the paid amount for the next unlock
            unlockCount++;
            PaidAmount = 0;

            // Update the unlockable buyer UI
            UpdateUnlockableBuyer();

            // Save the progress of the restaurant data
            SaveSystem.SaveData<RestaurantData>(data, restaurantID);
        }

        void UpdateUnlockableBuyer()
        {
            // Check if there are any unlockables in the scene
            if (unlockables?.Count(unlockable => unlockable != null) == 0)
            {
                // Log a warning if no unlockables are present
                Debug.LogWarning("There are no unlockables present in the scene! Please add the necessary unlockable items to proceed.");
                return;
            }

            // If there are still unlockables to purchase, update the UI for the next unlockable
            if (unlockCount < unlockables.Count)
            {
                var unlockable = unlockables[unlockCount];

                // Position the unlockable buyer at the correct location
                unlockableBuyer.transform.position = unlockable.GetBuyingPoint();

                // Calculate the price for the next unlockable
                int price = Mathf.RoundToInt(Mathf.Round(baseUnlockPrice * Mathf.Pow(unlockGrowthFactor, unlockCount)) / 5f) * 5;

                // Initialize the unlockable buyer UI with the unlockable and its price
                unlockableBuyer.Initialize(unlockable, price, PaidAmount);
            }
            else
            {
                // Mark the restaurant as fully unlocked if all unlockables have been bought
                data.IsUnlocked = true;

                // Hide the unlockable buyer UI
                unlockableBuyer.gameObject.SetActive(false);
            }

            // Calculate the progress based on how many unlockables have been purchased
            float progress = data.UnlockCount / (float)unlockables.Count;

            // Trigger the OnUnlock event to notify listeners about the unlock progress
            OnUnlock?.Invoke(progress);
        }

        public void AdjustMoney(int change)
        {
            data.Money += change;
            moneyDisplay.text = GetFormattedMoney(data.Money);
        }

        public long GetMoney()
        {
            return data.Money;
        }

        /// <summary>
        /// Formats the given amount of money as a human-readable string.
        /// Converts the value into a short-form format with appropriate suffixes for thousands, millions, billions, etc.
        /// </summary>
        /// <param name="money">The amount of money to format.</param>
        /// <returns>A string representing the money value in a readable format (e.g., "1.5k", "2.3m", etc.).</returns>
        public string GetFormattedMoney(long money)
        {
            if (money < 1000) return money.ToString(); // No suffix for values under 1000
            else if (money < 1000000) return (money / 1000f).ToString("0.##") + "k"; // Thousands (k)
            else if (money < 1000000000) return (money / 1000000f).ToString("0.##") + "m"; // Millions (m)
            else if (money < 1000000000000) return (money / 1000000000f).ToString("0.##") + "b"; // Billions (b)
            else return (money / 1000000000000f).ToString("0.##") + "t"; // Trillions (t)
        }

        /// <summary>
        /// Purchases an upgrade for the restaurant, deducting the required money from the player's balance and applying the corresponding upgrade.
        /// The upgrade could be related to employee speed, capacity, number of employees, or player stats like speed and capacity.
        /// </summary>
        /// <param name="upgrade">The upgrade to purchase. This could be an employee upgrade or a player upgrade.</param>
        public void PurchaseUpgrade(Upgrade upgrade)
        {
            int price = GetUpgradePrice(upgrade); // Calculate the price for the selected upgrade
            AdjustMoney(-price); // Deduct the price from the player's money

            // Apply the selected upgrade based on its type
            switch (upgrade)
            {
                case Upgrade.EmployeeSpeed:
                    data.EmployeeSpeed++; // Increase employee speed
                    break;

                case Upgrade.EmployeeCapacity:
                    data.EmployeeCapacity++; // Increase the number of items an employee can carry
                    break;

                case Upgrade.EmployeeAmount:
                    data.EmployeeAmount++; // Increase the number of employees in the restaurant
                    SpawnEmployee(); // Spawn a new employee
                    break;

                case Upgrade.PlayerSpeed:
                    data.PlayerSpeed++; // Increase the player's movement speed
                    break;

                case Upgrade.PlayerCapacity:
                    data.PlayerCapacity++; // Increase the player's carrying capacity
                    break;

                case Upgrade.Profit:
                    data.Profit++; // Increase the profit multiplier
                    break;

                default:
                    break;
            }

            AudioManager.Instance.PlaySFX(AudioID.Kaching); // Play a sound effect to indicate the upgrade has been purchased

            SaveSystem.SaveData<RestaurantData>(data, restaurantID); // Save the updated data to persistent storage

            OnUpgrade?.Invoke(); // Trigger any external actions after the upgrade is purchased
        }

        /// <summary>
        /// Calculates the price for a specific upgrade based on the current level of the upgrade and a growth factor.
        /// The price increases exponentially as the upgrade level rises, with each upgrade being more expensive than the previous one.
        /// </summary>
        /// <param name="upgrade">The upgrade for which to calculate the price.</param>
        /// <returns>The calculated price for the specified upgrade.</returns>
        public int GetUpgradePrice(Upgrade upgrade)
        {
            int currentLevel = GetUpgradeLevel(upgrade); // Get the current level of the selected upgrade
            return Mathf.RoundToInt(Mathf.Round(baseUpgradePrice * Mathf.Pow(upgradeGrowthFactor, currentLevel)) / 50f) * 50; // Calculate the price based on the upgrade's growth factor
        }

        /// <summary>
        /// Retrieves the current level of a specified upgrade.
        /// The level corresponds to the data stored for each upgrade type, such as employee speed, player speed, etc.
        /// </summary>
        /// <param name="upgrade">The upgrade for which to retrieve the current level.</param>
        /// <returns>The current level of the specified upgrade.</returns>
        public int GetUpgradeLevel(Upgrade upgrade) => upgrade switch
        {
            Upgrade.EmployeeSpeed => data.EmployeeSpeed,
            Upgrade.EmployeeCapacity => data.EmployeeCapacity,
            Upgrade.EmployeeAmount => data.EmployeeAmount,
            Upgrade.PlayerSpeed => data.PlayerSpeed,
            Upgrade.PlayerCapacity => data.PlayerCapacity,
            Upgrade.Profit => data.Profit,
            _ => 0
        };

        /// <summary>
        /// Loads a different restaurant scene by saving the current state and transitioning to the specified scene.
        /// The scene is only loaded if the specified scene index differs from the current one.
        /// </summary>
        /// <param name="index">The index of the scene to load.</param>
        public void LoadRestaurant(int index)
        {
            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            if (index == currentSceneIndex) return; // Avoid reloading the current scene

            // Fade out the current screen and load the new scene after saving the data
            screenFader.FadeIn(() =>
            {
                SaveSystem.SaveData<RestaurantData>(data, restaurantID); // Save current restaurant data
                SceneManager.LoadScene(index); // Load the specified scene
            });
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                SaveSystem.SaveData<RestaurantData>(data, restaurantID);
        }

        void OnApplicationQuit()
        {
            SaveSystem.SaveData<RestaurantData>(data, restaurantID);
        }

        void OnDisable()
        {
            DOTween.KillAll();
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (employeePoint == null) return;

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(employeePoint.position, employeeSpawnRadius);
        }
#endif
    }

    public enum Upgrade
    {
        EmployeeSpeed, EmployeeCapacity, EmployeeAmount,
        PlayerSpeed, PlayerCapacity, Profit
    }
}
