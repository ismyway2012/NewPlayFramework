using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace NewPlay.ArcadeIdle
{
    public class UnlockableBuyer : Interactable
    {
        [SerializeField, Tooltip("The interval between each payment during the buying process.")]
        private float payingInterval = 0.03f;

        [SerializeField, Tooltip("The total time it takes to complete the payment.")]
        private float payingTime = 3f;

        [SerializeField, Tooltip("The UI image used to represent the payment progress.")]
        private Image progressFill;

        [SerializeField, Tooltip("The label displaying the remaining price for the unlockable.")]
        private TMP_Text priceLabel;

        private long playerMoney => RestaurantManager.Instance.GetMoney();  // The current amount of money the player has

        private Unlockable unlockable;  // The unlockable object that can be bought
        private int unlockPrice;  // The price required to unlock the unlockable
        private int paidAmount;  // The amount the player has already paid

        /// <summary>
        /// Initializes the UnlockableBuyer with the provided unlockable and price details.
        /// </summary>
        /// <param name="unlockable">The unlockable object to be bought</param>
        /// <param name="unlockPrice">The price of the unlockable</param>
        /// <param name="paidAmount">The amount already paid towards the unlockable</param>
        public void Initialize(Unlockable unlockable, int unlockPrice, int paidAmount)
        {
            this.unlockable = unlockable;
            this.unlockPrice = unlockPrice;
            this.paidAmount = paidAmount;

            UpdatePayment(0);  // Update the payment progress display
        }

        /// <summary>
        /// Updates the payment progress and the remaining price label.
        /// </summary>
        /// <param name="amount">The amount to add to the paid amount</param>
        void UpdatePayment(int amount)
        {
            paidAmount += amount;
            RestaurantManager.Instance.PaidAmount = paidAmount;

            progressFill.fillAmount = (float)paidAmount / unlockPrice;  // Update the progress bar
            priceLabel.text = RestaurantManager.Instance.GetFormattedMoney(unlockPrice - paidAmount);  // Update the price label
        }

        /// <summary>
        /// Triggered when the player enters the interactable area, starting the payment process.
        /// </summary>
        protected override void OnPlayerEnter()
        {
            StartCoroutine(Pay());  // Start the payment coroutine
        }

        /// <summary>
        /// Coroutine that handles the process of paying for the unlockable item.
        /// </summary>
        IEnumerator Pay()
        {
            // Keep paying while the player is inside the trigger, the unlockable is not fully paid, and the player has money
            while (player != null && paidAmount < unlockPrice && playerMoney > 0)
            {
                float paymentRate = unlockPrice * payingInterval / payingTime;  // Calculate the rate of payment per interval
                paymentRate = Mathf.Min(playerMoney, paymentRate);  // Make sure payment does not exceed the player's money
                int payment = Mathf.Max(1, Mathf.RoundToInt(paymentRate));  // Ensure a minimum payment of 1

                UpdatePayment(payment);  // Update the progress
                RestaurantManager.Instance.AdjustMoney(-payment);  // Deduct the payment from the player's money

                PlayMoneyAnimation();  // Play the money animation

                // If the total amount paid is equal to or greater than the unlock price, complete the purchase
                if (paidAmount >= unlockPrice)
                    RestaurantManager.Instance.BuyUnlockable();

                yield return new WaitForSeconds(payingInterval);  // Wait for the next payment interval
            }
        }

        /// <summary>
        /// Plays the animation for money moving from the player to the unlockable.
        /// </summary>
        void PlayMoneyAnimation()
        {
            var moneyObj = PoolManager.Instance.SpawnObject("Money");  // Spawn a money object
            moneyObj.transform.position = player.transform.position + Vector3.up * 2;  // Position it above the player
            moneyObj.transform.DOJump(transform.position, 3f, 1, 0.5f)  // Animate the money moving to the unlockable
                .OnComplete(() => PoolManager.Instance.ReturnObject(moneyObj));  // Return the object to the pool after animation

            AudioManager.Instance.PlaySFX(AudioID.Money);  // Play the money sound effect
        }
    }
}
