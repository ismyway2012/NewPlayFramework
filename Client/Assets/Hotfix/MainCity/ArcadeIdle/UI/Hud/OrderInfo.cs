using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;

namespace NewPlay.ArcadeIdle
{
    public class OrderInfo : HudBase
    {

        [SerializeField, Tooltip("The icon image to be displayed for the order info.")]
        private Image fillImage;

        [SerializeField, Tooltip("The icon image to be displayed for the order info.")]
        private GameObject iconImage;

        [SerializeField, Tooltip("The text component that shows the amount for the order.")]
        private TMP_Text amountText;

        protected override void Start()
        {
            base.Start();
            HideInfo(); // Ensure the order info is hidden at the start
        }

        /// <summary>
        /// Displays the order information UI at the specified displayer's position with the given amount.
        /// If the amount is greater than 0, the icon will be shown and the amount will be displayed. 
        /// If the amount is 0 or less, the icon is hidden and the text will display "NO SEAT!".
        /// </summary>
        /// <param name="displayer">The Transform of the object (e.g., a customer) to which the order info is attached.</param>
        /// <param name="amount">The amount of the order to display. If 0 or less, it shows "NO SEAT!".</param>
        public void ShowInfo(Transform displayer, int amount, string noneTips = "")
        {
            gameObject.SetActive(true); // Enable the order info UI element
            this.Target = displayer; // Set the displayer's transform

            bool active = amount > 0; // Determine if the amount is greater than 0
            iconImage.SetActive(active); // Set the icon's active state based on the amount
            amountText.text = active ? amount.ToString() : noneTips; // Display the amount or a message if no seat
            fillImage.fillAmount = 0;
            fillImage.DOKill();
        }

        public void ShowInfo(Transform displayer, string info = "")
        {
            gameObject.SetActive(true); // Enable the order info UI element
            this.Target = displayer; // Set the displayer's transform

            iconImage.SetActive(false); // Set the icon's active state based on the amount
            amountText.text = info; // Display the amount or a message if no seat
            fillImage.DOKill();
            fillImage.fillAmount = 0;
        }

        /// <summary>
        /// Hides the order information UI and resets the displayer to null.
        /// This is used to make the order info UI disappear when it is no longer needed.
        /// </summary>
        public void HideInfo()
        {
            gameObject.SetActive(false); // Disable the order info UI element
            Target = null; // Reset the displayer reference
        }

        public void ShowDoing(Transform displayer, float time)
        {
            gameObject.SetActive(true);
            this.Target = displayer; // Set the displayer's transform
            iconImage.SetActive(true);
            amountText.text = string.Empty;
            fillImage.DOKill();
            fillImage.fillAmount = 0;
            fillImage.DOFillAmount(1, time).SetEase(Ease.Linear);
        }
    }
}
