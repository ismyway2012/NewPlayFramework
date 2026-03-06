using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace NewPlay.ArcadeIdle
{
    [RequireComponent(typeof(Image))]
    public class ScreenFader : MonoBehaviour
    {
        [SerializeField, Tooltip("Duration of the fade-in and fade-out effects in seconds.")]
        private float fadeTime = 1f;

        private Image image;

        void Awake()
        {
            // Initializes the Image component reference.
            image = GetComponent<Image>();
        }

        /// <summary>
        /// Fades the screen to black over the specified duration and invokes a callback when complete.
        /// </summary>
        /// <param name="onComplete">Optional callback to execute after the fade-in is complete.</param>
        public void FadeIn(System.Action onComplete = null)
        {
            // Set initial transparency and enable raycast blocking during fade
            image.color = new Color(0, 0, 0, 0);
            image.raycastTarget = true;

            // Perform the fade-in effect
            image.DOFade(1f, fadeTime).OnComplete(() =>
            {
                onComplete?.Invoke();
            });
        }

        /// <summary>
        /// Fades the screen from black to transparent over the specified duration and invokes a callback when complete.
        /// </summary>
        /// <param name="onComplete">Optional callback to execute after the fade-out is complete.</param>
        public void FadeOut(System.Action onComplete = null)
        {
            // Set initial color to black and disable raycast blocking during fade
            image.color = Color.black;
            image.raycastTarget = false;

            // Perform the fade-out effect
            image.DOFade(0f, fadeTime).OnComplete(() =>
            {
                onComplete?.Invoke();
            });
        }
    }
}
