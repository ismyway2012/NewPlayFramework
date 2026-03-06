using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

namespace NewPlay.ArcadeIdle
{
    public class MainMenuManager : MonoBehaviour
    {
        [SerializeField, Tooltip("The button to start the game")]
        private Button startButton;

        [SerializeField, Tooltip("Screen fader for transitioning between scenes")]
        private ScreenFader screenFader;

        [SerializeField, Tooltip("Background music for the main menu")]
        private AudioClip backgroundMusic;

        void Start()
        {
            // Set the frame rate and disable vertical sync
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            // Get the default scene (first level) name
            string defaultScenePath = SceneUtility.GetScenePathByBuildIndex(1);
            string defaultSceneName = System.IO.Path.GetFileNameWithoutExtension(defaultScenePath);

            // Get the name of the latest save file, if any
            string latestFileName = SaveSystem.GetLatestSaveFileName();
            string latestSceneName = string.IsNullOrEmpty(latestFileName) ? defaultSceneName : latestFileName;

            // Add listener to start button
            startButton.onClick.AddListener(() => StartGame(latestSceneName));

            // Animate the start button with a scaling effect
            startButton.transform.DOScale(Vector3.one * 1.1f, 0.5f)
                .SetLoops(-1, LoopType.Yoyo);

            // Play background music
            AudioManager.Instance.PlayBGM(backgroundMusic);
        }

        void StartGame(string latestSceneName)
        {
            // Stop the button scale animation
            DOTween.Kill(startButton.transform);

            // Fade the screen and load the target scene
            screenFader.FadeIn(() => SceneManager.LoadScene(latestSceneName));

            // Play magical sound effect when starting the game
            AudioManager.Instance.PlaySFX(AudioID.Magical);
        }
    }
}
