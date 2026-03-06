using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NewPlay.ArcadeIdle
{
    public class UpgradeHandler : MonoBehaviour
    {
        [SerializeField, Tooltip("Type of upgrade handled by this component.")]
        private Upgrade upgradeType;

        [SerializeField, Tooltip("Button used to purchase the upgrade.")]
        private Button upgradeButton;

        [SerializeField, Tooltip("Text label displaying the upgrade price.")]
        private TMP_Text priceLabel;

        [SerializeField, Tooltip("Visual indicators for the upgrade level.")]
        private Image[] indicators;

        private Color activeColor = new Color(1f, 0.5f, 0.25f, 1f);

        void Start()
        {
            // Initializes the upgrade button and subscribes to relevant events.
            upgradeButton.onClick.AddListener(() =>
                RestaurantManager.Instance.PurchaseUpgrade(upgradeType)
            );

            RestaurantManager.Instance.OnUpgrade += UpdateHandler;
        }

        void OnEnable()
        {
            // Updates the upgrade handler's state when enabled.
            UpdateHandler();
        }

        /// <summary>
        /// Updates the indicators, button interactivity, and price label,
        /// based on the current upgrade level and available money.
        /// </summary>
        void UpdateHandler()
        {
            int level = RestaurantManager.Instance.GetUpgradeLevel(upgradeType);

            for (int i = 0; i < indicators.Length; i++)
            {
                indicators[i].color = i < level ? activeColor : Color.gray;
            }

            if (level < 5)
            {
                int price = RestaurantManager.Instance.GetUpgradePrice(upgradeType);
                priceLabel.text = RestaurantManager.Instance.GetFormattedMoney(price);

                bool hasEnoughMoney = RestaurantManager.Instance.GetMoney() >= price;
                upgradeButton.interactable = hasEnoughMoney;
            }
            else
            {
                priceLabel.text = "MAX";
                upgradeButton.interactable = false;
            }
        }
    }
}
