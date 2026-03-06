using UnityEngine;
using TMPro;

namespace NewPlay.ArcadeIdle
{
    public class ProgressDisplay : MonoBehaviour
    {
        [SerializeField, Tooltip("Gradient used to visually represent progress from start to completion.")]
        private Gradient progressGradient;

        private SlicedFilledImage progressFill;
        private TMP_Text progressText;

        void Awake()
        {
            // Initializes references to child components for progress display.
            progressFill = GetComponentInChildren<SlicedFilledImage>();
            progressText = GetComponentInChildren<TMP_Text>();
        }

        void OnEnable()
        {
            // Subscribes to the RestaurantManager's OnUnlock event when enabled.
            RestaurantManager.Instance.OnUnlock += UpdateProgress;
        }

        void OnDisable()
        {
            // Unsubscribes from the RestaurantManager's OnUnlock event when disabled.
            RestaurantManager.Instance.OnUnlock -= UpdateProgress;
        }

        /// <summary>
        /// Updates the visual representation of the progress bar and text.
        /// </summary>
        /// <param name="progress">A float value representing the current progress (0 to 1).</param>
        void UpdateProgress(float progress)
        {
            // Update fill color based on gradient evaluation
            progressFill.color = progressGradient.Evaluate(progress);

            // Update fill amount of the progress bar
            progressFill.fillAmount = progress;

            // Update progress text with percentage format
            progressText.text = $"PROGRESS {progress * 100:0.##}%";
        }
    }
}
